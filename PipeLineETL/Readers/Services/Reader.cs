using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

namespace Readers.Services
{
    public class Reader
    {
        private readonly string _filePath;
        public Reader(string filePath)
        {
            _filePath = filePath;
        }

        
        public void ReadFile<T>() where T : class
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine($"El archivo '{_filePath}' no existe.");
                return;
            }

            try
            {
                var extension = Path.GetExtension(_filePath).ToLowerInvariant();

                switch (extension)
                {
                    case ".csv":
                        PrintCsv<T>();
                        break;

                    case ".json":
                        ReadApi();
                        break;
                    default:
                        Console.WriteLine($"Formato no soportado: {extension}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error al leer el archivo: {ex.Message}");
            }
        }

        
        public List<T> ReadAll<T>() where T : class
        {
            var list = new List<T>();
            if (!File.Exists(_filePath)) return list;

            try
            {
                var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    Encoding = Encoding.UTF8,
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    BadDataFound = null
                };

                using var reader = new StreamReader(_filePath, Encoding.UTF8);
                using var csv = new CsvReader(reader, cfg);

                var dateOpts = csv.Context.TypeConverterOptionsCache.GetOptions<DateTime>();
                dateOpts.Formats = new[]
                {
                    "yyyy-MM-dd",
                    "M/d/yyyy",
                    "MM/dd/yyyy",
                    "d/M/yyyy",
                    "dd/MM/yyyy"
                };

                var dateOptsNullable = csv.Context.TypeConverterOptionsCache.GetOptions<DateTime?>();
                dateOptsNullable.Formats = dateOpts.Formats;

                list = csv.GetRecords<T>().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar CSV: {ex.Message}");
            }

            return list;
        }

        
        private void PrintCsv<T>() where T : class
        {
            var records = ReadAll<T>();
            foreach (var r in records)
                Console.WriteLine(r);
        }

        private void ReadApi()
        {
            throw new NotImplementedException();
        }

        private void ReadDb()
        {
            throw new NotImplementedException();
        }
    }
}
