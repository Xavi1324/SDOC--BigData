using Microsoft.EntityFrameworkCore;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Domain.Entities.DB;
using SDOC.Persitences.Context;

namespace SDOC.Persitences.Repositories
{
    public class WebReviewSourceRepository : IWebReviewSourceRepository
    {
        private readonly SourceOpinionsContext _context;

        public WebReviewSourceRepository(SourceOpinionsContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<WebReviewDB>> GetAllAsync()
        {
            return await _context.WebReviews
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
