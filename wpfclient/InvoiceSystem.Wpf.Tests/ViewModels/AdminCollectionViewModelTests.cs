using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Tests.Fakes;
using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace InvoiceSystem.Wpf.Tests.ViewModels;

public class AdminCollectionViewModelTests
{
    [Fact]
    public void Constructor_InitialValues_AreExpected()
    {
        var service = new FakeInvoiceService();

        var vm = new AdminCollectionViewModel(service, 123);

        Assert.Equal("EMAIL", vm.SelectedChannel);
        Assert.Equal("NORMAL", vm.SelectedTone);
        Assert.Equal("メール", vm.SelectedChannelLabel);
        Assert.Equal("標準", vm.SelectedToneLabel);
        Assert.Equal("テンプレ送付。", vm.Memo);
        Assert.False(vm.IsLoading);
        Assert.False(vm.IsSaving);
        Assert.False(vm.HasError);
        Assert.False(vm.CanRecord); // Total/Paid未設定なので RemainingAmount=0
        Assert.Equal("送信完了として記録", vm.RecordButtonText);

        Assert.Equal(3, vm.ChannelOptions.Count);
        Assert.Contains(vm.ChannelOptions, x => x.Value == "EMAIL" && x.Label == "メール");
        Assert.Contains(vm.ChannelOptions, x => x.Value == "PHONE" && x.Label == "電話");
        Assert.Contains(vm.ChannelOptions, x => x.Value == "LETTER" && x.Label == "書面");

        Assert.Equal(3, vm.ToneOptions.Count);
        Assert.Contains(vm.ToneOptions, x => x.Value == "SOFT" && x.Label == "ソフト");
        Assert.Contains(vm.ToneOptions, x => x.Value == "NORMAL" && x.Label == "標準");
        Assert.Contains(vm.ToneOptions, x => x.Value == "STRONG" && x.Label == "強め");
    }

    [Fact]
    public async Task LoadAsync_Success_AppliesSnapshotAndLogs_AndUsesLatestNextActionDate()
    {
        var service = new FakeInvoiceService
        {
            SnapshotToReturn = new CollectionSnapshotDto
            {
                InvoiceId = 10,
                InvoiceNumber = "INV-001",
                MemberName = "山田太郎",
                MemberEmail = "taro@example.com",
                InvoiceDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 10),
                Total = 100000m,
                PaidTotal = 30000m
            },
            LogsToReturn = new List<DunningLogDto>
            {
                new()
                {
                    Id = 1,
                    At = new DateTime(2026, 4, 11, 9, 0, 0),
                    Channel = "EMAIL",
                    Tone = "SOFT",
                    Title = "初回督促",
                    Memo = "1回目",
                    NextActionDate = new DateTime(2026, 4, 15)
                },
                new()
                {
                    Id = 2,
                    At = new DateTime(2026, 4, 12, 10, 0, 0),
                    Channel = "PHONE",
                    Tone = "NORMAL",
                    Title = "2回目督促",
                    Memo = null,
                    NextActionDate = new DateTime(2026, 4, 20)
                }
            }
        };

        var vm = new AdminCollectionViewModel(service, 999);

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.False(vm.HasError);

        Assert.Equal(10, vm.InvoiceId);
        Assert.Equal("INV-001", vm.InvoiceNumber);
        Assert.Equal("山田太郎", vm.MemberName);
        Assert.Equal("taro@example.com", vm.MemberEmail);
        Assert.Equal(new DateTime(2026, 4, 1), vm.InvoiceDate);
        Assert.Equal(new DateTime(2026, 4, 10), vm.DueDate);

        Assert.Equal(100000m, vm.TotalAmount);
        Assert.Equal(30000m, vm.PaidTotal);
        Assert.Equal(70000m, vm.RemainingAmount);
        Assert.True(vm.CanRecord);

        Assert.Equal("￥100,000", vm.TotalText);
        Assert.Equal("入金済：￥30,000", vm.PaidTotalText);
        Assert.Equal("￥70,000", vm.RemainingText);
        Assert.Equal("請求日：2026/04/01", vm.InvoiceDateText);
        Assert.Equal("2026/04/10", vm.DueDateText);

