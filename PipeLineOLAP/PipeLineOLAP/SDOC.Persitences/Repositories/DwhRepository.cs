using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SDOC.Application.Dto.DimDtos;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Application.Interfaces.IServices;
using SDOC.Application.Result;
using SDOC.Domain.Entities.Dwh.Dimensions;
using SDOC.Persitences.Context;
using System.Globalization;

namespace SDOC.Persitences.Repositories
{
    public class DwhRepository : IDwhRepository
    {
        private readonly OlapOpinionsContext _context;
        private readonly ILogger<DwhRepository> _logger;
        private readonly IEmailService _emailService;
        private readonly IErrorNotificationService _errorNotificationService;

        public DwhRepository(OlapOpinionsContext context, ILogger<DwhRepository> logger, IEmailService emailService, IErrorNotificationService errorNotificationService)
        {
            _context = context;
            _logger = logger;
            _emailService = emailService;
            _errorNotificationService = errorNotificationService;
        }

        public async Task<ServiceResult> DimensionsLoader(DimDtos dimDtos)
        {
            var result = new ServiceResult();

            try
            {
                var cleanResult = await CleanDimensionTables();
                if (!cleanResult.IsSuccess)
                {
                    _logger.LogError(cleanResult.Message);
                    return cleanResult;
                }
                var webReviews = dimDtos.WebReviews;
                var surveys = dimDtos.Surveys;
                var socialComments = dimDtos.SocialComments;

                
                var times = surveys
                    .Select(s => s.Fecha.Date)
                    .Distinct()
                    .Select(d => new DimTime
                    {
                        TimeSK = int.Parse(d.ToString("yyyyMMdd")),
                        Date = d,
                        Year = d.Year,
                        Month = (byte)d.Month,
                        MonthName = d.ToString("MMMM", new CultureInfo("es-ES")),
                        Day = (byte)d.Day,
                        DayName = d.ToString("dddd", new CultureInfo("es-ES")),
                        Quarter = (byte)((d.Month - 1) / 3 + 1),
                        WeekOfYear = (byte)ISOWeek.GetWeekOfYear(d)
                    })
                    .GroupBy(t => t.TimeSK)
                    .Select(g => g.First())
                    .ToList();

                await _context.DimTimes.AddRangeAsync(times);

                

                var defaultCategory = new DimCategory
                {
                    CategoryName = "Default"
                };

                await _context.DimCategories.AddAsync(defaultCategory);
                await _context.SaveChangesAsync();
                var productIds = webReviews
                    .Select(w => w.ProductId)
                    .Concat(surveys.Select(s => s.IdProducto))
                    .Concat(socialComments.Select(sc => sc.ProductId))
                    .Distinct()
                    .ToList();

                var products = productIds
                    .Select(id => new DimProduct
                    {
                        ProductName = $"Product {id}",  
                        CategorySK = defaultCategory.CategorySK
                    })
                    .ToList();

                await _context.DimProducts.AddRangeAsync(products);

                
                var clientIds = webReviews
                    .Select(w => w.ClientId)
                    .Concat(surveys.Select(s => (int?)s.IdCliente))
                    .Concat(socialComments.Select(sc => sc.ClientId))
                    .Where(c => c.HasValue)
                    .Select(c => c!.Value)
                    .Distinct()
                    .ToList();

                var clients = clientIds
                    .Select(id => new DimClient
                    {
                        ClientName = $"Client {id}",
                        LastName = string.Empty,
                        Email = string.Empty,
                        Country = string.Empty
                    })
                    .ToList();

                await _context.DimClients.AddRangeAsync(clients);

                

                var sourcesFromSurvey = surveys
                    .Select(s => s.Fuente?.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .Select(name => new DimSource
                    {
                        SourceName = name!,
                        SourceType = "Survey"
                    });

                var sourcesFromSocial = socialComments
                    .Select(s => s.Source?.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .Select(name => new DimSource
                    {
                        SourceName = name!,
                        SourceType = "Social"
                    });

                var sourcesFromWeb = webReviews
                    .Select(w => w.FuenteId)
                    .Distinct()
                    .Select(id => new DimSource
                    {
                        SourceName = $"Source {id}",
                        SourceType = "WebReview"
                    });

                var allSources = sourcesFromSurvey
                    .Concat(sourcesFromSocial)
                    .Concat(sourcesFromWeb)
                    .GroupBy(s => new { s.SourceName, s.SourceType })
                    .Select(g => g.First())
                    .ToList();

                await _context.DimSources.AddRangeAsync(allSources);



                var classesFromSurvey = surveys
                    .Select(s => s.Clasificacion?.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .Distinct()
                    .Select(text => new DimClass
                    {
                        ClassCode = BuildClassCode(text!),  // 🔹 transformación aquí
                        ClassName = text!                   // nombre completo amigable
                    });

                var classesFromWeb = webReviews
                    .Select(w => w.ClassId)
                    .Distinct()
                    .Select(id => new DimClass
                    {
                        ClassCode = id.ToString(),    // Ej: "1", "2", "3"
                        ClassName = $"Class {id}"
                    });

                var allClasses = classesFromSurvey
                    .Concat(classesFromWeb)
                    .GroupBy(c => c.ClassCode)
                    .Select(g => g.First())
                    .ToList();

                await _context.DimClasses.AddRangeAsync(allClasses);

                
                await _context.SaveChangesAsync();

                result.IsSuccess = true;
                result.Message = "Dimensiones cargadas correctamente.";
            }
            catch (Exception ex)
            {
                var msg = $"Error cargando las dimensiones del DWH SDOC";
                _logger.LogError(ex, msg);
                result.IsSuccess = false;

                await _errorNotificationService.NotifySourceErrorAsync(
                    sourceName: "DWH Loader",
                    errorMessage: msg,
                    exception: ex);
                throw;
            }

            return result;

        }

        private async Task<ServiceResult> CleanDimensionTables()
        {
            var result = new ServiceResult();

            try
            {
                await _context.DimProducts.ExecuteDeleteAsync();
                await _context.DimCategories.ExecuteDeleteAsync();
                await _context.DimClients.ExecuteDeleteAsync();
                await _context.DimSources.ExecuteDeleteAsync();
                await _context.DimClasses.ExecuteDeleteAsync();
                await _context.DimTimes.ExecuteDeleteAsync();

                await _context.SaveChangesAsync();

                result.IsSuccess = true;
                result.Message = "Dimensiones limpiadas correctamente.";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Error limpiando dimensiones: {ex.Message}";
            }

            return result;
        }
        private static string BuildClassCode(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
                return "UNK"; // Unknown

            var t = rawText.Trim().ToLowerInvariant();

            return t switch
            {
                "positivo" => "POS",
                "negativo" => "NEG",
                "neutral" => "NEU",

                "pos" => "POS",
                "neg" => "NEG",
                "neu" => "NEU",

                _ => t.Length <= 3
                        ? t.ToUpperInvariant()
                        : t.Substring(0, 3).ToUpperInvariant()
            };
        }

    }
}
