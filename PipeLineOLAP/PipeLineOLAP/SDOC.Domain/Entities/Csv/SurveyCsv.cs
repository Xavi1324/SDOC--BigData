using SDOC.Domain.Base;

namespace SDOC.Domain.Entities.csv
{
    public class SurveyCsv : BaseEntity<int>
    {
        public int IdClient { get; set; }
        public int IdProduct { get; set; }
        public string? Comment { get; set; }
        public string Class { get; set; } = string.Empty;
        public int SatisfactionScore { get; set; }
        public string Source { get; set; } = string.Empty;
    }
}
