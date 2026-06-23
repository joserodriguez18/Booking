namespace Booking.Application.Common.DTOs;

public record PropertyMetricsDto(
    Guid    PropertyId,
    string  Name,
    int     TotalReservas,
    decimal Ingresos,
    double  TasaOcupacion
);

public record DashboardDto(
    int                            TotalPropiedades,
    int                            TotalReservasConfirmadas,
    decimal                        IngresosTotales,
    double                         TasaOcupacionGeneral,
    DateTimeOffset                 Desde,
    DateTimeOffset                 Hasta,
    IReadOnlyList<PropertyMetricsDto> MetricasPorPropiedad
);
