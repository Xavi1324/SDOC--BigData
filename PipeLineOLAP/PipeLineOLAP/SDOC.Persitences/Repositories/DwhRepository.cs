using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SDOC.Application.Dto.DimDtos;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Application.Interfaces.IServices;
using SDOC.Application.Result;
using SDOC.Domain.Entities.Dwh.Dimensions;
using SDOC.Domain.Entities.Dwh.Facts;
using SDOC.Persitences.Context;
using System.Globalization;
using System.Linq;

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


                var timeDates = webReviews
                    .Select(w => w.Fecha.Date)
                    .Concat(surveys
                    .Select(s => s.Fecha.Date))
                    .Concat(socialComments
                    .Select(sc => sc.Date.Date))
                    .Distinct()
                    .ToList();

                var times = timeDates
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

        public async Task<ServiceResult> FactLoader(DimDtos dimDtos)
        {
            var result = new ServiceResult();

            try
            {
                var webReviews = dimDtos.WebReviews;
                var surveys = dimDtos.Surveys;
                var socialComments = dimDtos.SocialComments;

                // 1) Limpiar tabla de hechos
                await _context.Opiniones.ExecuteDeleteAsync();
                await _context.SaveChangesAsync();

                // =====================================================================
                // 2) LOOKUPS DE DIMENSIONES
                // =====================================================================

                var timeLookup = await _context.DimTimes
                    .AsNoTracking()
                    .ToDictionaryAsync(t => t.Date.Date, t => t.TimeSK);

                // mismo criterio de nombre de producto que en DimensionsLoader
                var productNameById = webReviews
                    .Select(w => new
                    {
                        w.ProductId,
                        ProductName = string.IsNullOrWhiteSpace(w.ProductName)
                            ? $"Producto {w.ProductId}"
                            : w.ProductName!.Trim()
                    })
                    .Distinct()
                    .ToDictionary(x => x.ProductId, x => x.ProductName);

                var productLookup = await _context.DimProducts
                    .AsNoTracking()
                    .ToDictionaryAsync(p => p.ProductName, p => p.ProductSK);

                // mismo criterio de nombre de cliente que en DimensionsLoader
                var clientNameById = webReviews
                    .Where(w => w.ClientId.HasValue)
                    .Select(w => new
                    {
                        Id = w.ClientId!.Value,
                        Name = string.IsNullOrWhiteSpace(w.ClientName)
                            ? $"Cliente {w.ClientId}"
                            : w.ClientName!.Trim()
                    })
                    .Distinct()
                    .ToDictionary(x => x.Id, x => x.Name);

                var clientLookup = await _context.DimClients
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.ClientName, c => c.ClientSK);

                // sources separados por tipo
                var allSources = await _context.DimSources
                    .AsNoTracking()
                    .ToListAsync();

                var sourceSurveyLookup = allSources
                    .Where(s => s.SourceType == "Survey")
                    .ToDictionary(s => s.SourceName, s => s.SourceSK);

                var sourceSocialLookup = allSources
                    .Where(s => s.SourceType == "Social")
                    .ToDictionary(s => s.SourceName, s => s.SourceSK);

                var sourceWebLookup = allSources
                    .Where(s => s.SourceType == "WebReview")
                    .ToDictionary(s => s.SourceName, s => s.SourceSK);

                var classLookup = await _context.DimClasses
                    .AsNoTracking()
                    .ToDictionaryAsync(c => c.ClassCode, c => c.ClassSk);

                // =====================================================================
                // 3) PROYECCIONES A FACT (SIN for / foreach)
                // =====================================================================

                // ---------- WEB REVIEWS ----------
                var factsFromWeb = webReviews
                    .Select(w => new
                    {
                        W = w,
                        Date = w.Fecha.Date,
                        ProductId = w.ProductId,
                        ClientId = w.ClientId,
                        SourceName = string.IsNullOrWhiteSpace(w.FuenteNombre)
                                        ? $"Source {w.FuenteId}"
                                        : w.FuenteNombre!.Trim(),
                        ClassCode = w.ClassId switch
                        {
                            1 => "NEU",
                            2 => "NEG",
                            3 => "POS",
                            _ => "UNK"
                        }
                    })
                    .Where(x =>
                           timeLookup.ContainsKey(x.Date)
                        && productNameById.ContainsKey(x.ProductId)
                        && x.ClientId.HasValue
                        && clientNameById.ContainsKey(x.ClientId.Value)
                        && sourceWebLookup.ContainsKey(x.SourceName)
                        && classLookup.ContainsKey(x.ClassCode))
                    .Select(x =>
                    {
                        var productName = productNameById[x.ProductId];
                        var clientName = clientNameById[x.ClientId!.Value];

                        return new FactOpinions
                        {
                            TimeSK = timeLookup[x.Date],
                            ProductSK = productLookup[productName],
                            ClientSK = clientLookup[clientName],
                            SourceSK = sourceWebLookup[x.SourceName],
                            ClassSk = (short)classLookup[x.ClassCode],
                            SatisfactionScore = 0
                        };
                    });

                // ---------- SURVEYS ----------
                var factsFromSurveys = surveys
                    .Select(s => new
                    {
                        S = s,
                        Date = s.Fecha.Date,
                        ProductId = s.IdProducto,
                        ClientId = s.IdCliente,
                        SourceName = (s.Fuente ?? string.Empty).Trim(),
                        ClassCode = BuildClassCode(s.Clasificacion ?? string.Empty)
                    })
                    .Where(x =>
                           timeLookup.ContainsKey(x.Date)
                        && productNameById.ContainsKey(x.ProductId)
                        && clientNameById.ContainsKey(x.ClientId)
                        && sourceSurveyLookup.ContainsKey(x.SourceName)
                        && classLookup.ContainsKey(x.ClassCode))
                    .Select(x =>
                    {
                        var productName = productNameById[x.ProductId];
                        var clientName = clientNameById[x.ClientId];

                        return new FactOpinions
                        {
                            TimeSK = timeLookup[x.Date],
                            ProductSK = productLookup[productName],
                            ClientSK = clientLookup[clientName],
                            SourceSK = sourceSurveyLookup[x.SourceName],
                            ClassSk = (short)classLookup[x.ClassCode],
                            SatisfactionScore = (short)x.S.PuntajeSatisfaccion
                        };
                    });

                // ---------- SOCIAL COMMENTS ----------
                var factsFromSocial = socialComments
                    .Select(sc => new
                    {
                        SC = sc,
                        Date = sc.Date.Date,
                        ProductId = sc.ProductId,
                        ClientId = sc.ClientId,
                        SourceName = (sc.Source ?? string.Empty).Trim(),
                        ClassCode = BuildClassCode(sc.ClassCode ?? string.Empty)
                    })
                    .Where(x =>
                           timeLookup.ContainsKey(x.Date)
                        && productNameById.ContainsKey(x.ProductId)
                        && x.ClientId.HasValue
                        && clientNameById.ContainsKey(x.ClientId.Value)
                        && sourceSocialLookup.ContainsKey(x.SourceName)
                        && classLookup.ContainsKey(x.ClassCode))
                    .Select(x =>
                    {
                        var productName = productNameById[x.ProductId];
                        var clientName = clientNameById[x.ClientId!.Value];

                        return new FactOpinions
                        {
                            TimeSK = timeLookup[x.Date],
                            ProductSK = productLookup[productName],
                            ClientSK = clientLookup[clientName],
                            SourceSK = sourceSocialLookup[x.SourceName],
                            ClassSk = (short)classLookup[x.ClassCode],
                            SatisfactionScore = 0
                        };
                    });

                // =====================================================================
                // 4) UNIR TODO Y GUARDAR
                // =====================================================================

                var allFacts = factsFromWeb
                    .Concat(factsFromSurveys)
                    .Concat(factsFromSocial)
                    .ToList();

                if (allFacts.Any())
                {
                    await _context.Opiniones.AddRangeAsync(allFacts);
                    await _context.SaveChangesAsync();
                }

                result.IsSuccess = true;
                result.Message = $"Hechos cargados correctamente. Total: {allFacts.Count}.";
            }
            catch (Exception ex)
            {
                var msg = "Error cargando los hechos del DWH SDOC";
                _logger.LogError(ex, msg);
                result.IsSuccess = false;

                await _errorNotificationService.NotifySourceErrorAsync(
                    sourceName: "DWH Fact Loader",
                    errorMessage: msg,
                    exception: ex);
            }

            return result;
        }

        private async Task<ServiceResult> CleanDimensionTables()
        {
            var result = new ServiceResult();

            try
            {
                await _context.Opiniones.ExecuteDeleteAsync();
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
