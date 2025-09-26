using System.Data;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Readers.Models.Csv;

namespace Readers.Services
{
    public class DwLoader
    {
        private readonly string _connStr;

        // Caches (evitan roundtrips)
        private readonly Dictionary<int, int> _prodCacheByExternalId = new();            // ext Product.IdProducto -> dw.Producto.Product_Id
        private readonly Dictionary<string, int> _prodCacheByName = new(StringComparer.OrdinalIgnoreCase); // ProductName -> Product_Id
        private readonly Dictionary<string, int> _catCache = new(StringComparer.OrdinalIgnoreCase);        // Categoria.Nombre -> Categoria_Id

        private readonly Dictionary<(string name, string? email), int> _cliCache = new();
        private readonly Dictionary<string, int> _paisCache = new(StringComparer.OrdinalIgnoreCase);       // Pais.Nombre -> Pais_Id

        private readonly Dictionary<DateTime, int> _timeCache = new();                                     // DateOnly -> Time_Id

        private readonly Dictionary<string, int> _tipoFuenteCache = new(StringComparer.OrdinalIgnoreCase); // TipoFuente.Descripcion -> TipoFuente_Id
        private readonly Dictionary<string, int> _fuenteCache = new(StringComparer.OrdinalIgnoreCase);     // Fuente.Nombre -> Fuente_Id

        private readonly Dictionary<string, short> _classCache = new(StringComparer.OrdinalIgnoreCase);    // Clasificacion.Code -> Class_Id

        public DwLoader(string connStr) => _connStr = connStr;

        /* ===================== Helpers ===================== */

