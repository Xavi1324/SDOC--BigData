namespace SDOC.Domain.Entities.DB
{
    
    public class WebReviewDB 
    {
        public long  OpinionId { get; set; }

        public int ProductId { get; set; }
        public int? ClientId { get; set; }
        public int FuenteId { get; set; }
        public int TimeId { get; set; }
        public string? Comment { get; set; } = string.Empty;
        public short ClassId { get; set; }

        public string? ClientName { get; set; }
        public string? LastName { get; set; } 
        public string? Email { get; set; } 

        public string? ProductName { get; set; } 

        public int CategoryId { get; set; }
        public string? CategoryName { get; set; } 

        public string? FuenteNombre { get; set; } 
        public string? TipoFuenteDesc { get; set; }

        public DateTime Fecha { get; set; }

        public string? ClassCode { get; set; } 

    }
}
