using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Booking.Application.Common.DTOs;
using Booking.Application.Common.Interfaces;
using Booking.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;

namespace Booking.Infrastructure.Services.AI;

/// <summary>
/// Servicio KYC que extrae datos de documentos de identidad mediante IA.
/// - Si GEMINI_API_KEY está configurada con una clave real → llama a Gemini Vision API.
/// - Si la clave es el placeholder → devuelve un mock coherente para desarrollo.
/// </summary>
public class KycService : IKycService
{
    private readonly string      _apiKey;
    private readonly string      _modelo;
    private readonly IMinioClient _minio;
    private readonly string      _bucketKyc;
    private readonly HttpClient  _httpClient;

    private const string GeminiEndpointBase =
        "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent?key={1}";

    public KycService(IMinioClient minio, IHttpClientFactory httpFactory, IConfiguration config)
    {
        _minio      = minio;
        _bucketKyc  = config["MINIO_BUCKET_KYC"] ?? "kyc-documents";
        _apiKey     = config["GEMINI_API_KEY"]    ?? string.Empty;
        _modelo     = config["GEMINI_MODEL"]      ?? "gemini-2.0-flash";
        _httpClient = httpFactory.CreateClient("GeminiClient");
    }

    public async Task<KycExtractionResult> ProcessIdentityDocumentAsync(
        string objectKey,
        CancellationToken ct = default)
    {
        // Modo desarrollo: clave no configurada o es el placeholder de .env.example
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey.Contains("YOUR_"))
            return GenerarMockCoherente(objectKey);

        try
        {
            return await LlamarGeminiAsync(objectKey, ct);
        }
        catch (Exception ex)
        {
            // Ante cualquier fallo de la API externa, no bloqueamos el flujo
            return new KycExtractionResult(
                Success: false,
                DocumentNumber: null,
                ExtractedNames: null,
                BirthDate: null,
                DocumentType: DocumentType.NationalId,
                ErrorMessage: $"Error al procesar el documento con IA: {ex.Message}"
            );
        }
    }

    // ── Llamada real a Gemini Vision ─────────────────────────────────────────

    private async Task<KycExtractionResult> LlamarGeminiAsync(string objectKey, CancellationToken ct)
    {
        // 1. Descargar la imagen desde MinIO para enviarla como inline_data
        byte[] imagenBytes;
        using (var ms = new MemoryStream())
        {
            var getArgs = new GetObjectArgs()
                .WithBucket(_bucketKyc)
                .WithObject(objectKey)
                .WithCallbackStream(stream => stream.CopyTo(ms));

            await _minio.GetObjectAsync(getArgs, ct);
            imagenBytes = ms.ToArray();
        }

        var imagenBase64 = Convert.ToBase64String(imagenBytes);

        // 2. Construir el cuerpo de la solicitud para Gemini Vision
        var prompt = """
            Analiza esta imagen de un documento de identidad y extrae la siguiente información.
            Devuelve ÚNICAMENTE un objeto JSON válido con exactamente estos campos:
            {
              "document_number": "número del documento",
              "full_names": "nombres y apellidos completos",
              "birth_date": "YYYY-MM-DD",
              "document_type": "NationalId o Passport"
            }
            No incluyas explicaciones, solo el JSON.
            """;

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = "image/jpeg",
                                data      = imagenBase64
                            }
                        }
                    }
                }
            }
        };

        var endpoint = string.Format(GeminiEndpointBase, _modelo, _apiKey);
        var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody, ct);
        response.EnsureSuccessStatusCode();

        var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: ct);
        var jsonTexto      = geminiResponse?.Candidates?.FirstOrDefault()
                                            ?.Content?.Parts?.FirstOrDefault()
                                            ?.Text ?? "{}";

        // Limpiar posibles bloques de código markdown que Gemini puede añadir
        jsonTexto = jsonTexto
            .Replace("```json", "")
            .Replace("```", "")
            .Trim();

        // 3. Parsear la respuesta de Gemini
        using var doc = JsonDocument.Parse(jsonTexto);
        var root      = doc.RootElement;

        var tipoStr  = root.TryGetProperty("document_type", out var tipoEl) ? tipoEl.GetString() : "NationalId";
        var tipo     = tipoStr == "Passport" ? DocumentType.Passport : DocumentType.NationalId;

        DateOnly? fechaNac = null;
        if (root.TryGetProperty("birth_date", out var fechaEl) &&
            DateOnly.TryParse(fechaEl.GetString(), out var f))
            fechaNac = f;

        return new KycExtractionResult(
            Success:        true,
            DocumentNumber: root.TryGetProperty("document_number", out var num)   ? num.GetString()  : null,
            ExtractedNames: root.TryGetProperty("full_names",       out var names) ? names.GetString() : null,
            BirthDate:      fechaNac,
            DocumentType:   tipo,
            ErrorMessage:   null
        );
    }

    // ── Mock inteligente para entorno de desarrollo ──────────────────────────

    private static KycExtractionResult GenerarMockCoherente(string objectKey)
    {
        // Usa el hash del objectKey para generar datos ficticios pero deterministas por documento
        var seed  = Math.Abs(objectKey.GetHashCode());
        var rng   = new Random(seed);

        var nombres = new[]
        {
            "María José García López",
            "Carlos Andrés Martínez Torres",
            "Ana Lucía Hernández Ruiz",
            "Luis Fernando Rodríguez Mora",
            "Valentina Gómez Castillo"
        };

        var numeroDoc    = rng.Next(10_000_000, 99_999_999).ToString();
        var añoNac       = rng.Next(1975, 2000);
        var mesNac       = rng.Next(1, 13);
        var diaNac       = rng.Next(1, 29);
        var nombreElegido = nombres[rng.Next(nombres.Length)];

        return new KycExtractionResult(
            Success:        true,
            DocumentNumber: numeroDoc,
            ExtractedNames: nombreElegido,
            BirthDate:      new DateOnly(añoNac, mesNac, diaNac),
            DocumentType:   DocumentType.NationalId,
            ErrorMessage:   null
        );
    }

    // ── Modelos internos para deserializar la respuesta de Gemini ────────────

    private sealed class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }
    }

    private sealed class GeminiContent
    {
        [JsonPropertyName("parts")]
        public List<GeminiPart>? Parts { get; set; }
    }

    private sealed class GeminiPart
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
