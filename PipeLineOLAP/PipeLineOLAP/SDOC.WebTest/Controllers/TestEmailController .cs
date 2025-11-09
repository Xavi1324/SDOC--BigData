using Microsoft.AspNetCore.Mvc;
using SDOC.Application.Interfaces.IServices;
using SDOC.Application.Dto.Email;

namespace SDOC.Web.Controllers
{
    public class TestEmailController : Controller
    {
        private readonly IEmailService _emailService;

        public TestEmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpGet]
        [Route("test-email")]
        public async Task<IActionResult> SendTestEmail()
        {
            var email = new EmailRequestDto
            {
                To = "hermesbankingrd@gmail.com", // pon tu correo real
                Subject = "Prueba de correo SDOC ✅",
                HtmlBody = @"
                    <h2>Correo de prueba SDOC</h2>
                    <p>Si ves este mensaje, el servicio de correo está funcionando correctamente.</p>
                    <p>Fecha: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + @"</p>"
            };

            await _emailService.SendAsync(email);

            return Ok("Correo de prueba enviado (si no falla el SMTP). Revisa tu bandeja o SPAM.");
        }
    }
}
