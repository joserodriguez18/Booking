using Booking.Domain.Exceptions;

namespace Booking.Domain.Entities;

public sealed class WishlistItem : BaseEntity
{
    public Guid UserId     { get; private set; }
    public Guid PropertyId { get; private set; }

    private WishlistItem() { }

    public static WishlistItem Create(Guid userId, Guid propertyId)
    {
        if (userId     == Guid.Empty) throw new DomainException("Usuario requerido.");
        if (propertyId == Guid.Empty) throw new DomainException("Propiedad requerida.");
        return new WishlistItem { UserId = userId, PropertyId = propertyId };
    }
}
