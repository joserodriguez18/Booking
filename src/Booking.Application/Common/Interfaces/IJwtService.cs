using System.Security.Claims;
using Booking.Domain.Entities;

namespace Booking.Application.Common.Interfaces;

/// <summary>
/// Contrato para generación y validación de tokens JWT (access + refresh).
/// </summary>
public interface IJwtService
{
    /// <summary>Genera un access token JWT firmado con los claims del usuario.</summary>
    string GenerateAccessToken(User user);

    /// <summary>Genera un refresh token opaco y criptográficamente seguro.</summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Valida un token JWT y devuelve sus claims.
    /// Devuelve null si el token es inválido o expiró.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}
