using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Application.Interfaces.IServices;
using SDOC.Domain.Entities.DB;
using SDOC.Persitences.Context;

namespace SDOC.Persitences.Repositories
{
    public class WebReviewSourceRepository : IWebReviewSourceRepository
    {
        private readonly SourceOpinionsContext _context;
        private readonly ILogger<WebReviewSourceRepository> _logger;
        private readonly IErrorNotificationService _errorNotificationService;

        public WebReviewSourceRepository(SourceOpinionsContext context, ILogger<WebReviewSourceRepository> logger, IErrorNotificationService errorNotificationService )
        {
            _context = context;
            _logger = logger;
            _errorNotificationService = errorNotificationService;
        }

        public async Task<IEnumerable<WebReviewDB>> ReadAsync()
        {          
            try
            {
                return await _context.WebReviews.AsNoTracking().ToListAsync();
                
            }
            catch (Exception ex)
            {
                var msg = "Error al leer reseñas web desde la base de datos.";
                _logger.LogError(ex, msg);

                await _errorNotificationService.NotifySourceErrorAsync(
                    sourceName: "WebReview DB",
                    errorMessage: msg,
                    exception: ex);
                throw;
            }
        }
    }
}
