using CsvHelper.Configuration.Attributes;

namespace Readers.Models.Csv
{
    public class ProductCsv
    {
        [Name("IdProducto")]
        public int IdProducto { get; set; }

        [Name("Nombre")]
        public string Nombre { get; set; } = string.Empty;
        [Name("Categoría", "Categoria")]
        public string? Categoria { get; set; }

        public override string ToString() => $"{IdProducto} | {Nombre} | {Categoria}";
    }
}

