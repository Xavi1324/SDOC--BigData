using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Domain.Entities.DB;
using SDOC.Persitences.Context;

namespace SDOC.Persitences.Repositories
{
    public class WebReviewSourceRepository : IWebReviewSourceRepository
    {
        private readonly SourceOpinionsContext _context;
        private readonly ILogger<WebReviewSourceRepository> _logger;

        public WebReviewSourceRepository(SourceOpinionsContext context, ILogger<WebReviewSourceRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<WebReviewDB>> ReadAsync()
        {          
            try
            {
                return await _context.WebReviews.AsNoTracking().Where(o => o.FuenteId == 1).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer reseñas web desde la base de datos.");
                throw;
            }
        }
    }
}
