using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
    {
        public void Configure(EntityTypeBuilder<Customer> builder)
        {
            #region basic configurations
            builder.ToTable("customer");

            builder.HasKey(c => c.Id);
            #endregion

            #region property configurations
            builder.Property(c => c.FullName)
                   .HasMaxLength(150)
                   .IsRequired();

            builder.Property(c => c.NationalIdOrRnc)
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(c => c.Email)
                   .HasMaxLength(150);

            builder.Property(c => c.CreatedAt)
                   .HasDefaultValueSql("CURRENT_TIMESTAMP");
            #endregion


           
        }
    }
}
