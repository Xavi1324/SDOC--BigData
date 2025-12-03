namespace SDOC.Application.Dto.Sources
{
    public class WebReviewDbDto
    {
        public long OpinionId { get; set; }

        // ===== PRODUCTO / CATEGORÍA =====
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        // ===== CLIENTE =====
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }
        public string? ClientLastName { get; set; }
        public string? ClientEmail { get; set; }

        // ===== FUENTE (DIM SOURCE) =====
        public int FuenteId { get; set; }
        public string FuenteNombre { get; set; } = string.Empty;
        public string TipoFuenteDesc { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Email { get; set; }

        // ===== TIEMPO =====
        public int TimeId { get; set; }
        public DateTime Fecha { get; set; }

        // ===== CLASIFICACIÓN =====
        public short ClassId { get; set; }
        public string ClassCode { get; set; } = string.Empty;

        // ===== OPINIÓN =====
        public string Comment { get; set; } = string.Empty;
    }
}
