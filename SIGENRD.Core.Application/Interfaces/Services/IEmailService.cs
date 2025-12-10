

using SIGENRD.Core.Application.DTOs.Email;

namespace SIGENRD.Core.Application.Interfaces.Services
{
    public interface  IEmailService
    {
        Task SendAsync(EmailRequest request);
    }
}
