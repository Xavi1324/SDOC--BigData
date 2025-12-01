using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SDOC.Application.Interfaces.IServices;
using SDOC.Domain.Setting;
using SDOC.Shared.Services;

namespace SDOC.Shared
{
    public static class ServiceRegistration
    {
        public static void AddSharedLayerIoc(this IServiceCollection services, IConfiguration config)
        {
            #region Configurations
            services.Configure<MailSettings>(config.GetSection("MailSettings"));
            #endregion

            #region Services IOC
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IErrorNotificationService, ErrorNotificationService>();
            #endregion
        }
    }
}
