using System;

namespace InvoiceSystem.Wpf.Models;

public class MemberListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public int Role { get; set; }
    public bool IsActive { get; set; }
}