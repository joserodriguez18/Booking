using Booking.Application.Common.Interfaces;
using Booking.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Properties.Commands.DeletePropertyPhoto;

public sealed record DeletePropertyPhotoCommand(
    Guid   PropertyId,
    Guid   OwnerId,
    string ObjectKey
) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class DeletePropertyPhotoCommandHandler : IRequestHandler<DeletePropertyPhotoCommand>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IStorageService       _storage;

    public DeletePropertyPhotoCommandHandler(IApplicationDbContext ctx, IStorageService storage)
    {
        _ctx     = ctx;
        _storage = storage;
    }

    public async Task Handle(DeletePropertyPhotoCommand req, CancellationToken ct)
    {
        var propiedad = await _ctx.Propiedades
            .FirstOrDefaultAsync(p => p.Id == req.PropertyId, ct)
            ?? throw new NotFoundException("Propiedad", req.PropertyId);

        if (propiedad.OwnerId != req.OwnerId)
            throw new DomainException("Solo el propietario puede eliminar fotos de su propiedad.");

        // Elimina la ruta de la BD (lanza DomainException si no existe)
        propiedad.RemovePhoto(req.ObjectKey);

        // Elimina el archivo físico del bucket público de fotos
        await _storage.DeletePublicFileAsync(req.ObjectKey, ct);

        await _ctx.SaveChangesAsync(ct);
    }
}
