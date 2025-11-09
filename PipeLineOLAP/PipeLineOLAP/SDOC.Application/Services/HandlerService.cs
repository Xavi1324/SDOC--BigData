using Microsoft.Extensions.Logging;
using SDOC.Application.Dto.Email;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Application.Interfaces.IServices;
using SDOC.Application.Result;

namespace SDOC.Application.Services
{
    public class HandlerService : IHandlerService
    {
        private readonly IApiSocialCommentSourceRepository _apiRepo;
        private readonly ICsvInternalSurveySourceRepository _csvRepo;
        private readonly IWebReviewSourceRepository _webRepo;
        private readonly ILogger<HandlerService> _logger;
        private readonly IEmailService _emailService;

        public HandlerService(  IApiSocialCommentSourceRepository apiRepo, ICsvInternalSurveySourceRepository csvRepo,
                                IWebReviewSourceRepository webRepo, ILogger<HandlerService> logger,
                                IEmailService emailService)

        {
            _apiRepo = apiRepo;
            _csvRepo = csvRepo;
            _webRepo = webRepo;
            _logger = logger;
            _emailService = emailService;
        }

        public  Task<ServiceResult> DataHandler()
        {
            
            throw new NotImplementedException();
        }

        private async Task<IEnumerable<T>> ExecuteWithErrorEmailAsync<T>(   Func<Task<IEnumerable<T>>> operation, 
                                                                            string sourceName)
        {
            try
            {
                var data = await operation();

                _logger.LogInformation(
                    "Se leyeron {Count} registros desde la fuente {SourceName}.",
                    data.Count(),
                    sourceName
                );

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer datos desde la fuente {SourceName}.", sourceName);

                var email = new EmailRequestDto
                {
                    To = "hermesbankingrd@gmail.com", 
                    Subject = $"[SDOC] Error al leer datos desde {sourceName}",
                    HtmlBody = $@"
                            <h2>Error en proceso SDOC</h2>
                            <p><strong>Fuente:</strong> {sourceName}</p>
                            <p><strong>Mensaje:</strong> {ex.Message}</p>
                            <p><strong>Tipo:</strong> {ex.GetType().Name}</p>
                            <p><strong>Fecha:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                            <pre>{ex}</pre>"
                };

                await _emailService.SendAsync(email);

                
                throw;
            }
        }

    }
}
