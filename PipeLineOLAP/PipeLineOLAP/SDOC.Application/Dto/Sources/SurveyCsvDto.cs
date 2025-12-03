namespace SDOC.Application.Dto.Sources
{
    public class SurveyCsvDto
    {
        public int IdOpinion { get; set; }

        // ===== CLIENTE =====
        public int IdCliente { get; set; }
        public string? ClienteNombre { get; set; }
        public string? ClienteApellido { get; set; }
        public string? ClienteEmail { get; set; }

        // ===== PRODUCTO =====
        public int IdProducto { get; set; }
        public string? ProductoNombre { get; set; }
        public string? CategoriaNombre { get; set; }

        // ===== OPINIÓN =====
        public DateTime Fecha { get; set; }
        public string Comentario { get; set; } = string.Empty;

        // ===== CLASIFICACIÓN =====
        public string Clasificacion { get; set; } = string.Empty;

        // ===== OTRAS =====
        public int PuntajeSatisfaccion { get; set; }
        public string Fuente { get; set; } = string.Empty;
    }
}
