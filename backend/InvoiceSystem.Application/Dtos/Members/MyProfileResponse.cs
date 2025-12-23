// Application/Dtos/Members/MyProfileResponse.cs
namespace InvoiceSystem.Application.Dtos.Members;

public record MyProfileResponse(
    long Id,
    string Name,
    string Email,
    string? PostalCode,
    string? Address,
    string? Phone,
    bool IsEmailConfirmed
);
