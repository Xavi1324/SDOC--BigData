using CsvHelper.Configuration.Attributes;

namespace Readers.Models.Csv
{
    public class ProductCsv
    {
        public int IdProducto { get; set; }

        public string Nombre { get; set; }
        [Name("Categoría", "Categoria")]
        public string? Categoria { get; set; }

        public override string ToString() => $"{IdProducto} | {Nombre} | {Categoria}";
    }
}

