using Booking.Application;
using Booking.Infrastructure;
using Booking.WebAPI.Middleware;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Carga .env en desarrollo local (en Docker las vars vienen del env_file de compose)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
if (File.Exists(envPath))
    Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

// Mapea las variables de entorno cargadas desde .env a la configuración de ASP.NET Core
builder.Configuration.AddEnvironmentVariables();

// Sobrescribe la cadena de conexión con la variable de entorno si está disponible
var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (!string.IsNullOrWhiteSpace(connectionString))
    builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;

// ── Registro de servicios ────────────────────────────────────────────────────

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Serializa enums como strings (ej: "Confirmed" en vez de 1)
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title       = "Booking Platform API",
        Version     = "v1",
        Description = "API REST para plataforma de rentas cortas. " +
                      "Para endpoints protegidos, agrega el header: Authorization: Bearer {tu_token_jwt}"
    });
});

// Casos de uso CQRS con MediatR + FluentValidation
builder.Services.AddApplication();

// Capa de infraestructura: DbContext, JWT, MinIO, Gemini, Email
builder.Services.AddInfrastructure(builder.Configuration);

// ── Autenticación JWT ────────────────────────────────────────────────────────
var jwtSecret   = Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? builder.Configuration["JWT_SECRET"]
    ?? throw new InvalidOperationException("Falta la variable JWT_SECRET en el entorno.");
var jwtIssuer   = Environment.GetEnvironmentVariable("JWT_ISSUER")   ?? "BookingPlatform";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "BookingPlatformUsers";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew                = TimeSpan.Zero
        };

        opciones.Events = new JwtBearerEvents
        {
            OnChallenge = contexto =>
            {
                contexto.HandleResponse();
                contexto.Response.StatusCode  = StatusCodes.Status401Unauthorized;
                contexto.Response.ContentType = "application/json";
                return contexto.Response.WriteAsync(
                    """{"error":"Token de autenticación requerido o inválido.","codigo":401}""");
            }
        };
    });

builder.Services.AddAuthorization();

// ── Construcción y pipeline HTTP ──────────────────────────────────────────────
var app = builder.Build();

// Middleware global de manejo de excepciones (debe ir primero)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger UI disponible en /swagger
app.UseSwagger();
app.UseSwaggerUI(opciones =>
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking Platform API v1"));

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Aplica las migraciones pendientes al iniciar (útil en Docker Compose)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<Booking.Infrastructure.Persistence.ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();
