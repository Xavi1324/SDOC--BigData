namespace Readers.Models.Dw
{
    public class ClienteDw
    {
        public int Client_Id { get; set; }
        public string ClientName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? Email { get; set; }
        public int? Pais_Id { get; set; }
    }
}
