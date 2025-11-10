using Microsoft.EntityFrameworkCore;
using SDOC.Api.Data.Context;
using SDOC.Api.Data.Interfaces;
using SDOC.Api.Data.Repository;

namespace SDOC.Api
{
    public static class ApiDependency
    {
        public static void AddApiDependency(this IServiceCollection services, IConfiguration configuration)
        {
            
            services.AddDbContext<SocialApiContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("SourceDb")));

            services.AddTransient<ISocialCommetsRepository, SocialCommetsRepository>();


        }
    }
}
