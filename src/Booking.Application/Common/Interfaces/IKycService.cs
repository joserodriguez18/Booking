using Booking.Application.Common.DTOs;

namespace Booking.Application.Common.Interfaces;

/// <summary>
/// Contrato para el servicio de validación de identidad asistido por IA (KYC).
/// En producción usa la API de Gemini Vision; en desarrollo usa un mock coherente.
/// </summary>
public interface IKycService
{
    /// <summary>
    /// Procesa una o más imágenes del documento de identidad (ej. cara frontal y trasera)
    /// y extrae los datos mediante IA en una sola llamada.
    /// </summary>
    /// <param name="imagenes">Imágenes a procesar (mínimo 1, máximo 3), con su tipo de contenido original.</param>
    Task<KycExtractionResult> ProcessIdentityDocumentAsync(IReadOnlyList<KycImagen> imagenes, CancellationToken ct = default);
}

/// <summary>Referencia a una imagen subida a MinIO junto con su tipo de contenido original (ej. image/png, image/jpeg).</summary>
public sealed record KycImagen(string ObjectKey, string ContentType);
