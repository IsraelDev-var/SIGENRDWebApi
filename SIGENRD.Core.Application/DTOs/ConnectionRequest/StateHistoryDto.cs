namespace SIGENRD.Core.Application.DTOs.ConnectionRequest
{
    public class StateHistoryDto
    {
        public int Id { get; set; }
        public string? PreviousState { get; set; }
        public string? NewState { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }

        // Usuario de Identity que hizo el cambio
        public string? CreatedBy { get; set; }
    }
}
