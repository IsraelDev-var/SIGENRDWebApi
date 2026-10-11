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
            builder.Property(s => s.Name).HasMaxLength(100);

            builder.Property(s => s.MonthlyConsumptionKwh).HasColumnType("numeric(12,2)");
            builder.Property(s => s.Location).HasColumnType("geography (point, 4326)");
            builder.Property(s => s.InstalledCapacityKw).HasColumnType("numeric(10,2)");
            builder.Property(s => s.FlatPricePerKwh).HasColumnType("numeric(10,4)");

            builder.Property(s => s.AnnualProductionKwh).HasColumnType("numeric(14,2)");
            builder.Property(s => s.CoveragePct).HasColumnType("numeric(10,2)");
            builder.Property(s => s.AnnualBillBefore).HasColumnType("numeric(14,2)");
            builder.Property(s => s.AnnualBillAfter).HasColumnType("numeric(14,2)");
            builder.Property(s => s.AnnualExportCreditKwh).HasColumnType("numeric(14,2)");
            builder.Property(s => s.SurplusPayment).HasColumnType("numeric(14,2)");
            builder.Property(s => s.AnnualSavings).HasColumnType("numeric(14,2)");
            builder.Property(s => s.InvestmentCost).HasColumnType("numeric(14,2)");
            builder.Property(s => s.PaybackYears).HasColumnType("numeric(8,2)");
            builder.Property(s => s.Roi).HasColumnType("numeric(10,2)");

            builder.Property(s => s.ViabilityStatus).HasConversion<string>().HasMaxLength(50);

            builder.Property(s => s.Recommendations).HasColumnType("text[]");
            builder.Property(s => s.FormulaVersion).HasMaxLength(20).IsRequired();
            builder.Property(s => s.Assumptions).HasColumnType("jsonb").IsRequired();

            builder.HasOne(s => s.Distributor)
                   .WithMany()
                   .HasForeignKey(s => s.DistributorId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(s => s.Transformer)
                   .WithMany()
                   .HasForeignKey(s => s.TransformerId)
                   .OnDelete(DeleteBehavior.SetNull);

            // Historial del usuario, más recientes primero (RF-26).
            builder.HasIndex(s => new { s.UserId, s.CreatedAt });
        }
    }
}
