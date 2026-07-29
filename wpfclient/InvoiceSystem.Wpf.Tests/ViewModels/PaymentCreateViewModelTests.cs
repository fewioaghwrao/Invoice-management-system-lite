using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class PaymentCreateViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitializeMethodOptions()
    {
        var service = new FakePaymentService();

        var vm = new PaymentCreateViewModel(service);

        Assert.Equal(4, vm.MethodOptions.Count);
        Assert.Equal("BANK_TRANSFER", vm.SelectedMethod?.Value);
        Assert.Equal("振込", vm.SelectedMethod?.Label);
        Assert.Equal(DateTime.Today, vm.PaymentDate);
        Assert.False(vm.IsLoading);
        Assert.False(vm.IsSaving);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task InitializeAsync_ShouldLoadMembers()
    {
        var service = new FakePaymentService
        {
            MemberOptionsToReturn = new List<MemberOptionDto>
            {
                new() { Id = 1, Name = "山田 太郎" },
                new() { Id = 2, Name = "佐藤 花子" }
            }
        };

        var vm = new PaymentCreateViewModel(service);

        await vm.InitializeAsync();

        Assert.Equal(1, service.GetMemberOptionsCallCount);
        Assert.Equal(2, vm.Members.Count);
        Assert.Equal("山田 太郎", vm.Members[0].Name);
        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetError_WhenServiceThrows()
    {
        var service = new FakePaymentService
        {
            ExceptionToThrowOnGetMemberOptions = new InvalidOperationException("member load error")
        };

        var vm = new PaymentCreateViewModel(service);

        await vm.InitializeAsync();

        Assert.True(vm.HasError);
        Assert.Contains("member load error", vm.ErrorMessage);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public void SelectedMember_ShouldSetPayerName_WhenPayerNameNotTouched()
    {
        var service = new FakePaymentService();
        var vm = new PaymentCreateViewModel(service);

        vm.SelectedMember = new MemberOptionDto
        {
            Id = 1,
            Name = "山田 太郎"
        };

        Assert.Equal("山田 太郎", vm.PayerName);
    }

    [Fact]
    public void SelectedMember_ShouldNotOverwritePayerName_WhenPayerNameTouched()
    {
        var service = new FakePaymentService();
        var vm = new PaymentCreateViewModel(service);

        vm.PayerName = "別名義";
        vm.MarkPayerNameTouched();

        vm.SelectedMember = new MemberOptionDto
        {
            Id = 1,
            Name = "山田 太郎"
        };

        Assert.Equal("別名義", vm.PayerName);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenNoMemberExists()
    {
        var service = new FakePaymentService();
        var vm = new PaymentCreateViewModel(service);

        var error = vm.Validate();

        Assert.NotNull(error);
        Assert.Contains("有効な会員が存在しません。", error);
        Assert.Contains("会員（入金元）を選択してください。", error);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenAmountIsInvalid()
    {
        var service = new FakePaymentService();
        var vm = new PaymentCreateViewModel(service);

        vm.Members.Add(new MemberOptionDto { Id = 1, Name = "山田 太郎" });
        vm.SelectedMember = vm.Members[0];
        vm.PayerName = "山田 太郎";
        vm.AmountText = "abc";

        var error = vm.Validate();

        Assert.NotNull(error);
        Assert.Contains("入金額は 1 以上の数値で入力してください。", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-100")]
    public void Validate_ShouldReturnError_WhenAmountIsEmptyOrNotPositive(string amountText)
    {
        var service = new FakePaymentService();
        var vm = new PaymentCreateViewModel(service);

        vm.Members.Add(new MemberOptionDto { Id = 1, Name = "山田 太郎" });
        vm.SelectedMember = vm.Members[0];
        vm.PayerName = "山田 太郎";
        vm.AmountText = amountText;

        var error = vm.Validate();

        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_ShouldReturnNull_WhenValid()
    {
        var service = new FakePaymentService();
        var vm = new PaymentCreateViewModel(service);

        vm.Members.Add(new MemberOptionDto { Id = 1, Name = "山田 太郎" });
        vm.SelectedMember = vm.Members[0];
        vm.PaymentDate = new DateTime(2026, 4, 25);
        vm.PayerName = "山田 太郎";
        vm.AmountText = "12000";

        var error = vm.Validate();

        Assert.Null(error);
    }

    [Fact]
    public async Task SaveAsync_ShouldCreatePayment_WhenValid()
    {
        var service = new FakePaymentService
        {
            CreateResultToReturn = 123
        };

        var vm = new PaymentCreateViewModel(service);

        vm.Members.Add(new MemberOptionDto { Id = 10, Name = "山田 太郎" });
        vm.SelectedMember = vm.Members[0];
        vm.PaymentDate = new DateTime(2026, 4, 25);
        vm.PayerName = " 山田 太郎 ";
        vm.AmountText = "12000";
        vm.SelectedMethod = vm.MethodOptions.First(x => x.Value == "BANK_TRANSFER");

        var id = await vm.SaveAsync();

        Assert.Equal(123, id);
        Assert.Equal(1, service.CreateCallCount);

        Assert.NotNull(service.LastCreateRequest);
        Assert.Equal(10, service.LastCreateRequest!.MemberId);
        Assert.Equal(new DateTime(2026, 4, 25), service.LastCreateRequest.PaymentDate);
        Assert.Equal(12000m, service.LastCreateRequest.Amount);
        Assert.Equal("山田 太郎", service.LastCreateRequest.PayerName);
        Assert.Equal("BANK_TRANSFER", service.LastCreateRequest.Method);

        Assert.False(vm.IsSaving);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_ShouldThrowAndSetError_WhenInvalid()
    {
        var service = new FakePaymentService();
        var vm = new PaymentCreateViewModel(service);

        var ex = await Assert.ThrowsAsync<Exception>(() => vm.SaveAsync());

        Assert.Contains("有効な会員が存在しません。", ex.Message);
        Assert.Equal(0, service.CreateCallCount);
    }

    [Fact]
    public async Task SaveAsync_ShouldSetError_WhenServiceThrows()
    {
        var service = new FakePaymentService
        {
            ExceptionToThrowOnCreate = new InvalidOperationException("create error")
        };

        var vm = new PaymentCreateViewModel(service);

        vm.Members.Add(new MemberOptionDto { Id = 1, Name = "山田 太郎" });
        vm.SelectedMember = vm.Members[0];
        vm.PaymentDate = new DateTime(2026, 4, 25);
        vm.PayerName = "山田 太郎";
        vm.AmountText = "1000";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => vm.SaveAsync());

        Assert.Equal("create error", ex.Message);
        Assert.True(vm.HasError);
        Assert.Contains("create error", vm.ErrorMessage);
        Assert.False(vm.IsSaving);
    }
}