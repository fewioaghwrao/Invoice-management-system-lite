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

public sealed class MemberInvoiceListViewModel : ViewModelBase
{
    private readonly object? _currentUser;
    private readonly IInvoiceService _invoiceService;
    private readonly Action<long> _openDetail;
    private readonly Action _openDashboard;

    private const int DefaultPageSize = 10;

    private int _currentPage = 1;
    private int _totalPages = 1;

    public ObservableCollection<MemberInvoiceRowViewModel> InvoiceRows { get; } = new();
    public ObservableCollection<int> YearOptions { get; } = new();
    public ObservableCollection<FilterOption> MonthOptions { get; } = new();
    public ObservableCollection<FilterOption> StatusOptions { get; } = new();

    private string _welcomeMessage = "ようこそ";
    public string WelcomeMessage
    {
        get => _welcomeMessage;
        set => SetProperty(ref _welcomeMessage, value);
    }

    private string _subWelcomeMessage = "請求書情報を表示します。";
    public string SubWelcomeMessage
    {
        get => _subWelcomeMessage;
        set => SetProperty(ref _subWelcomeMessage, value);
    }

    private int _selectedYear;
    public int SelectedYear
    {
        get => _selectedYear;
        set => SetProperty(ref _selectedYear, value);
    }

    private FilterOption? _selectedMonthOption;
    public FilterOption? SelectedMonthOption
    {
        get => _selectedMonthOption;
        set => SetProperty(ref _selectedMonthOption, value);
    }

    private FilterOption? _selectedStatusOption;
    public FilterOption? SelectedStatusOption
    {
        get => _selectedStatusOption;
        set => SetProperty(ref _selectedStatusOption, value);
    }

    private string _keyword = string.Empty;
    public string Keyword
    {
        get => _keyword;
        set => SetProperty(ref _keyword, value);
    }

    private string _summaryText = "0 件";
    public string SummaryText
    {
        get => _summaryText;
        set => SetProperty(ref _summaryText, value);
    }

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

    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

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

    private string _pageInfoText = "1 / 1";
    public string PageInfoText
    {
        get => _pageInfoText;
        set => SetProperty(ref _pageInfoText, value);
    }

    public bool CanGoPrevious => _currentPage > 1;
    public bool CanGoNext => _currentPage < _totalPages;

    public ICommand SearchCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand PrevCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand BackToDashboardCommand { get; }
    public ICommand OpenDetailCommand { get; }
    public ICommand OpenPdfCommand { get; }

    public MemberInvoiceListViewModel(
        object? currentUser,
        IInvoiceService invoiceService,
        Action<long> openDetail,
        Action openDashboard)
    {
        _currentUser = currentUser;
        _invoiceService = invoiceService;
        _openDetail = openDetail;
        _openDashboard = openDashboard;

        SearchCommand = new RelayCommand(async () => await SearchAsync(), () => !IsLoading);
        RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsLoading);
        ClearCommand = new RelayCommand(async () => await ClearAsync(), () => !IsLoading);
        PrevCommand = new RelayCommand(async () => await GoPreviousAsync(), () => !IsLoading && CanGoPrevious);
        NextCommand = new RelayCommand(async () => await GoNextAsync(), () => !IsLoading && CanGoNext);
        BackToDashboardCommand = new RelayCommand(BackToDashboard, () => !IsLoading);
        OpenDetailCommand = new RelayCommand(
            parameter => OpenDetail(parameter),
            parameter => !IsLoading && parameter is MemberInvoiceRowViewModel);
        OpenPdfCommand = new RelayCommand(
            async parameter => await OpenPdfAsync(parameter),
            parameter => !IsLoading && parameter is MemberInvoiceRowViewModel);

