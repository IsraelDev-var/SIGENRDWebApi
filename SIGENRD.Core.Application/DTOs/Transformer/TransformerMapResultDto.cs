namespace SIGENRD.Core.Application.DTOs.Transformer
{
    public class TransformerMapResultDto
    {
        public List<TransformerMapPointDto> Items { get; set; } = new();

        // true cuando había más transformadores que el límite: el cliente debe acercar el mapa o filtrar
        public bool Truncated { get; set; }
    }
}
