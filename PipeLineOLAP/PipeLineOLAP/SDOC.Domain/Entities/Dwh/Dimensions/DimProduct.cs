namespace SDOC.Domain.Entities.Dwh.Dimensions
{
    public class DimProduct
    {
        public int ProductSK { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CategorySK { get; set; }
        public DimCategory? Category { get; set; }
    }
}
