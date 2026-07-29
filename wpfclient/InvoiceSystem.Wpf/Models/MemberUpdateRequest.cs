namespace InvoiceSystem.Wpf.Models;

public sealed class MemberUpdateRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
}
