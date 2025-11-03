using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDOC.Persitences.Context;

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
        }
    }
}
