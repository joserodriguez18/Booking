namespace Booking.Domain.Enums;

public enum NotificationType
{
    ConfirmacionReserva   = 0,
    CancelacionReserva    = 1,
    KycAprobado           = 2,
    KycRechazado          = 3,
    RecordatorioCheckIn   = 4,
    RecordatorioCheckOut  = 5,
    NuevaReserva          = 6   // Notificación al propietario
}
