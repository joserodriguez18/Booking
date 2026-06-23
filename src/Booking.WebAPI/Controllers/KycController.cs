using Booking.Application.Common.DTOs;
using Booking.Application.KYC.Commands.UploadIdentityDocument;
using Booking.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Booking.WebAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class KycController : ControllerBase
{
    private readonly IMediator _mediator;

    public KycController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Sube un documento de identidad para verificación KYC.
    /// El documento se procesa con IA y se elimina de MinIO después de la verificación.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(KycExtractionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload([FromForm] KycUploadRequest req, CancellationToken ct)
    {
        if (req.Archivo is null || req.Archivo.Length == 0)
            return BadRequest(new { error = "Debes adjuntar un archivo de imagen del documento." });

        var userId = ObtenerUsuarioId();

        await using var stream = req.Archivo.OpenReadStream();
        var resultado = await _mediator.Send(
            new UploadIdentityDocumentCommand(userId, stream, req.Archivo.FileName, req.Archivo.ContentType, req.TipoDocumento), ct);

        return Ok(resultado);
    }

    private Guid ObtenerUsuarioId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());
}

public sealed class KycUploadRequest
{
    public IFormFile     Archivo       { get; set; } = null!;
    public DocumentType  TipoDocumento { get; set; }
}
