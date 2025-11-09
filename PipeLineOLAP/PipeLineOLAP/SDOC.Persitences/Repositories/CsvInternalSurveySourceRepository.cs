using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Domain.Entities.csv;
using System.Globalization;

namespace SDOC.Persitences.Repositories
{
    public class CsvInternalSurveySourceRepository : ICsvInternalSurveySourceRepository
    {
        private readonly string _filePath;
        private readonly ILogger<CsvInternalSurveySourceRepository> _logger;

        public CsvInternalSurveySourceRepository(IConfiguration configuration, ILogger<CsvInternalSurveySourceRepository> logger)
        {
            
            _filePath = configuration["CsvPaths:InternalSurvey"] ?? throw new ArgumentException("No se encontró la ruta CSV en CsvPaths:InternalSurvey");
            _logger = logger;
        }

        public async Task<IEnumerable<SurveyCsv>> ReadAsync()
        {
            _logger.LogInformation("Leyendo encuestas internas desde el archivo CSV '{FilePath}'", _filePath);
            try
            {
                var result = new List<SurveyCsv>();

                using var reader = new StreamReader(_filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                await foreach (var record in csv.GetRecordsAsync<SurveyCsv>())
                {
                    result.Add(record);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer encuestas internas desde el archivo CSV '{FilePath}'", _filePath);
                throw;
            }
        }


    }
}
