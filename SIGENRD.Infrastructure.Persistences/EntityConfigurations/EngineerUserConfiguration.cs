
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class EngineerUserConfiguration : IEntityTypeConfiguration<EngineerUser>
    {
        public void Configure(EntityTypeBuilder<EngineerUser> builder)
        {
            #region Basic cofiguration
            builder.HasKey(x => x.Id);
            builder.ToTable("Engineers");
            #endregion

            #region Property cofiguration
            builder.ToTable("EngineerUsers");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.ApplicationUserId)
                .HasMaxLength(450) // Identity uses string Ids
                .IsRequired();

            builder.Property(e => e.CodiaNumber)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Specialty)
                .HasMaxLength(120);

            builder.Property(e => e.IsActive)
                .HasDefaultValue(true);
            #endregion

            #region Property cofiguration
            
            #endregion
        }
    }
}
