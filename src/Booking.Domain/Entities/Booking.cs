using Booking.Domain.Enums;
using Booking.Domain.Exceptions;
using Booking.Domain.ValueObjects;

namespace Booking.Domain.Entities;

public sealed class Booking : BaseEntity
{
    public Guid PropertyId { get; private set; }
    public Guid GuestId { get; private set; }
    public BookingDateRange DateRange { get; private set; } = null!;

    // Convenience accessors — times are always 14:00 / 12:00 UTC by invariant.
    public DateTimeOffset CheckInDate  => DateRange.CheckIn;
    public DateTimeOffset CheckOutDate => DateRange.CheckOut;

    public Money TotalPrice { get; private set; } = Money.Of(0);
    public BookingStatus Status { get; private set; }

    private Booking() { }

    private Booking(Guid propertyId, Guid guestId, BookingDateRange dateRange, Money totalPrice)
    {
        PropertyId = propertyId;
        GuestId    = guestId;
        DateRange  = dateRange;
        TotalPrice = totalPrice;
        Status     = BookingStatus.Pending;
    }

    /// <summary>
    /// Factory method. Enforces time invariant (14:00/12:00 UTC) and prevents double-booking.
    /// The caller (Application layer) must supply all confirmed bookings for the property
    /// that overlap with the requested dates.
    /// </summary>
    public static Booking Create(
        Guid propertyId,
        Guid guestId,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        Money pricePerNight,
        IEnumerable<Booking> existingConfirmedBookings)
    {
        if (propertyId == Guid.Empty) throw new DomainException("La propiedad es obligatoria.");
        if (guestId    == Guid.Empty) throw new DomainException("El huésped es obligatorio.");

        var dateRange = BookingDateRange.Create(checkInDate, checkOutDate);

        EnsureNoConflict(propertyId, dateRange, existingConfirmedBookings);

        var totalPrice = pricePerNight * dateRange.Nights;
        return new Booking(propertyId, guestId, dateRange, totalPrice);
    }

    public void Confirm(IEnumerable<Booking> existingConfirmedBookings)
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException($"Solo las reservas pendientes pueden confirmarse. Estado actual: {Status}.");

        EnsureNoConflict(PropertyId, DateRange, existingConfirmedBookings);
        Status = BookingStatus.Confirmed;
        SetUpdatedAt();
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new DomainException("La reserva ya fue cancelada.");
        if (Status == BookingStatus.Confirmed && CheckInDate <= DateTimeOffset.UtcNow)
            throw new DomainException("No se puede cancelar una reserva después de la fecha de check-in.");
        Status = BookingStatus.Cancelled;
        SetUpdatedAt();
    }

    // Pure domain rule: no confirmed booking for this property may overlap with the requested range.
    private static void EnsureNoConflict(
        Guid propertyId,
        BookingDateRange dateRange,
        IEnumerable<Booking> existingConfirmedBookings)
    {
        foreach (var existing in existingConfirmedBookings)
        {
            if (existing.Status == BookingStatus.Confirmed && dateRange.OverlapsWith(existing.DateRange))
                throw new BookingConflictException(propertyId, dateRange.CheckIn, dateRange.CheckOut);
        }
    }
}
