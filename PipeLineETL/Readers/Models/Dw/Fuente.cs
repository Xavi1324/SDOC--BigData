namespace Readers.Models.Dw
{
    public class Fuente
    {
        public int Fuente_Id { get; set; }
        public int TipoFuente_Id { get; set; }
        public string Nombre { get; set; } = "";
        public string? UrlPath { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
