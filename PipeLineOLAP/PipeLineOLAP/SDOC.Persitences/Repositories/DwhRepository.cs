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
        private readonly IErrorNotificationService _errorNotificationService;

        public DwhRepository(OlapOpinionsContext context, ILogger<DwhRepository> logger, IErrorNotificationService errorNotificationService)
        {
            _context = context;
            _logger = logger;
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



                var categoryNames = webReviews
                    .Where(w => !string.IsNullOrWhiteSpace(w.CategoryName))
                    .Select(w => w.CategoryName!.Trim())
                    .Distinct()
                    .ToList();

                var dimCategories = categoryNames
                    .Select(name => new DimCategory
                    {
                        CategoryName = name
                    })
                    .ToList();

                await _context.DimCategories.AddRangeAsync(dimCategories);
                await _context.SaveChangesAsync(); 
                var productRows = webReviews
                    .Select(w => new
                    {
                        w.ProductId,
                        ProductName = string.IsNullOrWhiteSpace(w.ProductName)
                                        ? $"Producto {w.ProductId}"
                                        : w.ProductName!.Trim(),
                        CategoryName = string.IsNullOrWhiteSpace(w.CategoryName)
                                        ? "Sin categoría"
                                        : w.CategoryName!.Trim()
                    })
                    .Distinct()
                    .ToList();

                
                var categoryLookup = await _context.DimCategories
                    .ToDictionaryAsync(c => c.CategoryName, c => c.CategorySK);

                
                var dimProducts = productRows
                    .Select(p =>
                    {
                        categoryLookup.TryGetValue(p.CategoryName, out var catSk);

                        return new DimProduct
                        {
                            ProductName = p.ProductName,
                            CategorySK = catSk  
                        };
                    })
                    .ToList();

                await _context.DimProducts.AddRangeAsync(dimProducts);


                var clientRows = webReviews
                    .Where(w => w.ClientId.HasValue)
                    .Select(w => new
                    {
                        Id = w.ClientId!.Value,
                        Name = string.IsNullOrWhiteSpace(w.ClientName)
                                    ? $"Cliente {w.ClientId}"
                                    : w.ClientName!.Trim(),
                        Last = w.LastName ?? string.Empty,
                        Email = w.Email ?? string.Empty
                    })
                    .Distinct()
                    .ToList();

                var dimClients = clientRows
                    .Select(c => new DimClient
                    {
                        ClientName = c.Name,
                        LastName = c.Last,
                        Email = c.Email,
                        Country = string.Empty
                    })
                    .ToList();

                await _context.DimClients.AddRangeAsync(dimClients);



                var sourcesFromSurvey = surveys
                    .Select(s => s.Fuente?.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(name => new DimSource
                    {
                        SourceName = name!,
                        SourceType = "Survey"
                    });

                
                var sourcesFromSocial = socialComments
                    .Select(s => s.Source?.Trim())
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Select(name => new DimSource
                    {
                        SourceName = name!,
                        SourceType = "Social"
                    });

                
                var sourcesFromWeb = webReviews
                    .Select(w => new
                    {
                        Name = string.IsNullOrWhiteSpace(w.FuenteNombre)
                            ? $"Source {w.FuenteId}"
                            : w.FuenteNombre!.Trim(),
                        Type = string.IsNullOrWhiteSpace(w.TipoFuenteDesc)
                            ? "WebReview"
                            : w.TipoFuenteDesc!.Trim()
                    })
                    .Distinct()
                    .Select(x => new DimSource
                    {
                        SourceName = x.Name,
                        SourceType = x.Type
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
                    .Select(text =>
                    {
                        
                        var code = BuildClassCode(text!);
                                                
                        return new DimClass
                        {
                            ClassCode = code,
                            ClassName = code
                        };
                    });

                var classesFromWeb = webReviews
                    .Select(w => w.ClassId)
                    .Distinct()
                    .Select(id =>
                    {
                        string code = id switch
                        {
                            1 => "NEU",
                            2 => "NEG",
                            3 => "POS",
                            _ => "UNK"
                        };

                        return new DimClass
                        {
                            ClassCode = code,
                            ClassName = code
                        };
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
                return "UNK"; 

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
