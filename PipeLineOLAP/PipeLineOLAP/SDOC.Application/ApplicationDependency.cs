using Microsoft.Extensions.DependencyInjection;
using SDOC.Application.Interfaces.IServices;
using SDOC.Application.Services;


namespace SDOC.Application
{
    public static class ApplicationDependency
    {
        public static void AddApplicationDependency(this IServiceCollection services)
        {
            services.AddScoped<IHandlerService, HandlerService>();
        }
    }
}
