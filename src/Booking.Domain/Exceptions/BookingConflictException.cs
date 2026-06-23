namespace Booking.Domain.Exceptions;

public sealed class BookingConflictException : DomainException
{
    public BookingConflictException(Guid propertyId, DateTimeOffset checkIn, DateTimeOffset checkOut)
        : base($"Property {propertyId} already has a confirmed booking overlapping {checkIn:yyyy-MM-dd} → {checkOut:yyyy-MM-dd}.") { }
}
