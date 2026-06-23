using Booking.Application.Bookings.Commands.CancelBooking;
using Booking.Application.Bookings.Commands.ConfirmBooking;
using Booking.Application.Bookings.Commands.CreateBooking;
using Booking.Application.Bookings.Queries.GetMyBookings;
using Booking.Application.Common.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Booking.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookingsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Crea una reserva en estado Pendiente.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest req, CancellationToken ct)
    {
        var guestId = ObtenerUsuarioId();
        var id = await _mediator.Send(
            new CreateBookingCommand(req.PropertyId, guestId, req.CheckIn, req.CheckOut), ct);
        return StatusCode(StatusCodes.Status201Created, id);
    }

    /// <summary>Confirma una reserva pendiente (requiere KYC en primera reserva).</summary>
    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken ct)
    {
        var userId = ObtenerUsuarioId();
        await _mediator.Send(new ConfirmBookingCommand(id, userId), ct);
        return NoContent();
    }

    /// <summary>Cancela una reserva. Solo el huésped o el propietario pueden cancelar.</summary>
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var userId = ObtenerUsuarioId();
        await _mediator.Send(new CancelBookingCommand(id, userId), ct);
        return NoContent();
    }

    /// <summary>Obtiene las reservas del usuario autenticado.</summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookings(CancellationToken ct)
    {
        var userId = ObtenerUsuarioId();
        var resultado = await _mediator.Send(new GetMyBookingsQuery(userId), ct);
        return Ok(resultado);
    }

    private Guid ObtenerUsuarioId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());
}

// ── DTOs de request ───────────────────────────────────────────────────────────

public sealed record CreateBookingRequest(Guid PropertyId, DateOnly CheckIn, DateOnly CheckOut);
