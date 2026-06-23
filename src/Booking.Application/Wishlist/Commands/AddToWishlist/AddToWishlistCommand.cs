using Booking.Domain.Entities;
using Booking.Domain.Exceptions;
using Booking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Wishlist.Commands.AddToWishlist;

public sealed record AddToWishlistCommand(Guid UserId, Guid PropertyId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand>
{
    private readonly IApplicationDbContext _ctx;

    public AddToWishlistCommandHandler(IApplicationDbContext ctx) => _ctx = ctx;

    public async Task Handle(AddToWishlistCommand req, CancellationToken ct)
    {
        var propiedad = await _ctx.Propiedades
            .FirstOrDefaultAsync(p => p.Id == req.PropertyId && p.IsActive, ct)
            ?? throw new NotFoundException("Propiedad", req.PropertyId);

        var yaExiste = await _ctx.ListaDeseos
            .AnyAsync(w => w.UserId == req.UserId && w.PropertyId == req.PropertyId, ct);

        if (yaExiste)
            throw new DomainException("Esta propiedad ya está en tu lista de deseos.");

        _ctx.ListaDeseos.Add(WishlistItem.Create(req.UserId, req.PropertyId));
        await _ctx.SaveChangesAsync(ct);
    }
}
