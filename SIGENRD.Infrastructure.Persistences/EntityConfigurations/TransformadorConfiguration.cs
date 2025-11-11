
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class TransformadorConfiguration : IEntityTypeConfiguration<Transformer>
    {
        public void Configure(EntityTypeBuilder<Transformer> builder)
        {
            #region basic configurations
            builder.ToTable("Transformers");
            builder.HasKey(t => t.Id);
            #endregion

            #region property configurations
            builder.Property(t => t.Code)
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(t => t.Status)
                   .HasMaxLength(50)
                   .HasDefaultValue("Available");

            builder.Property(t => t.TotalCapacityKva)
                   .HasColumnType("numeric(10,2)");

            builder.Property(t => t.AvailableCapacityKva)
                   .HasColumnType("numeric(10,2)");

            // 🔹 Convierte el ValueObject (GeoCoordinate) a Point de NTS
            builder.OwnsOne(t => t.Location, loc =>
            {
                loc.Property(l => l.Latitude).HasColumnName("Latitude").IsRequired();
                loc.Property(l => l.Longitude).HasColumnName("Longitude").IsRequired();
            });
            #endregion


            #region relationship configurations
            builder.HasOne(t => t.Distributor)
                  .WithMany(d => d.Transformers)
                  .HasForeignKey(t => t.DistributorId)
                  .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(t => t.ServiceZone)
                   .WithMany(z => z.Transformers)
                   .HasForeignKey(t => t.ServiceZoneId)
                   .OnDelete(DeleteBehavior.Cascade);

            #endregion

        }
    }
}
