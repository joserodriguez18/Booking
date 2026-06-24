using Booking.Application.Common.DTOs;
using Booking.Application.Common.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Booking.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.KYC.Commands.UploadIdentityDocument;

public sealed record DocumentoArchivo(Stream Stream, string FileName, string ContentType);

public sealed record UploadIdentityDocumentCommand(
    Guid                         UserId,
    IReadOnlyList<DocumentoArchivo> Archivos,
    DocumentType                 DocumentType
) : IRequest<KycExtractionResult>;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class UploadIdentityDocumentCommandHandler
    : IRequestHandler<UploadIdentityDocumentCommand, KycExtractionResult>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IStorageService      _storage;
    private readonly IKycService          _kyc;
    private readonly IEmailService        _email;

    public UploadIdentityDocumentCommandHandler(
        IApplicationDbContext ctx,
        IStorageService storage,
        IKycService kyc,
        IEmailService email)
    {
        _ctx     = ctx;
        _storage = storage;
        _kyc     = kyc;
        _email   = email;
    }

    public async Task<KycExtractionResult> Handle(UploadIdentityDocumentCommand req, CancellationToken ct)
    {
        var usuario = await _ctx.Usuarios.FindAsync([req.UserId], ct)
            ?? throw new NotFoundException("Usuario", req.UserId);

        if (usuario.IsIdentityVerified)
            throw new DomainException("La identidad de este usuario ya fue verificada.");

        // 1. Sube todos los archivos a MinIO (almacenamiento seguro temporal)
        var objectKeys = new List<string>(req.Archivos.Count);
        foreach (var archivo in req.Archivos)
        {
            var key = await _storage.UploadFileAsync(
                archivo.Stream, archivo.FileName, archivo.ContentType, "kyc", ct);
            objectKeys.Add(key);
        }

        // 2. Crea el registro del documento usando la primera clave como referencia
        var documento = IdentityDocument.CreatePending(req.UserId, req.DocumentType);
        documento.SetDocumentUrl(objectKeys[0]);
        _ctx.DocumentosIdentidad.Add(documento);

        usuario.SetKycPending();

        // 3. Procesa todas las imágenes con IA en una sola llamada (cara frontal + trasera, etc.)
        var resultado = await _kyc.ProcessIdentityDocumentAsync(objectKeys, ct);

        if (resultado.Success)
        {
            documento.ApplyExtractedData(
                resultado.DocumentNumber!,
                resultado.ExtractedNames!,
                resultado.BirthDate!.Value);
            usuario.ApproveKyc(resultado.DocumentNumber, resultado.BirthDate);

            _ctx.Notificaciones.Add(Notification.Create(
                usuario.Id,
                "¡Identidad verificada!",
                "Tu documento fue procesado y tu identidad fue verificada exitosamente.",
                NotificationType.KycAprobado));
        }
        else
        {
            usuario.RejectKyc();

            _ctx.Notificaciones.Add(Notification.Create(
                usuario.Id,
                "Verificación fallida",
                $"No pudimos verificar tu identidad: {resultado.ErrorMessage}",
                NotificationType.KycRechazado));
        }

        // 4. Borrado seguro de todos los archivos post-verificación (requisito de privacidad)
        foreach (var key in objectKeys)
            await _storage.DeleteFileAsync(key, ct);
        documento.MarkDocumentAsDeleted();

        await _ctx.SaveChangesAsync(ct);

        // 5. Notificación por email
        var tipoNotif = resultado.Success ? "aprobada" : "rechazada";
        _ = _email.SendAlertAsync(
            usuario.Email,
            $"Verificación de identidad {tipoNotif} — Booking Platform",
            $"<p>Hola {usuario.Name}, tu verificación de identidad fue <strong>{tipoNotif}</strong>.</p>",
            ct).ConfigureAwait(false);

        return resultado;
    }
}
