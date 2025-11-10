using Microsoft.EntityFrameworkCore;
using SDOC.Api.Data.Context;
using SDOC.Api.Data.entites;
using SDOC.Api.Data.Interfaces;

namespace SDOC.Api.Data.Repository
{
    public class SocialCommetsRepository : ISocialCommetsRepository
    { 
        private readonly SocialApiContext _socialApiContext;

        public SocialCommetsRepository(SocialApiContext socialApiContext)
        {
            _socialApiContext = socialApiContext;
        }

        public async Task<IEnumerable<SocialCommetsApi>> ReadAsync()
        {
            return await _socialApiContext.SocialCommets.ToListAsync();
        }
    }
}
