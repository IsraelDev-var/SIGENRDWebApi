using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class GeneratorConfiguration : IEntityTypeConfiguration<Generator>
    {
        public void Configure(EntityTypeBuilder<Generator> builder)
        {
            builder.ToTable("Generators");
            builder.HasKey(g => g.Id);

            // 🔹 Campos básicos
            builder.Property(g => g.GenerationSystemType)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(g => g.InverterModel)
                   .HasMaxLength(150);

            builder.Property(g => g.Manufacturer)
                   .HasMaxLength(150);

            builder.Property(g => g.RatedPowerKva)
                   .HasColumnType("numeric(10,2)");

            builder.Property(g => g.ConnectionVoltage)
                   .HasColumnType("numeric(10,2)");

            builder.Property(g => g.NominalCurrent)
                   .HasColumnType("numeric(10,2)");

            builder.Property(g => g.PowerFactorMin)
                   .HasColumnType("numeric(5,2)");

            builder.Property(g => g.PowerFactorMax)
                   .HasColumnType("numeric(5,2)");

            builder.Property(g => g.SwitchingType)
                   .HasMaxLength(100);

            builder.Property(g => g.HarmonicDistortionPercent)
                   .HasColumnType("numeric(5,2)");

            // 🔹 Relación con NetMeteringRequest
            builder.HasOne(g => g.NetMeteringRequest)
                   .WithMany(r => r.Generators)
                   .HasForeignKey(g => g.NetMeteringRequestId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
