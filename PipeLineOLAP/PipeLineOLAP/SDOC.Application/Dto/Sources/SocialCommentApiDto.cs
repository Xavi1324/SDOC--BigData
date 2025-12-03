namespace SDOC.Application.Dto.Sources
{
    public class SocialCommentApiDto
    {
        public long OpinionId { get; set; }

        // ===== CLIENTE (si la API lo conoce) =====
        public int? ClientId { get; set; }
        public string? ClientName { get; set; }

        // ===== PRODUCTO =====
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? CategoryName { get; set; }

        // ===== FUENTE =====
        public string Source { get; set; } = string.Empty;  // Instagram / Twitter / FB

        // ===== OPINIÓN =====
        public string Comment { get; set; } = string.Empty;

        // ===== CLASIFICACIÓN (si luego agregas NLP) =====
        public string? ClassCode { get; set; }
    }
}
