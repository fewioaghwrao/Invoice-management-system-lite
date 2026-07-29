namespace InvoiceSystem.Wpf.Models;

public class CurrentUser
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string Token { get; set; } = "";
}
