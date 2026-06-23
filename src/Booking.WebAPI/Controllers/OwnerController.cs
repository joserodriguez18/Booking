using Booking.Application.Common.DTOs;
using Booking.Application.Owner.Queries.ExportReport;
using Booking.Application.Owner.Queries.GetDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Booking.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/owner")]
public sealed class OwnerController : ControllerBase
{
    private readonly IMediator _mediator;

    public OwnerController(IMediator mediator) => _mediator = mediator;

    /// <summary>Dashboard con métricas de ocupación e ingresos por propiedad.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Dashboard(
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        CancellationToken ct)
    {
        var ownerId = ObtenerUsuarioId();
        var resultado = await _mediator.Send(new GetOwnerDashboardQuery(ownerId, desde, hasta), ct);
        return Ok(resultado);
    }

    /// <summary>Exporta reservas confirmadas a un archivo Excel (.xlsx).</summary>
    [HttpGet("report/export")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportReport(
        [FromQuery] Guid? propertyId,
        CancellationToken ct)
    {
        var ownerId = ObtenerUsuarioId();
        var bytes = await _mediator.Send(new ExportBookingsReportCommand(ownerId, propertyId), ct);

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"reservas_{DateTime.UtcNow:yyyyMMdd}.xlsx");
    }

    private Guid ObtenerUsuarioId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());
}
