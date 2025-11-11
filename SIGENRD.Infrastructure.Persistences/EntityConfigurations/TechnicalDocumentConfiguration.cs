using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;


namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class TechnicalDocumentConfiguration : IEntityTypeConfiguration<TechnicalDocument>
    {
        public void Configure(EntityTypeBuilder<TechnicalDocument> builder)
        {
            builder.ToTable("TechnicalDocuments");
            builder.HasKey(d => d.Id);

            builder.Property(d => d.DocumentType)
                   .HasConversion<int>()
                   .IsRequired();

            builder.Property(d => d.FileName)
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(d => d.FileUrl)
                   .HasMaxLength(300)
                   .IsRequired();

            builder.Property(d => d.UpdatedAt)
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
