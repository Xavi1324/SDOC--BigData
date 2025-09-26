namespace Readers.Models.Staging;

public class Client
{
    public int IdCliente { get; set; }              
    public string Nombre { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Country { get; set; }            
    public string? LastName { get; set; }
}
