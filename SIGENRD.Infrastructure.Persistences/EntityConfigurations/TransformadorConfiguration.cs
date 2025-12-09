
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;

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
                   .HasDefaultValue(TransformerStatus.Available);

            builder.Property(t => t.TotalCapacityKva)
                   .HasColumnType("numeric(10,2)");

            builder.Property(t => t.AvailableCapacityKva)
                   .HasColumnType("numeric(10,2)");

            // 🔹 Convierte el ValueObject (GeoCoordinate) a Point de NTS
            builder.Property(t => t.Location)
                 .HasColumnType("geography (Point, 4326)");
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
