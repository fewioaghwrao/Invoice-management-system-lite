using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace InvoiceSystem.Wpf.ViewModels;

public class PaymentListViewModel : INotifyPropertyChanged
{
    private readonly IPaymentService _paymentService;

    private bool _isLoading;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private int _selectedYear;
    private PaymentFilterOption? _selectedMonth;
    private PaymentFilterOption? _selectedStatus;
    private string _keyword = string.Empty;
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalCount;
    private PaymentListItemDto? _selectedPayment;

    public PaymentListViewModel(IPaymentService paymentService)
    {
        _paymentService = paymentService;

        var currentYear = DateTime.Today.Year;
        SelectedYear = currentYear;

        YearOptions = new ObservableCollection<int>(
            Enumerable.Range(currentYear - 1, 3));

        MonthOptions = new ObservableCollection<PaymentFilterOption>(
            new[]
            {
                new PaymentFilterOption { Value = "all", Label = "すべて" },
                new PaymentFilterOption { Value = "1", Label = "1月" },
                new PaymentFilterOption { Value = "2", Label = "2月" },
                new PaymentFilterOption { Value = "3", Label = "3月" },
                new PaymentFilterOption { Value = "4", Label = "4月" },
                new PaymentFilterOption { Value = "5", Label = "5月" },
                new PaymentFilterOption { Value = "6", Label = "6月" },
                new PaymentFilterOption { Value = "7", Label = "7月" },
                new PaymentFilterOption { Value = "8", Label = "8月" },
                new PaymentFilterOption { Value = "9", Label = "9月" },
                new PaymentFilterOption { Value = "10", Label = "10月" },
                new PaymentFilterOption { Value = "11", Label = "11月" },
                new PaymentFilterOption { Value = "12", Label = "12月" },
            });

        StatusOptions = new ObservableCollection<PaymentFilterOption>(
            new[]
            {
                new PaymentFilterOption { Value = "all", Label = "すべて" },
                new PaymentFilterOption { Value = "UNALLOCATED", Label = "未割当" },
                new PaymentFilterOption { Value = "PARTIAL", Label = "一部割当" },
                new PaymentFilterOption { Value = "ALLOCATED", Label = "割当済" },
            });

        SelectedMonth = MonthOptions.FirstOrDefault();
        SelectedStatus = StatusOptions.FirstOrDefault();

        Payments = new ObservableCollection<PaymentListItemDto>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<int> YearOptions { get; }
    public ObservableCollection<PaymentFilterOption> MonthOptions { get; }
    public ObservableCollection<PaymentFilterOption> StatusOptions { get; }
    public ObservableCollection<PaymentListItemDto> Payments { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public int SelectedYear
    {
        get => _selectedYear;
        set => SetProperty(ref _selectedYear, value);
    }

    public PaymentFilterOption? SelectedMonth
    {
        get => _selectedMonth;
        set => SetProperty(ref _selectedMonth, value);
    }

    public PaymentFilterOption? SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public string Keyword
    {
        get => _keyword;
        set => SetProperty(ref _keyword, value);
    }

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(CurrentPageText));
                OnPropertyChanged(nameof(CanMovePrev));
                OnPropertyChanged(nameof(CanMoveNext));
            }
        }
    }

    public int PageSize
    {
        get => _pageSize;
        set => SetProperty(ref _pageSize, value);
    }

    public int TotalCount
    {
        get => _totalCount;
        set
        {
            if (SetProperty(ref _totalCount, value))
            {
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CurrentPageText));
                OnPropertyChanged(nameof(ResultCountText));
                OnPropertyChanged(nameof(CanMovePrev));
                OnPropertyChanged(nameof(CanMoveNext));
            }
        }
    }

    public PaymentListItemDto? SelectedPayment
    {
        get => _selectedPayment;
        set => SetProperty(ref _selectedPayment, value);
    }

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        set
        {
            if (SetProperty(ref _totalAmount, value))
            {
                OnPropertyChanged(nameof(TotalAmountText));
            }
        }
    }

    private decimal _allocatedTotal;
    public decimal AllocatedTotal
    {
        get => _allocatedTotal;
        set
        {
            if (SetProperty(ref _allocatedTotal, value))
            {
                OnPropertyChanged(nameof(AllocatedTotalText));
            }
        }
    }

    private decimal _unallocatedTotal;
    public decimal UnallocatedTotal
    {
        get => _unallocatedTotal;
        set
        {
            if (SetProperty(ref _unallocatedTotal, value))
            {
                OnPropertyChanged(nameof(UnallocatedTotalText));
            }
        }
    }

    public string TotalAmountText => FormatCurrency(TotalAmount);
    public string AllocatedTotalText => FormatCurrency(AllocatedTotal);
    public string UnallocatedTotalText => FormatCurrency(UnallocatedTotal);

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));
    public bool CanMovePrev => CurrentPage > 1;
    public bool CanMoveNext => CurrentPage < TotalPages;
    public string CurrentPageText => $"{CurrentPage} / {TotalPages} ページ";
    public string ResultCountText => $"入金一覧（{TotalCount}件）";

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            var request = new PaymentSearchRequest
            {
                Year = SelectedYear,
                Month = SelectedMonth?.Value ?? "all",
                Status = SelectedStatus?.Value ?? "all",
                Q = Keyword ?? string.Empty,
                Page = CurrentPage,
                PageSize = PageSize
            };

            var result = await _paymentService.SearchAsync(request);

            Payments.Clear();
            foreach (var item in result.Rows ?? new List<PaymentListItemDto>())
            {
                Payments.Add(item);
            }

            TotalCount = result.TotalCount;
            TotalAmount = result.Summary?.TotalAmount ?? 0m;
            AllocatedTotal = result.Summary?.AllocatedTotal ?? 0m;
            UnallocatedTotal = result.Summary?.UnallocatedTotal ?? 0m;

            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            Payments.Clear();
            TotalCount = 0;
            TotalAmount = 0m;
            AllocatedTotal = 0m;
            UnallocatedTotal = 0m;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ResetSearch()
    {
        var currentYear = DateTime.Today.Year;
        SelectedYear = currentYear;
        SelectedMonth = MonthOptions.FirstOrDefault();
        SelectedStatus = StatusOptions.FirstOrDefault();
        Keyword = string.Empty;
    }

    public async Task MovePrevPageAsync()
    {
        if (!CanMovePrev)
            return;

        CurrentPage--;
        await LoadAsync();
    }

    public async Task MoveNextPageAsync()
    {
        if (!CanMoveNext)
            return;

        CurrentPage++;
        await LoadAsync();
    }

    private static string FormatCurrency(decimal value)
    {
        return value.ToString("C0", CultureInfo.GetCultureInfo("ja-JP"));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
