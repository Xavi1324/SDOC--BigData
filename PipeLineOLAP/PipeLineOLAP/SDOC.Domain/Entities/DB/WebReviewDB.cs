namespace SDOC.Domain.Entities.DB
{
    
    public class WebReviewDB 
    {
        public long  OpinionId { get; set; }
        public int ProductId { get; set; }
        public int? ClientId { get; set; }
        public int FuenteId { get; set; }
        public int TimeId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public short ClassId { get; set; }

    }
}
