using Booking.Domain.Entities;
using Booking.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

// Alias para evitar ambigüedad entre la clase Booking y el namespace raíz Booking.*
using ReservationBooking = Booking.Domain.Entities.Booking;

namespace Booking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<ReservationBooking>
{
    public void Configure(EntityTypeBuilder<ReservationBooking> builder)
    {
        builder.ToTable("reservas");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.PropertyId)
            .HasColumnName("propiedad_id")
            .IsRequired();

        builder.Property(r => r.GuestId)
            .HasColumnName("huesped_id")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("estado")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("creado_en")
            .HasColumnType("timestamp with time zone");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("actualizado_en")
            .HasColumnType("timestamp with time zone");

        // Value Object BookingDateRange — columnas incrustadas con horas fijas 14:00 / 12:00 UTC
        builder.OwnsOne(r => r.DateRange, rango =>
        {
            rango.Property(d => d.CheckIn)
                .HasColumnName("check_in")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            rango.Property(d => d.CheckOut)
                .HasColumnName("check_out")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            // Propiedad calculada — no se persiste en BD
            rango.Ignore(d => d.Nights);
        });

        // Value Object Money para precio total
        builder.OwnsOne(r => r.TotalPrice, precio =>
        {
            precio.Property(m => m.Amount)
                .HasColumnName("precio_total")
                .HasColumnType("numeric(10,2)")
                .IsRequired();

            precio.Property(m => m.Currency)
                .HasColumnName("moneda")
                .HasMaxLength(3)
                .HasDefaultValue("USD");
        });

        // Propiedades de conveniencia calculadas desde DateRange — no se persisten
        builder.Ignore(r => r.CheckInDate);
        builder.Ignore(r => r.CheckOutDate);

        // Índice único para prevenir double-booking a nivel de base de datos (segunda línea de defensa)
        builder.HasIndex(r => new { r.PropertyId, r.Status })
            .HasDatabaseName("ix_reservas_propiedad_estado");

        // Una propiedad tiene muchas reservas
        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_reservas_propiedad");

        // Un huésped (User) tiene muchas reservas
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(r => r.GuestId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_reservas_huesped");
    }
}
