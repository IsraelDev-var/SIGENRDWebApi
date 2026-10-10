using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.UserId).HasMaxLength(450);
            builder.Property(a => a.Action).HasMaxLength(100).IsRequired();
            builder.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
            builder.Property(a => a.EntityId).HasMaxLength(100).IsRequired();

            builder.Property(a => a.OldValues).HasColumnType("jsonb");
            builder.Property(a => a.NewValues).HasColumnType("jsonb");

            builder.Property(a => a.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasIndex(a => new { a.EntityName, a.EntityId });
            builder.HasIndex(a => a.CreatedAt);
        }
    }
}
