// Application/Dtos/Members/UpdateMyProfileRequest.cs
namespace InvoiceSystem.Application.Dtos.Members;

public record UpdateMyProfileRequest(
    string Name,
    string Email,
    string? PostalCode,
    string? Address,
    string? Phone
);
