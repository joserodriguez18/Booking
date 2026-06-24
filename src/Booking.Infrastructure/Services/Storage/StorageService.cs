using Booking.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace Booking.Infrastructure.Services.Storage;

/// <summary>
/// Servicio de almacenamiento sobre MinIO (compatible con S3).
/// Bucket privado (kyc-documents) para documentos KYC con URL prefirmada.
/// Bucket público (property-photos) para fotos de propiedades con URL directa.
/// </summary>
public class StorageService : IStorageService
{
    private readonly IMinioClient _minio;
    private readonly string _bucketKyc;
    private readonly string _bucketFotos;
    private readonly string _endpointInterno;
    private readonly string _endpointPublico;

    public StorageService(IMinioClient minio, IConfiguration config)
    {
        _minio           = minio;
        _bucketKyc       = config["MINIO_BUCKET_KYC"]        ?? "kyc-documents";
        _bucketFotos     = config["MINIO_BUCKET_PROPERTIES"]  ?? "property-photos";
        _endpointInterno = config["MINIO_ENDPOINT"]           ?? "http://minio:9000";
        _endpointPublico = config["MINIO_PUBLIC_ENDPOINT"]    ?? _endpointInterno;
    }

    // ── Bucket privado (KYC) ─────────────────────────────────────────────────

    public async Task<string> UploadFileAsync(
        Stream fileStream, string fileName, string contentType,
        string carpeta = "kyc", CancellationToken ct = default)
    {
        await AsegurarBucketPrivadoAsync(ct);

        var objectKey = $"{carpeta}/{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketKyc)
            .WithObject(objectKey)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType), ct);

        return objectKey;
    }

    public async Task<string> GetPresignedUrlAsync(
        string objectKey, int expirationMinutes = 30, CancellationToken ct = default)
    {
        var url = await _minio.PresignedGetObjectAsync(new PresignedGetObjectArgs()
            .WithBucket(_bucketKyc)
            .WithObject(objectKey)
            .WithExpiry(expirationMinutes * 60));

        // Reemplaza el hostname interno de Docker por el endpoint público accesible desde el exterior
        if (_endpointInterno != _endpointPublico)
            url = url.Replace(_endpointInterno, _endpointPublico, StringComparison.OrdinalIgnoreCase);

        return url;
    }

    public async Task DeleteFileAsync(string objectKey, CancellationToken ct = default)
    {
        await _minio.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucketKyc)
            .WithObject(objectKey), ct);
    }

    // ── Bucket público (fotos de propiedades) ────────────────────────────────

    public async Task<string> UploadPublicFileAsync(
        Stream fileStream, string fileName, string contentType,
        string carpeta, CancellationToken ct = default)
    {
        await AsegurarBucketPublicoAsync(ct);

        var objectKey = $"{carpeta}/{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        await _minio.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucketFotos)
            .WithObject(objectKey)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType), ct);

        return objectKey;
    }

    public string GetPublicUrl(string objectKey) =>
        $"{_endpointPublico}/{_bucketFotos}/{objectKey}";

    public async Task DeletePublicFileAsync(string objectKey, CancellationToken ct = default)
    {
        await _minio.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_bucketFotos)
            .WithObject(objectKey), ct);
    }

    // ── Helpers privados ─────────────────────────────────────────────────────

    private async Task AsegurarBucketPrivadoAsync(CancellationToken ct)
    {
        if (await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketKyc), ct))
            return;
        await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketKyc), ct);
    }

    private async Task AsegurarBucketPublicoAsync(CancellationToken ct)
    {
        if (await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucketFotos), ct))
            return;

        await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucketFotos), ct);

        // Política de lectura pública: cualquiera puede hacer GET de los objetos
        var policy = $$"""
            {
              "Version": "2012-10-17",
              "Statement": [{
                "Effect": "Allow",
                "Principal": {"AWS": ["*"]},
                "Action": ["s3:GetObject"],
                "Resource": ["arn:aws:s3:::{{_bucketFotos}}/*"]
              }]
            }
            """;
        await _minio.SetPolicyAsync(new SetPolicyArgs()
            .WithBucket(_bucketFotos)
            .WithPolicy(policy), ct);
    }
}
