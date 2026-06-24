namespace Booking.Application.Common.Interfaces;

/// <summary>
/// Contrato para almacenamiento de archivos (implementado con MinIO).
/// Dos modos: privado con URL prefirmada (documentos KYC) y público con URL directa (fotos de propiedades).
/// </summary>
public interface IStorageService
{
    // ── Bucket privado (KYC) ─────────────────────────────────────────────────

    /// <summary>Sube un archivo al bucket privado. Devuelve el object key.</summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string carpeta = "kyc", CancellationToken ct = default);

    /// <summary>Genera una URL prefirmada temporal (acceso privado, expira en <paramref name="expirationMinutes"/> minutos).</summary>
    Task<string> GetPresignedUrlAsync(string objectKey, int expirationMinutes = 30, CancellationToken ct = default);

    /// <summary>Elimina permanentemente el objeto del bucket privado (borrado seguro post-KYC).</summary>
    Task DeleteFileAsync(string objectKey, CancellationToken ct = default);

    // ── Bucket público (fotos de propiedades) ────────────────────────────────

    /// <summary>Sube un archivo al bucket público de fotos. Devuelve el object key.</summary>
    Task<string> UploadPublicFileAsync(Stream fileStream, string fileName, string contentType, string carpeta, CancellationToken ct = default);

    /// <summary>Devuelve la URL pública directa para un object key del bucket de fotos (sin expiración).</summary>
    string GetPublicUrl(string objectKey);

    /// <summary>Elimina permanentemente el objeto del bucket público de fotos.</summary>
    Task DeletePublicFileAsync(string objectKey, CancellationToken ct = default);
}
