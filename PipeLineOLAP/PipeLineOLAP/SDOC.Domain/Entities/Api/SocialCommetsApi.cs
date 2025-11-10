using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDOC.Domain.Entities.Api
{
    [Table("vw_SocialCommetsApi", Schema = "dw")]

    public class SocialCommetsApi 
    {
        [Key]
        public long OpinionId { get; set; }
        public int? IdClient { get; set; }
        public int IdProduct { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;


    }
}
