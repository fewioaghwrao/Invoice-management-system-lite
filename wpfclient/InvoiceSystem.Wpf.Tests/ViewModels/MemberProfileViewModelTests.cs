using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using InvoiceSystem.Wpf.Views;
using System.Windows;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class MemberProfileViewModelTests
{
    [Fact]
    public async Task InitializeAsync_ShouldLoadProfile()
    {
        var service = new FakeAccountService
        {
            ProfileToReturn = new AccountProfileDto
            {
                Id = 1,
                Name = "山田 太郎",
                Email = "yamada@example.com",
                Phone = "090-1111-2222",
                PostalCode = "100-0001",
                Address = "東京都千代田区"
            }
        };

        var vm = new MemberProfileViewModel(
            service,
            openDashboard: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => true);

        await vm.InitializeAsync();

        Assert.Equal(1, service.GetProfileCallCount);
        Assert.Equal("山田 太郎", vm.Name);
        Assert.Equal("yamada@example.com", vm.Email);
        Assert.Equal("090-1111-2222", vm.Phone);
        Assert.Equal("100-0001", vm.PostalCode);
        Assert.Equal("東京都千代田区", vm.Address);
        Assert.False(vm.IsLoading);
        Assert.Equal(Visibility.Collapsed, vm.LoadingVisibility);
        Assert.False(vm.IsEmailChanged);
        Assert.Equal(Visibility.Collapsed, vm.EmailChangedNoticeVisibility);
    }

    [Fact]
    public async Task EmailChangedNotice_ShouldBeVisible_WhenEmailChanged()
    {
        var service = new FakeAccountService
        {
            ProfileToReturn = new AccountProfileDto
            {
                Id = 1,
                Name = "山田 太郎",
                Email = "old@example.com"
            }
        };

        var vm = new MemberProfileViewModel(
            service,
            openDashboard: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => true);

        await vm.InitializeAsync();

        vm.Email = "new@example.com";

        Assert.True(vm.IsEmailChanged);
        Assert.Contains("変更前：old@example.com", vm.EmailChangedText);
        Assert.Contains("変更後：new@example.com", vm.EmailChangedText);
        Assert.Equal(Visibility.Visible, vm.EmailChangedNoticeVisibility);
    }

    [Fact]
    public void BackCommand_ShouldCallOpenDashboard()
    {
        var service = new FakeAccountService();
        var called = false;

        var vm = new MemberProfileViewModel(
            service,
            openDashboard: () => called = true,
            openLogin: () => { },
            showConfirmDialog: _ => true);

        vm.BackCommand.Execute(null);

        Assert.True(called);
    }

    [Fact]
    public void SaveCommand_ShouldNotUpdate_WhenNameIsEmpty()
    {
        var service = new FakeAccountService();
        var confirmCalled = false;

        var vm = new MemberProfileViewModel(
            service,
            openDashboard: () => { },
            openLogin: () => { },
            showConfirmDialog: _ =>
            {
                confirmCalled = true;
                return true;
            });

        vm.Name = "";
        vm.Email = "test@example.com";

        vm.SaveCommand.Execute(null);

        Assert.Equal(0, service.UpdateProfileCallCount);
        Assert.False(confirmCalled);
        Assert.Contains("氏名は必須です。", vm.ErrorMessage);
    }

    [Fact]
    public void SaveCommand_ShouldNotUpdate_WhenEmailIsInvalid()
    {
        var service = new FakeAccountService();
        var confirmCalled = false;

        var vm = new MemberProfileViewModel(
            service,
            openDashboard: () => { },
            openLogin: () => { },
            showConfirmDialog: _ =>
            {
                confirmCalled = true;
                return true;
            });

        vm.Name = "山田 太郎";
        vm.Email = "invalid-email";

        vm.SaveCommand.Execute(null);

        Assert.Equal(0, service.UpdateProfileCallCount);
        Assert.False(confirmCalled);
        Assert.Contains("メールアドレスの形式が正しくありません。", vm.ErrorMessage);
    }

    [Fact]
    public void SaveCommand_ShouldNotUpdate_WhenConfirmCanceled()
    {
        var service = new FakeAccountService();

        var vm = new MemberProfileViewModel(
            service,
            openDashboard: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => false);

        vm.Name = "山田 太郎";
        vm.Email = "yamada@example.com";

        vm.SaveCommand.Execute(null);

        Assert.Equal(0, service.UpdateProfileCallCount);
        Assert.Equal(string.Empty, vm.ErrorMessage);
    }

    [Fact]
    public void SaveCommand_ShouldUpdateProfile_WhenValidAndConfirmed()
    {
        var service = new FakeAccountService();
        ConfirmDialogRequest? request = null;

        var vm = new MemberProfileViewModel(
            service,
            openDashboard: () => { },
            openLogin: () => { },
            showConfirmDialog: r =>
            {
                request = r;
                return true;
            });

        vm.Name = " 山田 太郎 ";
        vm.Email = " yamada@example.com ";
        vm.Phone = " 090-1111-2222 ";
        vm.PostalCode = " 100-0001 ";
        vm.Address = " 東京都千代田区 ";

        vm.SaveCommand.Execute(null);

        Assert.Equal(1, service.UpdateProfileCallCount);
        Assert.NotNull(service.LastUpdatedProfile);
        Assert.Equal("山田 太郎", service.LastUpdatedProfile!.Name);
        Assert.Equal("yamada@example.com", service.LastUpdatedProfile.Email);
        Assert.Equal("090-1111-2222", service.LastUpdatedProfile.Phone);
        Assert.Equal("100-0001", service.LastUpdatedProfile.PostalCode);
        Assert.Equal("東京都千代田区", service.LastUpdatedProfile.Address);

        Assert.NotNull(request);
        Assert.Equal("保存内容の確認", request!.Title);
        Assert.Equal("保存する", request.ConfirmText);
    }

    [Fact]
    public void SaveCommand_ShouldConvertBlankOptionalFieldsToNull()
    {
        var service = new FakeAccountService();

        var vm = new MemberProfileViewModel(
            service,
            openDashboard: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => true);

        vm.Name = "山田 太郎";
        vm.Email = "yamada@example.com";
        vm.Phone = " ";
        vm.PostalCode = "";
        vm.Address = "   ";

        vm.SaveCommand.Execute(null);

        Assert.Equal(1, service.UpdateProfileCallCount);
        Assert.Null(service.LastUpdatedProfile!.Phone);
        Assert.Null(service.LastUpdatedProfile.PostalCode);
        Assert.Null(service.LastUpdatedProfile.Address);
    }

    [Fact]
    public void WithdrawCommand_ShouldNotDelete_WhenConfirmCanceled()
    {
        var service = new FakeAccountService();

        var vm = new MemberProfileViewModel(
            service,
            openDashboard: () => { },
            openLogin: () => { },
            showConfirmDialog: _ => false);

        vm.WithdrawCommand.Execute(null);

        Assert.Equal(0, service.DeleteAccountCallCount);
    }

    [Fact]
    public void WithdrawCommand_ShouldDeleteAndOpenLogin_WhenConfirmed()
    {
        var service = new FakeAccountService();
        var loginOpened = false;

        var vm = new MemberProfileViewModel(
            service,
            openDashboard: () => { },
            openLogin: () => loginOpened = true,
            showConfirmDialog: _ => true);

        vm.WithdrawCommand.Execute(null);

        Assert.Equal(1, service.DeleteAccountCallCount);
        Assert.True(loginOpened);
    }
}