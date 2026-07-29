using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class AdminInvoiceDetailViewModelTests
{
    [Fact]
    public void Constructor_InitialValues_AreExpected()
    {
        var service = new FakeInvoiceService();

        var vm = new AdminInvoiceDetailViewModel(service, 123);

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);

        Assert.Equal(0, vm.Id);
        Assert.Equal(0, vm.MemberId);
        Assert.Equal(string.Empty, vm.MemberName);
        Assert.Equal(string.Empty, vm.InvoiceNumber);
        Assert.Equal(0m, vm.TotalAmount);
        Assert.Equal(0m, vm.PaidAmount);
        Assert.Equal(0m, vm.RemainingAmount);
        Assert.Equal(0, vm.StatusId);
        Assert.Equal(string.Empty, vm.StatusName);
        Assert.Null(vm.PdfPath);
        Assert.Null(vm.Remarks);

        Assert.Empty(vm.Lines);
        Assert.Empty(vm.Allocations);
        Assert.Empty(vm.Reminders);
    }

    [Fact]
    public async Task LoadAsync_Success_AppliesDtoToPropertiesAndCollections()
    {
        var dto = new InvoiceDetailDto
        {
            Id = 10,
            MemberId = 20,
            MemberName = "山田太郎",
            InvoiceNumber = "INV-001",
            InvoiceDate = new DateTime(2026, 4, 1),
            DueDate = new DateTime(2026, 4, 30),
            TotalAmount = 100000m,
            PaidAmount = 25000m,
            RemainingAmount = 75000m,
            StatusId = 1,
            StatusName = "未入金",
            PdfPath = "/pdf/inv-001.pdf",
            Remarks = "備考テスト",
            CreatedAt = new DateTime(2026, 4, 1, 9, 30, 0),
            Lines = new List<InvoiceLineDto>
            {
                new InvoiceLineDto(),
                new InvoiceLineDto()
            },
            Allocations = new List<InvoicePaymentAllocationDto>
            {
                new InvoicePaymentAllocationDto()
            },
            Reminders = new List<InvoiceReminderHistoryDto>
            {
                new InvoiceReminderHistoryDto(),
                new InvoiceReminderHistoryDto(),
                new InvoiceReminderHistoryDto()
            }
        };

        var service = new FakeInvoiceService
        {
            AdminInvoiceDetailToReturn = dto
        };

        var vm = new AdminInvoiceDetailViewModel(service, 999);

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);

        Assert.Equal(999, service.LastInvoiceIdForAdminDetail);

        Assert.Equal(10, vm.Id);
        Assert.Equal(20, vm.MemberId);
        Assert.Equal("山田太郎", vm.MemberName);
        Assert.Equal("INV-001", vm.InvoiceNumber);
        Assert.Equal(new DateTime(2026, 4, 1), vm.InvoiceDate);
        Assert.Equal(new DateTime(2026, 4, 30), vm.DueDate);
        Assert.Equal(100000m, vm.TotalAmount);
        Assert.Equal(25000m, vm.PaidAmount);
        Assert.Equal(75000m, vm.RemainingAmount);
        Assert.Equal(1, vm.StatusId);
        Assert.Equal("未入金", vm.StatusName);
        Assert.Equal("/pdf/inv-001.pdf", vm.PdfPath);
        Assert.Equal("備考テスト", vm.Remarks);
        Assert.Equal(new DateTime(2026, 4, 1, 9, 30, 0), vm.CreatedAt);

        Assert.Equal("￥100,000", vm.TotalAmountText);
        Assert.Equal("￥25,000", vm.PaidAmountText);
        Assert.Equal("￥75,000", vm.RemainingAmountText);

        Assert.Equal(2, vm.Lines.Count);
        Assert.Equal(1, vm.Allocations.Count);
        Assert.Equal(3, vm.Reminders.Count);
    }

    [Fact]
    public async Task LoadAsync_WhenServiceFails_SetsErrorAndClearsCollections()
    {
        var service = new FakeInvoiceService
        {
            ExceptionToThrowOnGetAdminInvoiceDetail = new InvalidOperationException("detail load failed")
        };

        var vm = new AdminInvoiceDetailViewModel(service, 1);
        vm.Lines.Add(new InvoiceLineDto());
        vm.Allocations.Add(new InvoicePaymentAllocationDto());
        vm.Reminders.Add(new InvoiceReminderHistoryDto());

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.True(vm.HasError);
        Assert.Contains("請求書詳細の取得に失敗しました。", vm.ErrorMessage);
        Assert.Contains("detail load failed", vm.ErrorMessage);

        Assert.Empty(vm.Lines);
        Assert.Empty(vm.Allocations);
        Assert.Empty(vm.Reminders);
    }

    [Theory]
    [InlineData(0, "￥0")]
    [InlineData(1000, "￥1,000")]
    [InlineData(123456, "￥123,456")]
    public void FormatCurrency_ReturnsJaJpCurrency(decimal value, string expected)
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceDetailViewModel(service, 1);

        var result = vm.FormatCurrency(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDate_ReturnsExpectedFormat()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceDetailViewModel(service, 1);

        var result = vm.FormatDate(new DateTime(2026, 4, 23));

        Assert.Equal("2026/04/23", result);
    }

    [Theory]
    [InlineData(null, "—")]
    [InlineData("", "—")]
    [InlineData("   ", "—")]
    [InlineData("BANK_TRANSFER", "銀行振込")]
    [InlineData("銀行振込", "銀行振込")]
    [InlineData("cash", "現金")]
    [InlineData("現金払い", "現金")]
    [InlineData("CARD", "カード")]
    [InlineData("カード決済", "カード")]
    [InlineData("OTHER", "OTHER")]
    public void FormatMethod_ReturnsExpectedText(string? method, string expected)
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceDetailViewModel(service, 1);

        var result = vm.FormatMethod(method);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("未入金", "#FF3F1D1D", "#FFFCA5A5", "#FFB91C1C")]
    [InlineData("一部入金", "#FF3B2A12", "#FFFCD34D", "#FFD97706")]
    [InlineData("入金済み", "#FF0F2F24", "#FF86EFAC", "#FF059669")]
    [InlineData("期限超過", "#FF4C0519", "#FFFDA4AF", "#FFE11D48")]
    [InlineData("キャンセル", "#FF1F2937", "#FFCBD5E1", "#FF475569")]
    [InlineData("その他", "#FF1E293B", "#FFFFFFFF", "#FF334155")]
    public void StatusBrushes_ReturnExpectedColors(
        string statusName,
        string backgroundHex,
        string foregroundHex,
        string borderHex)
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceDetailViewModel(service, 1)
        {
            StatusName = statusName
        };

        Assert.Equal(backgroundHex, ToHex(vm.StatusBackground));
        Assert.Equal(foregroundHex, ToHex(vm.StatusForeground));
        Assert.Equal(borderHex, ToHex(vm.StatusBorderBrush));
    }

    [Fact]
    public void AmountTextProperties_ReflectCurrentValues()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceDetailViewModel(service, 1)
        {
            TotalAmount = 50000m,
            PaidAmount = 12000m,
            RemainingAmount = 38000m
        };

        Assert.Equal("￥50,000", vm.TotalAmountText);
        Assert.Equal("￥12,000", vm.PaidAmountText);
        Assert.Equal("￥38,000", vm.RemainingAmountText);
    }

    private static string ToHex(Brush brush)
    {
        var solid = Assert.IsType<SolidColorBrush>(brush);
        return solid.Color.ToString();
    }
}