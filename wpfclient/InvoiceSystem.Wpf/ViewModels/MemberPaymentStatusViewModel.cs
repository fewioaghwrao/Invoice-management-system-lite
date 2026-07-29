using InvoiceSystem.Wpf.Infrastructure;
using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace InvoiceSystem.Wpf.ViewModels;

public sealed class MemberPaymentStatusViewModel : ViewModelBase
{
    private readonly object? _currentUser;
    private readonly IInvoiceService _invoiceService;
    private readonly Action<long> _openDetail;
    private readonly Action _openDashboard;

    private const int PageSize = 50;

    public ObservableCollection<MemberPaymentStatusRowViewModel> Rows { get; } = new();

    private string _welcomeMessage = "ようこそ";
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }

    private string _subWelcomeMessage = "未払い状況を表示しています。";
    public string SubWelcomeMessage
    {
        get => _subWelcomeMessage;
        set => SetProperty(ref _subWelcomeMessage, value);
    }

    private string _summaryText = "0 件";
    public string SummaryText
    {
        get => _summaryText;
        set => SetProperty(ref _summaryText, value);
    }

    private string _unpaidCountText = "0 件";
    public string UnpaidCountText
    {
        get => _unpaidCountText;
        set => SetProperty(ref _unpaidCountText, value);
    }

    private string _remainingTotalText = "0 円";
    public string RemainingTotalText
    {
        get => _remainingTotalText;
        set => SetProperty(ref _remainingTotalText, value);
    }

    private string _overdueCountText = "0 件";
    public string OverdueCountText
    {
        get => _overdueCountText;
        set => SetProperty(ref _overdueCountText, value);
    }

    private bool _isOverdueBannerVisible;
    public bool IsOverdueBannerVisible
    {
        get => _isOverdueBannerVisible;
        set
        {
            if (SetProperty(ref _isOverdueBannerVisible, value))
            {
                RaisePropertyChanged(nameof(OverdueBannerVisibility));
            }
        }
    }

    public Visibility OverdueBannerVisibility =>
        IsOverdueBannerVisible ? Visibility.Visible : Visibility.Collapsed;

    private string _loadingMessage = string.Empty;
    public string LoadingMessage
    {
        get => _loadingMessage;
        set
        {
            if (SetProperty(ref _loadingMessage, value))
            {
                RaisePropertyChanged(nameof(IsLoading));
                RaisePropertyChanged(nameof(LoadingVisibility));
            }
        }
    }

    public bool IsLoading => !string.IsNullOrWhiteSpace(LoadingMessage);

    public Visibility LoadingVisibility =>
        IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public ICommand RefreshCommand { get; }
    public ICommand BackToDashboardCommand { get; }
    public ICommand OpenDetailCommand { get; }
    public ICommand OpenPdfCommand { get; }

    public MemberPaymentStatusViewModel(
        object? currentUser,
        IInvoiceService invoiceService,
        Action<long> openDetail,
        Action openDashboard)
    {
        _currentUser = currentUser;
        _invoiceService = invoiceService;
        _openDetail = openDetail;
        _openDashboard = openDashboard;

        RefreshCommand = new RelayCommand(async () => await LoadAsync(), () => !IsLoading);
        BackToDashboardCommand = new RelayCommand(BackToDashboard, () => !IsLoading);
        OpenDetailCommand = new RelayCommand(
            parameter => OpenDetail(parameter),
            parameter => !IsLoading && parameter is MemberPaymentStatusRowViewModel);
        OpenPdfCommand = new RelayCommand(
            async parameter => await OpenPdfAsync(parameter),
            parameter => !IsLoading && parameter is MemberPaymentStatusRowViewModel);

        InitializeUserHeader();
    }

    public async Task InitializeAsync()
    {
        await LoadAsync();
    }

    private void InitializeUserHeader()
    {
        var name = ReadProperty(_currentUser, "Name") ?? "会員ユーザー";
        var email = ReadProperty(_currentUser, "Email") ?? "メールアドレス未設定";

        WelcomeMessage = $"ようこそ、{name}さん";
        SubWelcomeMessage = $"{email} の未払い状況を表示しています";
    }

    private async Task LoadAsync()
    {
        try
        {
            ToggleLoading(true, "読み込み中...");
            Rows.Clear();

            var year = DateTime.Now.Year;
            const string month = "all";
            const string q = "";
            const int page = 1;

            var unpaid = await _invoiceService.GetMemberInvoicesWithBalanceAsync(
                year,
                month,
                "unpaid",
                q,
                page,
                PageSize);

            var partial = await _invoiceService.GetMemberInvoicesWithBalanceAsync(
                year,
                month,
                "partial",
                q,
                page,
                PageSize);

            var mergedItems = (unpaid.Items ?? Enumerable.Empty<AccountInvoiceListItemDto>())
                .Concat(partial.Items ?? Enumerable.Empty<AccountInvoiceListItemDto>())
                .OrderBy(x => x.DueAt)
                .Take(PageSize)
                .ToList();

            foreach (var item in mergedItems)
            {
                Rows.Add(MemberPaymentStatusRowViewModel.FromDto(item));
            }

            BindSummary();
        }
        catch (Exception ex)
        {
            Rows.Clear();
            BindSummary();

            MessageBox.Show(
                $"入金確認情報の取得に失敗しました。\n\n{ex.Message}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ToggleLoading(false);
        }
    }

    private void BindSummary()
    {
        var count = Rows.Count;
        var remainingTotal = Rows.Sum(x => Math.Max(0m, x.RemainingAmount));
        var overdueCount = Rows.Count(x => x.IsOverdue && x.RemainingAmount > 0);

        SummaryText = $"{count:N0} 件";
        UnpaidCountText = $"{count:N0} 件";
        RemainingTotalText = $"{remainingTotal:N0} 円";
        OverdueCountText = $"{overdueCount:N0} 件";
        IsOverdueBannerVisible = overdueCount > 0;
    }

    private void BackToDashboard()
    {
        _openDashboard();
    }

    private void OpenDetail(object? parameter)
    {
        if (parameter is not MemberPaymentStatusRowViewModel row)
            return;

        _openDetail(row.Id);
    }

    private async Task OpenPdfAsync(object? parameter)
    {
        if (parameter is not MemberPaymentStatusRowViewModel row)
            return;

        try
        {
            ToggleLoading(true, "読み込み中...");

            var pdf = await _invoiceService.GetMemberInvoicePdfAsync(row.Id);

            if (!string.Equals(pdf.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("PDF形式ではないレスポンスを受信しました。");
            }

            var filePath = InvoiceService.SavePdfToTempFile(pdf);

            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"PDFを開けませんでした。\n\n{ex.Message}",
                "PDF表示エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ToggleLoading(false);
        }
    }

    private void ToggleLoading(bool isLoading, string message = "")
    {
        LoadingMessage = isLoading ? message : string.Empty;
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        if (RefreshCommand is RelayCommand refreshCommand)
            refreshCommand.RaiseCanExecuteChanged();

        if (BackToDashboardCommand is RelayCommand backCommand)
            backCommand.RaiseCanExecuteChanged();

        if (OpenDetailCommand is RelayCommand detailCommand)
            detailCommand.RaiseCanExecuteChanged();

        if (OpenPdfCommand is RelayCommand pdfCommand)
            pdfCommand.RaiseCanExecuteChanged();
    }

    private static string? ReadProperty(object? target, string propertyName)
    {
        if (target == null) return null;

        var prop = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        var value = prop?.GetValue(target);
        return value?.ToString();
    }
}