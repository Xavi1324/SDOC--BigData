using CsvHelper.Configuration.Attributes;

namespace Readers.Models.Staging;

public class DataSource
{
    [Name("IdFuente")]
    public string IdFuente { get; set; } = string.Empty;

    [Name("TipoFuente")]
    public string TipoFuente { get; set; } = string.Empty;

    [Name("FechaCarga")]
    public DateTime FechaCarga { get; set; }

    public override string ToString() => $"{IdFuente} | {TipoFuente} | {FechaCarga:d}";
}
