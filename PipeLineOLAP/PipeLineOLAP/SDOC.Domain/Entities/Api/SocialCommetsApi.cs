using SDOC.Domain.Base;

namespace SDOC.Domain.Entities.Api
{
    public class SocialCommetsApi : BaseEntity<int>
    {
        public int IdClient { get; set; }
        public int IdProduct { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;


    }
}
