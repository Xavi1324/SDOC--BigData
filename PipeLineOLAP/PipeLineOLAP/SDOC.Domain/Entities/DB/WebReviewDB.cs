using SDOC.Domain.Base;

namespace SDOC.Domain.Entities.DB
{
    public class WebReviewDB : BaseEntity<int>
    {
        public int IdClient { get; set; }
        public int IdProduct { get; set; }
        public string? Comment { get; set; }
        public int Rating { get; set; }
       
    }
}
