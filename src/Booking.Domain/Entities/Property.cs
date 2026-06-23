using Booking.Domain.Exceptions;
using Booking.Domain.ValueObjects;

namespace Booking.Domain.Entities;

public sealed class Property : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public Money PricePerNight { get; private set; } = Money.Of(0);
    public Guid OwnerId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Property() { }

    private Property(string name, string description, string location, Money pricePerNight, Guid ownerId)
    {
        Name = name;
        Description = description;
        Location = location;
        PricePerNight = pricePerNight;
        OwnerId = ownerId;
    }

    public static Property Create(string name, string description, string location, Money pricePerNight, Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(name))     throw new DomainException("Property name is required.");
        if (string.IsNullOrWhiteSpace(location)) throw new DomainException("Location is required.");
        if (ownerId == Guid.Empty)               throw new DomainException("Owner is required.");
        return new Property(name, description, location, pricePerNight, ownerId);
    }

    public void Update(string name, string description, string location, Money pricePerNight)
    {
        if (string.IsNullOrWhiteSpace(name))     throw new DomainException("Property name is required.");
        if (string.IsNullOrWhiteSpace(location)) throw new DomainException("Location is required.");
        Name = name;
        Description = description;
        Location = location;
        PricePerNight = pricePerNight;
        SetUpdatedAt();
    }

    public void Deactivate() { IsActive = false; SetUpdatedAt(); }
    public void Activate()   { IsActive = true;  SetUpdatedAt(); }
}
