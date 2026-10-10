using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            builder.ToTable("UserProfiles");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.UserId).HasMaxLength(450).IsRequired();
            builder.Property(p => p.FullName).HasMaxLength(150).IsRequired();
            builder.Property(p => p.Phone).HasMaxLength(30);

            builder.HasIndex(p => p.UserId).IsUnique();
        }
    }
}
