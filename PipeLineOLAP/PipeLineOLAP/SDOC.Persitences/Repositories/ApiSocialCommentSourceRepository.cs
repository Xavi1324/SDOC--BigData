using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Domain.Entities.Api;

namespace SDOC.Persitences.Repositories
{
    public class ApiSocialCommentSourceRepository : IApiSocialCommentSourceRepository
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;
        private readonly ILogger<ApiSocialCommentSourceRepository> _logger;

        public ApiSocialCommentSourceRepository(HttpClient httpClient, string url, ILogger<ApiSocialCommentSourceRepository> logger)
        {
            _httpClient = httpClient;
            _url = url;
            _logger = logger;
        }

        public async Task<IEnumerable<SocialCommetsApi>> ReadAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<List<SocialCommetsApi>>(json);

                return data ?? new List<SocialCommetsApi>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer comentarios sociales desde la API '{Url}'", _url);
                throw;
            }
        }
    }
}
