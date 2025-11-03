using SDOC.Application.Interfaces.IRepository;
using SDOC.Domain.Entities.Api;
using System.Text.Json;

namespace SDOC.Persitences.Repositories
{
    public class ApiSocialCommentSourceRepository : ISocialCommentSourceRepository
    {
        private readonly HttpClient _httpClient;
        private readonly string _url;

        public ApiSocialCommentSourceRepository(HttpClient httpClient, string url)
        {
            _httpClient = httpClient;
            _url = url;
        }

        public async Task<IReadOnlyList<SocialCommetsApi>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync(_url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<List<SocialCommetsApi>>(json);

            return data ?? new List<SocialCommetsApi>();
        }
    }
}
