using CsvHelper.Configuration.Attributes;

namespace Readers.Models.Staging;

public class Survey
{
    [Name("IdOpinion")]
    public int IdOpinion { get; set; }

    [Name("IdCliente")]
    public int IdCliente { get; set; }

    [Name("IdProducto")]
    public int IdProducto { get; set; }

    [Name("Fecha")]
    public DateTime Fecha { get; set; }

    [Name("Comentario")]
    public string Comentario { get; set; } = string.Empty;

    [Name("Clasificación")]
    public string Clasificacion { get; set; } = string.Empty;

    [Name("PuntajeSatisfacción")]
    public int PuntajeSatisfaccion { get; set; }

    [Name("Fuente")]
    public string Fuente { get; set; } = string.Empty;

    public override string ToString() =>
        $"{IdOpinion} | {IdCliente} | {IdProducto} | {Fecha:d} | {Clasificacion} | {PuntajeSatisfaccion} | {Fuente} | {Comentario}";
}
