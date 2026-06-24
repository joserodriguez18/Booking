using Booking.Application.Common.DTOs;
using Booking.Domain.Enums;
using Booking.Domain.ValueObjects;
using Booking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Properties.Queries.GetProperties;

/// <summary>
/// Búsqueda pública de propiedades — no requiere autenticación.
/// Filtra por ubicación y excluye las ya reservadas en el rango de fechas.
/// </summary>
public sealed record GetPropertiesQuery(
    string?  Location,
    DateOnly? CheckIn,
    DateOnly? CheckOut
) : IRequest<List<PropertyDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetPropertiesQueryHandler : IRequestHandler<GetPropertiesQuery, List<PropertyDto>>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IStorageService       _storage;

    public GetPropertiesQueryHandler(IApplicationDbContext ctx, IStorageService storage)
    {
        _ctx     = ctx;
        _storage = storage;
    }

    public async Task<List<PropertyDto>> Handle(GetPropertiesQuery req, CancellationToken ct)
    {
        var query = _ctx.Propiedades.Where(p => p.IsActive).AsQueryable();

        // Filtro por ubicación (búsqueda parcial, insensible a mayúsculas)
        if (!string.IsNullOrWhiteSpace(req.Location))
            query = query.Where(p => p.Location.ToLower().Contains(req.Location.ToLower()));

        // Filtro de disponibilidad: excluye propiedades con reservas confirmadas que se solapan
        if (req.CheckIn.HasValue && req.CheckOut.HasValue)
        {
            var rango    = BookingDateRange.Create(req.CheckIn.Value, req.CheckOut.Value);
            var checkIn  = rango.CheckIn;
            var checkOut = rango.CheckOut;

            query = query.Where(p => !_ctx.Reservas.Any(r =>
                r.PropertyId == p.Id &&
                r.Status     == BookingStatus.Confirmed &&
                r.DateRange.CheckIn  < checkOut &&
                r.DateRange.CheckOut > checkIn));
        }

        // Se materializa primero para poder convertir objectKeys a URLs públicas en memoria
        var propiedades = await query
            .Select(p => new {
                p.Id, p.Name, p.Description, p.Location,
                Monto  = p.PricePerNight.Amount,
                Moneda = p.PricePerNight.Currency,
                p.OwnerId, p.IsActive, p.CreatedAt, p.PhotoUrls
            })
            .ToListAsync(ct);

        return propiedades.Select(p => new PropertyDto(
            p.Id, p.Name, p.Description, p.Location,
            p.Monto, p.Moneda, p.OwnerId, p.IsActive, p.CreatedAt,
            p.PhotoUrls.Select(_storage.GetPublicUrl).ToList()
        )).ToList();
    }
}
