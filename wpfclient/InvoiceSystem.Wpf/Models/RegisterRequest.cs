namespace InvoiceSystem.Wpf.Models;

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
}