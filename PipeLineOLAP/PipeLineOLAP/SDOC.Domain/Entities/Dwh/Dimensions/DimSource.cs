namespace SDOC.Domain.Entities.Dwh.Dimensions
{
    public class DimSource
    {
        public int SourceSK { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string SourceType {  get; set; } = string.Empty;
    }
}
