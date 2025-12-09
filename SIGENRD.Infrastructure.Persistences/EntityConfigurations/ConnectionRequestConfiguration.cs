using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Infrastructure.Persistences.EntityConfigurations
{
    public class ConnectionRequestConfiguration : IEntityTypeConfiguration<ConnectionRequest>
    {
        public void Configure(EntityTypeBuilder<ConnectionRequest> builder)
        {
            // 1. Nombre de tabla y Clave Primaria
            builder.ToTable("ConnectionRequests");
            builder.HasKey(c => c.Id);

            // 2. Propiedades Básicas
            builder.Property(c => c.RequestCode)
                .IsRequired()
                .HasMaxLength(50); // Ajusta según tu lógica de negocio

            builder.Property(c => c.ProjectDescription)
                .HasMaxLength(500);

            builder.Property(c => c.ProjectAddress)
                .HasMaxLength(250);

            builder.Property(c => c.RequestedAt)
                .IsRequired();

            // 3. Configuración Geoespacial (CRUCIAL PARA EL ERROR ANTERIOR)
            builder.Property(c => c.Coordinates)
                .HasColumnType("geography (Point, 4326)");
            // 'geography' calcula distancias en metros (curvatura de la tierra)
            // 'geometry' calcula en plano cartesiano. Para GPS usa geography.

            // 4. Configuración de Enums (Guardar como String es más legible en DB)
            builder.Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(c => c.UsageType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(c => c.TariffType)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(c => c.InterconnectionType)
                .HasConversion<string>()
                .HasMaxLength(50);

            // 5. Relaciones (Foreign Keys)

            // Relación con Customer
            builder.HasOne(c => c.Customer)
                .WithMany(u => u.ConnectionRequests) // Asegúrate que Customer tenga esta colección
                .HasForeignKey(c => c.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Evita borrar el cliente si tiene solicitudes

            // Relación con InstallerCompany
            builder.HasOne(c => c.InstallerCompany)
                .WithMany(i => i.ConnectionRequests)
                .HasForeignKey(c => c.InstallerCompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con Distributor
            builder.HasOne(c => c.Distributor)
                .WithMany(d => d.ConnectionRequests)
                .HasForeignKey(c => c.DistributorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con Transformer
            builder.HasOne(c => c.Transformer)
                .WithMany(t => t.ConnectionRequests)
                .HasForeignKey(c => c.TransformerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación 1 a 1 con NetMeteringRequest (Opcional)
            builder.HasOne(c => c.NetMeteringRequest)
                .WithOne(n => n.ConnectionRequest)
                .HasForeignKey<NetMeteringRequest>(n => n.ConnectionRequestId)
                .OnDelete(DeleteBehavior.Cascade); // Si borras la solicitud, se borra el trámite de medición neta
        }
    }
}