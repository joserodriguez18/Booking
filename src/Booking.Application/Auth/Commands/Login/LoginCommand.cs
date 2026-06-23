using System.Security.Cryptography;
using System.Text;
using Booking.Application.Auth.Commands.Register;
using Booking.Application.Common.DTOs;
using Booking.Application.Common.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Exceptions;
using DomainRefreshToken = Booking.Domain.Entities.RefreshToken;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Auth.Commands.Login;

public sealed record LoginCommand(string Email, string Password) : IRequest<TokenResult>;

// ── Validador ────────────────────────────────────────────────────────────────

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Correo electrónico inválido.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResult>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IPasswordHasher      _hasher;
    private readonly IJwtService          _jwt;

    public LoginCommandHandler(IApplicationDbContext ctx, IPasswordHasher hasher, IJwtService jwt)
    {
        _ctx    = ctx;
        _hasher = hasher;
        _jwt    = jwt;
    }

    public async Task<TokenResult> Handle(LoginCommand req, CancellationToken ct)
    {
        var usuario = await _ctx.Usuarios
            .FirstOrDefaultAsync(u => u.Email == req.Email.ToLower(), ct);

        // Mensaje genérico para no revelar si el email existe
        if (usuario is null || !_hasher.Verify(req.Password, usuario.PasswordHash))
            throw new DomainException("Credenciales inválidas. Verifica tu correo y contraseña.");

        var accessToken  = _jwt.GenerateAccessToken(usuario);
        var refreshToken = _jwt.GenerateRefreshToken();

        _ctx.RefreshTokens.Add(DomainRefreshToken.Create(usuario.Id, RegisterCommandHandler.HashToken(refreshToken)));
        await _ctx.SaveChangesAsync(ct);

        return new TokenResult(
            accessToken, refreshToken,
            DateTimeOffset.UtcNow.AddMinutes(60),
            DateTimeOffset.UtcNow.AddDays(7));
    }
}