        private static string Sha256(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static string MapClasificacionToCode(string? clasif)
        {
            var v = (clasif ?? "").Trim().ToLower();
            return v switch
            {
                "positiva" or "positivo" or "pos" or "positive" => "POS",
                "negativa" or "negativo" or "neg" or "negative" => "NEG",
                "neutra" or "neutral" or "neu" => "NEU",
                _ => "NEU"
            };
        }

        private static string RatingToCode(int rating)
        {
            if (rating >= 4) return "POS";
            if (rating <= 2) return "NEG";
            return "NEU";
        }

        private static string InferTipoFuente(string? nameOrType)
        {
            var s = (nameOrType ?? "").ToLower();
            if (string.IsNullOrWhiteSpace(s)) return "Otro";
            if (s.Contains("twitter") || s.Contains("instagram") || s.Contains("facebook") || s.Contains("tiktok")) return "Social";
            if (s.Contains("amazon") || s.Contains("ebay") || s.Contains("mercado") || s.Contains("ecommerce") || s.Contains("webreviews")) return "Ecommerce";
            if (s.Contains("encuesta") || s.Contains("survey")) return "Encuesta";
            if (s == "api") return "API";
            if (s == "csv") return "CSV";
            if (s == "bd" || s.Contains("sql") || s.Contains("database")) return "BD";
            return "Otro";
        }

        private static string BuildHashKey(string fuenteNombre, int productId, int? clientId, DateTime dt, string comment, string classCode)
            => $"{fuenteNombre}|{productId}|{(clientId?.ToString() ?? "")}|{dt:yyyyMMddHH}|{comment}|{classCode}";

        /* ===================== Ensures: Dimensiones ===================== */

        private async Task<int> EnsureTiempoAsync(SqlConnection cn, SqlTransaction tx, DateTime date)
        {
            var d = date.Date;
            if (_timeCache.TryGetValue(d, out var id)) return id;

            const string sel = "SELECT Time_Id FROM dw.Tiempo WHERE [Date]=@Date;";
            id = await cn.ExecuteScalarAsync<int?>(sel, new { Date = d }, tx) ?? 0;
            if (id == 0)
            {
                const string ins = @"
INSERT INTO dw.Tiempo([Date],[Year],[Month],[Day],[Hour])
VALUES(@Date, YEAR(@Date), MONTH(@Date), DAY(@Date), NULL);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                id = await cn.ExecuteScalarAsync<int>(ins, new { Date = d }, tx);
            }
            _timeCache[d] = id;
            return id;
        }

        private async Task<int> EnsureCategoriaAsync(SqlConnection cn, SqlTransaction tx, string? raw)
        {
            var nombre = (raw ?? "Sin categoria").Trim();
            if (string.IsNullOrEmpty(nombre)) nombre = "Sin categoria";

            if (_catCache.TryGetValue(nombre, out var id)) return id;

            const string sel = "SELECT Categoria_Id FROM dw.Categoria WHERE Nombre=@Nombre;";
            id = await cn.ExecuteScalarAsync<int?>(sel, new { Nombre = nombre }, tx) ?? 0;
            if (id == 0)
            {
                const string ins = @"
INSERT INTO dw.Categoria(Nombre)
VALUES(@Nombre);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                id = await cn.ExecuteScalarAsync<int>(ins, new { Nombre = nombre }, tx);
            }
            _catCache[nombre] = id;
            return id;
        }

        private async Task<int> EnsureProductoAsync(SqlConnection cn, SqlTransaction tx, ProductCsv p)
        {
            // Por nombre (tu staging trae Nombre y Categoria)
            if (!string.IsNullOrWhiteSpace(p.Nombre) && _prodCacheByName.TryGetValue(p.Nombre, out var cachedId))
            {
                _prodCacheByExternalId[p.IdProducto] = cachedId;
                return cachedId;
            }

            var catId = await EnsureCategoriaAsync(cn, tx, p.Categoria);

            const string sel = "SELECT Product_Id FROM dw.Producto WHERE ProductName=@Name AND Categoria_Id=@CatId;";
            var id = await cn.ExecuteScalarAsync<int?>(sel, new { Name = p.Nombre, CatId = catId }, tx) ?? 0;

            if (id == 0)
            {
                const string ins = @"
INSERT INTO dw.Producto(ProductName, Categoria_Id)
VALUES(@Name, @CatId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                id = await cn.ExecuteScalarAsync<int>(ins, new { Name = p.Nombre, CatId = catId }, tx);
            }

            _prodCacheByExternalId[p.IdProducto] = id;
            _prodCacheByName[p.Nombre] = id;
            return id;
        }

        private async Task<int?> EnsurePaisAsync(SqlConnection cn, SqlTransaction tx, string? raw)
        {
            var nombre = (raw ?? "").Trim();
            if (string.IsNullOrEmpty(nombre)) return null;

            if (_paisCache.TryGetValue(nombre, out var id)) return id;

            const string sel = "SELECT Pais_Id FROM dw.Pais WHERE Nombre=@Nombre;";
            id = await cn.ExecuteScalarAsync<int?>(sel, new { Nombre = nombre }, tx) ?? 0;
            if (id == 0)
            {
                const string ins = @"
INSERT INTO dw.Pais(Nombre)
VALUES(@Nombre);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                id = await cn.ExecuteScalarAsync<int>(ins, new { Nombre = nombre }, tx);
            }
            _paisCache[nombre] = id;
            return id;
        }

        private async Task<int> EnsureClienteAsync(SqlConnection cn, SqlTransaction tx, ClientCsv c)
        {
            var key = (name: c.Nombre, email: string.IsNullOrWhiteSpace(c.Email) ? null : c.Email);
            if (_cliCache.TryGetValue(key, out var id)) return id;

            int? paisId = await EnsurePaisAsync(cn, tx, c.Country);

            const string sel = "SELECT Client_Id FROM dw.Cliente WHERE ClientName=@Name AND ISNULL(Email,'')=ISNULL(@Email,'');";
            id = await cn.ExecuteScalarAsync<int?>(sel, new { Name = c.Nombre, Email = c.Email }, tx) ?? 0;

            if (id == 0)
            {
                const string ins = @"
INSERT INTO dw.Cliente(ClientName, LastName, Email, Pais_Id)
VALUES(@Name, @LastName, @Email, @Pais_Id);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                id = await cn.ExecuteScalarAsync<int>(ins, new
                {
                    Name = c.Nombre,
                    LastName = string.IsNullOrWhiteSpace(c.Apellido) ? "" : c.Apellido, // si tu modelo tiene LastName
                    Email = c.Email,
                    Pais_Id = paisId
                }, tx);
            }
            else
            {
                const string upd = "UPDATE dw.Cliente SET ClientName=@Name, Pais_Id=@Pais_Id WHERE Client_Id=@Id;";
                await cn.ExecuteAsync(upd, new { Id = id, Name = c.Nombre, Pais_Id = paisId }, tx);
            }

            _cliCache[key] = id;
            return id;
        }

        private async Task<int> EnsureTipoFuenteAsync(SqlConnection cn, SqlTransaction tx, string? raw)
        {
            var desc = string.IsNullOrWhiteSpace(raw) ? "Otro" : raw.Trim();
            if (_tipoFuenteCache.TryGetValue(desc, out var id)) return id;

            const string sel = "SELECT TipoFuente_Id FROM dw.TipoFuente WHERE Descripcion=@Desc;";
            id = await cn.ExecuteScalarAsync<int?>(sel, new { Desc = desc }, tx) ?? 0;
            if (id == 0)
            {
                const string ins = @"
INSERT INTO dw.TipoFuente(Descripcion)
VALUES(@Desc);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                id = await cn.ExecuteScalarAsync<int>(ins, new { Desc = desc }, tx);
            }
            _tipoFuenteCache[desc] = id;
            return id;
        }

        private async Task<int> EnsureFuenteAsync(SqlConnection cn, SqlTransaction tx, string? nombreRaw, string? tipoRaw, string? url, DateTime? fecha)
        {
            var nombre = string.IsNullOrWhiteSpace(nombreRaw) ? "Fuente Desconocida" : nombreRaw.Trim();
            if (_fuenteCache.TryGetValue(nombre, out var id)) return id;

            var tipo = string.IsNullOrWhiteSpace(tipoRaw) ? InferTipoFuente(nombre) : tipoRaw.Trim();
            var tipoId = await EnsureTipoFuenteAsync(cn, tx, tipo);
            var fechaReg = (fecha ?? DateTime.UtcNow).Date;

            const string sel = "SELECT Fuente_Id FROM dw.Fuente WHERE Nombre=@Nombre;";
            id = await cn.ExecuteScalarAsync<int?>(sel, new { Nombre = nombre }, tx) ?? 0;
            if (id == 0)
            {
                const string ins = @"
INSERT INTO dw.Fuente(TipoFuente_Id, Nombre, UrlPath, FechaRegistro)
VALUES(@TipoFuente_Id, @Nombre, @UrlPath, @FechaRegistro);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                id = await cn.ExecuteScalarAsync<int>(ins, new
                {
                    TipoFuente_Id = tipoId,
                    Nombre = nombre,
                    UrlPath = string.IsNullOrWhiteSpace(url) ? null : url,
                    FechaRegistro = fechaReg
                }, tx);
            }
            _fuenteCache[nombre] = id;
            return id;
        }

        private async Task<short> EnsureClasificacionAsync(SqlConnection cn, SqlTransaction tx, string code)
        {
            if (_classCache.TryGetValue(code, out var id)) return id;

            const string sel = "SELECT Class_Id FROM dw.Clasificacion WHERE Code=@Code;";
            var found = await cn.ExecuteScalarAsync<short?>(sel, new { Code = code }, tx);
            if (found.HasValue)
            {
                _classCache[code] = found.Value;
                return found.Value;
            }

            // Si no existe, lo creamos con Nombre=Code
            const string ins = @"
INSERT INTO dw.Clasificacion(Code, Nombre)
VALUES(@Code, @Nombre);
SELECT CAST(SCOPE_IDENTITY() AS SMALLINT);";
            var newId = await cn.ExecuteScalarAsync<short>(ins, new { Code = code, Nombre = code }, tx);
            _classCache[code] = newId;
            return newId;
        }

        /* ===================== Insert Opinion ===================== */

        private async Task InsertOpinionIfNotExistsAsync(
            SqlConnection cn, SqlTransaction tx,
            int productId, int? clientId, int fuenteId, int timeId,
            string comment, string classCode, string fuenteNombreForHash, DateTime dateForHash)
        {
            var key = BuildHashKey(fuenteNombreForHash, productId, clientId, dateForHash, comment, classCode);
            var hash = Sha256(key);

            const string exists = "SELECT 1 FROM dw.Opinion WHERE HashUnique=@Hash;";
            var found = await cn.ExecuteScalarAsync<int?>(exists, new { Hash = hash }, tx);
            if (found.HasValue) return;

            var classId = await EnsureClasificacionAsync(cn, tx, classCode);

            const string ins = @"
INSERT INTO dw.Opinion(Product_Id, Client_Id, Fuente_Id, Time_Id, [Comment], Class_Id, HashUnique)
VALUES(@Product_Id, @Client_Id, @Fuente_Id, @Time_Id, @Comment, @Class_Id, @HashUnique);";

            await cn.ExecuteAsync(ins, new
            {
                Product_Id = productId,
                Client_Id = clientId,
                Fuente_Id = fuenteId,
                Time_Id = timeId,
                Comment = comment,
                Class_Id = classId,
                HashUnique = hash
            }, tx);
        }

        /* ===================== Public Loaders ===================== */

        public async Task LoadProductsAsync(IEnumerable<ProductCsv> products)
        {
            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();
            foreach (var p in products)
                await EnsureProductoAsync(cn, tx, p);
            await tx.CommitAsync();
        }

        public async Task LoadClientsAsync(IEnumerable<ClientCsv> clients)
        {
            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();
            foreach (var c in clients)
                await EnsureClienteAsync(cn, tx, c);
            await tx.CommitAsync();
        }

        // Carga catálogo TipoFuente y tabla Fuente a partir de DataSource
        public async Task LoadDataSourcesAsync(IEnumerable<DataSourceCsv> sources)
        {
            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();

            foreach (var s in sources)
            {
                var tipo = string.IsNullOrWhiteSpace(s.TipoFuente) ? InferTipoFuente(s.NombreFuente ?? s.IdFuente) : s.TipoFuente!;
                await EnsureTipoFuenteAsync(cn, tx, tipo);
                await EnsureFuenteAsync(cn, tx,
                    nombreRaw: s.NombreFuente ?? s.IdFuente ?? "Fuente Desconocida",
                    tipoRaw: tipo,
                    url: s.UrlPath,
                    fecha: s.FechaCarga);
            }

            await tx.CommitAsync();
        }

        public async Task LoadOpinionsFromSurveysAsync(
            IEnumerable<SurveyCsv> surveys,
            IEnumerable<ProductCsv> products,
            IEnumerable<ClientCsv> clients)
        {
            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();

            // Asegurar dimensiones mínimas
            foreach (var p in products) await EnsureProductoAsync(cn, tx, p);
            foreach (var c in clients) await EnsureClienteAsync(cn, tx, c);

            foreach (var s in surveys)
            {
                // Productos/Clientes por ID externo
                var prodId = await EnsureProductoAsync(cn, tx, new ProductCsv { IdProducto = s.IdProducto, Nombre = $"Producto_{s.IdProducto}", Categoria = "Sin categoria" });
                var cliId = await EnsureClienteAsync(cn, tx, new ClientCsv { IdCliente = s.IdCliente, Nombre = $"Cliente_{s.IdCliente}", Email = $"cliente{s.IdCliente}@mail.com", Country = null });

                var timeId = await EnsureTiempoAsync(cn, tx, s.Fecha);

                // Fuente y tipo
                var fuenteNombre = string.IsNullOrWhiteSpace(s.Fuente) ? "Encuesta" : s.Fuente!;
                var tipo = "Encuesta";
                var fuenteId = await EnsureFuenteAsync(cn, tx, fuenteNombre, tipo, url: null, fecha: s.Fecha);

                // Clasificación
                var code = MapClasificacionToCode(s.Clasificacion);

                await InsertOpinionIfNotExistsAsync(cn, tx, prodId, cliId, fuenteId, timeId, s.Comentario, code, fuenteNombre, s.Fecha);
            }

            await tx.CommitAsync();
        }

        public async Task LoadOpinionsFromWebReviewsAsync(
            IEnumerable<WebReviewCsv> reviews,
            IEnumerable<ProductCsv> products,
            IEnumerable<ClientCsv> clients)
        {
            int ToInt(string s) => int.TryParse(new string((s ?? "").Where(char.IsDigit).ToArray()), out var n) ? n : 0;

            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();

            foreach (var p in products) await EnsureProductoAsync(cn, tx, p);
            foreach (var c in clients) await EnsureClienteAsync(cn, tx, c);

            foreach (var r in reviews)
            {
                var pIdInt = ToInt(r.IdProducto);
                var cIdInt = ToInt(r.IdCliente);

                var prodId = await EnsureProductoAsync(cn, tx, new ProductCsv { IdProducto = pIdInt, Nombre = $"Producto_{pIdInt}", Categoria = "Sin categoria" });
                var cliId = await EnsureClienteAsync(cn, tx, new ClientCsv { IdCliente = cIdInt, Nombre = $"Cliente_{cIdInt}", Email = $"cliente{cIdInt}@mail.com" });

                var timeId = await EnsureTiempoAsync(cn, tx, r.Fecha);

                var fuenteNombre = "WebReviews";
                var fuenteId = await EnsureFuenteAsync(cn, tx, fuenteNombre, "Ecommerce", url: null, fecha: r.Fecha);

                var code = RatingToCode(r.Rating);

                await InsertOpinionIfNotExistsAsync(cn, tx, prodId, cliId, fuenteId, timeId, r.Comentario, code, fuenteNombre, r.Fecha);
            }

            await tx.CommitAsync();
        }

        public async Task LoadOpinionsFromSocialAsync(
            IEnumerable<SocialCommentCsv> social,
            IEnumerable<ProductCsv> products,
            IEnumerable<ClientCsv> clients)
        {
            int ToInt(string? s) => int.TryParse(new string((s ?? "").Where(char.IsDigit).ToArray()), out var n) ? n : 0;

            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();

            foreach (var p in products) await EnsureProductoAsync(cn, tx, p);
            foreach (var c in clients) await EnsureClienteAsync(cn, tx, c);

            foreach (var s in social)
            {
                var pIdInt = ToInt(s.IdProducto);
                var cIdInt = string.IsNullOrWhiteSpace(s.IdCliente) ? 0 : ToInt(s.IdCliente);

                var prodId = await EnsureProductoAsync(cn, tx, new ProductCsv { IdProducto = pIdInt, Nombre = $"Producto_{pIdInt}", Categoria = "Sin categoria" });

                int? cliId = null;
                if (cIdInt > 0)
                    cliId = await EnsureClienteAsync(cn, tx, new ClientCsv { IdCliente = cIdInt, Nombre = $"Cliente_{cIdInt}", Email = $"cliente{cIdInt}@mail.com" });

                var timeId = await EnsureTiempoAsync(cn, tx, s.Fecha);

                var fuenteNombre = string.IsNullOrWhiteSpace(s.Fuente) ? "Twitter" : s.Fuente!;
                var tipo = InferTipoFuente(fuenteNombre);
                var fuenteId = await EnsureFuenteAsync(cn, tx, fuenteNombre, tipo, url: null, fecha: s.Fecha);

                var code = "NEU"; // sin NLP, asumimos neutro

                await InsertOpinionIfNotExistsAsync(cn, tx, prodId, cliId, fuenteId, timeId, s.Comentario, code, fuenteNombre, s.Fecha);
            }

            await tx.CommitAsync();
        }
    }
}
