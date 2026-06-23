using Booking.Application.Common.Interfaces;
using Booking.Infrastructure.Identity;
using Booking.Infrastructure.Persistence;
using Booking.Infrastructure.Services.AI;
using Booking.Infrastructure.Services.Email;
using Booking.Infrastructure.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;

namespace Booking.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Base de datos ────────────────────────────────────────────────────
        var cadenaConexion = configuration.GetConnectionString("DefaultConnection")
            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión. " +
                "Verifica 'ConnectionStrings:DefaultConnection' en appsettings " +
                "o la variable de entorno CONNECTION_STRING en el archivo .env.");

        services.AddDbContext<ApplicationDbContext>(opciones =>
            opciones.UseNpgsql(cadenaConexion, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                // Reintentos automáticos ante fallos transitorios de red en Docker
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            }));

        // Permite que los handlers de Application resuelvan IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        // ── Cliente MinIO (singleton — thread-safe, diseñado para reutilización) ──
        services.AddSingleton<IMinioClient>(_ =>
        {
            var endpointUrl = configuration["MINIO_ENDPOINT"] ?? "http://minio:9000";
            var accessKey   = configuration["MINIO_ACCESS_KEY"] ?? "minio_admin";
            var secretKey   = configuration["MINIO_SECRET_KEY"]
                              ?? throw new InvalidOperationException("Falta la variable de entorno MINIO_SECRET_KEY.");

            // Extraer host:puerto desde la URL del .env (ej: "http://minio:9000" → "minio:9000")
            var uri      = new Uri(endpointUrl);
            var endpoint = $"{uri.Host}:{uri.Port}";
            var usarSsl  = uri.Scheme == "https";

            var builder = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey);

            if (usarSsl)
                builder = builder.WithSSL();

            return builder.Build();
        });

        // ── HttpClient para llamadas a la API de Gemini ──────────────────────
        services.AddHttpClient("GeminiClient", cliente =>
        {
            cliente.Timeout = TimeSpan.FromSeconds(45);
        });

        // ── Servicios de infraestructura (scoped — un ciclo por request HTTP) ──
        services.AddScoped<IJwtService,      JwtService>();
        services.AddScoped<IPasswordHasher,  BcryptPasswordHasher>();
        services.AddScoped<IStorageService,  StorageService>();
        services.AddScoped<IKycService,      KycService>();
        services.AddScoped<IEmailService,    EmailService>();

        return services;
    }
}
