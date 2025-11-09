using SDOC.Application.Interfaces.IRepository;
using SDOC.Domain.Entities.csv;
using SDOC.Domain.Entities.DB;

namespace SDOC.WksServices
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public Worker(ILogger<Worker> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var surveyReader = scope.ServiceProvider
                        .GetRequiredService<ISourceReader<SurveyCsv>>();

                    var webReviewReader = scope.ServiceProvider
                        .GetRequiredService<ISourceReader<WebReviewDB>>();


                    var surveys = await surveyReader.ReadAsync();
                    var webReviews = await webReviewReader.ReadAsync();

                    var count = surveys.Count();
                    var webCount = webReviews.Count();
                    _logger.LogInformation("Se leyeron {Count} encuestas desde CSV.", count);
                    _logger.LogInformation("Se leyeron {Count} reseñas web desde la base de datos.", webCount);


                    
                }

                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
