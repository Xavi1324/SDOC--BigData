using Microsoft.EntityFrameworkCore;
using SDOC.Api.Data.entites;

namespace SDOC.Api.Data.Context
{
    public class SocialApiContext : DbContext
    {
        public SocialApiContext(DbContextOptions<SocialApiContext> options) : base(options)
        {
        }
        public DbSet<SocialCommetsApi> SocialCommets { get; set; } = null!;
    }
}
