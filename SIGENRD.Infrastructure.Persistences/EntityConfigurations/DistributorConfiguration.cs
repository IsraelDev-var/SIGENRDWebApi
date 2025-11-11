

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class DistributorConfiguration : IEntityTypeConfiguration<Distributor>
    {
        public void Configure(EntityTypeBuilder<Distributor> builder)
        {
            builder.ToTable("Distributors");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.Name)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(d => d.Type)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(d => d.ContactEmail)
                   .HasMaxLength(150);

            builder.Property(d => d.ContactPhone)
                   .HasMaxLength(20);
        }
    }
}
