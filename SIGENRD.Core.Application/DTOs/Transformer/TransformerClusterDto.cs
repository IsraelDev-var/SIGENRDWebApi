namespace SIGENRD.Core.Application.DTOs.Transformer
{
    /// <summary>
    /// Grupo de transformadores para la vista amplia del mapa (RNF-02, RNF-23). Con Count = 1 trae el transformador
    /// (id, código, estado y color) para dibujarlo como punto.
    /// </summary>
    public class TransformerClusterDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Count { get; set; }
        public int Available { get; set; }
        public int Conditioned { get; set; }
        public int Critical { get; set; }
        public int Saturated { get; set; }
        public int Maintenance { get; set; }
        public int? TransformerId { get; set; }
        public string? Code { get; set; }
        public string? Status { get; set; }
        public string? Color { get; set; }
    }

    public class TransformerClusterResultDto
    {
        public List<TransformerClusterDto> Items { get; set; } = new();
        /// <summary>Transformadores en el área con los filtros aplicados (suma de todos los grupos).</summary>
        public int Total { get; set; }
        /// <summary>Lado de la celda de agrupación en grados, para depurar y para las pruebas.</summary>
        public double CellSizeDegrees { get; set; }
    }
}
