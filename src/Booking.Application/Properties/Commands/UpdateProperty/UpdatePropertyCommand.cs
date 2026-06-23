using Booking.Domain.Exceptions;
using Booking.Domain.ValueObjects;
using Booking.Application.Common.Interfaces;
using MediatR;

namespace Booking.Application.Properties.Commands.UpdateProperty;

public sealed record UpdatePropertyCommand(
    Guid    PropertyId,
    Guid    OwnerId,
    string  Name,
    string  Description,
    string  Location,
    decimal PricePerNight,
    string  Currency
) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class UpdatePropertyCommandHandler : IRequestHandler<UpdatePropertyCommand>
{
    private readonly IApplicationDbContext _ctx;

    public UpdatePropertyCommandHandler(IApplicationDbContext ctx) => _ctx = ctx;

    public async Task Handle(UpdatePropertyCommand req, CancellationToken ct)
    {
        var propiedad = await _ctx.Propiedades.FindAsync([req.PropertyId], ct)
            ?? throw new NotFoundException("Propiedad", req.PropertyId);

        if (propiedad.OwnerId != req.OwnerId)
            throw new DomainException("No tienes permiso para editar esta propiedad.");

        propiedad.Update(req.Name, req.Description, req.Location, Money.Of(req.PricePerNight, req.Currency));
        await _ctx.SaveChangesAsync(ct);
    }
}
