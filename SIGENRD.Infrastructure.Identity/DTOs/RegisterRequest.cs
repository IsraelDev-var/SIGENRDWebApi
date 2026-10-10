using SIGENRD.Core.Domain.Enums;

using System.ComponentModel.DataAnnotations;


namespace SIGENRD.Infrastructure.Identity.DTOs
{
    public class RegisterRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public UserType UserType { get; set; }

        public DistributorType? DistributorType { get; set; }

        public int? InstallerCompanyId { get; set; }
    }
}
