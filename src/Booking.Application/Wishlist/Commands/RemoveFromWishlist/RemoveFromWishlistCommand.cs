using Booking.Domain.Exceptions;
using Booking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Wishlist.Commands.RemoveFromWishlist;

public sealed record RemoveFromWishlistCommand(Guid UserId, Guid PropertyId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand>
{
    private readonly IApplicationDbContext _ctx;

    public RemoveFromWishlistCommandHandler(IApplicationDbContext ctx) => _ctx = ctx;

    public async Task Handle(RemoveFromWishlistCommand req, CancellationToken ct)
    {
        var item = await _ctx.ListaDeseos
            .FirstOrDefaultAsync(w => w.UserId == req.UserId && w.PropertyId == req.PropertyId, ct)
            ?? throw new DomainException("La propiedad no está en tu lista de deseos.");

        _ctx.ListaDeseos.Remove(item);
        await _ctx.SaveChangesAsync(ct);
    }
}
