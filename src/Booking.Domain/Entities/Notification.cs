using Booking.Domain.Enums;
using Booking.Domain.Exceptions;

namespace Booking.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public Guid             UserId  { get; private set; }
    public string           Title   { get; private set; } = string.Empty;
    public string           Body    { get; private set; } = string.Empty;
    public NotificationType Type    { get; private set; }
    public bool             IsRead  { get; private set; }

    private Notification() { }

    public static Notification Create(Guid userId, string title, string body, NotificationType type)
    {
        if (userId == Guid.Empty)            throw new DomainException("Usuario requerido.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Título requerido.");
        return new Notification { UserId = userId, Title = title, Body = body, Type = type };
    }

    public void MarkAsRead() { IsRead = true; SetUpdatedAt(); }
}
