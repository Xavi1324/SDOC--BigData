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

        // Caches para reducir roundtrips
        private readonly Dictionary<int, int> _prodCacheByExternalId = new();                       // ext Product.IdProducto -> dw.Producto.Product_Id
        private readonly Dictionary<(string name, int catId), int> _prodCacheByNameCat = new();     // (ProductName, Categoria_Id) -> Product_Id
        private readonly Dictionary<string, int> _catCache = new(StringComparer.OrdinalIgnoreCase); // Categoria.Nombre -> Categoria_Id

        private readonly Dictionary<(string? email, string first, string last), int> _cliCache = new();
        private readonly Dictionary<DateTime, int> _timeCache = new();                              // Date.Date -> Time_Id

        private readonly Dictionary<string, int> _tipoFuenteCache = new(StringComparer.OrdinalIgnoreCase);              // Descripcion -> TipoFuente_Id
        private readonly Dictionary<(int tipoId, string nombre), int> _fuenteCache = new();                              // (TipoFuente_Id, Nombre) -> Fuente_Id

        private readonly Dictionary<string, short> _classCache = new(StringComparer.OrdinalIgnoreCase);                 // Code -> Class_Id

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
            var v = (clasif ?? "").Trim().ToLowerInvariant();
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
            var s = (nameOrType ?? "").ToLowerInvariant();
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

        private static (string first, string last) SplitName(string full)
        {
            var parts = (full ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return ("N/A", "N/A");
            if (parts.Length == 1) return (parts[0], "N/A");
            return (string.Join(' ', parts[..^1]), parts[^1]);
        }

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
            var nombre = string.IsNullOrWhiteSpace(raw) ? "Sin categoría" : raw.Trim();
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
            // Usa cache por id externo si existe
            if (_prodCacheByExternalId.TryGetValue(p.IdProducto, out var idCached))
                return idCached;

            var catId = await EnsureCategoriaAsync(cn, tx, p.Categoria ?? "Sin categoría");

            // Cache por (Nombre, Categoria_Id)
            var key = (name: p.Nombre.Trim(), catId);
            if (_prodCacheByNameCat.TryGetValue(key, out var idByName))
            {
                _prodCacheByExternalId[p.IdProducto] = idByName;
                return idByName;
            }

            const string sel = "SELECT Product_Id FROM dw.Producto WHERE ProductName=@Name AND Categoria_Id=@CatId;";
            var id = await cn.ExecuteScalarAsync<int?>(sel, new { Name = p.Nombre.Trim(), CatId = catId }, tx) ?? 0;

            if (id == 0)
            {
                const string ins = @"
INSERT INTO dw.Producto(ProductName, Categoria_Id)
VALUES(@Name, @CatId);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                id = await cn.ExecuteScalarAsync<int>(ins, new { Name = p.Nombre.Trim(), CatId = catId }, tx);
            }

            _prodCacheByExternalId[p.IdProducto] = id;
            _prodCacheByNameCat[key] = id;
            return id;
        }

        private async Task<int> EnsureClienteAsync(SqlConnection cn, SqlTransaction tx, ClientCsv c)
        {
            var (first, last) = SplitName(c.Nombre);
            var email = string.IsNullOrWhiteSpace(c.Email) ? null : c.Email.Trim().ToLowerInvariant();
            var cacheKey = (email, first, last);

            if (_cliCache.TryGetValue(cacheKey, out var cached)) return cached;

            
            if (!string.IsNullOrWhiteSpace(email))
            {
                const string selEmail = "SELECT Client_Id FROM dw.Cliente WHERE Email=@Email;";
                var idByEmail = await cn.ExecuteScalarAsync<int?>(selEmail, new { Email = email }, tx) ?? 0;
                if (idByEmail > 0)
                {
                    // opcionalmente refrescar nombre
                    const string upd = "UPDATE dw.Cliente SET ClientName=@First, LastName=@Last WHERE Client_Id=@Id;";
                    await cn.ExecuteAsync(upd, new { Id = idByEmail, First = first, Last = last }, tx);
                    _cliCache[cacheKey] = idByEmail;
                    return idByEmail;
                }
            }

            // 2) Fallback: por (ClientName, LastName) con Email NULL
            const string selNames = "SELECT Client_Id FROM dw.Cliente WHERE ClientName=@First AND LastName=@Last AND ((Email IS NULL AND @Email IS NULL) OR Email=@Email);";
            var id = await cn.ExecuteScalarAsync<int?>(selNames, new { First = first, Last = last, Email = email }, tx) ?? 0;

            if (id == 0)
            {
                const string ins = @"
INSERT INTO dw.Cliente(ClientName, LastName, Email, Pais_Id)
VALUES(@First, @Last, @Email, @Pais_Id);
SELECT CAST(SCOPE_IDENTITY() AS INT);";
                id = await cn.ExecuteScalarAsync<int>(ins, new
                {
                    First = first,
                    Last = last,
                    Email = email,
                    Pais_Id = (int?)null
                }, tx);
            }

            _cliCache[cacheKey] = id;
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
            var tipo = string.IsNullOrWhiteSpace(tipoRaw) ? InferTipoFuente(nombre) : tipoRaw.Trim();
            var tipoId = await EnsureTipoFuenteAsync(cn, tx, tipo);
            var fechaReg = (fecha ?? DateTime.UtcNow).Date;

            var key = (tipoId, nombre);
            if (_fuenteCache.TryGetValue(key, out var idCached)) return idCached;

            const string sel = "SELECT Fuente_Id FROM dw.Fuente WHERE TipoFuente_Id=@Tipo AND Nombre=@Nombre;";
            var id = await cn.ExecuteScalarAsync<int?>(sel, new { Tipo = tipoId, Nombre = nombre }, tx) ?? 0;
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

            _fuenteCache[key] = id;
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

            const string ins = @"
INSERT INTO dw.Clasificacion(Code, Nombre)
VALUES(@Code, @Nombre);
SELECT CAST(SCOPE_IDENTITY() AS SMALLINT);";
            var newId = await cn.ExecuteScalarAsync<short>(ins, new { Code = code, Nombre = code }, tx);
            _classCache[code] = newId;
            return newId;
        }

        

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

        // Carga catálogo TipoFuente y tabla Fuente a partir de DataSourceCsv
        public async Task LoadDataSourcesAsync(IEnumerable<DataSourceCsv> sources)
        {
            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();

            foreach (var s in sources)
            {
                var tipo = string.IsNullOrWhiteSpace(s.TipoFuente) ? InferTipoFuente(s.IdFuente) : s.TipoFuente!;
                await EnsureTipoFuenteAsync(cn, tx, tipo);
                await EnsureFuenteAsync(cn, tx,
                    nombreRaw: s.IdFuente,
                    tipoRaw: tipo,
                    url: null,
                    fecha: s.FechaCarga);
            }

            await tx.CommitAsync();
        }

        public async Task LoadOpinionsFromSurveysAsync(
            IEnumerable<SurveyCsv> surveys,
            IEnumerable<ProductCsv> products,
            IEnumerable<ClientCsv> clients)
        {
            // Index auxiliares para resolver por Id externo real (sin inventar)
            var prodById = products.ToDictionary(p => p.IdProducto, p => p);
            var cliById = clients.ToDictionary(c => c.IdCliente, c => c);

            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();

            // Asegurar maestros
            foreach (var p in products) await EnsureProductoAsync(cn, tx, p);
            foreach (var c in clients) await EnsureClienteAsync(cn, tx, c);

            foreach (var s in surveys)
            {
                if (!prodById.TryGetValue(s.IdProducto, out var pCsv)) continue; // sin producto maestro -> omite
                var prodId = await EnsureProductoAsync(cn, tx, pCsv);

                int? cliId = null;
                if (cliById.TryGetValue(s.IdCliente, out var cCsv))
                    cliId = await EnsureClienteAsync(cn, tx, cCsv);

                var timeId = await EnsureTiempoAsync(cn, tx, s.Fecha);

                var fuenteNombre = string.IsNullOrWhiteSpace(s.Fuente) ? "Encuesta" : s.Fuente.Trim();
                var fuenteId = await EnsureFuenteAsync(cn, tx, fuenteNombre, "Encuesta", url: null, fecha: s.Fecha);

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
            // Diccionarios para buscar por id externo
            var prodById = products.ToDictionary(p => p.IdProducto, p => p);
            var cliById = clients.ToDictionary(c => c.IdCliente, c => c);

            static int ToInt(string s) => int.TryParse(new string((s ?? "").Where(char.IsDigit).ToArray()), out var n) ? n : 0;

            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();

            // Asegurar maestros base
            foreach (var p in products) await EnsureProductoAsync(cn, tx, p);
            foreach (var c in clients) await EnsureClienteAsync(cn, tx, c);

            foreach (var r in reviews)
            {
                var pIdExt = ToInt(r.IdProducto);
                if (pIdExt == 0 || !prodById.TryGetValue(pIdExt, out var pCsv)) continue;
                var prodId = await EnsureProductoAsync(cn, tx, pCsv);

                int? cliId = null;
                var cIdExt = ToInt(r.IdCliente);
                if (cIdExt > 0 && cliById.TryGetValue(cIdExt, out var cCsv))
                    cliId = await EnsureClienteAsync(cn, tx, cCsv);

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
            var prodById = products.ToDictionary(p => p.IdProducto, p => p);
            var cliById = clients.ToDictionary(c => c.IdCliente, c => c);

            static int ToInt(string? s) => int.TryParse(new string((s ?? "").Where(char.IsDigit).ToArray()), out var n) ? n : 0;

            await using var cn = new SqlConnection(_connStr);
            await cn.OpenAsync();
            using var tx = cn.BeginTransaction();

            foreach (var p in products) await EnsureProductoAsync(cn, tx, p);
            foreach (var c in clients) await EnsureClienteAsync(cn, tx, c);

            foreach (var s in social)
            {
                var pIdExt = ToInt(s.IdProducto);
                if (pIdExt == 0 || !prodById.TryGetValue(pIdExt, out var pCsv)) continue;
                var prodId = await EnsureProductoAsync(cn, tx, pCsv);

                int? cliId = null;
                var cIdExt = ToInt(s.IdCliente);
                if (cIdExt > 0 && cliById.TryGetValue(cIdExt, out var cCsv))
                    cliId = await EnsureClienteAsync(cn, tx, cCsv);

                var timeId = await EnsureTiempoAsync(cn, tx, s.Fecha);

                var fuenteNombre = string.IsNullOrWhiteSpace(s.Fuente) ? "Social" : s.Fuente.Trim();
                var tipo = InferTipoFuente(fuenteNombre);
                var fuenteId = await EnsureFuenteAsync(cn, tx, fuenteNombre, tipo, url: null, fecha: s.Fecha);

                var code = "NEU"; // baseline sin NLP

                await InsertOpinionIfNotExistsAsync(cn, tx, prodId, cliId, fuenteId, timeId, s.Comentario, code, fuenteNombre, s.Fecha);
            }

            await tx.CommitAsync();
        }
    }
}
