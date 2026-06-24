using Booking.Application.Common.Interfaces;
using Booking.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Properties.Commands.UploadPropertyPhoto;

public sealed record FotoArchivo(Stream Stream, string FileName, string ContentType);

public sealed record UploadPropertyPhotoCommand(
    Guid                      PropertyId,
    Guid                      OwnerId,
    IReadOnlyList<FotoArchivo> Fotos
) : IRequest<IReadOnlyList<string>>; // retorna las rutas guardadas en la BD

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class UploadPropertyPhotoCommandHandler
    : IRequestHandler<UploadPropertyPhotoCommand, IReadOnlyList<string>>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IStorageService       _storage;

    public UploadPropertyPhotoCommandHandler(IApplicationDbContext ctx, IStorageService storage)
    {
        _ctx     = ctx;
        _storage = storage;
    }

    public async Task<IReadOnlyList<string>> Handle(UploadPropertyPhotoCommand req, CancellationToken ct)
    {
        var propiedad = await _ctx.Propiedades
            .FirstOrDefaultAsync(p => p.Id == req.PropertyId, ct)
            ?? throw new NotFoundException("Propiedad", req.PropertyId);

        if (propiedad.OwnerId != req.OwnerId)
            throw new DomainException("Solo el propietario puede subir fotos de su propiedad.");

        var carpeta    = $"properties/{req.PropertyId}";
        var objectKeys = new List<string>(req.Fotos.Count);

        foreach (var foto in req.Fotos)
        {
            var objectKey = await _storage.UploadPublicFileAsync(
                foto.Stream, foto.FileName, foto.ContentType, carpeta, ct);

            propiedad.AddPhoto(objectKey);
            objectKeys.Add(objectKey);
        }

        await _ctx.SaveChangesAsync(ct);

        return objectKeys;
    }
}
