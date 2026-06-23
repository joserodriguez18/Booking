using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ReservationBooking = Booking.Domain.Entities.Booking;

namespace Booking.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User>               Usuarios            { get; }
    DbSet<Property>           Propiedades         { get; }
    DbSet<ReservationBooking> Reservas            { get; }
    DbSet<IdentityDocument>   DocumentosIdentidad { get; }
    DbSet<WishlistItem>       ListaDeseos         { get; }
    DbSet<RefreshToken>       RefreshTokens       { get; }
    DbSet<Notification>       Notificaciones      { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
