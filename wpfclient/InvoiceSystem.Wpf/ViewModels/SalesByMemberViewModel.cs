using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace InvoiceSystem.Wpf.ViewModels;

public class SalesByMemberViewModel : INotifyPropertyChanged
{
    private readonly ISalesService _salesService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SalesByMemberViewModel(ISalesService salesService)
    {
        _salesService = salesService;

        var now = DateTime.Now;
        SelectedYear = now.Year;

        MonthOptions.Add(new MonthOption(null, "全月"));
        for (int i = 1; i <= 12; i++)
        {
            MonthOptions.Add(new MonthOption(i, $"{i}月"));
        }

        SelectedMonth = MonthOptions[0];
    }

    public ObservableCollection<SalesByMemberRowDto> Rows { get; } = new();
    public ObservableCollection<MonthOption> MonthOptions { get; } = new();

    private int _selectedYear;
    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (_selectedYear != value)
            {
                _selectedYear = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedYearText));
            }
        }
    }

    public string SelectedYearText => $"{SelectedYear}年";

    private MonthOption? _selectedMonth;
    public MonthOption? SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            _selectedMonth = value;
            OnPropertyChanged();
        }
    }

    private string _keyword = string.Empty;
    public string Keyword
    {
        get => _keyword;
        set
        {
            if (_keyword != value)
            {
                _keyword = value;
                OnPropertyChanged();
            }
        }
    }

    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (_currentPage != value)
            {
                _currentPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PageText));
            }
        }
    }

    private int _pageSize = 10;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (_pageSize != value)
            {
                _pageSize = value;
                OnPropertyChanged();
            }
        }
    }

    private int _totalCount;
    public int TotalCount
    {
        get => _totalCount;
        set
        {
            if (_totalCount != value)
            {
                _totalCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(PageText));
            }
        }
    }

    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, PageSize)));
    public string PageText => $"{CurrentPage} / {TotalPages} ページ";

    private decimal _invoiceTotal;
    public decimal InvoiceTotal
    {
        get => _invoiceTotal;
        set
        {
            if (_invoiceTotal != value)
            {
                _invoiceTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InvoiceTotalText));
            }
        }
    }

    public string InvoiceTotalText => FormatCurrency(InvoiceTotal);

    private decimal _paidTotal;
    public decimal PaidTotal
    {
        get => _paidTotal;
        set
        {
            if (_paidTotal != value)
            {
                _paidTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PaidTotalText));
            }
        }
    }

    public string PaidTotalText => FormatCurrency(PaidTotal);

    private decimal _remainingTotal;
    public decimal RemainingTotal
    {
        get => _remainingTotal;
        set
        {
            if (_remainingTotal != value)
            {
                _remainingTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RemainingTotalText));
            }
        }
    }

    public string RemainingTotalText => FormatCurrency(RemainingTotal);

    private double _recoveryRate;
    public double RecoveryRate
    {
        get => _recoveryRate;
        set
        {
            if (Math.Abs(_recoveryRate - value) > 0.0001)
            {
                _recoveryRate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RecoveryRateText));
            }
        }
    }

    public string RecoveryRateText => $"{RecoveryRate:F1}%";

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var result = await _salesService.GetSalesByMemberAsync(
                SelectedYear,
                SelectedMonth?.Value,
                Keyword?.Trim() ?? string.Empty,
                CurrentPage,
                PageSize);

            Rows.Clear();
            foreach (var row in result.Rows)
            {
                Rows.Add(row);
            }

            TotalCount = result.TotalCount;
            InvoiceTotal = result.Summary?.InvoiceTotal ?? 0;
            PaidTotal = result.Summary?.PaidTotal ?? 0;
            RemainingTotal = result.Summary?.RemainingTotal ?? 0;
            RecoveryRate = result.Summary?.RecoveryRate ?? 0;

            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"顧客別売上集計の読込に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void MovePreviousYear() => SelectedYear--;
    public void MoveNextYear() => SelectedYear++;

    public async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    public async Task MovePreviousPageAsync()
    {
        if (CurrentPage <= 1) return;
        CurrentPage--;
        await LoadAsync();
    }

    public async Task MoveNextPageAsync()
    {
        if (CurrentPage >= TotalPages) return;
        CurrentPage++;
        await LoadAsync();
    }

    private static string FormatCurrency(decimal value)
    {
        return value.ToString("C0", CultureInfo.GetCultureInfo("ja-JP"));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }
}
