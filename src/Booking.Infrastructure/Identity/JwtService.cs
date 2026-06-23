using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Booking.Application.Common.Interfaces;
using Booking.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Booking.Infrastructure.Identity;

/// <summary>
/// Servicio de autenticación JWT.
/// - Access token: corta duración (JWT firmado con HMAC-SHA256).
/// - Refresh token: opaco, generado con datos criptográficamente aleatorios.
/// </summary>
public class JwtService : IJwtService
{
    private readonly string _secretAcceso;
    private readonly string _emisor;
    private readonly string _audiencia;
    private readonly int _minutosExpiracion;

    public JwtService(IConfiguration config)
    {
        _secretAcceso    = config["JWT_SECRET"]          ?? throw new InvalidOperationException("Falta la variable de entorno JWT_SECRET.");
        _emisor          = config["JWT_ISSUER"]          ?? "BookingApp";
        _audiencia       = config["JWT_AUDIENCE"]        ?? "BookingAppUsers";
        _minutosExpiracion = int.TryParse(config["JWT_EXPIRY_MINUTES"], out var min) ? min : 60;
    }

    public string GenerateAccessToken(User user)
    {
        var claveSeguridad  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretAcceso));
        var credenciales    = new SigningCredentials(claveSeguridad, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name,  user.Name),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new(ClaimTypes.Role,               user.Role.ToString()),
            new("kyc_status",                  user.KycStatus.ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject            = new ClaimsIdentity(claims),
            Expires            = DateTime.UtcNow.AddMinutes(_minutosExpiracion),
            Issuer             = _emisor,
            Audience           = _audiencia,
            SigningCredentials = credenciales
        };

        var handler = new JwtSecurityTokenHandler();
        var token   = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // Token opaco de 64 bytes usando un CSPRNG — no es un JWT
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        var parametros = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidIssuer              = _emisor,
            ValidateAudience         = true,
            ValidAudience            = _audiencia,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretAcceso)),
            ClockSkew                = TimeSpan.Zero  // Sin margen de tolerancia extra
        };

        try
        {
            return handler.ValidateToken(token, parametros, out _);
        }
        catch
        {
            // Token inválido, expirado o manipulado
            return null;
        }
    }
}