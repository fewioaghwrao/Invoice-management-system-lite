using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class AdminInvoiceEditViewModelTests
{
    [Fact]
    public void Constructor_NewMode_InitialState_IsExpected()
    {
        var service = new FakeInvoiceService();

        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        Assert.True(vm.IsNewMode);
        Assert.False(vm.IsEditMode);
        Assert.Equal("請求書作成", vm.ScreenTitle);
        Assert.Equal("作成", vm.SaveButtonText);

        Assert.False(vm.IsLoading);
        Assert.False(vm.IsSaving);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);

        Assert.Equal(5, vm.StatusOptions.Count);
        Assert.Empty(vm.Lines);
        Assert.Empty(vm.Members);
        Assert.False(vm.HasMembers);
    }

    [Fact]
    public async Task LoadAsync_NewMode_LoadsMembers_InitializesDefaults_AndAddsOneLine()
    {
        var service = new FakeInvoiceService
        {
            MemberOptionsToReturn = new List<MemberOptionDto>
            {
                new() { Id = 10, Name = "会員A" },
                new() { Id = 20, Name = "会員B" }
            }
        };

        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(1, service.GetMemberOptionsCallCount);

        Assert.Equal(2, vm.Members.Count);
        Assert.True(vm.HasMembers);

        Assert.Equal("INV-NEW", vm.InvoiceNumber);
        Assert.Equal(DateTime.Today, vm.InvoiceDate);
        Assert.Equal(DateTime.Today, vm.DueDate);
        Assert.Equal(string.Empty, vm.Remarks);

        Assert.NotNull(vm.SelectedStatus);
        Assert.Equal(1, vm.SelectedStatus!.Id);

        Assert.NotNull(vm.SelectedMember);
        Assert.Equal(10, vm.SelectedMember!.Id);
        Assert.Equal(10, vm.MemberId);
        Assert.Equal("会員A", vm.MemberName);

        Assert.Single(vm.Lines);
        Assert.Equal(1, vm.Lines[0].LineNo);
        Assert.Equal(string.Empty, vm.Lines[0].Name);
        Assert.Equal(1m, vm.Lines[0].Qty);
        Assert.Equal(0m, vm.Lines[0].UnitPrice);
        Assert.Equal(0m, vm.TotalAmount);
        Assert.Equal("￥0", vm.TotalAmountText);
    }

    [Fact]
    public async Task LoadAsync_EditMode_LoadsMembersAndInvoiceDetail()
    {
        var service = new FakeInvoiceService
        {
            MemberOptionsToReturn = new List<MemberOptionDto>
            {
                new() { Id = 10, Name = "会員A" },
                new() { Id = 20, Name = "会員B" }
            },
            AdminInvoiceDetailToReturn = new InvoiceDetailDto
            {
                Id = 999,
                MemberId = 20,
                MemberName = "会員B",
                InvoiceNumber = "INV-999",
                InvoiceDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 30),
                StatusId = 2,
                Remarks = "備考あり",
                Lines = new List<InvoiceLineDto>
                {
                    new() { Id = 101, LineNo = 2, Name = "明細B", Qty = 2, UnitPrice = 3000m },
                    new() { Id = 100, LineNo = 1, Name = "明細A", Qty = 1, UnitPrice = 5000m }
                }
            }
        };

        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.Edit, 999);

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);
        Assert.Equal(1, service.GetMemberOptionsCallCount);
        Assert.Equal(999, service.LastInvoiceIdForAdminDetail);

        Assert.Equal(20, vm.MemberId);
        Assert.Equal("会員B", vm.MemberName);
        Assert.Equal("INV-999", vm.InvoiceNumber);
        Assert.Equal(new DateTime(2026, 4, 1), vm.InvoiceDate);
        Assert.Equal(new DateTime(2026, 4, 30), vm.DueDate);
        Assert.Equal("備考あり", vm.Remarks);

        Assert.NotNull(vm.SelectedStatus);
        Assert.Equal(2, vm.SelectedStatus!.Id);

        Assert.NotNull(vm.SelectedMember);
        Assert.Equal(20, vm.SelectedMember!.Id);

        Assert.Equal(2, vm.Lines.Count);
        Assert.Equal(1, vm.Lines[0].LineNo);
        Assert.Equal("明細A", vm.Lines[0].Name);
        Assert.Equal(1m, vm.Lines[0].Qty);
        Assert.Equal(5000m, vm.Lines[0].UnitPrice);

        Assert.Equal(2, vm.Lines[1].LineNo);
        Assert.Equal("明細B", vm.Lines[1].Name);
        Assert.Equal(2m, vm.Lines[1].Qty);
        Assert.Equal(3000m, vm.Lines[1].UnitPrice);

        Assert.Equal(11000m, vm.TotalAmount);
        Assert.Equal("￥11,000", vm.TotalAmountText);
    }

    [Fact]
    public async Task LoadAsync_EditMode_WithoutInvoiceId_SetsError()
    {
        var service = new FakeInvoiceService
        {
            MemberOptionsToReturn = new List<MemberOptionDto>
            {
                new() { Id = 1, Name = "会員A" }
            }
        };

        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.Edit);

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.True(vm.HasError);
        Assert.Contains("請求書編集データの取得に失敗しました。", vm.ErrorMessage);
        Assert.Contains("編集対象の請求書IDが指定されていません。", vm.ErrorMessage);
    }

    [Fact]
    public async Task LoadAsync_WhenMemberLoadFails_SetsError()
    {
        var service = new FakeInvoiceService
        {
            ExceptionToThrowOnGetMemberOptions = new InvalidOperationException("member load failed")
        };

        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.True(vm.HasError);
        Assert.Contains("請求書作成データの取得に失敗しました。", vm.ErrorMessage);
        Assert.Contains("member load failed", vm.ErrorMessage);
    }

    [Fact]
    public void SelectedMember_WhenChanged_UpdatesMemberIdAndMemberName()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        var member = new MemberOptionDto { Id = 55, Name = "テスト会員" };

        vm.SelectedMember = member;

        Assert.Equal(55, vm.MemberId);
        Assert.Equal("テスト会員", vm.MemberName);
    }

    [Fact]
    public void AddLine_AddsLineAndRecalculates()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.AddLine();
        vm.Lines[0].Name = "A";
        vm.Lines[0].Qty = 2m;
        vm.Lines[0].UnitPrice = 300m;

        vm.AddLine();
        vm.Lines[1].Name = "B";
        vm.Lines[1].Qty = 1m;
        vm.Lines[1].UnitPrice = 500m;

        Assert.Equal(2, vm.Lines.Count);
        Assert.Equal(1, vm.Lines[0].LineNo);
        Assert.Equal(2, vm.Lines[1].LineNo);
        Assert.Same(vm.Lines[1], vm.SelectedLine);
        Assert.Equal(1100m, vm.TotalAmount);
    }

    [Fact]
    public void RemoveSelectedLine_RemovesLine_AndKeepsAtLeastOneLine()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.AddLine();
        vm.Lines[0].Name = "A";
        vm.Lines[0].Qty = 1;
        vm.Lines[0].UnitPrice = 100;

        vm.AddLine();
        vm.Lines[1].Name = "B";
        vm.Lines[1].Qty = 1;
        vm.Lines[1].UnitPrice = 200;

        vm.SelectedLine = vm.Lines[1];
        vm.RemoveSelectedLine();

        Assert.Single(vm.Lines);
        Assert.Equal(1, vm.Lines[0].LineNo);
        Assert.Equal("A", vm.Lines[0].Name);
        Assert.Equal(100m, vm.TotalAmount);

        vm.SelectedLine = vm.Lines[0];
        vm.RemoveSelectedLine();

        Assert.Single(vm.Lines);
        Assert.Equal(1, vm.Lines[0].LineNo);
    }

    [Fact]
    public void MoveSelectedLineUp_ChangesOrderAndRenumbers()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.AddLine();
        vm.Lines[0].Name = "A";

        vm.AddLine();
        vm.Lines[1].Name = "B";

        vm.AddLine();
        vm.Lines[2].Name = "C";

        vm.SelectedLine = vm.Lines[2];
        vm.MoveSelectedLineUp();

        Assert.Equal(new[] { "A", "C", "B" }, vm.Lines.Select(x => x.Name).ToArray());
        Assert.Equal(1, vm.Lines[0].LineNo);
        Assert.Equal(2, vm.Lines[1].LineNo);
        Assert.Equal(3, vm.Lines[2].LineNo);
    }

    [Fact]
    public void MoveSelectedLineDown_ChangesOrderAndRenumbers()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.AddLine();
        vm.Lines[0].Name = "A";

        vm.AddLine();
        vm.Lines[1].Name = "B";

        vm.AddLine();
        vm.Lines[2].Name = "C";

        vm.SelectedLine = vm.Lines[0];
        vm.MoveSelectedLineDown();

        Assert.Equal(new[] { "B", "A", "C" }, vm.Lines.Select(x => x.Name).ToArray());
        Assert.Equal(1, vm.Lines[0].LineNo);
        Assert.Equal(2, vm.Lines[1].LineNo);
        Assert.Equal(3, vm.Lines[2].LineNo);
    }

    [Fact]
    public void MoveSelectedLineUp_OnFirstLine_DoesNothing()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.AddLine();
        vm.Lines[0].Name = "A";

        vm.AddLine();
        vm.Lines[1].Name = "B";

        vm.SelectedLine = vm.Lines[0];
        vm.MoveSelectedLineUp();

        Assert.Equal(new[] { "A", "B" }, vm.Lines.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void MoveSelectedLineDown_OnLastLine_DoesNothing()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.AddLine();
        vm.Lines[0].Name = "A";

        vm.AddLine();
        vm.Lines[1].Name = "B";

        vm.SelectedLine = vm.Lines[1];
        vm.MoveSelectedLineDown();

        Assert.Equal(new[] { "A", "B" }, vm.Lines.Select(x => x.Name).ToArray());
    }

    [Fact]
    public async Task SaveAsync_NewMode_CreatesInvoiceRequest()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.SelectedMember = new MemberOptionDto { Id = 10, Name = "会員A" };
        vm.SelectedStatus = new InvoiceStatusOption { Id = 1, Name = "未入金" };
        vm.InvoiceNumber = " INV-001 ";
        vm.InvoiceDate = new DateTime(2026, 4, 1);
        vm.DueDate = new DateTime(2026, 4, 30);
        vm.Remarks = " 備考あり ";

        vm.AddLine();
        vm.Lines[0].Name = " 明細A ";
        vm.Lines[0].Qty = 2;
        vm.Lines[0].UnitPrice = 1500;

        var result = await vm.SaveAsync();

        Assert.True(result);
        Assert.False(vm.IsSaving);
        Assert.False(vm.HasError);

        Assert.NotNull(service.LastCreateAdminInvoiceRequest);
        var request = service.LastCreateAdminInvoiceRequest!;

        Assert.Equal(10, request.MemberId);
        Assert.Equal("INV-001", request.InvoiceNumber);
        Assert.Equal(new DateTime(2026, 4, 1), request.InvoiceDate);
        Assert.Equal(new DateTime(2026, 4, 30), request.DueDate);
        Assert.Equal(1, request.StatusId);
        Assert.Equal("備考あり", request.Remarks);

        Assert.Single(request.Lines);
        Assert.Null(request.Lines[0].Id);
        Assert.Equal(1, request.Lines[0].LineNo);
        Assert.Equal("明細A", request.Lines[0].Name);
        Assert.Equal(2m, request.Lines[0].Qty);
        Assert.Equal(1500m, request.Lines[0].UnitPrice);
    }

    [Fact]
    public async Task SaveAsync_EditMode_UpdatesInvoiceRequest()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.Edit, 777);

        vm.SelectedMember = new MemberOptionDto { Id = 20, Name = "会員B" };
        vm.SelectedStatus = new InvoiceStatusOption { Id = 2, Name = "一部入金" };
        vm.InvoiceNumber = "INV-777";
        vm.InvoiceDate = new DateTime(2026, 5, 1);
        vm.DueDate = new DateTime(2026, 5, 31);
        vm.Remarks = "更新テスト";

        vm.AddLine();
        vm.Lines[0].Id = 500;
        vm.Lines[0].Name = "明細X";
        vm.Lines[0].Qty = 3;
        vm.Lines[0].UnitPrice = 2000;

        var result = await vm.SaveAsync();

        Assert.True(result);
        Assert.False(vm.IsSaving);

        Assert.Equal(777, service.LastUpdateAdminInvoiceId);
        Assert.NotNull(service.LastUpdateAdminInvoiceRequest);

        var request = service.LastUpdateAdminInvoiceRequest!;
        Assert.Equal(20, request.MemberId);
        Assert.Equal("INV-777", request.InvoiceNumber);
        Assert.Equal(2, request.StatusId);
        Assert.Equal("更新テスト", request.Remarks);

        Assert.Single(request.Lines);
        Assert.Equal(500, request.Lines[0].Id);
        Assert.Equal("明細X", request.Lines[0].Name);
        Assert.Equal(3m, request.Lines[0].Qty);
        Assert.Equal(2000m, request.Lines[0].UnitPrice);
    }

    [Fact]
    public async Task SaveAsync_WhenCreateFails_ReturnsFalse_AndSetsError()
    {
        var service = new FakeInvoiceService
        {
            ExceptionToThrowOnCreateAdminInvoice = new InvalidOperationException("create failed")
        };

        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.SelectedMember = new MemberOptionDto { Id = 10, Name = "会員A" };
        vm.SelectedStatus = new InvoiceStatusOption { Id = 1, Name = "未入金" };
        vm.InvoiceNumber = "INV-001";
        vm.InvoiceDate = new DateTime(2026, 4, 1);
        vm.DueDate = new DateTime(2026, 4, 30);

        vm.AddLine();
        vm.Lines[0].Name = "明細A";
        vm.Lines[0].Qty = 1;
        vm.Lines[0].UnitPrice = 1000;

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.False(vm.IsSaving);
        Assert.True(vm.HasError);
        Assert.Contains("請求書の作成に失敗しました。", vm.ErrorMessage);
        Assert.Contains("create failed", vm.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_WhenUpdateFails_ReturnsFalse_AndSetsError()
    {
        var service = new FakeInvoiceService
        {
            ExceptionToThrowOnUpdateAdminInvoice = new InvalidOperationException("update failed")
        };

        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.Edit, 1);

        vm.SelectedMember = new MemberOptionDto { Id = 10, Name = "会員A" };
        vm.SelectedStatus = new InvoiceStatusOption { Id = 1, Name = "未入金" };
        vm.InvoiceNumber = "INV-001";
        vm.InvoiceDate = new DateTime(2026, 4, 1);
        vm.DueDate = new DateTime(2026, 4, 30);

        vm.AddLine();
        vm.Lines[0].Name = "明細A";
        vm.Lines[0].Qty = 1;
        vm.Lines[0].UnitPrice = 1000;

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.False(vm.IsSaving);
        Assert.True(vm.HasError);
        Assert.Contains("請求書の更新に失敗しました。", vm.ErrorMessage);
        Assert.Contains("update failed", vm.ErrorMessage);
    }

    [Theory]
    [InlineData("", "請求番号を入力してください。")]
    [InlineData("   ", "請求番号を入力してください。")]
    public async Task SaveAsync_WhenInvoiceNumberInvalid_ReturnsFalse(string invoiceNumber, string expectedMessage)
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.SelectedMember = new MemberOptionDto { Id = 10, Name = "会員A" };
        vm.SelectedStatus = new InvoiceStatusOption { Id = 1, Name = "未入金" };
        vm.InvoiceNumber = invoiceNumber;
        vm.InvoiceDate = new DateTime(2026, 4, 1);
        vm.DueDate = new DateTime(2026, 4, 30);

        vm.AddLine();
        vm.Lines[0].Name = "明細A";
        vm.Lines[0].Qty = 1;
        vm.Lines[0].UnitPrice = 1000;

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.Contains(expectedMessage, vm.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_WhenMemberNotSelected_ReturnsFalse()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.SelectedStatus = new InvoiceStatusOption { Id = 1, Name = "未入金" };
        vm.InvoiceNumber = "INV-001";
        vm.InvoiceDate = new DateTime(2026, 4, 1);
        vm.DueDate = new DateTime(2026, 4, 30);

        vm.AddLine();
        vm.Lines[0].Name = "明細A";
        vm.Lines[0].Qty = 1;
        vm.Lines[0].UnitPrice = 1000;

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.Contains("会員を選択してください。", vm.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_WhenStatusNotSelected_ReturnsFalse()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.SelectedMember = new MemberOptionDto { Id = 10, Name = "会員A" };
        vm.InvoiceNumber = "INV-001";
        vm.InvoiceDate = new DateTime(2026, 4, 1);
        vm.DueDate = new DateTime(2026, 4, 30);

        vm.AddLine();
        vm.Lines[0].Name = "明細A";
        vm.Lines[0].Qty = 1;
        vm.Lines[0].UnitPrice = 1000;

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.Contains("ステータスを選択してください。", vm.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_WhenDueDateBeforeInvoiceDate_ReturnsFalse()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.SelectedMember = new MemberOptionDto { Id = 10, Name = "会員A" };
        vm.SelectedStatus = new InvoiceStatusOption { Id = 1, Name = "未入金" };
        vm.InvoiceNumber = "INV-001";
        vm.InvoiceDate = new DateTime(2026, 4, 30);
        vm.DueDate = new DateTime(2026, 4, 1);

        vm.AddLine();
        vm.Lines[0].Name = "明細A";
        vm.Lines[0].Qty = 1;
        vm.Lines[0].UnitPrice = 1000;

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.Contains("支払期限は請求日以降にしてください。", vm.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_WhenLineNameMissing_ReturnsFalse()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.SelectedMember = new MemberOptionDto { Id = 10, Name = "会員A" };
        vm.SelectedStatus = new InvoiceStatusOption { Id = 1, Name = "未入金" };
        vm.InvoiceNumber = "INV-001";
        vm.InvoiceDate = new DateTime(2026, 4, 1);
        vm.DueDate = new DateTime(2026, 4, 30);

        vm.AddLine();
        vm.Lines[0].Name = " ";
        vm.Lines[0].Qty = 1;
        vm.Lines[0].UnitPrice = 1000;

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.Contains("明細名を入力してください。", vm.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_WhenQtyInvalid_ReturnsFalse()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.SelectedMember = new MemberOptionDto { Id = 10, Name = "会員A" };
        vm.SelectedStatus = new InvoiceStatusOption { Id = 1, Name = "未入金" };
        vm.InvoiceNumber = "INV-001";
        vm.InvoiceDate = new DateTime(2026, 4, 1);
        vm.DueDate = new DateTime(2026, 4, 30);

        vm.AddLine();
        vm.Lines[0].Name = "明細A";
        vm.Lines[0].Qty = 0;
        vm.Lines[0].UnitPrice = 1000;

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.Contains("数量は 0 より大きい値にしてください。", vm.ErrorMessage);
    }

    [Fact]
    public async Task SaveAsync_WhenUnitPriceInvalid_ReturnsFalse()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminInvoiceEditViewModel(service, InvoiceEditorMode.New);

        vm.SelectedMember = new MemberOptionDto { Id = 10, Name = "会員A" };
        vm.SelectedStatus = new InvoiceStatusOption { Id = 1, Name = "未入金" };
        vm.InvoiceNumber = "INV-001";
        vm.InvoiceDate = new DateTime(2026, 4, 1);
        vm.DueDate = new DateTime(2026, 4, 30);

        vm.AddLine();
        vm.Lines[0].Name = "明細A";
        vm.Lines[0].Qty = 1;
        vm.Lines[0].UnitPrice = -1;

        var result = await vm.SaveAsync();

        Assert.False(result);
        Assert.Contains("単価は 0 以上にしてください。", vm.ErrorMessage);
    }
}