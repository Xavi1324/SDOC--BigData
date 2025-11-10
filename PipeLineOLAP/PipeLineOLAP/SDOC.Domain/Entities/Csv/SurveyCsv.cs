namespace SDOC.Domain.Entities.csv
{
    public class SurveyCsv 
    {
        public int IdOpinion { get; set; }
        public int IdCliente { get; set; }
        public int IdProducto { get; set; }
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public string Clasificación { get; set; } = string.Empty;
        public int PuntajeSatisfacción { get; set; }
        public string Fuente { get; set; } = string.Empty;
    }
}
