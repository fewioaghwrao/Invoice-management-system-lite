using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public sealed class MemberDetailViewModelTests
{
    [StaFact]
    public async Task LoadAsync_WhenMemberExists_SetsProperties()
    {
        var service = new FakeMemberService
        {
            MemberToReturn = new MemberDetailDto
            {
                Id = 10,
                Name = "山田太郎",
                Email = "yamada@example.com",
                PostalCode = "100-0001",
                Address = "東京都",
                Phone = "090-0000-0000",
                Role = 2,
                IsActive = true,
                CreatedAt = new DateTime(2026, 4, 24)
            }
        };

        var vm = new MemberDetailViewModel(service, 10, new Window());

        await vm.LoadAsync();

        Assert.Equal(10, vm.Id);
        Assert.Equal("山田太郎", vm.Name);
        Assert.Equal("yamada@example.com", vm.Email);
        Assert.Equal("100-0001", vm.PostalCode);
        Assert.Equal("東京都", vm.Address);
        Assert.Equal("090-0000-0000", vm.Phone);
        Assert.Equal(2, vm.Role);
        Assert.True(vm.IsActive);
        Assert.Equal("一般会員", vm.RoleText);
        Assert.Equal("有効", vm.StatusText);
        Assert.False(vm.HasError);
    }

    [StaFact]
    public async Task LoadAsync_WhenMemberIsNull_SetsErrorMessage()
    {
        var service = new FakeMemberService
        {
            MemberToReturn = null
        };

        var vm = new MemberDetailViewModel(service, 1, new Window());

        await vm.LoadAsync();

        Assert.True(vm.HasError);
        Assert.Equal("会員情報の取得に失敗しました。", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [StaFact]
    public async Task LoadAsync_WhenServiceThrows_SetsErrorMessage()
    {
        var service = new FakeMemberService
        {
            GetByIdException = new InvalidOperationException("API error")
        };

        var vm = new MemberDetailViewModel(service, 1, new Window());

        await vm.LoadAsync();

        Assert.True(vm.HasError);
        Assert.Contains("会員情報の取得に失敗しました。", vm.ErrorMessage);
        Assert.Contains("API error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [StaTheory]
    [InlineData(1, "管理者")]
    [InlineData(2, "一般会員")]
    [InlineData(9, "退会")]
    [InlineData(99, "不明(99)")]
    public void RoleText_ReturnsExpectedText(int role, string expected)
    {
        var vm = new MemberDetailViewModel(new FakeMemberService(), 1, new Window());

        vm.Role = role;

        Assert.Equal(expected, vm.RoleText);
    }

    [StaTheory]
    [InlineData(true, "有効")]
    [InlineData(false, "無効")]
    public void StatusText_ReturnsExpectedText(bool isActive, string expected)
    {
        var vm = new MemberDetailViewModel(new FakeMemberService(), 1, new Window());

        vm.IsActive = isActive;

        Assert.Equal(expected, vm.StatusText);
    }

    [StaFact]
    public void SaveCommand_WhenValidMember_CanExecuteTrue()
    {
        var vm = new MemberDetailViewModel(new FakeMemberService(), 1, new Window())
        {
            Id = 1,
            Name = "山田太郎",
            Email = "yamada@example.com",
            Role = 2,
            IsActive = true
        };

        Assert.True(vm.SaveCommand.CanExecute(null));
    }

    [StaTheory]
    [InlineData("", "yamada@example.com", 2, true)]
    [InlineData("山田太郎", "", 2, true)]
    [InlineData("山田太郎", "invalid-mail", 2, true)]
    [InlineData("山田太郎", "yamada@example.com", 1, true)]
    [InlineData("山田太郎", "yamada@example.com", 9, true)]
    [InlineData("山田太郎", "yamada@example.com", 2, false)]
    public void SaveCommand_WhenInvalidMember_CanExecuteFalse(
        string name,
        string email,
        int role,
        bool isActive)
    {
        var vm = new MemberDetailViewModel(new FakeMemberService(), 1, new Window())
        {
            Id = 1,
            Name = name,
            Email = email,
            Role = role,
            IsActive = isActive
        };

        Assert.False(vm.SaveCommand.CanExecute(null));
    }

    [StaFact]
    public void DisableCommand_WhenNormalActiveMember_CanExecuteTrue()
    {
        var vm = new MemberDetailViewModel(new FakeMemberService(), 1, new Window())
        {
            Id = 1,
            Name = "山田太郎",
            Email = "yamada@example.com",
            Role = 2,
            IsActive = true
        };

        Assert.True(vm.DisableCommand.CanExecute(null));
    }

    [StaTheory]
    [InlineData(0, 2, true)]
    [InlineData(1, 1, true)]
    [InlineData(1, 9, true)]
    [InlineData(1, 2, false)]
    public void DisableCommand_WhenInvalidState_CanExecuteFalse(
        int id,
        int role,
        bool isActive)
    {
        var vm = new MemberDetailViewModel(new FakeMemberService(), 1, new Window())
        {
            Id = id,
            Name = "山田太郎",
            Email = "yamada@example.com",
            Role = role,
            IsActive = isActive
        };

        Assert.False(vm.DisableCommand.CanExecute(null));
    }
}