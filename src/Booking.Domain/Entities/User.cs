using Booking.Domain.Enums;
using Booking.Domain.Exceptions;

namespace Booking.Domain.Entities;

public sealed class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public KycStatus KycStatus { get; private set; } = KycStatus.NotStarted;

    // Convenience property: true only when KYC is fully approved.
    public bool IsIdentityVerified => KycStatus == KycStatus.Approved;

    private User() { } // Required by EF Core

    private User(string name, string email, string passwordHash, UserRole role)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public static User Create(string name, string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Name is required.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        return new User(name, email.ToLowerInvariant().Trim(), passwordHash, role);
    }

    public void SetKycPending()
    {
        if (KycStatus == KycStatus.Approved)
            throw new DomainException("Identity is already verified.");
        KycStatus = KycStatus.Pending;
        SetUpdatedAt();
    }

    public void ApproveKyc()
    {
        KycStatus = KycStatus.Approved;
        SetUpdatedAt();
    }

    public void RejectKyc()
    {
        KycStatus = KycStatus.Rejected;
        SetUpdatedAt();
    }
}