        Assert.Equal(new DateTime(2026, 4, 20), vm.NextActionDate);
        Assert.Equal("2026/04/20", vm.NextActionDateTextForDialog);

        Assert.Equal(2, vm.Logs.Count);

        var first = vm.Logs[0];
        Assert.Equal(2, first.Id); // At desc
        Assert.Equal("PHONE", first.Channel);
        Assert.Equal("電話", first.ChannelLabel);
        Assert.Equal("NORMAL", first.Tone);
        Assert.Equal("標準", first.ToneLabel);
        Assert.Equal("2回目督促", first.Title);
        Assert.Equal("—", first.Memo);
        Assert.Equal("2026/04/20", first.NextActionDateText);

        var second = vm.Logs[1];
        Assert.Equal(1, second.Id);
        Assert.Equal("EMAIL", second.Channel);
        Assert.Equal("メール", second.ChannelLabel);
        Assert.Equal("SOFT", second.Tone);
        Assert.Equal("ソフト", second.ToneLabel);

        Assert.Equal(999, service.LastInvoiceIdForSnapshot);
        Assert.Equal(999, service.LastInvoiceIdForLogs);

        Assert.Contains("INV-001", vm.SubjectPreview);
        Assert.Contains("山田太郎 様", vm.BodyPreview);
        Assert.Contains("2026/04/10", vm.BodyPreview);
        Assert.Contains("￥70,000", vm.BodyPreview);
    }

    [Fact]
    public async Task LoadAsync_WhenLogsHaveNoNextActionDate_FallsBackToDueDate()
    {
        var service = new FakeInvoiceService
        {
            SnapshotToReturn = new CollectionSnapshotDto
            {
                InvoiceId = 11,
                InvoiceNumber = "INV-002",
                MemberName = "佐藤花子",
                MemberEmail = "hanako@example.com",
                InvoiceDate = new DateTime(2026, 4, 2),
                DueDate = new DateTime(2026, 4, 25),
                Total = 50000m,
                PaidTotal = 10000m
            },
            LogsToReturn = new List<DunningLogDto>
            {
                new()
                {
                    Id = 1,
                    At = new DateTime(2026, 4, 26, 8, 0, 0),
                    Channel = "EMAIL",
                    Tone = "NORMAL",
                    Title = "督促",
                    Memo = "確認依頼",
                    NextActionDate = null
                }
            }
        };

        var vm = new AdminCollectionViewModel(service, 11);

        await vm.LoadAsync();

        Assert.Equal(new DateTime(2026, 4, 25), vm.NextActionDate);
        Assert.Equal("2026/04/25", vm.NextActionDateTextForDialog);
    }

    [Fact]
    public async Task LoadAsync_WhenSnapshotFails_SetsError_AndClearsLogs()
    {
        var service = new FakeInvoiceService
        {
            ExceptionToThrowOnGetSnapshot = new InvalidOperationException("snapshot error"),
            LogsToReturn = new List<DunningLogDto>
            {
                new()
                {
                    Id = 1,
                    At = DateTime.Now,
                    Channel = "EMAIL",
                    Title = "dummy"
                }
            }
        };

        var vm = new AdminCollectionViewModel(service, 1);
        vm.Logs.Add(new DunningLogRowViewModel { Id = 99, Title = "old" });

        await vm.LoadAsync();

        Assert.False(vm.IsLoading);
        Assert.True(vm.HasError);
        Assert.Contains("催促情報の取得に失敗しました。", vm.ErrorMessage);
        Assert.Contains("snapshot error", vm.ErrorMessage);
        Assert.Empty(vm.Logs);
    }

    [Fact]
    public async Task RecordAsync_WhenRemainingAmountIsZero_ReturnsFalse_AndDoesNotCallCreate()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminCollectionViewModel(service, 5)
        {
            TotalAmount = 10000m,
            PaidTotal = 10000m
        };

        var result = await vm.RecordAsync();

        Assert.False(result);
        Assert.Equal("未回収残額が0円のため、催促記録は不要です。", vm.ErrorMessage);
        Assert.Null(service.LastCreateRequest);
        Assert.Equal(0, service.LastInvoiceIdForCreate);
    }

    [Fact]
    public async Task RecordAsync_Success_FirstRecord_CreatesRequestAndReloadsLogs()
    {
        var service = new FakeInvoiceService
        {
            SnapshotToReturn = new CollectionSnapshotDto
            {
                InvoiceId = 20,
                InvoiceNumber = "INV-020",
                MemberName = "田中一郎",
                MemberEmail = "ichiro@example.com",
                InvoiceDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 30),
                Total = 80000m,
                PaidTotal = 10000m
            },
            LogsToReturn = new List<DunningLogDto>()
        };

        var vm = new AdminCollectionViewModel(service, 20);
        await vm.LoadAsync();

        vm.SelectedChannel = "PHONE";
        vm.SelectedTone = "STRONG";
        vm.NextActionDate = new DateTime(2026, 5, 2);
        vm.Memo = "至急連絡";

        service.LogsToReturn = new List<DunningLogDto>
        {
            new()
            {
                Id = 100,
                At = new DateTime(2026, 5, 1, 10, 0, 0),
                Channel = "PHONE",
                Tone = "STRONG",
                Title = "初回督促（強め）",
                Memo = "至急連絡",
                NextActionDate = new DateTime(2026, 5, 2)
            }
        };

        var result = await vm.RecordAsync();

        Assert.True(result);
        Assert.False(vm.IsSaving);
        Assert.False(vm.HasError);

        Assert.Equal(20, service.LastInvoiceIdForCreate);
        Assert.NotNull(service.LastCreateRequest);

        Assert.Equal("PHONE", service.LastCreateRequest!.Channel);
        Assert.Equal("STRONG", service.LastCreateRequest.Tone);
        Assert.Equal("初回督促（強め）", service.LastCreateRequest.Title);
        Assert.Equal("至急連絡", service.LastCreateRequest.Memo);
        Assert.Equal(new DateTime(2026, 5, 2), service.LastCreateRequest.NextActionDate);
        Assert.Contains("【至急】お支払いのお願い（INV-020）", service.LastCreateRequest.Subject);
        Assert.Contains("田中一郎 様", service.LastCreateRequest.BodyText);
        Assert.Contains("至急ご対応をお願いいたします。", service.LastCreateRequest.BodyText);

        Assert.Single(vm.Logs);
        Assert.Equal("初回督促（強め）", vm.Logs[0].Title);
    }

    [Fact]
    public async Task RecordAsync_WhenMemoIsBlank_UsesDefaultMemo()
    {
        var service = new FakeInvoiceService
        {
            SnapshotToReturn = new CollectionSnapshotDto
            {
                InvoiceId = 21,
                InvoiceNumber = "INV-021",
                MemberName = "鈴木次郎",
                MemberEmail = "jiro@example.com",
                InvoiceDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 15),
                Total = 30000m,
                PaidTotal = 0m
            },
            LogsToReturn = new List<DunningLogDto>()
        };

        var vm = new AdminCollectionViewModel(service, 21);
        await vm.LoadAsync();

        vm.Memo = "   ";

        var result = await vm.RecordAsync();

        Assert.True(result);
        Assert.NotNull(service.LastCreateRequest);
        Assert.Equal("テンプレ送付。", service.LastCreateRequest!.Memo);
    }

    [Fact]
    public async Task RecordAsync_WhenLogsAlreadyExist_UsesIncrementedTitle()
    {
        var service = new FakeInvoiceService
        {
            SnapshotToReturn = new CollectionSnapshotDto
            {
                InvoiceId = 22,
                InvoiceNumber = "INV-022",
                MemberName = "高橋三郎",
                MemberEmail = "saburo@example.com",
                InvoiceDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 20),
                Total = 90000m,
                PaidTotal = 10000m
            },
            LogsToReturn = new List<DunningLogDto>
            {
                new()
                {
                    Id = 1,
                    At = new DateTime(2026, 4, 21, 9, 0, 0),
                    Channel = "EMAIL",
                    Tone = "SOFT",
                    Title = "初回督促（ソフト）",
                    Memo = "1回目"
                },
                new()
                {
                    Id = 2,
                    At = new DateTime(2026, 4, 22, 9, 0, 0),
                    Channel = "PHONE",
                    Tone = "NORMAL",
                    Title = "督促（2回目 / 標準）",
                    Memo = "2回目"
                }
            }
        };

        var vm = new AdminCollectionViewModel(service, 22);
        await vm.LoadAsync();

        vm.SelectedTone = "SOFT";

        var result = await vm.RecordAsync();

        Assert.True(result);
        Assert.NotNull(service.LastCreateRequest);
        Assert.Equal("督促（3回目 / ソフト）", service.LastCreateRequest!.Title);
    }

    [Fact]
    public async Task RecordAsync_WhenCreateFails_ReturnsFalse_AndSetsError()
    {
        var service = new FakeInvoiceService
        {
            SnapshotToReturn = new CollectionSnapshotDto
            {
                InvoiceId = 23,
                InvoiceNumber = "INV-023",
                MemberName = "伊藤四郎",
                MemberEmail = "shiro@example.com",
                InvoiceDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 10),
                Total = 20000m,
                PaidTotal = 0m
            },
            LogsToReturn = new List<DunningLogDto>()
        };

        var vm = new AdminCollectionViewModel(service, 23);
        await vm.LoadAsync();

        service.ExceptionToThrowOnCreateLog = new InvalidOperationException("create failed");

        var result = await vm.RecordAsync();

        Assert.False(result);
        Assert.False(vm.IsSaving);
        Assert.True(vm.HasError);
        Assert.Contains("催促履歴の登録に失敗しました。", vm.ErrorMessage);
        Assert.Contains("create failed", vm.ErrorMessage);
    }

    [Theory]
    [InlineData("SOFT", "【ご確認】お支払い状況のご確認のお願い（INV-TONE）", "いつもお世話になっております。", "過ぎている可能性がございます。")]
    [InlineData("NORMAL", "【重要】お支払いのお願い（INV-TONE）", "お世話になっております。", "ご入金状況をご確認のうえ、ご対応をお願いいたします。")]
    [InlineData("STRONG", "【至急】お支払いのお願い（INV-TONE）", "恐れ入りますが、至急ご確認ください。", "至急ご対応をお願いいたします。")]
    public async Task ToneChange_UpdatesSubjectAndBodyPreview(
        string tone,
        string expectedSubject,
        string expectedIntro,
        string expectedBodyPart)
    {
        var service = new FakeInvoiceService
        {
            SnapshotToReturn = new CollectionSnapshotDto
            {
                InvoiceId = 24,
                InvoiceNumber = "INV-TONE",
                MemberName = "テスト太郎",
                MemberEmail = "test@example.com",
                InvoiceDate = new DateTime(2026, 4, 1),
                DueDate = new DateTime(2026, 4, 18),
                Total = 40000m,
                PaidTotal = 5000m
            },
            LogsToReturn = new List<DunningLogDto>()
        };

        var vm = new AdminCollectionViewModel(service, 24);
        await vm.LoadAsync();

        vm.SelectedTone = tone;

        Assert.Equal(expectedSubject, vm.SubjectPreview);
        Assert.Contains("テスト太郎 様", vm.BodyPreview);
        Assert.Contains(expectedIntro, vm.BodyPreview);
        Assert.Contains(expectedBodyPart, vm.BodyPreview);
        Assert.Contains("【請求書番号】INV-TONE", vm.BodyPreview);
        Assert.Contains("【未回収残額】￥35,000", vm.BodyPreview);
    }

    [Fact]
    public void ChannelAndToneLabel_UnknownValues_AreReturnedAsIs()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminCollectionViewModel(service, 1);

        vm.SelectedChannel = "SMS";
        vm.SelectedTone = "CUSTOM";

        Assert.Equal("SMS", vm.SelectedChannelLabel);
        Assert.Equal("CUSTOM", vm.SelectedToneLabel);
    }

    [Fact]
    public void RemainingAmount_DoesNotGoBelowZero()
    {
        var service = new FakeInvoiceService();
        var vm = new AdminCollectionViewModel(service, 1)
        {
            TotalAmount = 1000m,
            PaidTotal = 5000m
        };

        Assert.Equal(0m, vm.RemainingAmount);
        Assert.Equal("￥0", vm.RemainingText);
        Assert.False(vm.CanRecord);
    }
}