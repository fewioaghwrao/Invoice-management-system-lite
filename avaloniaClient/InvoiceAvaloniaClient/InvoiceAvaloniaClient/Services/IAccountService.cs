using InvoiceAvaloniaClient.Models;

namespace InvoiceAvaloniaClient.Services;

public interface IAccountService
{
    Task<AccountProfileDto> GetMyProfileAsync();
    Task UpdateMyProfileAsync(AccountProfileDto profile);
    Task DeleteMyAccountAsync();
}