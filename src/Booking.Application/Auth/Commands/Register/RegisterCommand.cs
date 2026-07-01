using System.Security.Cryptography;
using System.Text;
using Booking.Application.Common.DTOs;
using Booking.Application.Common.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Enums;
using DomainRefreshToken = Booking.Domain.Entities.RefreshToken;
using Booking.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Auth.Commands.Register;

public sealed record RegisterCommand(
    string?  Name,
    string   Email,
    string   Password,
    UserRole Role
) : IRequest<TokenResult>;

// ── Validador ────────────────────────────────────────────────────────────────

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        // Los propietarios deben identificarse desde el registro; los huéspedes solo dan correo y
        // contraseña — su nombre real se completa al verificar su identidad (KYC) antes de reservar.
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio para propietarios.")
            .When(x => x.Role == UserRole.Owner);

        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("El nombre no puede superar 200 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El correo electrónico no tiene un formato válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches(@"[A-Z]").WithMessage("La contraseña debe contener al menos una letra mayúscula.")
            .Matches(@"\d").WithMessage("La contraseña debe contener al menos un número.");
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, TokenResult>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IPasswordHasher      _hasher;
    private readonly IJwtService          _jwt;
    private readonly IEmailService        _email;

    public RegisterCommandHandler(
        IApplicationDbContext ctx,
        IPasswordHasher hasher,
        IJwtService jwt,
        IEmailService email)
    {
        _ctx    = ctx;
        _hasher = hasher;
        _jwt    = jwt;
        _email  = email;
    }

    public async Task<TokenResult> Handle(RegisterCommand req, CancellationToken ct)
    {
        // Verifica unicidad del email
        var existe = await _ctx.Usuarios.AnyAsync(u => u.Email == req.Email.ToLower(), ct);
        if (existe)
            throw new DomainException("Ya existe una cuenta con ese correo electrónico.");

        // Si no se dio nombre (registro simplificado de huésped), se usa un placeholder temporal;
        // el nombre real lo reemplaza el dato leído del documento cuando se aprueba el KYC.
        var nombre = string.IsNullOrWhiteSpace(req.Name) ? req.Email.Split('@')[0] : req.Name;

        var usuario = User.Create(nombre, req.Email, _hasher.Hash(req.Password), req.Role);
        _ctx.Usuarios.Add(usuario);

        // Genera tokens y guarda el refresh token (hash SHA-256)
        var (accessToken, refreshToken, tokenDto) = GenerarTokens(usuario);
        _ctx.RefreshTokens.Add(DomainRefreshToken.Create(usuario.Id, HashToken(refreshToken)));

        await _ctx.SaveChangesAsync(ct);

        // Notificación de bienvenida (no crítica — falla silenciosa)
        _ = _email.SendAlertAsync(
            usuario.Email,
            "¡Bienvenido a Booking Platform!",
            $"<h2>Hola {usuario.Name},</h2><p>Tu cuenta ha sido creada exitosamente.</p>",
            ct).ConfigureAwait(false);

        return tokenDto;
    }

    private (string access, string refresh, TokenResult dto) GenerarTokens(User usuario)
    {
        var access  = _jwt.GenerateAccessToken(usuario);
        var refresh = _jwt.GenerateRefreshToken();
        var dto     = new TokenResult(
            access, refresh,
            DateTimeOffset.UtcNow.AddMinutes(60),
            DateTimeOffset.UtcNow.AddDays(7));
        return (access, refresh, dto);
    }

    internal static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLower();
    }
}
