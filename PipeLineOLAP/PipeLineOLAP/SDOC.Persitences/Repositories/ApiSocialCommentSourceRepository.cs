using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Application.Interfaces.IServices;
using SDOC.Domain.Entities.Api;
using System.Net.Http.Json;

namespace SDOC.Persitences.Repositories
{
    public class ApiSocialCommentSourceRepository : IApiSocialCommentSourceRepository
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly ILogger<ApiSocialCommentSourceRepository> _logger;
        private readonly IErrorNotificationService _errorNotificationService;

        public ApiSocialCommentSourceRepository(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            ILogger<ApiSocialCommentSourceRepository> logger,
             IErrorNotificationService errorNotificationService)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _logger = logger;
            _baseUrl = _configuration.GetValue<string>("SocialCommentsApi:BaseUrl")
           ?? throw new ArgumentException("ApiConfig:BaseUrl no configurado");
            _errorNotificationService = errorNotificationService;

        }

        public async Task<IEnumerable<SocialCommetsApi>> ReadAsync()
        {

            List<SocialCommetsApi> commets = new();

            try
            {
                using var client = _clientFactory.CreateClient("SocialCommentsApiClient");
                client.BaseAddress = new Uri(_baseUrl);

                var response = await client.GetAsync("api/SocialCommentsApi/GetAll");
                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<IEnumerable<SocialCommetsApi>>();
                    if (data != null)
                    {
                        commets = data.ToList();
                    }
                }
                else
                {
                    _logger.LogError("Error al leer comentarios sociales desde la API. Código de estado: {StatusCode}", response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                var msg = "Error al leer comentarios sociales desde la API.";
                _logger.LogError(ex, msg);
                await _errorNotificationService.NotifySourceErrorAsync(
                    sourceName: "SocialComments API",
                    errorMessage: msg,
                    exception: ex);
            }   
            return commets;

        }
    }
}
