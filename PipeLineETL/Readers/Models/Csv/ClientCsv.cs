using CsvHelper.Configuration.Attributes;

namespace Readers.Models.Csv
{
    public class ClientCsv
    {
        [Name("IdCliente")]
        public int IdCliente { get; set; }

        [Name("Nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Name("Email")]
        public string Email { get; set; } = string.Empty;

        public override string ToString() => $"{IdCliente} | {Nombre} | {Email}";
    }
}
