using SDOC.Application.Interfaces.IRepository;
using SDOC.Application.Interfaces.IServices;
using SDOC.Domain.Entities.Api;
using SDOC.Domain.Entities.csv;
using SDOC.Domain.Entities.DB;

namespace SDOC.WksServices
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceProvider _serviceProvider;
        

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            
            _serviceProvider = serviceProvider;


        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            // Resolver IHandlerService dentro del scope (igual que el profe)
                            var handlerService = scope.ServiceProvider.GetRequiredService<IHandlerService>();

                            await handlerService.DataHandler();

                            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error procesando los datos SDOC en el Worker.");
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }

        }

        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        using (var scope = _scopeFactory.CreateScope())
        //        {
        //            var surveyReader = scope.ServiceProvider
        //                .GetRequiredService<ISourceReader<SurveyCsv>>();

        //            var webReviewReader = scope.ServiceProvider
        //                .GetRequiredService<ISourceReader<WebReviewDB>>();

        //            var apiSocialCommentReader = scope.ServiceProvider
        //                .GetRequiredService<ISourceReader<SocialCommetsApi>>();


        //            var surveys = await surveyReader.ReadAsync();
        //            var webReviews = await webReviewReader.ReadAsync();
        //            var apiSocialComments = await apiSocialCommentReader.ReadAsync();

        //            var count = surveys.Count();
        //            var webCount = webReviews.Count();
        //            var apiCount = apiSocialComments.Count();
        //            _logger.LogInformation("Se leyeron {Count} encuestas desde CSV.", count);
        //            _logger.LogInformation("Se leyeron {Count} reseñas web desde la base de datos.", webCount);
        //            _logger.LogInformation("Se leyeron {Count} comentarios sociales desde la API.", apiCount);



        //        }

        //        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        //        if (_logger.IsEnabled(LogLevel.Information))
        //        {
        //            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        //        }
        //        await Task.Delay(1000, stoppingToken);
        //    }
        //}
    }
}
