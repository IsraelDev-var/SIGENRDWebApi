using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;


namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class ReviewObservationConfiguration : IEntityTypeConfiguration<ReviewObservation>
    {
        public void Configure(EntityTypeBuilder<ReviewObservation> builder)
        {
            builder.ToTable("ReviewObservations");
            builder.HasKey(o => o.Id);

            builder.Property(o => o.DocumentType)
                   .HasConversion<int>()
                   .HasDefaultValue(DocumentType.TechnicalSheet);

            

            builder.Property(o => o.Comment)
                   .HasMaxLength(500)
                   .IsRequired();

            builder.Property(o => o.CreatedAt)
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
