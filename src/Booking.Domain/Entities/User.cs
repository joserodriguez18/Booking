using Booking.Domain.Enums;
using Booking.Domain.Exceptions;

namespace Booking.Domain.Entities;

public sealed class User : BaseEntity
{
    public string   Name              { get; private set; } = string.Empty;
    public string   Email             { get; private set; } = string.Empty;
    public string   PasswordHash      { get; private set; } = string.Empty;
    public UserRole Role              { get; private set; }
    public KycStatus KycStatus        { get; private set; } = KycStatus.NotStarted;

    // Datos extraídos del documento de identidad durante el proceso KYC
    public string?   NumeroDocumento  { get; private set; }
    public DateOnly? FechaNacimiento  { get; private set; }

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
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("El correo electrónico es obligatorio.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("El hash de contraseña es obligatorio.");
        return new User(name, email.ToLowerInvariant().Trim(), passwordHash, role);
    }

    public void SetKycPending()
    {
        if (KycStatus == KycStatus.Approved)
            throw new DomainException("La identidad de este usuario ya fue verificada.");
        KycStatus = KycStatus.Pending;
        SetUpdatedAt();
    }

    public void ApproveKyc(string? numeroDocumento = null, DateOnly? fechaNacimiento = null, string? nombreVerificado = null)
    {
        KycStatus        = KycStatus.Approved;
        NumeroDocumento  = numeroDocumento;
        FechaNacimiento  = fechaNacimiento;
        // El nombre leído del documento reemplaza el nombre ingresado (o el placeholder) en el registro.
        if (!string.IsNullOrWhiteSpace(nombreVerificado))
            Name = nombreVerificado;
        SetUpdatedAt();
    }

    public void RejectKyc()
    {
        KycStatus = KycStatus.Rejected;
        SetUpdatedAt();
    }
}
