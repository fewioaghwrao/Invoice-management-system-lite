using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class MemberListViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeFilterOptions()
    {
        var service = new FakeMemberService();

        var vm = new MemberListViewModel(service);

        Assert.Equal(4, vm.RoleOptions.Count);
        Assert.Equal("すべて", vm.SelectedRole?.Label);

        Assert.Equal(3, vm.StatusOptions.Count);
        Assert.Equal("すべて", vm.SelectedStatus?.Label);

        Assert.Equal(1, vm.CurrentPage);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadMembers()
    {
        var service = new FakeMemberService
        {
            MembersToReturn = new List<MemberListItemDto>
            {
                new()
                {
                    Id = 1,
                    Name = "山田 太郎",
                    Email = "yamada@example.com",
                    Role = 2,
                    IsActive = true
                }
            }
        };

        var vm = new MemberListViewModel(service);

        await vm.LoadAsync();

        Assert.Single(vm.Members);
        Assert.Equal(1, vm.Members[0].Id);
        Assert.Equal("山田 太郎", vm.Members[0].Name);
        Assert.Equal("一般会員", vm.Members[0].RoleText);
        Assert.Equal("有効", vm.Members[0].StatusText);
        Assert.True(vm.Members[0].CanDisable);

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetHasNextPage_WhenReturnedCountEqualsPageSize()
    {
        var service = new FakeMemberService
        {
            MembersToReturn = Enumerable.Range(1, 5)
                .Select(i => new MemberListItemDto
                {
                    Id = i,
                    Name = $"User{i}",
                    Email = $"user{i}@example.com",
                    Role = 2,
                    IsActive = true
                })
                .ToList()
        };

        var vm = new MemberListViewModel(service);

        await vm.LoadAsync();

        Assert.True(vm.HasNextPage);
        Assert.Equal("1–5件を表示（1ページあたり 5件）", vm.PageInfoText);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetErrorMessage_WhenServiceThrows()
    {
        var service = new FakeMemberService
        {
            GetMembersException = new InvalidOperationException("API error")
        };

        var vm = new MemberListViewModel(service);

        await vm.LoadAsync();

        Assert.True(vm.HasError);
        Assert.Contains("会員一覧の読込に失敗しました。", vm.ErrorMessage);
        Assert.Contains("API error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Theory]
    [InlineData(1, true, "管理者", "有効", false)]
    [InlineData(2, true, "一般会員", "有効", true)]
    [InlineData(2, false, "一般会員", "無効", false)]
    [InlineData(9, true, "退会", "有効", false)]
    public void MemberRowViewModel_ShouldConvertDisplayValues(
        int role,
        bool isActive,
        string expectedRoleText,
        string expectedStatusText,
        bool expectedCanDisable)
    {
        var dto = new MemberListItemDto
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = role,
            IsActive = isActive
        };

        var row = new MemberRowViewModel(dto);

        Assert.Equal(expectedRoleText, row.RoleText);
        Assert.Equal(expectedStatusText, row.StatusText);
        Assert.Equal(expectedCanDisable, row.CanDisable);
    }
}