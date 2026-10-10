using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");
            builder.HasKey(n => n.Id);

            builder.Property(n => n.UserId).HasMaxLength(450).IsRequired();
            builder.Property(n => n.Title).HasMaxLength(150).IsRequired();
            builder.Property(n => n.Message).IsRequired();
            builder.Property(n => n.NotificationType).HasConversion<string>().HasMaxLength(50);

            builder.HasIndex(n => new { n.UserId, n.IsRead });
        }
    }
}
