namespace Booking.Application.Common.DTOs;

public record PropertyDto(
    Guid            Id,
    string          Name,
    string          Description,
    string          Location,
    decimal         PricePerNight,
    string          Currency,
    Guid            OwnerId,
    bool            IsActive,
    DateTimeOffset  CreatedAt
);
