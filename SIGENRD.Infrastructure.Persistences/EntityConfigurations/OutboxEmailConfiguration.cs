using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class OutboxEmailConfiguration : IEntityTypeConfiguration<OutboxEmail>
    {
        public void Configure(EntityTypeBuilder<OutboxEmail> builder)
        {
            builder.ToTable("OutboxEmails");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.To).IsRequired().HasMaxLength(256);
            builder.Property(e => e.Subject).IsRequired().HasMaxLength(300);
            builder.Property(e => e.From).HasMaxLength(256);
            builder.Property(e => e.LastError).HasMaxLength(1000);

            // El despachador solo busca los pendientes por fecha de próximo intento.
            builder.HasIndex(e => e.NextAttemptAt)
                .HasDatabaseName("IX_OutboxEmails_Pending")
                .HasFilter("\"SentAt\" IS NULL AND \"FailedAt\" IS NULL");

            // Limpieza de los ya enviados.
            builder.HasIndex(e => e.SentAt).HasFilter("\"SentAt\" IS NOT NULL");
        }
    }
}
