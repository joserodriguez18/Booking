using Booking.Application.Common.Interfaces;
using Booking.Domain.Enums;
using Booking.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ReservationBooking = Booking.Domain.Entities.Booking;

namespace Booking.Application.Bookings.Commands.CreateBooking;

public sealed record CreateBookingCommand(
    Guid     PropertyId,
    Guid     GuestId,
    DateOnly CheckIn,
    DateOnly CheckOut
) : IRequest<Guid>;

// ── Validador ────────────────────────────────────────────────────────────────

public sealed class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty().WithMessage("La propiedad es obligatoria.");
        RuleFor(x => x.GuestId).NotEmpty().WithMessage("El huésped es obligatorio.");
        RuleFor(x => x.CheckOut).GreaterThan(x => x.CheckIn)
            .WithMessage("La fecha de salida debe ser posterior a la fecha de entrada.");
        RuleFor(x => x.CheckIn).GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha de entrada no puede ser en el pasado.");
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Guid>
{
    private readonly IApplicationDbContext _ctx;
    private readonly IEmailService        _email;

    public CreateBookingCommandHandler(IApplicationDbContext ctx, IEmailService email)
    {
        _ctx   = ctx;
        _email = email;
    }

    public async Task<Guid> Handle(CreateBookingCommand req, CancellationToken ct)
    {
        var propiedad = await _ctx.Propiedades
            .FirstOrDefaultAsync(p => p.Id == req.PropertyId && p.IsActive, ct)
            ?? throw new NotFoundException("Propiedad", req.PropertyId);

        var huesped = await _ctx.Usuarios.FindAsync([req.GuestId], ct)
            ?? throw new NotFoundException("Usuario", req.GuestId);

        // Obtiene reservas confirmadas existentes para la propiedad (anti double-booking)
        var reservasExistentes = await _ctx.Reservas
            .Where(r => r.PropertyId == req.PropertyId && r.Status == BookingStatus.Confirmed)
            .ToListAsync(ct);

        // El dominio valida solapamiento de fechas y aplica las horas fijas 14:00/12:00
        var reserva = ReservationBooking.Create(
            req.PropertyId, req.GuestId,
            req.CheckIn, req.CheckOut,
            propiedad.PricePerNight,
            reservasExistentes);

        _ctx.Reservas.Add(reserva);

        // Notificación in-app al huésped
        _ctx.Notificaciones.Add(Booking.Domain.Entities.Notification.Create(
            huesped.Id,
            "Reserva creada",
            $"Tu reserva en {propiedad.Name} fue creada. Pendiente de confirmación.",
            Booking.Domain.Enums.NotificationType.ConfirmacionReserva));

        await _ctx.SaveChangesAsync(ct);

        // Notificación por email (no crítica)
        _ = _email.SendAlertAsync(
            huesped.Email,
            "Reserva recibida — Booking Platform",
            $"<p>Hola {huesped.Name}, tu reserva en <strong>{propiedad.Name}</strong> " +
            $"del {req.CheckIn:dd/MM/yyyy} al {req.CheckOut:dd/MM/yyyy} fue recibida.</p>",
            ct).ConfigureAwait(false);

        return reserva.Id;
    }
}
