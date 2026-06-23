using Booking.Application.Common.DTOs;
using Booking.Domain.Enums;
using Booking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Owner.Queries.GetDashboard;

public sealed record GetOwnerDashboardQuery(
    Guid           OwnerId,
    DateTimeOffset? Desde,
    DateTimeOffset? Hasta
) : IRequest<DashboardDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetOwnerDashboardQueryHandler : IRequestHandler<GetOwnerDashboardQuery, DashboardDto>
{
    private readonly IApplicationDbContext _ctx;

    public GetOwnerDashboardQueryHandler(IApplicationDbContext ctx) => _ctx = ctx;

    public async Task<DashboardDto> Handle(GetOwnerDashboardQuery req, CancellationToken ct)
    {
        // Período por defecto: últimos 30 días
        var hasta = req.Hasta ?? DateTimeOffset.UtcNow;
        var desde = req.Desde ?? hasta.AddDays(-30);

        var propiedades = await _ctx.Propiedades
            .Where(p => p.OwnerId == req.OwnerId)
            .ToListAsync(ct);

        var propIds = propiedades.Select(p => p.Id).ToList();

        // Reservas confirmadas en el período
        var reservas = await _ctx.Reservas
            .Where(r => propIds.Contains(r.PropertyId) &&
                        r.Status == BookingStatus.Confirmed &&
                        r.DateRange.CheckIn  >= desde &&
                        r.DateRange.CheckOut <= hasta)
            .ToListAsync(ct);

        var diasPeriodo = (hasta - desde).TotalDays;

        // Métricas por propiedad
        var metricasPorPropiedad = propiedades.Select(p =>
        {
            var reservasPropiedad = reservas.Where(r => r.PropertyId == p.Id).ToList();
            var ingresos          = reservasPropiedad.Sum(r => r.TotalPrice.Amount);
            var nochesOcupadas    = reservasPropiedad.Sum(r => r.DateRange.Nights);
            var tasaOcupacion     = diasPeriodo > 0 ? nochesOcupadas / diasPeriodo * 100 : 0;

            return new PropertyMetricsDto(p.Id, p.Name, reservasPropiedad.Count, ingresos, tasaOcupacion);
        }).ToList();

        var ingresosTotales    = metricasPorPropiedad.Sum(m => m.Ingresos);
        var totalNochesOcup    = reservas.Sum(r => r.DateRange.Nights);
        var capacidadTotal     = diasPeriodo * propiedades.Count;
        var tasaOcupGeneral    = capacidadTotal > 0 ? totalNochesOcup / capacidadTotal * 100 : 0;

        return new DashboardDto(
            propiedades.Count,
            reservas.Count,
            ingresosTotales,
            tasaOcupGeneral,
            desde, hasta,
            metricasPorPropiedad);
    }
}
