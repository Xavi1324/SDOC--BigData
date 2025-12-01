using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SDOC.Application.Dto.Email;
using SDOC.Application.Interfaces.IServices;
using System.Text;

namespace SDOC.Shared.Services
{
    public class ErrorNotificationService : IErrorNotificationService
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ErrorNotificationService> _logger;
        private readonly string _toEmail;

        public ErrorNotificationService(
            IEmailService emailService,
            ILogger<ErrorNotificationService> logger,
            IConfiguration configuration)
        {
            _emailService = emailService;
            _logger = logger;
            _toEmail = configuration["ErrorNotification:To"]
                       ?? throw new ArgumentException("No se encontró ErrorNotification:To en configuración");
        }

        public async Task NotifySourceErrorAsync(string sourceName, string errorMessage, Exception? exception = null)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"<h2>Error en origen de datos: {sourceName}</h2>");
                sb.AppendLine($"<p><strong>Mensaje:</strong> {errorMessage}</p>");
                sb.AppendLine($"<p><strong>Fecha:</strong> {DateTime.Now}</p>");

                if (exception != null)
                {
                    sb.AppendLine("<h3>Detalle de la excepción</h3>");
                    sb.AppendLine($"<p><strong>Tipo:</strong> {exception.GetType().FullName}</p>");
                    sb.AppendLine($"<p><strong>Message:</strong> {exception.Message}</p>");
                    sb.AppendLine("<pre>");
                    sb.AppendLine(exception.StackTrace);
                    sb.AppendLine("</pre>");
                }

                var emailRequest = new EmailRequestDto
                {
                    To = _toEmail,
                    Subject = $"[SDOC] Error en origen: {sourceName}",
                    HtmlBody = sb.ToString()
                };

                await _emailService.SendAsync(emailRequest);
            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex, "Error enviando notificación de error para el origen {SourceName}", sourceName);
            }
        }
    }

}
