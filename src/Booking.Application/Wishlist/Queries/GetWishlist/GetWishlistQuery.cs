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
    private readonly IStorageService       _storage;

    public GetWishlistQueryHandler(IApplicationDbContext ctx, IStorageService storage)
    {
        _ctx     = ctx;
        _storage = storage;
    }

    public async Task<List<PropertyDto>> Handle(GetWishlistQuery req, CancellationToken ct)
    {
        var items = await (
            from w in _ctx.ListaDeseos
            join p in _ctx.Propiedades on w.PropertyId equals p.Id
            where w.UserId == req.UserId && p.IsActive
            orderby w.CreatedAt descending
            select new {
                p.Id, p.Name, p.Description, p.Location,
                Monto  = p.PricePerNight.Amount,
                Moneda = p.PricePerNight.Currency,
                p.OwnerId, p.IsActive, p.CreatedAt, p.PhotoUrls
            }
        ).ToListAsync(ct);

        return items.Select(p => new PropertyDto(
            p.Id, p.Name, p.Description, p.Location,
            p.Monto, p.Moneda, p.OwnerId, p.IsActive, p.CreatedAt,
            p.PhotoUrls.Select(_storage.GetPublicUrl).ToList()
        )).ToList();
    }
}
