using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;

namespace SIGENRD.Core.Domain.Rules
{
    /// <summary>Grupo de transformadores cercanos para la vista del país (RNF-02, RNF-23).</summary>
    public sealed record TransformerCluster(
        double Latitude,
        double Longitude,
        int Count,
        int Available,
        int Conditioned,
        int Critical,
        int Saturated,
        int Maintenance,
        /// <summary>Solo si el grupo tiene un único transformador: el mapa lo dibuja como punto.</summary>
        int? TransformerId,
        string? Code,
        TransformerStatus? Status);

    /// <summary>
    /// Agrupa transformadores en una cuadrícula cuyo tamaño depende del zoom del mapa: a menos zoom, celdas más grandes.
    /// Cada grupo se dibuja en el promedio de sus puntos y cuenta cuántos hay de cada estado.
    /// </summary>
    public static class TransformerGrid
    {
        /// <summary>Lado de la celda en píxeles de pantalla, con teselas de 512 px (MapLibre).</summary>
        public const double CellSizePixels = 64;

        public const double MinZoom = 0;
        public const double MaxZoom = 22;

        /// <summary>Lado de la celda en grados para un zoom (proyección Web Mercator, ancho del mundo = 512·2^zoom px).</summary>
        public static double CellSizeDegrees(double zoom)
        {
            var clamped = Math.Clamp(zoom, MinZoom, MaxZoom);
            return CellSizePixels * 360d / (512d * Math.Pow(2, clamped));
        }

        public static IReadOnlyList<TransformerCluster> Aggregate(IEnumerable<Transformer> transformers, double zoom)
        {
            var cell = CellSizeDegrees(zoom);
            var cells = new Dictionary<(long, long), Accumulator>();

            foreach (var t in transformers)
            {
                var lat = t.Location.Y;
                var lng = t.Location.X;
                var key = ((long)Math.Floor(lng / cell), (long)Math.Floor(lat / cell));
                if (!cells.TryGetValue(key, out var acc))
                {
                    acc = new Accumulator();
                    cells[key] = acc;
                }
                acc.Add(t, lat, lng);
            }

            // Orden estable: los más poblados primero y, a igual tamaño, por posición.
            return cells.Values
                .Select(a => a.ToCluster())
                .OrderByDescending(c => c.Count)
                .ThenBy(c => c.Latitude)
                .ThenBy(c => c.Longitude)
                .ToList();
        }

        private sealed class Accumulator
        {
            private double _latSum, _lngSum;
            private int _count, _available, _conditioned, _critical, _saturated, _maintenance;
            private Transformer? _single;

            public void Add(Transformer t, double lat, double lng)
            {
                _latSum += lat;
                _lngSum += lng;
                _count++;
                _single = _count == 1 ? t : null;

                switch (t.Status)
                {
                    case TransformerStatus.Available: _available++; break;
                    case TransformerStatus.Conditioned: _conditioned++; break;
                    case TransformerStatus.Critical: _critical++; break;
                    case TransformerStatus.Saturated: _saturated++; break;
                    case TransformerStatus.Maintenance: _maintenance++; break;
                }
            }

            public TransformerCluster ToCluster() => new(
                Math.Round(_latSum / _count, 6),
                Math.Round(_lngSum / _count, 6),
                _count, _available, _conditioned, _critical, _saturated, _maintenance,
                _single?.Id, _single?.Code, _single?.Status);
        }
    }
}
