using CsvHelper.Configuration.Attributes;

namespace Readers.Models.Csv
{
    public class SocialCommentCsv
    {
        [Name("IdComment")]
        public string IdComment { get; set; } = string.Empty;

        
        [Name("IdCliente")]
        public string? IdCliente { get; set; }

        
        [Name("IdProduct")]
        public string IdProducto { get; set; } = string.Empty;

        [Name("Fuente")]
        public string Fuente { get; set; } = string.Empty;

        [Name("Fecha")]
        public DateTime Fecha { get; set; }

        [Name("Comentario")]
        public string Comentario { get; set; } = string.Empty;

        public override string ToString() =>
            $"{IdComment} | {IdCliente ?? "(sin cliente)"} | {IdProducto} | {Fuente} | {Fecha:d} | {Comentario}";
    }
}
