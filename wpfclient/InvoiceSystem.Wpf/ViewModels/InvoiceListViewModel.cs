using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;

namespace InvoiceSystem.Wpf.ViewModels;

public class InvoiceListViewModel : INotifyPropertyChanged
{
    private readonly IInvoiceService _invoiceService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public InvoiceListViewModel(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;

        StatusOptions = new ObservableCollection<InvoiceStatusOption>
        {
            new() { Id = null, Name = "すべて" },
            new() { Id = 1, Name = "未入金" },
            new() { Id = 2, Name = "一部入金" },
            new() { Id = 3, Name = "入金済み" },
            new() { Id = 4, Name = "期限超過" },
            new() { Id = 5, Name = "キャンセル" }
        };
    }

    public ObservableCollection<InvoiceListItemDto> Invoices { get; } = new();
    public ObservableCollection<InvoiceStatusOption> StatusOptions { get; }

    private string _invoiceNumber = string.Empty;
    public string InvoiceNumber
    {
        get => _invoiceNumber;
        set
        {
            if (_invoiceNumber != value)
            {
                _invoiceNumber = value;
                OnPropertyChanged();
            }
        }
    }

    private string _memberName = string.Empty;
    public string MemberName
    {
        get => _memberName;
        set
        {
            if (_memberName != value)
            {
                _memberName = value;
                OnPropertyChanged();
            }
        }
    }

    private InvoiceStatusOption? _selectedStatus;
    public InvoiceStatusOption? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (_selectedStatus != value)
            {
                _selectedStatus = value;
                OnPropertyChanged();
            }
        }
    }

    private DateTime? _fromInvoiceDate;
    public DateTime? FromInvoiceDate
    {
        get => _fromInvoiceDate;
        set
        {
            if (_fromInvoiceDate != value)
            {
                _fromInvoiceDate = value;
                OnPropertyChanged();
            }
        }
    }

    private DateTime? _toInvoiceDate;
    public DateTime? ToInvoiceDate
    {
        get => _toInvoiceDate;
        set
        {
            if (_toInvoiceDate != value)
            {
                _toInvoiceDate = value;
                OnPropertyChanged();
            }
        }
    }

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

    private string _resultSummary = "0件";
    public string ResultSummary
    {
        get => _resultSummary;
        set
        {
            if (_resultSummary != value)
            {
                _resultSummary = value;
                OnPropertyChanged();
            }
        }
    }

    private InvoiceListItemDto? _selectedInvoice;
    public InvoiceListItemDto? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (_selectedInvoice != value)
            {
                _selectedInvoice = value;
                OnPropertyChanged();
            }
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var request = new InvoiceSearchRequest
            {
                InvoiceNumber = string.IsNullOrWhiteSpace(InvoiceNumber) ? null : InvoiceNumber.Trim(),
                MemberName = string.IsNullOrWhiteSpace(MemberName) ? null : MemberName.Trim(),
                StatusId = SelectedStatus?.Id,
                FromInvoiceDate = FromInvoiceDate,
                ToInvoiceDate = ToInvoiceDate,
                Page = CurrentPage,
                PageSize = PageSize
            };

            var items = await _invoiceService.SearchInvoicesAsync(request);

            Invoices.Clear();
            foreach (var item in items)
            {
                Invoices.Add(item);
            }

            HasNextPage = items.Count == PageSize;
            ResultSummary = $"{CurrentPage}ページ / {Invoices.Count}件表示";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"請求書一覧の取得に失敗しました。{Environment.NewLine}{ex.Message}";
            Invoices.Clear();
            ResultSummary = "0件";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ResetSearch()
    {
        InvoiceNumber = string.Empty;
        MemberName = string.Empty;
        SelectedStatus = StatusOptions.FirstOrDefault();
        FromInvoiceDate = null;
        ToInvoiceDate = null;
    }

    public string FormatCurrency(decimal value)
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
                OnPropertyChanged(nameof(PageInfoText));
                OnPropertyChanged(nameof(CanMovePrev));
            }
        }
    }

    private int _pageSize = 20;
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

    private bool _hasNextPage;
    public bool HasNextPage
    {
        get => _hasNextPage;
        set
        {
            if (_hasNextPage != value)
            {
                _hasNextPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanMoveNext));
            }
        }
    }

    public bool CanMovePrev => CurrentPage > 1;
    public bool CanMoveNext => HasNextPage;

    public string PageInfoText => $"{CurrentPage} ページ";

    public async Task MovePrevPageAsync()
    {
        if (CurrentPage <= 1) return;
        CurrentPage--;
        await LoadAsync();
    }

    public async Task MoveNextPageAsync()
    {
        if (!HasNextPage) return;
        CurrentPage++;
        await LoadAsync();
    }
}

