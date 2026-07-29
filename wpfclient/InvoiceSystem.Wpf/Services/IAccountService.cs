using InvoiceSystem.Wpf.Models;

namespace InvoiceSystem.Wpf.Services;

public interface IAccountService
{
    Task<AccountProfileDto> GetMyProfileAsync();
    Task UpdateMyProfileAsync(AccountProfileDto profile);
    Task DeleteMyAccountAsync();
}