namespace SDOC.Domain.Entities.Dwh.Dimensions
{
    public class DimCategory
    {
        public int CategorySK { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public ICollection<DimProduct> Products { get; set; } = new List<DimProduct>();
    }
}
