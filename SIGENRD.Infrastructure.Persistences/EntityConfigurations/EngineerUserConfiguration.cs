
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
            

            builder.Property(e => e.ApplicationUserId)
                .HasMaxLength(450) // Identity uses string Ids
                .IsRequired();

            builder.Property(e => e.CodiaNumber)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.Specialty)
                .HasMaxLength(120);

            builder.Property(e => e.IsVerifiedByCodia)
                .HasDefaultValue(true);
            #endregion

            #region relationship configurations
            builder.HasOne(e => e.InstallerCompany)
                   .WithMany(c => c.Engineers)
                   .HasForeignKey(e => e.InstallerCompanyId)
                   .OnDelete(DeleteBehavior.Cascade);

            #endregion
        }
    }
}
