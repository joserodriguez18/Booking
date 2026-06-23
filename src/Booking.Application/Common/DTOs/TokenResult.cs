namespace Booking.Application.Common.DTOs;

/// <summary>
/// Resultado devuelto tras una autenticación exitosa.
/// Contiene el access token (corta duración) y el refresh token (larga duración).
/// </summary>
public record TokenResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenVenceEn,
    DateTimeOffset RefreshTokenVenceEn
);
