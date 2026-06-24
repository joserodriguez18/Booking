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
    /// <param name="objectKeys">Claves de los objetos en MinIO (mínimo 1, máximo 3).</param>
    Task<KycExtractionResult> ProcessIdentityDocumentAsync(IReadOnlyList<string> objectKeys, CancellationToken ct = default);
}
