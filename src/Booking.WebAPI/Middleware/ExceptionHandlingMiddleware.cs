using Booking.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace Booking.WebAPI.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (Exception ex)
        {
            // Deja que Swashbuckle maneje sus propios errores internamente
            if (ctx.Request.Path.StartsWithSegments("/swagger"))
                throw;

            await ManejarExcepcionAsync(ctx, ex);
        }
    }

    private async Task ManejarExcepcionAsync(HttpContext ctx, Exception ex)
    {
        int statusCode;
        string mensaje;

        object cuerpo;

        switch (ex)
        {
            case ValidationException validacion:
                statusCode = (int)HttpStatusCode.BadRequest;
                var errores = validacion.Errors.Select(e => e.ErrorMessage).ToList();
                mensaje = string.Join(" ", errores);
                cuerpo  = new { error = mensaje, errores, codigo = statusCode, ruta = ctx.Request.Path.Value };
                break;

            case BookingConflictException conflicto:
                statusCode = (int)HttpStatusCode.Conflict;
                mensaje    = conflicto.Message;
                cuerpo     = new { error = mensaje, codigo = statusCode, ruta = ctx.Request.Path.Value };
                break;

            case NotFoundException noEncontrado:
                statusCode = (int)HttpStatusCode.NotFound;
                mensaje    = noEncontrado.Message;
                cuerpo     = new { error = mensaje, codigo = statusCode, ruta = ctx.Request.Path.Value };
                break;

            case DomainException dominio:
                statusCode = (int)HttpStatusCode.BadRequest;
                mensaje    = dominio.Message;
                cuerpo     = new { error = mensaje, codigo = statusCode, ruta = ctx.Request.Path.Value };
                break;

            case UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                mensaje    = "No está autorizado para realizar esta acción.";
                cuerpo     = new { error = mensaje, codigo = statusCode, ruta = ctx.Request.Path.Value };
                break;

            default:
                _logger.LogError(ex, "Error no controlado al procesar {Method} {Path}",
                    ctx.Request.Method, ctx.Request.Path);
                statusCode = (int)HttpStatusCode.InternalServerError;
                mensaje    = "Ocurrió un error interno. Por favor intente de nuevo más tarde.";
                cuerpo     = new { error = mensaje, codigo = statusCode, ruta = ctx.Request.Path.Value };
                break;
        }

        ctx.Response.StatusCode  = statusCode;
        ctx.Response.ContentType = "application/json";

        await ctx.Response.WriteAsync(
            JsonSerializer.Serialize(cuerpo,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
