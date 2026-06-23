using Booking.Domain.Exceptions;
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

        switch (ex)
        {
            case BookingConflictException conflicto:
                statusCode = (int)HttpStatusCode.Conflict;
                mensaje    = conflicto.Message;
                break;

            case NotFoundException noEncontrado:
                statusCode = (int)HttpStatusCode.NotFound;
                mensaje    = noEncontrado.Message;
                break;

            case DomainException dominio:
                statusCode = (int)HttpStatusCode.BadRequest;
                mensaje    = dominio.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = (int)HttpStatusCode.Unauthorized;
                mensaje    = "No está autorizado para realizar esta acción.";
                break;

            default:
                _logger.LogError(ex, "Error no controlado al procesar {Method} {Path}",
                    ctx.Request.Method, ctx.Request.Path);
                statusCode = (int)HttpStatusCode.InternalServerError;
                mensaje    = "Ocurrió un error interno. Por favor intente de nuevo más tarde.";
                break;
        }

        ctx.Response.StatusCode  = statusCode;
        ctx.Response.ContentType = "application/json";

        var respuesta = JsonSerializer.Serialize(new
        {
            error   = mensaje,
            codigo  = statusCode,
            ruta    = ctx.Request.Path.Value
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await ctx.Response.WriteAsync(respuesta);
    }
}
