using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;


namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class StateHistoryConfiguration : IEntityTypeConfiguration<StateHistory>
    {
        public void Configure(EntityTypeBuilder<StateHistory> builder)
        {
            builder.ToTable("StateHistories");
            builder.HasKey(h => h.Id);

            builder.Property(h => h.PreviousState).HasMaxLength(50);
            builder.Property(h => h.NewState).HasMaxLength(50);
            builder.Property(h => h.Comment).HasMaxLength(250);
            builder.Property(h => h.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasOne(h => h.ConnectionRequest)
                   .WithMany(r => r.StateHistories)
                   .HasForeignKey(h => h.ConnectionRequestId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
