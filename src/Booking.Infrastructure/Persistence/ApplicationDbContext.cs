using Booking.Application.Common.Interfaces;
using Booking.Domain.Entities;
using Booking.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

// Alias para evitar colisión entre la clase Booking y el namespace raíz Booking.*
using ReservationBooking = Booking.Domain.Entities.Booking;

namespace Booking.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User>               Usuarios            => Set<User>();
    public DbSet<Property>           Propiedades         => Set<Property>();
    public DbSet<ReservationBooking> Reservas            => Set<ReservationBooking>();
    public DbSet<IdentityDocument>   DocumentosIdentidad => Set<IdentityDocument>();
    public DbSet<WishlistItem>       ListaDeseos         => Set<WishlistItem>();
    public DbSet<RefreshToken>       RefreshTokens       => Set<RefreshToken>();
    public DbSet<Notification>       Notificaciones      => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas las configuraciones IEntityTypeConfiguration<T> del ensamblado
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
