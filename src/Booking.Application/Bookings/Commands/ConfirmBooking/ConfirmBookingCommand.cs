using Booking.Application.Common.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Bookings.Commands.ConfirmBooking;

public sealed record ConfirmBookingCommand(Guid BookingId, Guid GuestId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class ConfirmBookingCommandHandler : IRequestHandler<ConfirmBookingCommand>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IEmailService        _email;

    public ConfirmBookingCommandHandler(IApplicationDbContext ctx, IEmailService email)
    {
        _ctx   = ctx;
        _email = email;
    }

    public async Task Handle(ConfirmBookingCommand req, CancellationToken ct)
    {
        var reserva = await _ctx.Reservas
            .FirstOrDefaultAsync(r => r.Id == req.BookingId, ct)
            ?? throw new NotFoundException("Reserva", req.BookingId);

        if (reserva.GuestId != req.GuestId)
            throw new DomainException("No tienes permiso para confirmar esta reserva.");

        var huesped = await _ctx.Usuarios.FindAsync([req.GuestId], ct)
            ?? throw new NotFoundException("Usuario", req.GuestId);

        // KYC obligatorio para confirmar la primera reserva
        var tieneReservasConfirmadas = await _ctx.Reservas
            .AnyAsync(r => r.GuestId == req.GuestId && r.Status == BookingStatus.Confirmed, ct);

        if (!tieneReservasConfirmadas && !huesped.IsIdentityVerified)
            throw new DomainException(
                "Debes completar la verificación de identidad (KYC) antes de confirmar tu primera reserva. " +
                "Ve a Mi Perfil → Verificar Identidad.");

        // Vuelve a obtener reservas confirmadas para la validación anti double-booking en el dominio
        var reservasConfirmadas = await _ctx.Reservas
            .Where(r => r.PropertyId == reserva.PropertyId &&
                        r.Status     == BookingStatus.Confirmed &&
                        r.Id         != reserva.Id)
            .ToListAsync(ct);

        reserva.Confirm(reservasConfirmadas);

        var propiedad = await _ctx.Propiedades.FindAsync([reserva.PropertyId], ct);

        // Notificación in-app
        _ctx.Notificaciones.Add(Booking.Domain.Entities.Notification.Create(
            huesped.Id,
            "¡Reserva confirmada!",
            $"Tu reserva en {propiedad?.Name} está confirmada. " +
            $"Check-in: {reserva.CheckInDate:dd/MM/yyyy HH:mm} UTC",
            NotificationType.ConfirmacionReserva));

        // Notificación al propietario
        if (propiedad is not null)
        {
            _ctx.Notificaciones.Add(Booking.Domain.Entities.Notification.Create(
                propiedad.OwnerId,
                "Nueva reserva confirmada",
                $"Tienes una nueva reserva en {propiedad.Name} confirmada.",
                NotificationType.NuevaReserva));
        }

        await _ctx.SaveChangesAsync(ct);

        _ = _email.SendAlertAsync(
            huesped.Email,
            "¡Reserva confirmada! — Booking Platform",
            $"<p>¡Hola {huesped.Name}! Tu reserva fue confirmada.<br/>" +
            $"<strong>Check-in:</strong> {reserva.CheckInDate:dd/MM/yyyy} a las 14:00 UTC<br/>" +
            $"<strong>Check-out:</strong> {reserva.CheckOutDate:dd/MM/yyyy} a las 12:00 UTC</p>",
            ct).ConfigureAwait(false);
    }
}
