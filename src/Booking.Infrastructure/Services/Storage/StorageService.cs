using Booking.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace Booking.Infrastructure.Services.Storage;

/// <summary>
/// Servicio de almacenamiento de archivos sobre MinIO (compatible con S3).
/// Los documentos KYC se guardan en un bucket privado y se borran tras la verificación.
/// </summary>
public class StorageService : IStorageService
{
    private readonly IMinioClient _minio;
    private readonly string _bucketKyc;

    public StorageService(IMinioClient minio, IConfiguration config)
    {
        _minio    = minio;
        _bucketKyc = config["MINIO_BUCKET_KYC"] ?? "kyc-documents";
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        await AsegurarBucketExisteAsync(ct);

        // Genera un nombre único para evitar colisiones y ocultar nombres originales
        var extension  = Path.GetExtension(fileName);
        var objectKey  = $"kyc/{Guid.NewGuid()}{extension}";

        var args = new PutObjectArgs()
            .WithBucket(_bucketKyc)
            .WithObject(objectKey)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType);

        await _minio.PutObjectAsync(args, ct);
        return objectKey;
    }

    public async Task<string> GetPresignedUrlAsync(
        string objectKey,
        int expirationMinutes = 30,
        CancellationToken ct = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucketKyc)
            .WithObject(objectKey)
            .WithExpiry(expirationMinutes * 60);

        // PresignedGetObjectAsync no acepta CancellationToken en Minio SDK v7
        return await _minio.PresignedGetObjectAsync(args);
    }

    public async Task DeleteFileAsync(string objectKey, CancellationToken ct = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_bucketKyc)
            .WithObject(objectKey);

        await _minio.RemoveObjectAsync(args, ct);
    }

    // Verifica y crea el bucket si no existe — se llama en cada subida (operación ligera)
    private async Task AsegurarBucketExisteAsync(CancellationToken ct)
    {
        var existeArgs = new BucketExistsArgs().WithBucket(_bucketKyc);
        bool existe    = await _minio.BucketExistsAsync(existeArgs, ct);

        if (!existe)
        {
            var crearArgs = new MakeBucketArgs().WithBucket(_bucketKyc);
            await _minio.MakeBucketAsync(crearArgs, ct);
        }
    }
}
