using Microsoft.Extensions.Logging;
using SDOC.Application.Dto.DimDtos;
using SDOC.Application.Dto.Sources;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Application.Interfaces.IServices;
using SDOC.Application.Result;
using SDOC.Domain.Entities.Api;
using SDOC.Domain.Entities.csv;
using SDOC.Domain.Entities.DB;

namespace SDOC.Application.Services
{
    public class HandlerService : IHandlerService
    {
        private readonly IApiSocialCommentSourceRepository _apiRepo;
        private readonly ICsvInternalSurveySourceRepository _csvRepo;
        private readonly IWebReviewSourceRepository _webRepo;
        private readonly IDwhRepository _dwhRepository;
        private readonly ILogger<HandlerService> _logger;
        private readonly IErrorNotificationService _errorNotifier;

        public HandlerService(
            IApiSocialCommentSourceRepository apiRepo,
            ICsvInternalSurveySourceRepository csvRepo,
            IWebReviewSourceRepository webRepo,
            IDwhRepository dwhRepository,
            ILogger<HandlerService> logger,
            IErrorNotificationService errorNotifier)
        {
            _apiRepo = apiRepo;
            _csvRepo = csvRepo;
            _webRepo = webRepo;
            _dwhRepository = dwhRepository;
            _logger = logger;
            _errorNotifier = errorNotifier;
        }

        public async Task<ServiceResult> DataHandler()
        {
            var result = new ServiceResult();

            try
            {
                _logger.LogInformation("Iniciando proceso SDOC - lectura de fuentes...");


                var webReviewsEntities = await ExecuteWithErrorEmailAsync(
            () => _webRepo.ReadAsync(), "WebReviews DB");

                var surveysEntities = await ExecuteWithErrorEmailAsync(
                    () => _csvRepo.ReadAsync(), "Internal Survey CSV");

                var socialCommentsEntities = await ExecuteWithErrorEmailAsync(
                    () => _apiRepo.ReadAsync(), "SocialComments API");

                var webReviewDtos = webReviewsEntities
                    .Select(w => new WebReviewDbDto
                    {
                        OpinionId = w.OpinionId,
                        ProductId = w.ProductId,
                        ClientId = w.ClientId,
                        FuenteId = w.FuenteId,
                        TimeId = w.TimeId,
                        Comment = w.Comment ?? string.Empty,
                        ClassId = w.ClassId,

                        ProductName = w.ProductName ?? $"Producto {w.ProductId}",
                        CategoryId = w.CategoryId,
                        CategoryName = w.CategoryName ?? "Sin categoría",

                        ClientName = w.ClientName ?? $"Cliente {w.ClientId ?? 0}",
                        LastName = w.LastName ?? string.Empty,
                        Email = w.Email ?? string.Empty,

                        FuenteNombre = w.FuenteNombre ?? $"Fuente {w.FuenteId}",
                        TipoFuenteDesc = w.TipoFuenteDesc ?? "Desconocido",

                        Fecha = w.Fecha,
                        ClassCode = w.ClassCode ?? string.Empty
                    })
                    .ToList();


                var surveyDtos = surveysEntities
                    .Select(s => new SurveyCsvDto
                    {
                        IdOpinion = s.IdOpinion,
                        IdCliente = s.IdCliente,
                        IdProducto = s.IdProducto,
                        Fecha = s.Fecha,
                        Comentario = s.Comentario,
                        Clasificacion = s.Clasificación,          
                        PuntajeSatisfaccion = s.PuntajeSatisfacción,
                        Fuente = s.Fuente
                    })
                    .ToList();

                var socialCommentDtos = socialCommentsEntities
                    .Select(sc => new SocialCommentApiDto
                    {
                        OpinionId = sc.OpinionId,
                        ClientId = sc.IdClient,
                        ProductId = sc.IdProduct,
                        Source = sc.Source,
                        Comment = sc.Comment
                    })
                    .ToList();

                
                var dimDtos = new DimDtos
                {
                    WebReviews = webReviewDtos,
                    Surveys = surveyDtos,
                    SocialComments = socialCommentDtos
                };

                _logger.LogInformation("Iniciando carga de dimensiones en el DWH...");


                result = await CargarDwhAsync(dimDtos);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Dimensiones cargadas correctamente en el DWH. Mensaje: {Message}", result.Message);
                }
                else
                {
                    _logger.LogError("Error al cargar dimensiones en el DWH: {Message}", result.Message);

                    await _errorNotifier.NotifySourceErrorAsync(
                        sourceName: "DWH Loader",
                        errorMessage: result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en DataHandler.");

                result.IsSuccess = false;
                result.Message = "Error procesando los datos de SDOC.";

                await _errorNotifier.NotifySourceErrorAsync(
                    sourceName: "DataHandler (Proceso completo)",
                    errorMessage: ex.Message,
                    exception: ex);
            }

            return result;
        }
        private async Task<ServiceResult> CargarDwhAsync(DimDtos dimDtos)
        {
            var dimResult = await _dwhRepository.DimensionsLoader(dimDtos);
            if (!dimResult.IsSuccess)
                return dimResult;

            var factResult = await _dwhRepository.FactLoader(dimDtos);
            return factResult;
        }


        private async Task<IEnumerable<T>> ExecuteWithErrorEmailAsync<T>(
            Func<Task<IEnumerable<T>>> operation,
            string sourceName)
        {
            try
            {
                var data = await operation();

                _logger.LogInformation(
                    "Se leyeron {Count} registros desde la fuente {SourceName}.",
                    data.Count(),
                    sourceName
                );

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer datos desde la fuente {SourceName}.", sourceName);

                await _errorNotifier.NotifySourceErrorAsync(
                    sourceName: sourceName,
                    errorMessage: ex.Message,
                    exception: ex);

                throw;
            }
        }
    }
}
