using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("lista_deseos");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).HasColumnName("id");
        builder.Property(w => w.UserId).HasColumnName("usuario_id").IsRequired();
        builder.Property(w => w.PropertyId).HasColumnName("propiedad_id").IsRequired();
        builder.Property(w => w.CreatedAt).HasColumnName("creado_en").HasColumnType("timestamp with time zone");
        builder.Property(w => w.UpdatedAt).HasColumnName("actualizado_en").HasColumnType("timestamp with time zone");

        // Un usuario no puede agregar la misma propiedad dos veces
        builder.HasIndex(w => new { w.UserId, w.PropertyId })
            .IsUnique()
            .HasDatabaseName("ix_lista_deseos_usuario_propiedad");

        builder.HasOne<User>().WithMany().HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Property>().WithMany().HasForeignKey(w => w.PropertyId).OnDelete(DeleteBehavior.Cascade);
    }
}