        InitializeUserHeader();
        InitializeFilters();
    }

    public async Task InitializeAsync()
    {
        await LoadInvoicesAsync(resetPage: true);
    }

    private void InitializeUserHeader()
    {
        var name = ReadProperty(_currentUser, "Name") ?? "会員ユーザー";
        var email = ReadProperty(_currentUser, "Email") ?? "メールアドレス未設定";

        WelcomeMessage = $"ようこそ、{name}さん";
        SubWelcomeMessage = $"{email} の請求書を表示しています";
    }

    private void InitializeFilters()
    {
        var now = DateTime.Now.Year;

        YearOptions.Clear();
        for (int year = now + 1; year >= now - 5; year--)
        {
            YearOptions.Add(year);
        }

        SelectedYear = DateTime.Now.Year;

        MonthOptions.Clear();
        MonthOptions.Add(new FilterOption("全て", "all"));
        for (int month = 1; month <= 12; month++)
        {
            MonthOptions.Add(new FilterOption($"{month}月", month.ToString()));
        }
        SelectedMonthOption = MonthOptions.FirstOrDefault();

        StatusOptions.Clear();
        StatusOptions.Add(new FilterOption("全て", "all"));
        StatusOptions.Add(new FilterOption("未入金", "unpaid"));
        StatusOptions.Add(new FilterOption("一部入金", "partial"));
        StatusOptions.Add(new FilterOption("入金済み", "paid"));
        SelectedStatusOption = StatusOptions.FirstOrDefault();

        Keyword = string.Empty;
    }

    private async Task SearchAsync()
    {
        await LoadInvoicesAsync(resetPage: true);
    }

    private async Task RefreshAsync()
    {
        await LoadInvoicesAsync(resetPage: false);
    }

    private async Task ClearAsync()
    {
        SelectedYear = DateTime.Now.Year;
        SelectedMonthOption = MonthOptions.FirstOrDefault(x => x.Value == "all");
        SelectedStatusOption = StatusOptions.FirstOrDefault(x => x.Value == "all");
        Keyword = string.Empty;
        _currentPage = 1;

        await LoadInvoicesAsync(resetPage: true);
    }

    private async Task GoPreviousAsync()
    {
        if (_currentPage <= 1)
            return;

        _currentPage--;
        await LoadInvoicesAsync(resetPage: false);
    }

    private async Task GoNextAsync()
    {
        if (_currentPage >= _totalPages)
            return;

        _currentPage++;
        await LoadInvoicesAsync(resetPage: false);
    }

    private void BackToDashboard()
    {
        _openDashboard();
    }

    private void OpenDetail(object? parameter)
    {
        if (parameter is not MemberInvoiceRowViewModel row)
            return;

        _openDetail(row.Id);
    }

    private async Task OpenPdfAsync(object? parameter)
    {
        if (parameter is not MemberInvoiceRowViewModel row)
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
    private async Task LoadInvoicesAsync(bool resetPage)
    {
        try
        {
            ToggleLoading(true, "読み込み中...");

            if (resetPage)
            {
                _currentPage = 1;
            }

            var selectedMonth = SelectedMonthOption?.Value ?? "all";
            var selectedStatus = SelectedStatusOption?.Value ?? "all";
            var keyword = Keyword?.Trim() ?? string.Empty;

            var result = await _invoiceService.GetMemberInvoicesAsync(
                SelectedYear,
                selectedMonth,
                selectedStatus,
                keyword,
                _currentPage,
                DefaultPageSize);

            BindResult(result);
        }
        catch
        {
            InvoiceRows.Clear();
            SummaryText = "0 件";
            PageInfoText = "1 / 1";
            IsOverdueBannerVisible = false;
        }
        finally
        {
            ToggleLoading(false);
        }
    }

    private void BindResult(AccountInvoiceListDto? result)
    {
        InvoiceRows.Clear();

        if (result == null)
        {
            SummaryText = "0 件";
            PageInfoText = "1 / 1";
            IsOverdueBannerVisible = false;
            RaisePagingStateChanged();
            return;
        }

        if (result.AvailableYears is { Count: > 0 })
        {
            var currentSelection = SelectedYear;

            YearOptions.Clear();
            foreach (var y in result.AvailableYears.OrderByDescending(x => x))
            {
                YearOptions.Add(y);
            }

            SelectedYear = YearOptions.Contains(currentSelection)
                ? currentSelection
                : YearOptions.First();
        }

        if (result.Items != null)
        {
            foreach (var item in result.Items)
            {
                InvoiceRows.Add(MemberInvoiceRowViewModel.FromDto(item));
            }
        }

        var totalCount = result.TotalCount;
        _totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)Math.Max(1, result.PageSize)));
        _currentPage = Math.Max(1, result.Page);

        SummaryText = $"{totalCount:N0} 件";
        PageInfoText = $"{_currentPage} / {_totalPages}";
        IsOverdueBannerVisible = InvoiceRows.Any(x => x.IsOverdue && !x.IsPaid);

        RaisePagingStateChanged();
    }

    private void ToggleLoading(bool isLoading, string message = "")
    {
        LoadingMessage = isLoading ? message : string.Empty;
        RaiseCommandStates();
    }

    private void RaisePagingStateChanged()
    {
        RaisePropertyChanged(nameof(CanGoPrevious));
        RaisePropertyChanged(nameof(CanGoNext));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        if (SearchCommand is RelayCommand searchCommand)
            searchCommand.RaiseCanExecuteChanged();

        if (RefreshCommand is RelayCommand refreshCommand)
            refreshCommand.RaiseCanExecuteChanged();

        if (ClearCommand is RelayCommand clearCommand)
            clearCommand.RaiseCanExecuteChanged();

        if (PrevCommand is RelayCommand prevCommand)
            prevCommand.RaiseCanExecuteChanged();

        if (NextCommand is RelayCommand nextCommand)
            nextCommand.RaiseCanExecuteChanged();

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

public sealed class FilterOption
{
    public string Label { get; }
    public string Value { get; }

    public FilterOption(string label, string value)
    {
        Label = label;
        Value = value;
    }
}
