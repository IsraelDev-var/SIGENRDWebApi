

namespace SIGENRD.Core.Domain.Entities
{
    public class EngineerUser
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
       

        public int InstallerCompanyId { get; set; }
        public InstallerCompany InstallerCompany { get; set; } = default!;

        public string CodiaNumber { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public bool IsVerifiedByCodia { get; set; } = false;
    }
}
