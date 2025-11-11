using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;


namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class ServiceZoneConfiguration : IEntityTypeConfiguration<ServiceZone>
    {
        public void Configure(EntityTypeBuilder<ServiceZone> builder)
        {
            builder.ToTable("ServiceZones");
            builder.HasKey(z => z.Id);

            builder.Property(z => z.ZoneName)
                   .HasMaxLength(150)
                   .IsRequired();

            builder.Property(z => z.Municipality)
                   .HasMaxLength(100);

            builder.Property(z => z.Province)
                   .HasMaxLength(100);
        }
    }
}
