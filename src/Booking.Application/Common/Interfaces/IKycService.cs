using Booking.Application.Common.DTOs;

namespace Booking.Application.Common.Interfaces;

/// <summary>
/// Contrato para el servicio de validación de identidad asistido por IA (KYC).
/// En producción usa la API de Gemini Vision; en desarrollo usa un mock coherente.
/// </summary>
public interface IKycService
{
    /// <summary>
    /// Procesa la imagen de un documento de identidad y extrae los datos mediante IA.
    /// </summary>
    /// <param name="objectKey">Clave del objeto en MinIO donde se almacenó el documento.</param>
    Task<KycExtractionResult> ProcessIdentityDocumentAsync(string objectKey, CancellationToken ct = default);
}
