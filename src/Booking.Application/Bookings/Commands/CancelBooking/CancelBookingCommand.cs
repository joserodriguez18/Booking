using Booking.Application.Common.Interfaces;
using Booking.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Booking.Application.Bookings.Commands.CancelBooking;

public sealed record CancelBookingCommand(Guid BookingId, Guid RequesterId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IEmailService        _email;

    public CancelBookingCommandHandler(IApplicationDbContext ctx, IEmailService email)
    {
        _ctx   = ctx;
        _email = email;
    }

    public async Task Handle(CancelBookingCommand req, CancellationToken ct)
    {
        var reserva = await _ctx.Reservas
            .FirstOrDefaultAsync(r => r.Id == req.BookingId, ct)
            ?? throw new NotFoundException("Reserva", req.BookingId);

        // Solo el huésped o el propietario pueden cancelar
        var propiedad = await _ctx.Propiedades.FindAsync([reserva.PropertyId], ct);
        var esPropietario = propiedad?.OwnerId == req.RequesterId;
        var esHuesped     = reserva.GuestId    == req.RequesterId;

        if (!esHuesped && !esPropietario)
            throw new DomainException("No tienes permiso para cancelar esta reserva.");

        reserva.Cancel(); // El dominio valida que no se haya pasado el check-in

        var huesped = await _ctx.Usuarios.FindAsync([reserva.GuestId], ct);

        _ctx.Notificaciones.Add(Booking.Domain.Entities.Notification.Create(
            reserva.GuestId,
            "Reserva cancelada",
            $"Tu reserva en {propiedad?.Name} fue cancelada.",
            Booking.Domain.Enums.NotificationType.CancelacionReserva));

        await _ctx.SaveChangesAsync(ct);

        if (huesped is not null)
            _ = _email.SendAlertAsync(
                huesped.Email,
                "Reserva cancelada — Booking Platform",
                $"<p>Tu reserva en <strong>{propiedad?.Name}</strong> ha sido cancelada.</p>",
                ct).ConfigureAwait(false);
    }
}
