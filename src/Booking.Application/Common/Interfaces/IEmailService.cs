namespace Booking.Application.Common.Interfaces;

/// <summary>
/// Contrato para el envío de notificaciones por correo electrónico.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía un correo de alerta o notificación al destinatario especificado.
    /// </summary>
    Task SendAlertAsync(string to, string subject, string body, CancellationToken ct = default);
}
