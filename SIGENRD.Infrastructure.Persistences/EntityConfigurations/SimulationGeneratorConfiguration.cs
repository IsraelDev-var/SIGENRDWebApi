using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class SimulationGeneratorConfiguration : IEntityTypeConfiguration<SimulationGenerator>
    {
        public void Configure(EntityTypeBuilder<SimulationGenerator> builder)
        {
            builder.ToTable("SimulationGenerators");
            builder.HasKey(g => g.Id);

            builder.Property(g => g.GenerationSystemType).HasConversion<string>().HasMaxLength(50);
            builder.Property(g => g.PanelModel).HasMaxLength(150);
            builder.Property(g => g.InverterModel).HasMaxLength(150);
            builder.Property(g => g.RatedPowerKw).HasColumnType("numeric(10,2)");

            builder.HasOne(g => g.NetMeteringSimulation)
                   .WithMany(s => s.Generators)
                   .HasForeignKey(g => g.NetMeteringSimulationId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
