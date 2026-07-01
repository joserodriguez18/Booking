using Booking.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Booking.Infrastructure.Services.Email;

/// <summary>
/// Servicio de correo electrónico usando MailKit sobre SMTP de Gmail.
/// Configurado con clave de aplicación (App Password) para evitar 2FA.
/// </summary>
public class EmailService : IEmailService
{
    private readonly string _host;
    private readonly int    _port;
    private readonly string _usuario;
    private readonly string _clave;
    private readonly string _remitente;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _host      = config["SMTP_HOST"]     ?? "smtp.gmail.com";
        _port      = int.TryParse(config["SMTP_PORT"], out var p) ? p : 587;
        _usuario   = config["SMTP_USER"]     ?? throw new InvalidOperationException("Falta la variable de entorno SMTP_USER.");
        _clave     = config["SMTP_PASSWORD"] ?? throw new InvalidOperationException("Falta la variable de entorno SMTP_PASSWORD.");
        _remitente = config["SMTP_FROM"]     ?? _usuario;
        _logger    = logger;
    }

    public async Task SendAlertAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        try
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(MailboxAddress.Parse(_remitente));
            mensaje.To.Add(MailboxAddress.Parse(to));
            mensaje.Subject = subject;

            // Acepta HTML para notificaciones con formato
            mensaje.Body = new TextPart("html") { Text = body };

            using var cliente = new SmtpClient();

            // Puerto 587 usa STARTTLS; puerto 465 usaría SslOnConnect
            await cliente.ConnectAsync(_host, _port, SecureSocketOptions.StartTls, ct);
            await cliente.AuthenticateAsync(_usuario, _clave, ct);
            await cliente.SendAsync(mensaje, ct);
            await cliente.DisconnectAsync(quit: true, ct);
        }
        catch (Exception ex)
        {
            // No crítico (fire-and-forget desde los handlers), pero se registra para poder
            // diagnosticar fallas de SMTP que de otro modo quedarían completamente silenciosas.
            _logger.LogWarning(ex, "No se pudo enviar el correo a {Destinatario} con asunto '{Asunto}'.", to, subject);
        }
    }
}
