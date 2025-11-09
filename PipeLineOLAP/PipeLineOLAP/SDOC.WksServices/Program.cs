using SDOC.Application;
using SDOC.Persitences;
using SDOC.WksServices;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddPersistenceDependency(builder.Configuration);
        builder.Services.AddApplicationDependency();
        builder.Services.AddHostedService<Worker>();


        var host = builder.Build();
        host.Run();
    }
}