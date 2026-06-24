using Booking.Application.Common.DTOs;
using Booking.Domain.Exceptions;
using Booking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Properties.Queries.GetPropertyById;

public sealed record GetPropertyByIdQuery(Guid PropertyId) : IRequest<PropertyDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetPropertyByIdQueryHandler : IRequestHandler<GetPropertyByIdQuery, PropertyDto>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IStorageService       _storage;

    public GetPropertyByIdQueryHandler(IApplicationDbContext ctx, IStorageService storage)
    {
        _ctx     = ctx;
        _storage = storage;
    }

    public async Task<PropertyDto> Handle(GetPropertyByIdQuery req, CancellationToken ct)
    {
        var p = await _ctx.Propiedades
            .Where(p => p.Id == req.PropertyId && p.IsActive)
            .Select(p => new {
                p.Id, p.Name, p.Description, p.Location,
                Monto  = p.PricePerNight.Amount,
                Moneda = p.PricePerNight.Currency,
                p.OwnerId, p.IsActive, p.CreatedAt, p.PhotoUrls
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("Propiedad", req.PropertyId);

        return new PropertyDto(
            p.Id, p.Name, p.Description, p.Location,
            p.Monto, p.Moneda, p.OwnerId, p.IsActive, p.CreatedAt,
            p.PhotoUrls.Select(_storage.GetPublicUrl).ToList()
        );
    }
}
