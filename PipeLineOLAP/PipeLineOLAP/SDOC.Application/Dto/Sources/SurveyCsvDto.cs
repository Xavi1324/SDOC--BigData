namespace SDOC.Application.Dto.Sources
{
    public class SurveyCsvDto
    {
        public int IdOpinion { get; set; }
        public int IdCliente { get; set; }
        public int IdProducto { get; set; }
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; } = string.Empty;

        // Sin acentos en el código, para evitar líos:
        public string Clasificacion { get; set; } = string.Empty;

        public int PuntajeSatisfaccion { get; set; }
        public string Fuente { get; set; } = string.Empty;
    }
}
