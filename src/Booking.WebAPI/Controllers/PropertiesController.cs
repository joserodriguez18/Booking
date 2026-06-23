using Booking.Application.Common.DTOs;
using Booking.Application.Properties.Commands.CreateProperty;
using Booking.Application.Properties.Commands.UpdateProperty;
using Booking.Application.Properties.Queries.GetProperties;
using Booking.Application.Properties.Queries.GetPropertyById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Booking.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class PropertiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PropertiesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Busca propiedades disponibles. Anónimo permitido.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PropertyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProperties(
        [FromQuery] string?   ubicacion,
        [FromQuery] DateOnly? checkIn,
        [FromQuery] DateOnly? checkOut,
        CancellationToken ct)
    {
        var resultado = await _mediator.Send(
            new GetPropertiesQuery(ubicacion, checkIn, checkOut), ct);
        return Ok(resultado);
    }

    /// <summary>Obtiene el detalle de una propiedad.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PropertyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var resultado = await _mediator.Send(new GetPropertyByIdQuery(id), ct);
        return Ok(resultado);
    }

    /// <summary>Crea una nueva propiedad. Solo propietarios.</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePropertyRequest req, CancellationToken ct)
    {
        var ownerId = ObtenerUsuarioId();
        var id = await _mediator.Send(
            new CreatePropertyCommand(req.Name, req.Description, req.Location,
                req.PricePerNight, req.Currency, ownerId), ct);
        return StatusCode(StatusCodes.Status201Created, id);
    }

    /// <summary>Actualiza una propiedad existente. Solo el propietario.</summary>
    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePropertyRequest req, CancellationToken ct)
    {
        var ownerId = ObtenerUsuarioId();
        await _mediator.Send(
            new UpdatePropertyCommand(id, ownerId, req.Name, req.Description, req.Location,
                req.PricePerNight, req.Currency), ct);
        return NoContent();
    }

    private Guid ObtenerUsuarioId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());
}

// ── DTOs de request ───────────────────────────────────────────────────────────

public sealed record CreatePropertyRequest(
    string  Name,
    string  Description,
    string  Location,
    decimal PricePerNight,
    string  Currency);

public sealed record UpdatePropertyRequest(
    string  Name,
    string  Description,
    string  Location,
    decimal PricePerNight,
    string  Currency);
