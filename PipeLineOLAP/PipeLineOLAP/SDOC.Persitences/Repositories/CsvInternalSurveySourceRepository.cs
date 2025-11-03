using CsvHelper;
using SDOC.Application.Interfaces.IRepository;
using SDOC.Domain.Entities.csv;
using System.Globalization;

namespace SDOC.Persitences.Repositories
{
    public class CsvInternalSurveySourceRepository : IInternalSurveySourceRepository
    {
        private readonly string _filePath;

        public CsvInternalSurveySourceRepository(string filePath)
        {
            _filePath = filePath;
        }

        public async Task<IReadOnlyList<SurveyCsv>> GetAllAsync()
        {
            using var reader = new StreamReader(_filePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            var records = csv.GetRecords<SurveyCsv>().ToList();
            return await Task.FromResult(records);
        }
    }
}
