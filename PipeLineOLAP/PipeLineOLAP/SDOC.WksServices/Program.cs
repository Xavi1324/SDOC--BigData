using SDOC.Application;
using SDOC.Persitences;
using SDOC.Shared;
using SDOC.WksServices;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddPersistenceDependency(builder.Configuration);
        builder.Services.AddApplicationDependency();
        builder.Services.AddSharedLayerIoc(builder.Configuration);
        builder.Services.AddHostedService<Worker>();



        var host = builder.Build();
        host.Run();
    }
}