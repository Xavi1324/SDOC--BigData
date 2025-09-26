using Readers.Models.Csv;
using System.Globalization;
using System.Text;

namespace Readers.Services
{
    // ----------------------------------------------------------------------
    // Resultado estándar: no se cambia el contrato que ya usas en tu pipeline
    // ----------------------------------------------------------------------
    public class TransformResult<T>
    {
        public List<T> Clean { get; set; } = new();
        public List<T> Invalid { get; set; } = new();
    }

    public static class TransformHelpers
    {
        // ---------- Normalización base ----------
        public static string CleanText(string? s) => (s ?? string.Empty).Trim();

        public static string ToTitle(string? s)
        {
            var v = CleanText(s);
            return string.IsNullOrEmpty(v)
                ? string.Empty
                : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(v.ToLower());
        }

        public static string RemoveDiacritics(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var norm = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(norm.Length);
            foreach (var ch in norm)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Mantengo tu helper para devolver solo la parte de fecha:
        public static DateTime DateOnly(DateTime dt) => dt.Date;

        // ---------- Emails, dígitos, clamps, longitudes ----------
        public static string NormalizeEmail(string? email)
        {
            var e = CleanText(email);
            return string.IsNullOrWhiteSpace(e) ? string.Empty : e.ToLowerInvariant();
        }

        public static string DigitsOnly(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var digits = new string(raw.Where(char.IsDigit).ToArray());
            return digits;
        }

        public static int DigitsOnlyToInt(string? raw)
            => int.TryParse(DigitsOnly(raw), out var n) ? n : 0;

        public static int Clamp(int value, int min, int max)
            => value < min ? min : (value > max ? max : value);

        public static string SanitizeLen(string? s, int max)
        {
            var v = CleanText(s);
            if (v.Length <= max) return v;
            return v[..max];
        }

        // ---------- Normalizadores de dominio ----------
        public static string NormalizeCategoria(string? cat)
        {
            var v = CleanText(cat);
            if (string.IsNullOrWhiteSpace(v)) return "Sin categoría";
            v = RemoveDiacritics(v);
            v = ToTitle(v);
            return SanitizeLen(v, 100); // dw.Categoria.Nombre VARCHAR(100)
        }

        public static string NormalizeProductName(string? name)
        {
            var v = ToTitle(name);
            return SanitizeLen(v, 150); // dw.Producto.ProductName VARCHAR(150)
        }

        public static string NormalizeClientName(string? name)
        {
            var v = ToTitle(name);
            return SanitizeLen(v, 100); // dw.Cliente.ClientName/LastName VARCHAR(100) (split en carga)
        }

        public static string NormalizeFuente(string? fuente)
        {
            var v = ToTitle(fuente);
            return SanitizeLen(v, 100); // dw.Fuente.Nombre, dw.TipoFuente.Descripcion VARCHAR(100)
        }
    }

    public class TransformService
    {
        
