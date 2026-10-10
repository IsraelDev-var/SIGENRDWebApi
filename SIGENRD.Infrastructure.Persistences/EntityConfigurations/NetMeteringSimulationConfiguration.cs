using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class NetMeteringSimulationConfiguration : IEntityTypeConfiguration<NetMeteringSimulation>
    {
        public void Configure(EntityTypeBuilder<NetMeteringSimulation> builder)
        {
            builder.ToTable("NetMeteringSimulations");
            builder.HasKey(s => s.Id);

            builder.Property(s => s.UserId).HasMaxLength(450).IsRequired();

            builder.Property(s => s.InstalledCapacityKw).HasColumnType("numeric(10,2)");
            builder.Property(s => s.AnnualProductionKwh).HasColumnType("numeric(14,2)");
            builder.Property(s => s.AnnualSavings).HasColumnType("numeric(14,2)");
            builder.Property(s => s.InvestmentCost).HasColumnType("numeric(14,2)");
            builder.Property(s => s.PaybackYears).HasColumnType("numeric(8,2)");
            builder.Property(s => s.Roi).HasColumnType("numeric(8,2)");

            builder.Property(s => s.ViabilityStatus).HasConversion<string>().HasMaxLength(50);

            builder.HasOne(s => s.Distributor)
                   .WithMany()
                   .HasForeignKey(s => s.DistributorId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(s => s.Transformer)
                   .WithMany()
                   .HasForeignKey(s => s.TransformerId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(s => s.UserId);
        }
    }
}
