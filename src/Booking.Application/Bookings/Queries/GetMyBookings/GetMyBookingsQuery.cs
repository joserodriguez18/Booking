using Booking.Application.Common.DTOs;
using Booking.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Bookings.Queries.GetMyBookings;

public sealed record GetMyBookingsQuery(Guid GuestId) : IRequest<List<BookingDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class GetMyBookingsQueryHandler : IRequestHandler<GetMyBookingsQuery, List<BookingDto>>
{
    private readonly IApplicationDbContext _ctx;

    public GetMyBookingsQueryHandler(IApplicationDbContext ctx) => _ctx = ctx;

    public async Task<List<BookingDto>> Handle(GetMyBookingsQuery req, CancellationToken ct)
    {
        return await (
            from r in _ctx.Reservas
            join p in _ctx.Propiedades on r.PropertyId equals p.Id
            where r.GuestId == req.GuestId
            orderby r.DateRange.CheckIn descending
            select new BookingDto(
                r.Id,
                r.PropertyId,
                p.Name,
                p.Location,
                r.GuestId,
                r.DateRange.CheckIn,
                r.DateRange.CheckOut,
                // Cálculo de noches en memoria tras la proyección
                (int)((r.DateRange.CheckOut - r.DateRange.CheckIn).TotalDays),
                r.TotalPrice.Amount,
                r.TotalPrice.Currency,
                r.Status.ToString(),
                r.CreatedAt)
        ).ToListAsync(ct);
    }
}
