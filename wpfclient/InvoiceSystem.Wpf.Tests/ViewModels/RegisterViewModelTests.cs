using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class RegisterViewModelTests
{
    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenNameIsEmpty()
    {
        var service = new FakeAuthService();
        var vm = new RegisterViewModel(service)
        {
            Name = "",
            Email = "test@example.com",
            Password = "password123"
        };

        await vm.RegisterAsync();

        Assert.True(vm.HasError);
        Assert.Equal("氏名を入力してください。", vm.ErrorMessage);
        Assert.Equal(0, service.RegisterCallCount);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenEmailIsEmpty()
    {
        var service = new FakeAuthService();
        var vm = new RegisterViewModel(service)
        {
            Name = "山田 太郎",
            Email = "",
            Password = "password123"
        };

        await vm.RegisterAsync();

        Assert.True(vm.HasError);
        Assert.Equal("メールアドレスを入力してください。", vm.ErrorMessage);
        Assert.Equal(0, service.RegisterCallCount);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenEmailIsInvalid()
    {
        var service = new FakeAuthService();
        var vm = new RegisterViewModel(service)
        {
            Name = "山田 太郎",
            Email = "invalid-email",
            Password = "password123"
        };

        await vm.RegisterAsync();

        Assert.True(vm.HasError);
        Assert.Equal("メールアドレスの形式が正しくありません。", vm.ErrorMessage);
        Assert.Equal(0, service.RegisterCallCount);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenPasswordIsEmpty()
    {
        var service = new FakeAuthService();
        var vm = new RegisterViewModel(service)
        {
            Name = "山田 太郎",
            Email = "test@example.com",
            Password = ""
        };

        await vm.RegisterAsync();

        Assert.True(vm.HasError);
        Assert.Equal("パスワードを入力してください。", vm.ErrorMessage);
        Assert.Equal(0, service.RegisterCallCount);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnError_WhenPasswordIsTooShort()
    {
        var service = new FakeAuthService();
        var vm = new RegisterViewModel(service)
        {
            Name = "山田 太郎",
            Email = "test@example.com",
            Password = "1234567"
        };

        await vm.RegisterAsync();

        Assert.True(vm.HasError);
        Assert.Equal("パスワードは8文字以上を推奨します。", vm.ErrorMessage);
        Assert.Equal(0, service.RegisterCallCount);
    }

    [Fact]
    public async Task RegisterAsync_ShouldCallRegister_WhenValid()
    {
        var service = new FakeAuthService
        {
            RegisterResultToReturn = (true, "登録が完了しました。")
        };

        var succeeded = false;

        var vm = new RegisterViewModel(service)
        {
            Name = " 山田 太郎 ",
            Email = " test@example.com ",
            Password = "password123",
            PostalCode = " 100-0001 ",
            Address = " 東京都千代田区 ",
            Phone = " 090-1111-2222 "
        };

        vm.RegisterSucceeded += () => succeeded = true;

        await vm.RegisterAsync();

        Assert.Equal(1, service.RegisterCallCount);
        Assert.NotNull(service.LastRegisterRequest);

        Assert.Equal("山田 太郎", service.LastRegisterRequest!.Name);
        Assert.Equal("test@example.com", service.LastRegisterRequest.Email);
        Assert.Equal("password123", service.LastRegisterRequest.Password);
        Assert.Equal("100-0001", service.LastRegisterRequest.PostalCode);
        Assert.Equal("東京都千代田区", service.LastRegisterRequest.Address);
        Assert.Equal("090-1111-2222", service.LastRegisterRequest.Phone);

        Assert.False(vm.HasError);
        Assert.True(vm.HasSuccess);
        Assert.Equal("登録が完了しました。", vm.SuccessMessage);

        Assert.Equal(string.Empty, vm.Name);
        Assert.Equal(string.Empty, vm.Email);
        Assert.Equal(string.Empty, vm.Password);
        Assert.Equal(string.Empty, vm.PostalCode);
        Assert.Equal(string.Empty, vm.Address);
        Assert.Equal(string.Empty, vm.Phone);

        Assert.True(succeeded);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSetError_WhenServiceReturnsFailure()
    {
        var service = new FakeAuthService
        {
            RegisterResultToReturn = (false, "このメールアドレスは既に使用されています。")
        };

        var vm = new RegisterViewModel(service)
        {
            Name = "山田 太郎",
            Email = "test@example.com",
            Password = "password123"
        };

        await vm.RegisterAsync();

        Assert.Equal(1, service.RegisterCallCount);
        Assert.True(vm.HasError);
        Assert.Equal("このメールアドレスは既に使用されています。", vm.ErrorMessage);
        Assert.False(vm.HasSuccess);
        Assert.Equal(string.Empty, vm.SuccessMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSetCommunicationError_WhenServiceThrows()
    {
        var service = new FakeAuthService
        {
            ExceptionToThrowOnRegister = new InvalidOperationException("network error")
        };

        var vm = new RegisterViewModel(service)
        {
            Name = "山田 太郎",
            Email = "test@example.com",
            Password = "password123"
        };

        await vm.RegisterAsync();

        Assert.Equal(1, service.RegisterCallCount);
        Assert.True(vm.HasError);
        Assert.Equal("通信エラーが発生しました。時間をおいて再度お試しください。", vm.ErrorMessage);
        Assert.False(vm.HasSuccess);
        Assert.False(vm.IsLoading);
    }
}