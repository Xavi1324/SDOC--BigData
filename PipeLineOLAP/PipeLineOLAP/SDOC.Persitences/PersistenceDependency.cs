using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Domain.Entities.Api;
using SDOC.Domain.Entities.csv;
using SDOC.Domain.Entities.DB;
using SDOC.Persitences.Context;
using SDOC.Persitences.Repositories;

namespace SDOC.Persitences
{
    public static class PersistenceDependency
    {
        public static void AddPersistenceDependency(this IServiceCollection services, IConfiguration configuration)
        {
            // Contexto de origen (base de datos transaccional)
            services.AddDbContext<SourceOpinionsContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("SourceDb")));

            // Contexto de destino (base de datos analítica OLAP)
            services.AddDbContext<OlapOpinionsContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("OlapDb")));

            services.AddTransient<ICsvInternalSurveySourceRepository, CsvInternalSurveySourceRepository>();
            services.AddTransient<IWebReviewSourceRepository, WebReviewSourceRepository>();


            services.AddHttpClient();
            
            services.AddTransient<IApiSocialCommentSourceRepository, ApiSocialCommentSourceRepository>();
            


            services.AddTransient<IDwhRepository, DwhRepository>();

            services.AddScoped<ISourceReader<SurveyCsv>>(sp =>
                sp.GetRequiredService<ICsvInternalSurveySourceRepository>());

            services.AddScoped<ISourceReader<WebReviewDB>>(sp =>
                sp.GetRequiredService<IWebReviewSourceRepository>());


            
            services.AddScoped<ISourceReader<SocialCommetsApi>, ApiSocialCommentSourceRepository>();
        }
    }
}
