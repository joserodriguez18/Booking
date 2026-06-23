using Booking.Application.Common.DTOs;
using Booking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Wishlist.Queries.GetWishlist;

public sealed record GetWishlistQuery(Guid UserId) : IRequest<List<PropertyDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, List<PropertyDto>>
{
    private readonly IApplicationDbContext _ctx;

    public GetWishlistQueryHandler(IApplicationDbContext ctx) => _ctx = ctx;

    public async Task<List<PropertyDto>> Handle(GetWishlistQuery req, CancellationToken ct)
    {
        return await (
            from w in _ctx.ListaDeseos
            join p in _ctx.Propiedades on w.PropertyId equals p.Id
            where w.UserId == req.UserId && p.IsActive
            orderby w.CreatedAt descending
            select new PropertyDto(
                p.Id, p.Name, p.Description, p.Location,
                p.PricePerNight.Amount, p.PricePerNight.Currency,
                p.OwnerId, p.IsActive, p.CreatedAt)
        ).ToListAsync(ct);
    }
}
