using CsvHelper.Configuration.Attributes;

namespace Readers.Models.Staging;

public class WebReview
{
    [Name("IdReview")]
    public string IdReview { get; set; } = string.Empty;

    [Name("IdCliente")]
    public string IdCliente { get; set; } = string.Empty;  

    [Name("IdProducto")]
    public string IdProducto { get; set; } = string.Empty;  

    [Name("Fecha")]
    public DateTime Fecha { get; set; }

    [Name("Comentario")]
    public string Comentario { get; set; } = string.Empty;

    [Name("Rating")]
    public int Rating { get; set; }

    public override string ToString() =>
        $"{IdReview} | {IdCliente} | {IdProducto} | {Fecha:d} | {Rating} | {Comentario}";
}
