using Booking.Application.Common.Interfaces;

namespace Booking.Infrastructure.Identity;

/// <summary>
/// Implementación de hash de contraseñas usando BCrypt con work factor 12
/// (costo computacional intencionalmente alto para dificultar ataques de fuerza bruta).
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
