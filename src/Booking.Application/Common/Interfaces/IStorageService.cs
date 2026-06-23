namespace Booking.Application.Common.Interfaces;

/// <summary>
/// Contrato para almacenamiento seguro de archivos (implementado con MinIO).
/// Usado principalmente para documentos de identidad del proceso KYC.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Sube un archivo al bucket de almacenamiento y devuelve la clave del objeto (object key).
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>
    /// Genera una URL prefirmada temporal para acceder al objeto de forma segura.
    /// </summary>
    Task<string> GetPresignedUrlAsync(string objectKey, int expirationMinutes = 30, CancellationToken ct = default);

    /// <summary>
    /// Elimina permanentemente el objeto del almacenamiento (borrado seguro post-KYC).
    /// </summary>
    Task DeleteFileAsync(string objectKey, CancellationToken ct = default);
}
