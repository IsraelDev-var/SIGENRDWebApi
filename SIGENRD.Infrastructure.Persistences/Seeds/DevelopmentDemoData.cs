using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Persistences.Contexts;
using SIGENRD.Infrastructure.Persistences.Repositories;

namespace SIGENRD.Infrastructure.Persistences.Seeds
{
    /// <summary>
    /// Datos de demostración para desarrollo y pruebas E2E: Edesur, una zona por ciudad y N transformadores
    /// sintéticos alrededor de ciudades reales, con todos los estados. Solo en Development y con
    /// Seed:DemoTransformers mayor que 0. Siempre genera lo mismo (semilla fija) y no se repite si ya existen.
    /// </summary>
    public static class DevelopmentDemoData
    {
        public const string CodePrefix = "TR-DEMO-";
        public const string DistributorName = "Edesur (demostración)";

        /// <summary>Ciudades de la zona de Edesur y alrededores, con un peso relativo de transformadores.</summary>
        public static readonly IReadOnlyList<(string Zone, string Province, double Lat, double Lng, int Weight)> Cities =
        [
            ("Distrito Nacional", "Distrito Nacional", 18.4861, -69.9312, 30),
            ("Santo Domingo Oeste", "Santo Domingo", 18.5003, -69.9902, 12),
            ("San Cristóbal", "San Cristóbal", 18.4166, -70.1063, 10),
            ("Baní", "Peravia", 18.2796, -70.3313, 8),
            ("Azua", "Azua", 18.4532, -70.7349, 8),
            ("San Juan de la Maguana", "San Juan", 18.8058, -71.2290, 8),
            ("Barahona", "Barahona", 18.2085, -71.1008, 7),
            ("Santiago", "Santiago", 19.4517, -70.6970, 9),
            ("La Romana", "La Romana", 18.4273, -68.9728, 8),
        ];

        public static async Task SeedAsync(AppContextSIGENRD db, int count, CancellationToken cancellationToken = default)
        {
            if (count <= 0) return;
            if (await db.Transformers.AnyAsync(t => t.Code.StartsWith(CodePrefix), cancellationToken)) return;

            var distributor = await EnsureDistributorAsync(db, cancellationToken);

            var zones = Cities
                .Select(c => db.ServiceZones.Add(new ServiceZone { ZoneName = c.Zone, Municipality = c.Zone, Province = c.Province }).Entity)
                .ToList();
            await db.SaveChangesAsync(cancellationToken);

            var transformers = Build(count, zones.Select(z => z.Id).ToList(), distributor, DateTime.UtcNow);
            foreach (var batch in transformers.Chunk(1000))
            {
                db.Transformers.AddRange(batch);
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        /// <summary>Edesur, la EDE de desarrollo: la usan los transformadores de demostración y el personal de prueba.</summary>
        public static async Task<Distributor> EnsureDistributorAsync(AppContextSIGENRD db, CancellationToken cancellationToken = default)
        {
            var distributor = await db.Distributors.FirstOrDefaultAsync(d => d.Name == DistributorName, cancellationToken);
            if (distributor is not null) return distributor;

            distributor = db.Distributors.Add(new Distributor { Name = DistributorName, Type = DistributorType.EDESUR }).Entity;
            await db.SaveChangesAsync(cancellationToken);
            return distributor;
        }

        /// <summary>Genera los transformadores sin tocar la base (se prueba aparte).</summary>
        public static List<Transformer> Build(int count, IReadOnlyList<int> zoneIds, Distributor distributor, DateTime now)
        {
            var random = new Random(2026);
            var totalWeight = Cities.Sum(c => c.Weight);
            var result = new List<Transformer>(count);

            for (var i = 0; i < count; i++)
            {
                // Ciudad según su peso y posición con dispersión normal (~4 km) alrededor del centro.
                var pick = random.Next(totalWeight);
                var cityIndex = 0;
                while (pick >= Cities[cityIndex].Weight) pick -= Cities[cityIndex++].Weight;
                var city = Cities[cityIndex];
                var lat = city.Lat + Normal(random) * 0.04;
                var lng = city.Lng + Normal(random) * 0.04;

                var total = new[] { 25m, 37.5m, 50m, 75m, 100m, 167m }[random.Next(6)];
                // 60 % Disponible, 20 % Condicionada, 10 % Crítica, 5 % Saturada, 5 % Mantenimiento.
                var roll = random.NextDouble();
                var pct = roll switch
                {
                    < 0.60 => 70 + random.NextDouble() * 30,
                    < 0.80 => 30 + random.NextDouble() * 39.9,
                    < 0.90 => 1 + random.NextDouble() * 28,
                    _ => -10 + random.NextDouble() * 10,
                };

                var transformer = new Transformer
                {
                    Code = $"{CodePrefix}{i + 1:D5}",
                    DistributorId = distributor.Id,
                    ServiceZoneId = zoneIds[cityIndex],
                    Location = TransformerQueries.ToPoint(Math.Round(lat, 6), Math.Round(lng, 6))
                };
                transformer.UpdateCapacity(total, Math.Round(total * (decimal)pct / 100m, 2),
                    distributor.AvailableThresholdPct, distributor.ConditionedThresholdPct);
                if (roll >= 0.95) transformer.EnterMaintenance();

                // Fechas de actualización variadas: algunas con más de 30 días, para ver el aviso de dato viejo.
                transformer.LastUpdated = now.AddDays(-random.Next(0, 60));
                result.Add(transformer);
            }
            return result;
        }

        private static double Normal(Random random)
        {
            // Box-Muller.
            var u1 = 1.0 - random.NextDouble();
            var u2 = random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
