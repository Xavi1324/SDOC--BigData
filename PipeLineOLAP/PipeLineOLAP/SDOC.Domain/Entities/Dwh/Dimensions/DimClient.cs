namespace SDOC.Domain.Entities.Dwh.Dimensions
{
    public class DimClient
    {
        public int ClientSK { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email {  get; set; } = string.Empty;
        public string Country {  get; set; } = string.Empty;

    }
}
