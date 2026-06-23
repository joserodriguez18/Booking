using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("propiedades");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Name)
            .HasColumnName("nombre")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("descripcion")
            .HasMaxLength(2000);

        builder.Property(p => p.Location)
            .HasColumnName("ubicacion")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.OwnerId)
            .HasColumnName("propietario_id")
            .IsRequired();

        builder.Property(p => p.IsActive)
            .HasColumnName("activo")
            .HasDefaultValue(true);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("creado_en")
            .HasColumnType("timestamp with time zone");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("actualizado_en")
            .HasColumnType("timestamp with time zone");

        // Value Object Money mapeado como entidad propia (columnas incrustadas)
        builder.OwnsOne(p => p.PricePerNight, dinero =>
        {
            dinero.Property(m => m.Amount)
                .HasColumnName("precio_por_noche")
                .HasColumnType("numeric(10,2)")
                .IsRequired();

            dinero.Property(m => m.Currency)
                .HasColumnName("moneda")
                .HasMaxLength(3)
                .HasDefaultValue("USD");
        });

        // Un propietario (User de tipo Owner) tiene muchas propiedades
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_propiedades_propietario");
    }
}
