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
    /// Sube uno o más archivos del documento de identidad para verificación KYC.
    /// Permite subir cara frontal y trasera del documento en una sola solicitud.
    /// Los archivos se procesan con IA en conjunto y se eliminan de MinIO tras la verificación.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(KycExtractionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload([FromForm] KycUploadRequest req, CancellationToken ct)
    {
        if (req.Archivos is null || req.Archivos.Count == 0)
            return BadRequest(new { error = "Debes adjuntar al menos una imagen del documento." });

        if (req.Archivos.Count > 3)
            return BadRequest(new { error = "Máximo 3 imágenes por verificación." });

        if (req.Archivos.Any(a => a.Length == 0))
            return BadRequest(new { error = "Uno o más archivos están vacíos." });

        var userId = ObtenerUsuarioId();

        var archivos = req.Archivos
            .Select(a => new DocumentoArchivo(a.OpenReadStream(), a.FileName, a.ContentType))
            .ToList();

        var resultado = await _mediator.Send(
            new UploadIdentityDocumentCommand(userId, archivos, req.TipoDocumento), ct);

        // Liberar los streams
        foreach (var archivo in archivos)
            await archivo.Stream.DisposeAsync();

        return Ok(resultado);
    }

    private Guid ObtenerUsuarioId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException());
}

public sealed class KycUploadRequest
{
    public List<IFormFile> Archivos      { get; set; } = new();
    public DocumentType    TipoDocumento { get; set; }
}
