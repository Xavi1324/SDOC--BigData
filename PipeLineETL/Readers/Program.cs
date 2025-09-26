using Readers.Services;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;
using Readers.Models.Csv;

namespace Readers
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Lector de Archivos (Extract -> Print -> Transform -> Save -> Load DW) ===");

            string basePath = @"C:\Proyecto\Archivo CSV Análisis de Opiniones de Clientes-20250923";
            string outputPath = Path.Combine(basePath, "output");

            if (!Directory.Exists(basePath))
            {
                Console.WriteLine($"La ruta no existe: {basePath}");
                return;
            }

            
            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            var transformer = new TransformService();

            
            var connStrDw = "Server = (localdb)\\LaptopXavier;Database=OpinionesClientesDW;Trusted_Connection=True;TrustServerCertificate=True;";
            var dw = new DwLoader(connStrDw);

            while (true)
            {
                var files = Directory.GetFiles(basePath, "*.csv", SearchOption.TopDirectoryOnly)
                                     .OrderBy(f => Path.GetFileName(f))
                                     .ToArray();

                if (files.Length == 0)
                {
                    Console.WriteLine("No se encontraron archivos .csv en la carpeta.");
                    return;
                }

                MostrarMenu(files);

                Console.Write("Selecciona una opción (número) o 0 para refrescar listado: ");
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Entrada vacía. Intenta de nuevo.");
                    continue;
                }

                if (input.Trim() == "0")
                {
                    Console.Clear();
                    continue;
                }

                if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Saliendo...");
                    break;
                }

                if (!int.TryParse(input, out int idx) || idx < 1 || idx > files.Length)
                {
                    Console.WriteLine("Opción inválida. Intenta de nuevo.");
                    continue;
                }

                var fullPath = files[idx - 1];
                var fileName = Path.GetFileName(fullPath);
                var fileKey = Path.GetFileNameWithoutExtension(fullPath).ToLowerInvariant();

                Console.WriteLine($"\n→ Archivo seleccionado: {fileName}\n");

                var reader = new Reader(fullPath);

                switch (fileKey)
                {
                    case "clients": reader.ReadFile<ClientCsv>(); break;
                    case "fuente_datos": reader.ReadFile<DataSourceCsv>(); break;
                    case "products": reader.ReadFile<ProductCsv>(); break;
                    case "social_comments": reader.ReadFile<SocialCommentCsv>(); break;
                    case "surveys_part1": reader.ReadFile<SurveyCsv>(); break;
                    case "web_reviews": reader.ReadFile<WebReviewCsv>(); break;
                    default:
                        Console.WriteLine($"No hay un modelo configurado para: {fileName}");
                        PostSelectionPrompt();
                        continue;
                }

                switch (fileKey)
                {
                    case "clients":
                        {
                            var raw = reader.ReadAll<ClientCsv>();
                            var res = transformer.CleanClients(raw);

                            var outPath = GetOutputPath(outputPath, fileName);
                            SaveCsv(outPath, res.Clean);
                            PrintTransformSummary("Clients", raw.Count, res.Clean.Count, res.Invalid.Count);

                            await dw.LoadClientsAsync(res.Clean);

                            Console.WriteLine($"Cargado a DW: {res.Clean.Count} clientes.\nSalida: {outPath}");
                            break;
                        }
                    case "fuente_datos":
                        {
                            var raw = reader.ReadAll<DataSourceCsv>();
                            var res = transformer.CleanDataSources(raw);

                            var outPath = GetOutputPath(outputPath, fileName);
                            SaveCsv(outPath, res.Clean);
                            PrintTransformSummary("DataSource", raw.Count, res.Clean.Count, res.Invalid.Count);

                            await dw.LoadDataSourcesAsync(res.Clean);

                            Console.WriteLine($"Cargado a DW: {res.Clean.Count} fuentes de datos.\nSalida: {outPath}");
                            break;
                        }
                    case "products":
                        {
                            var raw = reader.ReadAll<ProductCsv>();
                            var res = transformer.CleanProducts(raw);

                            var outPath = GetOutputPath(outputPath, fileName);
                            SaveCsv(outPath, res.Clean);
                            PrintTransformSummary("Products", raw.Count, res.Clean.Count, res.Invalid.Count);

                            await dw.LoadProductsAsync(res.Clean);

                            Console.WriteLine($"Cargado a DW: {res.Clean.Count} productos.\nSalida: {outPath}");
                            break;
                        }
                    case "social_comments":
                        {
                            var raw = reader.ReadAll<SocialCommentCsv>();
                            var cleaned = transformer.CleanSocialComments(raw);

                            var productsRaw = new Reader(Path.Combine(basePath, "products.csv")).ReadAll<ProductCsv>();
                            var productsCleanList = transformer.CleanProducts(productsRaw).Clean.ToList();
                            var productsCleanSet = productsCleanList.Select(p => p.IdProducto).ToHashSet();

                            var clientsRaw = new Reader(Path.Combine(basePath, "clients.csv")).ReadAll<ClientCsv>();
                            var clientsCleanList = transformer.CleanClients(clientsRaw).Clean.ToList();
                            var clientsCleanSet = clientsCleanList.Select(c => c.IdCliente).ToHashSet();

                            var validated = transformer.ValidateSocialFK(cleaned.Clean, clientsCleanSet, productsCleanSet);

                            var outPath = GetOutputPath(outputPath, fileName);
                            SaveCsv(outPath, validated.Clean);
                            PrintTransformSummary("SocialComments", raw.Count, validated.Clean.Count, cleaned.Invalid.Count + validated.Invalid.Count);

                            await dw.LoadProductsAsync(productsCleanList);
                            await dw.LoadClientsAsync(clientsCleanList);
                            await dw.LoadOpinionsFromSocialAsync(validated.Clean, productsCleanList, clientsCleanList);

                            Console.WriteLine($"Cargado a DW: {validated.Clean.Count} opiniones (social).\nSalida: {outPath}");
                            break;
                        }
                    case "surveys_part1":
                        {
                            var raw = reader.ReadAll<SurveyCsv>();
                            var cleaned = transformer.CleanSurveys(raw);

                            var productsRaw = new Reader(Path.Combine(basePath, "products.csv")).ReadAll<ProductCsv>();
                            var productsCleanList = transformer.CleanProducts(productsRaw).Clean.ToList();
                            var productsCleanSet = productsCleanList.Select(p => p.IdProducto).ToHashSet();

                            var clientsRaw = new Reader(Path.Combine(basePath, "clients.csv")).ReadAll<ClientCsv>();
                            var clientsCleanList = transformer.CleanClients(clientsRaw).Clean.ToList();
                            var clientsCleanSet = clientsCleanList.Select(c => c.IdCliente).ToHashSet();

                            var validated = transformer.ValidateSurveysFK(cleaned.Clean, clientsCleanSet, productsCleanSet);

                            var outPath = GetOutputPath(outputPath, fileName);
                            SaveCsv(outPath, validated.Clean);
                            PrintTransformSummary("Surveys", raw.Count, validated.Clean.Count, cleaned.Invalid.Count + validated.Invalid.Count);

                            await dw.LoadProductsAsync(productsCleanList);
                            await dw.LoadClientsAsync(clientsCleanList);
                            await dw.LoadOpinionsFromSurveysAsync(validated.Clean, productsCleanList, clientsCleanList);

                            Console.WriteLine($"Cargado a DW: {validated.Clean.Count} opiniones (surveys).\nSalida: {outPath}");
                            break;
                        }
                    case "web_reviews":
                        {
                            var raw = reader.ReadAll<WebReviewCsv>();
                            var cleaned = transformer.CleanWebReviews(raw);

                            var productsRaw = new Reader(Path.Combine(basePath, "products.csv")).ReadAll<ProductCsv>();
                            var productsCleanList = transformer.CleanProducts(productsRaw).Clean.ToList();
                            var productsCleanSet = productsCleanList.Select(p => p.IdProducto).ToHashSet();

                            var clientsRaw = new Reader(Path.Combine(basePath, "clients.csv")).ReadAll<ClientCsv>();
                            var clientsCleanList = transformer.CleanClients(clientsRaw).Clean.ToList();
                            var clientsCleanSet = clientsCleanList.Select(c => c.IdCliente).ToHashSet();

                            var validated = transformer.ValidateWebReviewsFK(cleaned.Clean, clientsCleanSet, productsCleanSet);

                            var outPath = GetOutputPath(outputPath, fileName);
                            SaveCsv(outPath, validated.Clean);
                            PrintTransformSummary("WebReviews", raw.Count, validated.Clean.Count, cleaned.Invalid.Count + validated.Invalid.Count);

                            await dw.LoadProductsAsync(productsCleanList);
                            await dw.LoadClientsAsync(clientsCleanList);
                            await dw.LoadOpinionsFromWebReviewsAsync(validated.Clean, productsCleanList, clientsCleanList);

                            Console.WriteLine($"Cargado a DW: {validated.Clean.Count} opiniones (web).\nSalida: {outPath}");
                            break;
                        }
                }

                PostSelectionPrompt();
            }
        }

        
        static void MostrarMenu(string[] files)
        {
            Console.WriteLine("\nArchivos encontrados:");
            for (int i = 0; i < files.Length; i++)
                Console.WriteLine($"{i + 1}. {Path.GetFileName(files[i])}");
            Console.WriteLine("0. Refrescar listado");
            Console.WriteLine("Q. Salir\n");
        }

        static void PostSelectionPrompt()
        {
            Console.WriteLine("\n---");
            Console.WriteLine("Presiona 0 para volver al listado y seleccionar otro archivo.");
            Console.WriteLine("Presiona cualquier otra tecla para salir.");
            var again = Console.ReadKey(intercept: true).KeyChar;
            Console.WriteLine();
            if (again == '0')
            {
                Console.Clear();
                return;
            }
            Environment.Exit(0);
        }

        static void PrintTransformSummary(string title, int read, int clean, int invalid)
        {
            Console.WriteLine($"\n[Transformación] {title}");
            Console.WriteLine($"Leídos: {read}  |  Limpios: {clean}  |  Descartados: {invalid}");
            Console.WriteLine($"Archivo transformado guardado en carpeta 'output/'.");
        }

        static string GetOutputPath(string outputDir, string originalFileName)
        {
            var name = Path.GetFileNameWithoutExtension(originalFileName);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm"); 
            return Path.Combine(outputDir, $"{name}_clean_{timestamp}.csv");
        }

        static void SaveCsv<T>(string path, IEnumerable<T> rows)
        {
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Encoding = Encoding.UTF8,
                HasHeaderRecord = true
            };

            using var writer = new StreamWriter(path, false, new UTF8Encoding(false));
            using var csv = new CsvWriter(writer, cfg);

            csv.WriteHeader<T>();
            csv.NextRecord();
            foreach (var r in rows)
            {
                csv.WriteRecord(r);
                csv.NextRecord();
            }
        }
    }
}
