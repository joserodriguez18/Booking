using Booking.Domain.Exceptions;

namespace Booking.Domain.ValueObjects;

/// <summary>
/// Enforces the business invariant: check-in always at 14:00 UTC, check-out at 12:00 UTC.
/// </summary>
public sealed class BookingDateRange
{
    public DateTimeOffset CheckIn { get; }
    public DateTimeOffset CheckOut { get; }
    public int Nights => (CheckOut.Date - CheckIn.Date).Days;

    private BookingDateRange(DateTimeOffset checkIn, DateTimeOffset checkOut)
    {
        CheckIn = checkIn;
        CheckOut = checkOut;
    }

    public static BookingDateRange Create(DateOnly checkInDate, DateOnly checkOutDate)
    {
        if (checkOutDate <= checkInDate)
            throw new DomainException("La fecha de salida debe ser posterior a la fecha de entrada.");

        var checkIn  = new DateTimeOffset(checkInDate.Year,  checkInDate.Month,  checkInDate.Day,  14, 0, 0, TimeSpan.Zero);
        var checkOut = new DateTimeOffset(checkOutDate.Year, checkOutDate.Month, checkOutDate.Day, 12, 0, 0, TimeSpan.Zero);

        return new BookingDateRange(checkIn, checkOut);
    }

    // Two ranges overlap when one starts before the other ends (open-interval logic).
    public bool OverlapsWith(BookingDateRange other) =>
        CheckIn < other.CheckOut && other.CheckIn < CheckOut;
}
