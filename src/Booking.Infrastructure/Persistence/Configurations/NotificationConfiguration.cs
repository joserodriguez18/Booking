using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notificaciones");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");
        builder.Property(n => n.UserId).HasColumnName("usuario_id").IsRequired();
        builder.Property(n => n.Title).HasColumnName("titulo").HasMaxLength(300).IsRequired();
        builder.Property(n => n.Body).HasColumnName("cuerpo").HasMaxLength(2000);
        builder.Property(n => n.Type).HasColumnName("tipo").HasConversion<string>().HasMaxLength(50);
        builder.Property(n => n.IsRead).HasColumnName("leida").HasDefaultValue(false);
        builder.Property(n => n.CreatedAt).HasColumnName("creado_en").HasColumnType("timestamp with time zone");
        builder.Property(n => n.UpdatedAt).HasColumnName("actualizado_en").HasColumnType("timestamp with time zone");

        builder.HasIndex(n => new { n.UserId, n.IsRead }).HasDatabaseName("ix_notificaciones_usuario_leida");
        builder.HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
