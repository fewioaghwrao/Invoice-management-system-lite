using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;

namespace InvoiceSystem.Wpf.Tests.Fakes;

public sealed class FakeAccountService : IAccountService
{
    public AccountProfileDto? ProfileToReturn { get; set; }

    public Exception? ExceptionToThrowOnGetProfile { get; set; }
    public Exception? ExceptionToThrowOnUpdateProfile { get; set; }
    public Exception? ExceptionToThrowOnDeleteAccount { get; set; }

    public int GetProfileCallCount { get; private set; }
    public int UpdateProfileCallCount { get; private set; }
    public int DeleteAccountCallCount { get; private set; }

    public AccountProfileDto? LastUpdatedProfile { get; private set; }

    public Task<AccountProfileDto> GetMyProfileAsync()
    {
        GetProfileCallCount++;

        if (ExceptionToThrowOnGetProfile != null)
        {
            return Task.FromException<AccountProfileDto>(ExceptionToThrowOnGetProfile);
        }

        return Task.FromResult(ProfileToReturn ?? new AccountProfileDto());
    }

    public Task UpdateMyProfileAsync(AccountProfileDto profile)
    {
        UpdateProfileCallCount++;
        LastUpdatedProfile = profile;

        if (ExceptionToThrowOnUpdateProfile != null)
        {
            return Task.FromException(ExceptionToThrowOnUpdateProfile);
        }

        return Task.CompletedTask;
    }

    public Task DeleteMyAccountAsync()
    {
        DeleteAccountCallCount++;

        if (ExceptionToThrowOnDeleteAccount != null)
        {
            return Task.FromException(ExceptionToThrowOnDeleteAccount);
        }

        return Task.CompletedTask;
    }
}