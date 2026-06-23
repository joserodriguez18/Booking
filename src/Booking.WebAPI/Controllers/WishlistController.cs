using Booking.Application.Common.DTOs;
using Booking.Application.Wishlist.Commands.AddToWishlist;
using Booking.Application.Wishlist.Commands.RemoveFromWishlist;
using Booking.Application.Wishlist.Queries.GetWishlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Booking.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;

    public WishlistController(IMediator mediator) => _mediator = mediator;

    /// <summary>Obtiene la lista de deseos del usuario autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PropertyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var userId = ObtenerUsuarioId();
        var resultado = await _mediator.Send(new GetWishlistQuery(userId), ct);
        return Ok(resultado);
    }

    /// <summary>Agrega una propiedad a la lista de deseos.</summary>
    [HttpPost("{propertyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add(Guid propertyId, CancellationToken ct)
    {
        var userId = ObtenerUsuarioId();
        await _mediator.Send(new AddToWishlistCommand(userId, propertyId), ct);
        return NoContent();
    }

    /// <summary>Elimina una propiedad de la lista de deseos.</summary>
    [HttpDelete("{propertyId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(Guid propertyId, CancellationToken ct)
    {
        var userId = ObtenerUsuarioId();
        await _mediator.Send(new RemoveFromWishlistCommand(userId, propertyId), ct);
        return NoContent();
    }

    private Guid ObtenerUsuarioId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());
}
