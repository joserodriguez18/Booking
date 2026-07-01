using Booking.Application.Auth.Commands.Login;
using Booking.Application.Auth.Commands.Register;
using Booking.Application.Auth.Commands.RefreshToken;
using Booking.Application.Common.DTOs;
using Booking.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Booking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>Registra un nuevo usuario (huésped o propietario).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(TokenResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        var resultado = await _mediator.Send(
            new RegisterCommand(req.Name, req.Email, req.Password, req.Role), ct);
        return StatusCode(StatusCodes.Status201Created, resultado);
    }

    /// <summary>Inicia sesión y devuelve un par de tokens JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var resultado = await _mediator.Send(new LoginCommand(req.Email, req.Password), ct);
        return Ok(resultado);
    }

    /// <summary>Rota el refresh token y devuelve un nuevo par de tokens.</summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req, CancellationToken ct)
    {
        var resultado = await _mediator.Send(new RefreshTokenCommand(req.Token), ct);
        return Ok(resultado);
    }
}

// ── DTOs de request ───────────────────────────────────────────────────────────

public sealed record RegisterRequest(string? Name, string Email, string Password, UserRole Role);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string Token);
