using Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

public class IdentityDocumentConfiguration : IEntityTypeConfiguration<IdentityDocument>
{
    public void Configure(EntityTypeBuilder<IdentityDocument> builder)
    {
        builder.ToTable("documentos_identidad");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");

        builder.Property(d => d.UserId)
            .HasColumnName("usuario_id")
            .IsRequired();

        builder.Property(d => d.DocumentNumber)
            .HasColumnName("numero_documento")
            .HasMaxLength(50);

        builder.Property(d => d.DocumentType)
            .HasColumnName("tipo_documento")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.ExtractedNames)
            .HasColumnName("nombres_extraidos")
            .HasMaxLength(400);

        builder.Property(d => d.ExtractedBirthDate)
            .HasColumnName("fecha_nacimiento_extraida")
            .HasColumnType("date");

        // URL del objeto en MinIO — nulo después de eliminación segura post-KYC
        builder.Property(d => d.DocumentUrl)
            .HasColumnName("url_documento")
            .HasMaxLength(1000);

        builder.Property(d => d.IsDocumentDeleted)
            .HasColumnName("documento_eliminado")
            .HasDefaultValue(false);

        builder.Property(d => d.UploadedAt)
            .HasColumnName("subido_en")
            .HasColumnType("timestamp with time zone");

        builder.Property(d => d.CreatedAt)
            .HasColumnName("creado_en")
            .HasColumnType("timestamp with time zone");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("actualizado_en")
            .HasColumnType("timestamp with time zone");

        // Un usuario tiene muchos documentos de identidad (historial de intentos KYC)
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_documentos_usuario");
    }
}