        public TransformResult<ClientCsv> CleanClients(IEnumerable<ClientCsv> rows)
        {
            var normalized = rows.Select(c => new ClientCsv
            {
                IdCliente = c.IdCliente,
                Nombre = TransformHelpers.NormalizeClientName(c.Nombre),
                Email = TransformHelpers.NormalizeEmail(c.Email)
            });

            var res = new TransformResult<ClientCsv>();
            var groups = normalized
                .Where(c => c.IdCliente > 0 && !string.IsNullOrWhiteSpace(c.Nombre))
                .GroupBy(c => c.IdCliente);

            foreach (var g in groups)
            {
                var first = g.First();
                res.Clean.Add(first);
                if (g.Count() > 1) res.Invalid.AddRange(g.Skip(1));
            }
            return res;
        }

        
        public TransformResult<ProductCsv> CleanProducts(IEnumerable<ProductCsv> rows)
        {
            var normalized = rows.Select(p => new ProductCsv
            {
                IdProducto = p.IdProducto,
                Nombre = TransformHelpers.NormalizeProductName(p.Nombre),
                Categoria = TransformHelpers.NormalizeCategoria(p.Categoria)
            });

            var res = new TransformResult<ProductCsv>();
            var groups = normalized.Where(p => p.IdProducto > 0 && !string.IsNullOrWhiteSpace(p.Nombre))
                                   .GroupBy(p => p.IdProducto);
            foreach (var g in groups)
            {
                var first = g.First();
                res.Clean.Add(first);
                if (g.Count() > 1) res.Invalid.AddRange(g.Skip(1));
            }
            return res;
        }

       
        public TransformResult<DataSourceCsv> CleanDataSources(IEnumerable<DataSourceCsv> rows)
        {
            var normalized = rows.Select(f => new DataSourceCsv
            {
                IdFuente = TransformHelpers.SanitizeLen(TransformHelpers.CleanText(f.IdFuente), 100),
                TipoFuente = TransformHelpers.NormalizeFuente(f.TipoFuente),
                FechaCarga = TransformHelpers.DateOnly(f.FechaCarga)
            });

            var res = new TransformResult<DataSourceCsv>();
            var groups = normalized.Where(f => !string.IsNullOrWhiteSpace(f.IdFuente))
                                   .GroupBy(f => f.IdFuente);
            foreach (var g in groups)
            {
                var first = g.First();
                res.Clean.Add(first);
                if (g.Count() > 1) res.Invalid.AddRange(g.Skip(1));
            }
            return res;
        }

        
        public TransformResult<SocialCommentCsv> CleanSocialComments(IEnumerable<SocialCommentCsv> rows)
        {
            var normalized = rows.Select(s => new SocialCommentCsv
            {
                IdComment = TransformHelpers.SanitizeLen(TransformHelpers.CleanText(s.IdComment), 100), // tamaño prudente
                IdCliente = string.IsNullOrWhiteSpace(s.IdCliente) ? null : TransformHelpers.DigitsOnly(s.IdCliente),
                IdProducto = TransformHelpers.DigitsOnly(s.IdProducto),
                Fuente = TransformHelpers.NormalizeFuente(s.Fuente),
                Fecha = TransformHelpers.DateOnly(s.Fecha),
                Comentario = TransformHelpers.CleanText(s.Comentario)
            });

            var res = new TransformResult<SocialCommentCsv>();
            var groups = normalized
                .Where(s => !string.IsNullOrWhiteSpace(s.IdComment) && !string.IsNullOrWhiteSpace(s.IdProducto))
                .GroupBy(s => s.IdComment);

            foreach (var g in groups)
            {
                var first = g.First();
                res.Clean.Add(first);
                if (g.Count() > 1) res.Invalid.AddRange(g.Skip(1));
            }
            return res;
        }

        
        public TransformResult<SurveyCsv> CleanSurveys(IEnumerable<SurveyCsv> rows)
        {
            var normalized = rows.Select(s => new SurveyCsv
            {
                IdOpinion = s.IdOpinion,
                IdCliente = s.IdCliente,
                IdProducto = s.IdProducto,
                Fecha = TransformHelpers.DateOnly(s.Fecha),
                Comentario = TransformHelpers.CleanText(s.Comentario),
                Clasificacion = TransformHelpers.ToTitle(s.Clasificacion),
                PuntajeSatisfaccion = s.PuntajeSatisfaccion,
                Fuente = TransformHelpers.NormalizeFuente(s.Fuente)
            });

            var res = new TransformResult<SurveyCsv>();
            var groups = normalized
                .Where(s => s.IdOpinion > 0 && s.IdCliente > 0 && s.IdProducto > 0)
                .GroupBy(s => s.IdOpinion);

            foreach (var g in groups)
            {
                var first = g.First();
                res.Clean.Add(first);
                if (g.Count() > 1) res.Invalid.AddRange(g.Skip(1));
            }
            return res;
        }

        
        public TransformResult<WebReviewCsv> CleanWebReviews(IEnumerable<WebReviewCsv> rows)
        {
            var normalized = rows.Select(r => new WebReviewCsv
            {
                IdReview = TransformHelpers.SanitizeLen(TransformHelpers.CleanText(r.IdReview), 100),
                IdCliente = TransformHelpers.DigitsOnly(r.IdCliente),
                IdProducto = TransformHelpers.DigitsOnly(r.IdProducto),
                Fecha = TransformHelpers.DateOnly(r.Fecha),
                Comentario = TransformHelpers.CleanText(r.Comentario),
                Rating = TransformHelpers.Clamp(r.Rating, 1, 5)
            });

            var res = new TransformResult<WebReviewCsv>();
            var groups = normalized
                .Where(r => !string.IsNullOrWhiteSpace(r.IdReview)
                            && !string.IsNullOrWhiteSpace(r.IdCliente)
                            && !string.IsNullOrWhiteSpace(r.IdProducto))
                .GroupBy(r => r.IdReview);

            foreach (var g in groups)
            {
                var first = g.First();
                res.Clean.Add(first);
                if (g.Count() > 1) res.Invalid.AddRange(g.Skip(1));
            }
            return res;
        }

       
        public TransformResult<SurveyCsv> ValidateSurveysFK(
            IEnumerable<SurveyCsv> surveys,
            HashSet<int> validClientIds,
            HashSet<int> validProductIds)
        {
            var res = new TransformResult<SurveyCsv>();
            foreach (var s in surveys)
            {
                if (validClientIds.Contains(s.IdCliente) && validProductIds.Contains(s.IdProducto))
                    res.Clean.Add(s);
                else
                    res.Invalid.Add(s);
            }
            return res;
        }

        public TransformResult<SocialCommentCsv> ValidateSocialFK(
            IEnumerable<SocialCommentCsv> social,
            HashSet<int> validClientIds,
            HashSet<int> validProductIds)
        {
            var res = new TransformResult<SocialCommentCsv>();
            foreach (var s in social)
            {
                var prodId = TransformHelpers.DigitsOnlyToInt(s.IdProducto);
                var cliId = string.IsNullOrWhiteSpace(s.IdCliente) ? 0 : TransformHelpers.DigitsOnlyToInt(s.IdCliente);

                var prodOk = validProductIds.Contains(prodId);
                var cliOk = string.IsNullOrWhiteSpace(s.IdCliente) || validClientIds.Contains(cliId);

                if (prodOk && cliOk) res.Clean.Add(s);
                else res.Invalid.Add(s);
            }
            return res;
        }

        public TransformResult<WebReviewCsv> ValidateWebReviewsFK(
            IEnumerable<WebReviewCsv> reviews,
            HashSet<int> validClientIds,
            HashSet<int> validProductIds)
        {
            var res = new TransformResult<WebReviewCsv>();
            foreach (var r in reviews)
            {
                var cliId = TransformHelpers.DigitsOnlyToInt(r.IdCliente);
                var prodId = TransformHelpers.DigitsOnlyToInt(r.IdProducto);

                if (validClientIds.Contains(cliId) && validProductIds.Contains(prodId))
                    res.Clean.Add(r);
                else
                    res.Invalid.Add(r);
            }
            return res;
        }
    }
}
