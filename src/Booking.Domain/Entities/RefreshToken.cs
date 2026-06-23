using Booking.Domain.Exceptions;

namespace Booking.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public Guid   UserId    { get; private set; }
    /// <summary>Hash SHA-256 del token real enviado al cliente.</summary>
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt  { get; private set; }
    public bool           IsRevoked  { get; private set; }

    public bool IsValid => !IsRevoked && ExpiresAt > DateTimeOffset.UtcNow;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, int expiryDays = 7)
    {
        if (userId    == Guid.Empty)             throw new DomainException("Usuario requerido.");
        if (string.IsNullOrWhiteSpace(tokenHash)) throw new DomainException("Token hash requerido.");
        return new RefreshToken
        {
            UserId    = userId,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(expiryDays)
        };
    }

    public void Revoke() { IsRevoked = true; SetUpdatedAt(); }
}
