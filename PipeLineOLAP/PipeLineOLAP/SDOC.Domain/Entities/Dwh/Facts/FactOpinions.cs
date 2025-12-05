using SDOC.Domain.Entities.Dwh.Dimensions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SDOC.Domain.Entities.Dwh.Facts
{
    [Table("Fact_Opiniones", Schema = "olap")]
    public class FactOpinions
    {

        [Key]
        [Column("Opinion_PK")]
        public long OpinionsSK { get; set; }

        [Column("Time_SK")]
        public int TimeSK { get; set; }

        [Column("Product_SK")]
        public int ProductSK { get; set; }

        [Column("Client_SK")]
        public int ClientSK { get; set; }

        [Column("Fuente_SK")]
        public int SourceSK { get; set; }

        [Column("Class_SK")]
        public short ClassSk { get; set; }

        [Column("TotalComentarios")]
        public byte TotalComments { get; set; } = 1;

        [Column("PuntajeSatisfaccion")]
        public short SatisfactionScore { get; set; }

    }
}
