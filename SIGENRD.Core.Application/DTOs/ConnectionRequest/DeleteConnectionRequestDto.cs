using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIGENRD.Core.Application.DTOs.ConnectionRequest
{
    public class DeleteConnectionRequestDto
    {
        [Required]
        public int Id { get; set; }

        // Opcional: Motivo de la eliminación (Útil si no borras físicamente el registro)
        public string? DeletionReason { get; set; }
    }
}
