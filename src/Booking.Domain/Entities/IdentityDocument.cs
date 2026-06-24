using Booking.Domain.Enums;
using Booking.Domain.Exceptions;

namespace Booking.Domain.Entities;

public sealed class IdentityDocument : BaseEntity
{
    public Guid UserId { get; private set; }
    public string DocumentNumber { get; private set; } = string.Empty;
    public DocumentType DocumentType { get; private set; }
    public string ExtractedNames { get; private set; } = string.Empty;
    public DateOnly? ExtractedBirthDate { get; private set; }

    /// <summary>
    /// MinIO object key. Null after the document has been securely deleted post-verification.
    /// </summary>
    public string? DocumentUrl { get; private set; }

    public DateTimeOffset UploadedAt { get; private set; }

    /// <summary>
    /// True once the file has been removed from storage (post-KYC verdict, per privacy requirement).
    /// </summary>
    public bool IsDocumentDeleted { get; private set; }

    private IdentityDocument() { }

    private IdentityDocument(Guid userId, DocumentType documentType)
    {
        UserId       = userId;
        DocumentType = documentType;
        UploadedAt   = DateTimeOffset.UtcNow;
    }

    public static IdentityDocument CreatePending(Guid userId, DocumentType documentType)
    {
        if (userId == Guid.Empty) throw new DomainException("El usuario es obligatorio.");
        return new IdentityDocument(userId, documentType);
    }

    public void SetDocumentUrl(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new DomainException("La clave del documento en almacenamiento es obligatoria.");
        DocumentUrl = objectKey;
        SetUpdatedAt();
    }

    /// <summary>
    /// Populated by the AI extraction service after Gemini processes the image.
    /// </summary>
    public void ApplyExtractedData(string documentNumber, string extractedNames, DateOnly birthDate)
    {
        if (string.IsNullOrWhiteSpace(documentNumber)) throw new DomainException("El número de documento es obligatorio.");
        if (string.IsNullOrWhiteSpace(extractedNames)) throw new DomainException("Los nombres extraídos son obligatorios.");
        DocumentNumber      = documentNumber;
        ExtractedNames      = extractedNames;
        ExtractedBirthDate  = birthDate;
        SetUpdatedAt();
    }

    /// <summary>
    /// Called after KYC verdict is issued. Signals that the file must be deleted from MinIO
    /// (cryptographic deletion + record nullification to comply with privacy requirements).
    /// </summary>
    public void MarkDocumentAsDeleted()
    {
        DocumentUrl       = null;
        IsDocumentDeleted = true;
        SetUpdatedAt();
    }
}
