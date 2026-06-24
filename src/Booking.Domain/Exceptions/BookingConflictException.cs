namespace Booking.Domain.Exceptions;

public sealed class BookingConflictException : DomainException
{
    public BookingConflictException(Guid propertyId, DateTimeOffset checkIn, DateTimeOffset checkOut)
        : base($"La propiedad ya tiene una reserva confirmada que se superpone con las fechas {checkIn:dd/MM/yyyy} → {checkOut:dd/MM/yyyy}.") { }
}
