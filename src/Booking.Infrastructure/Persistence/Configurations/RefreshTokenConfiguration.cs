using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.UserId).HasColumnName("usuario_id").IsRequired();
        builder.Property(r => r.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.Property(r => r.ExpiresAt).HasColumnName("vence_en").HasColumnType("timestamp with time zone");
        builder.Property(r => r.IsRevoked).HasColumnName("revocado").HasDefaultValue(false);
        builder.Property(r => r.CreatedAt).HasColumnName("creado_en").HasColumnType("timestamp with time zone");
        builder.Property(r => r.UpdatedAt).HasColumnName("actualizado_en").HasColumnType("timestamp with time zone");

        builder.Ignore(r => r.IsValid); // Propiedad calculada

        builder.HasIndex(r => r.TokenHash).IsUnique().HasDatabaseName("ix_refresh_tokens_hash");
        builder.HasIndex(r => r.UserId).HasDatabaseName("ix_refresh_tokens_usuario");

        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
