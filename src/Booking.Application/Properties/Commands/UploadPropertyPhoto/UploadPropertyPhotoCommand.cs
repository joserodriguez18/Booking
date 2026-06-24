using Booking.Application.Common.Interfaces;
using Booking.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Properties.Commands.UploadPropertyPhoto;

public sealed record UploadPropertyPhotoCommand(
    Guid   PropertyId,
    Guid   OwnerId,
    Stream PhotoStream,
    string FileName,
    string ContentType
) : IRequest<string>; // retorna la ruta guardada en la BD

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class UploadPropertyPhotoCommandHandler
    : IRequestHandler<UploadPropertyPhotoCommand, string>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IStorageService       _storage;

    public UploadPropertyPhotoCommandHandler(IApplicationDbContext ctx, IStorageService storage)
    {
        _ctx     = ctx;
        _storage = storage;
    }

    public async Task<string> Handle(UploadPropertyPhotoCommand req, CancellationToken ct)
    {
        var propiedad = await _ctx.Propiedades
            .FirstOrDefaultAsync(p => p.Id == req.PropertyId, ct)
            ?? throw new NotFoundException("Propiedad", req.PropertyId);

        if (propiedad.OwnerId != req.OwnerId)
            throw new DomainException("Solo el propietario puede subir fotos de su propiedad.");

        // Sube la foto al bucket público de fotos bajo la carpeta de la propiedad
        var carpeta   = $"properties/{req.PropertyId}";
        var objectKey = await _storage.UploadPublicFileAsync(
            req.PhotoStream, req.FileName, req.ContentType, carpeta, ct);

        // Guarda solo la ruta en la BD
        propiedad.AddPhoto(objectKey);
        await _ctx.SaveChangesAsync(ct);

        return objectKey;
    }
}
