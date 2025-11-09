using SDOC.Application.Dto.Email;

namespace SDOC.Application.Interfaces.IServices
{
    public interface IEmailService
    {
        Task SendAsync(EmailRequestDto emailRequestDto);
    }
}
