using SDOC.Domain.Entities.Dwh.Dimensions;

namespace SDOC.Domain.Entities.Dwh.Facts
{
    public class FactOpinions
    {
        public long OpinionsSK { get; set; }
        public int ProductSK { get; set; }
        public int ClientSK { get; set; }
        public int SourceSK { get; set; }
        public short ClassSk { get; set; }
        public int TimeSK { get; set; }

        public byte TotalComments { get; set; } = 1;
        public short SatisfactionScore { get; set; }

        
        public DimProduct? Product { get; set; }
        public DimClient? Client { get; set; }
        public DimSource? Source { get; set; }
        public DimClass? Class { get; set; }

    }
}
