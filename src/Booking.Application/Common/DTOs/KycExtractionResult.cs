using Booking.Domain.Enums;

namespace Booking.Application.Common.DTOs;

/// <summary>
/// Resultado del procesamiento de un documento de identidad por IA (Gemini).
/// </summary>
public record KycExtractionResult(
    bool Success,
    string? DocumentNumber,
    string? ExtractedNames,
    DateOnly? BirthDate,
    DocumentType DocumentType,
    string? ErrorMessage
);
