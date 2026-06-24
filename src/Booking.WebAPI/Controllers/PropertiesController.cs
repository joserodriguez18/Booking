using Booking.Application.Common.DTOs;
using Booking.Application.Common.Interfaces;
using Booking.Application.Properties.Commands.CreateProperty;
using Booking.Application.Properties.Commands.DeletePropertyPhoto;
using Booking.Application.Properties.Commands.UpdateProperty;
using Booking.Application.Properties.Commands.UploadPropertyPhoto;
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
    private readonly IMediator       _mediator;
    private readonly IStorageService _storage;

    public PropertiesController(IMediator mediator, IStorageService storage)
    {
        _mediator = mediator;
        _storage  = storage;
    }

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

    /// <summary>Sube una foto a la propiedad (máx. 10). Solo el propietario.</summary>
    [Authorize]
    [HttpPost("{id:guid}/photos")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PhotoUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadPhoto(Guid id, [FromForm] PropertyPhotoRequest req, CancellationToken ct)
    {
        if (req.Foto is null || req.Foto.Length == 0)
            return BadRequest(new { error = "Debes adjuntar un archivo de imagen." });

        var ownerId   = ObtenerUsuarioId();
        await using var stream = req.Foto.OpenReadStream();

        var objectKey = await _mediator.Send(
            new UploadPropertyPhotoCommand(id, ownerId, stream, req.Foto.FileName, req.Foto.ContentType), ct);

        return StatusCode(StatusCodes.Status201Created,
            new PhotoUploadResponse(objectKey, _storage.GetPublicUrl(objectKey)));
    }

    /// <summary>
    /// Devuelve la URL pública directa de una foto (sin expiración).
    /// Ya no se necesita este endpoint pues GET /api/properties/{id} incluye las URLs completas,
    /// pero se mantiene por compatibilidad con clientes que tengan el objectKey.
    /// </summary>
    [HttpGet("{id:guid}/photos/url")]
    [ProducesResponseType(typeof(PhotoUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetPhotoUrl(Guid id, [FromQuery] string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return BadRequest(new { error = "El parámetro objectKey es obligatorio." });

        var url = _storage.GetPublicUrl(objectKey);
        return Ok(new PhotoUrlResponse(url));
    }

    /// <summary>Elimina una foto de la propiedad. Solo el propietario.</summary>
    [Authorize]
    [HttpDelete("{id:guid}/photos")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoto(Guid id, [FromQuery] string objectKey, CancellationToken ct)
    {
        var ownerId = ObtenerUsuarioId();
        await _mediator.Send(new DeletePropertyPhotoCommand(id, ownerId, objectKey), ct);
        return NoContent();
    }

    private Guid ObtenerUsuarioId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());
}

// ── DTOs de fotos ─────────────────────────────────────────────────────────────

public sealed class PropertyPhotoRequest
{
    public IFormFile Foto { get; set; } = null!;
}

public sealed record PhotoUploadResponse(string ObjectKey, string Url);
public sealed record PhotoUrlResponse(string Url);

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
