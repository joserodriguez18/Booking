namespace Booking.Application.Common.DTOs;

public record BookingDto(
    Guid            Id,
    Guid            PropertyId,
    string          PropertyName,
    string          PropertyLocation,
    Guid            GuestId,
    DateTimeOffset  CheckIn,
    DateTimeOffset  CheckOut,
    int             Nights,
    decimal         TotalPrice,
    string          Currency,
    string          Status,
    DateTimeOffset  CreatedAt
);
