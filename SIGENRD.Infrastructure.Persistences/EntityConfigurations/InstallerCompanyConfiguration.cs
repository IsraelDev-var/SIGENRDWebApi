

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class InstallerCompanyConfiguration : IEntityTypeConfiguration<InstallerCompany>
    {
        public void Configure(EntityTypeBuilder<InstallerCompany> builder)
        {
            builder.ToTable("InstallerCompanies");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.TradeName)
                   .HasMaxLength(150)
                   .IsRequired();

            builder.Property(c => c.Rnc)
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(c => c.ContactEmail)
                   .HasMaxLength(150);

            builder.Property(c => c.ContactPhone)
                   .HasMaxLength(20);

            builder.Property(c => c.CreatedAt)
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasMany(c => c.Engineers)
                   .WithOne(e => e.InstallerCompany)
                   .HasForeignKey(e => e.InstallerCompanyId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
