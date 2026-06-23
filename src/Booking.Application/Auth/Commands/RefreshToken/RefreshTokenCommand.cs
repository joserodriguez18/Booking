using Booking.Application.Auth.Commands.Register;
using Booking.Application.Common.DTOs;
using Booking.Application.Common.Interfaces;
using Booking.Domain.Entities;
using Booking.Domain.Exceptions;
using DomainRefreshToken = Booking.Domain.Entities.RefreshToken;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string Token) : IRequest<TokenResult>;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResult>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IJwtService          _jwt;

    public RefreshTokenCommandHandler(IApplicationDbContext ctx, IJwtService jwt)
    {
        _ctx = ctx;
        _jwt = jwt;
    }

    public async Task<TokenResult> Handle(RefreshTokenCommand req, CancellationToken ct)
    {
        var hash = RegisterCommandHandler.HashToken(req.Token);

        var tokenGuardado = await _ctx.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct)
            ?? throw new DomainException("Refresh token inválido.");

        if (!tokenGuardado.IsValid)
            throw new DomainException("El refresh token ha expirado o fue revocado.");

        var usuario = await _ctx.Usuarios
            .FirstOrDefaultAsync(u => u.Id == tokenGuardado.UserId, ct)
            ?? throw new DomainException("Usuario no encontrado.");

        // Rota el refresh token — invalida el anterior
        tokenGuardado.Revoke();

        var nuevoAccess  = _jwt.GenerateAccessToken(usuario);
        var nuevoRefresh = _jwt.GenerateRefreshToken();
        _ctx.RefreshTokens.Add(DomainRefreshToken.Create(usuario.Id, RegisterCommandHandler.HashToken(nuevoRefresh)));

        await _ctx.SaveChangesAsync(ct);

        return new TokenResult(
            nuevoAccess, nuevoRefresh,
            DateTimeOffset.UtcNow.AddMinutes(60),
            DateTimeOffset.UtcNow.AddDays(7));
    }
}
