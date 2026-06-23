namespace Booking.Application.Common.Interfaces;

/// <summary>
/// Contrato para hash y verificación de contraseñas (implementado con BCrypt).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Genera el hash BCrypt de una contraseña en texto plano.</summary>
    string Hash(string password);

    /// <summary>Verifica si una contraseña en texto plano coincide con su hash BCrypt.</summary>
    bool Verify(string password, string hash);
}
