using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace InvoiceSystem.Wpf.ViewModels;

public class AdminInvoiceDetailViewModel : INotifyPropertyChanged
{
    private readonly IInvoiceService _invoiceService;
    private readonly long _invoiceId;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AdminInvoiceDetailViewModel(IInvoiceService invoiceService, long invoiceId)
    {
        _invoiceService = invoiceService;
        _invoiceId = invoiceId;
    }

    public ObservableCollection<InvoiceLineDto> Lines { get; } = new();
    public ObservableCollection<InvoicePaymentAllocationDto> Allocations { get; } = new();
    public ObservableCollection<InvoiceReminderHistoryDto> Reminders { get; } = new();

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

    private long _id;
    public long Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    private long _memberId;
    public long MemberId
    {
        get => _memberId;
        set { _memberId = value; OnPropertyChanged(); }
    }

    private string _memberName = string.Empty;
    public string MemberName
    {
        get => _memberName;
        set { _memberName = value; OnPropertyChanged(); }
    }

    private string _invoiceNumber = string.Empty;
    public string InvoiceNumber
    {
        get => _invoiceNumber;
        set { _invoiceNumber = value; OnPropertyChanged(); }
    }

    private DateTime _invoiceDate;
    public DateTime InvoiceDate
    {
        get => _invoiceDate;
        set { _invoiceDate = value; OnPropertyChanged(); }
    }

    private DateTime _dueDate;
    public DateTime DueDate
    {
        get => _dueDate;
        set { _dueDate = value; OnPropertyChanged(); }
    }

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        set { _totalAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalAmountText)); }
    }

    private decimal _paidAmount;
    public decimal PaidAmount
    {
        get => _paidAmount;
        set { _paidAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(PaidAmountText)); }
    }

    private decimal _remainingAmount;
    public decimal RemainingAmount
    {
        get => _remainingAmount;
        set { _remainingAmount = value; OnPropertyChanged(); OnPropertyChanged(nameof(RemainingAmountText)); }
    }

    private long _statusId;
    public long StatusId
    {
        get => _statusId;
        set { _statusId = value; OnPropertyChanged(); }
    }

    private string _statusName = string.Empty;
    public string StatusName
    {
        get => _statusName;
        set
        {
            _statusName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusBackground));
            OnPropertyChanged(nameof(StatusForeground));
            OnPropertyChanged(nameof(StatusBorderBrush));
        }
    }

    private string? _pdfPath;
    public string? PdfPath
    {
        get => _pdfPath;
        set { _pdfPath = value; OnPropertyChanged(); }
    }

    private string? _remarks;
    public string? Remarks
    {
        get => _remarks;
        set { _remarks = value; OnPropertyChanged(); }
    }

    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set { _createdAt = value; OnPropertyChanged(); }
    }

    public string TotalAmountText => FormatCurrency(TotalAmount);
    public string PaidAmountText => FormatCurrency(PaidAmount);
    public string RemainingAmountText => FormatCurrency(RemainingAmount);

    private string StatusKey => (StatusName ?? string.Empty).Trim();

    public Brush StatusBackground => StatusKey switch
    {
        "未入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F1D1D")),
        "一部入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B2A12")),
        "入金済み" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F2F24")),
        "期限超過" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C0519")),
        "キャンセル" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E293B"))
    };

    public Brush StatusForeground => StatusKey switch
    {
        "未入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5")),
        "一部入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D")),
        "入金済み" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC")),
        "期限超過" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDA4AF")),
        "キャンセル" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CBD5E1")),
        _ => Brushes.White
    };

    public Brush StatusBorderBrush => StatusKey switch
    {
        "未入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B91C1C")),
        "一部入金" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706")),
        "入金済み" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669")),
        "期限超過" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E11D48")),
        "キャンセル" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569")),
        _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#334155"))
    };

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var dto = await _invoiceService.GetAdminInvoiceDetailAsync(_invoiceId);

            Id = dto.Id;
            MemberId = dto.MemberId;
            MemberName = dto.MemberName;
            InvoiceNumber = dto.InvoiceNumber;
            InvoiceDate = dto.InvoiceDate;
            DueDate = dto.DueDate;
            TotalAmount = dto.TotalAmount;
            PaidAmount = dto.PaidAmount;
            RemainingAmount = dto.RemainingAmount;
            StatusId = dto.StatusId;
            StatusName = dto.StatusName;
            PdfPath = dto.PdfPath;
            Remarks = dto.Remarks;
            CreatedAt = dto.CreatedAt;

            Lines.Clear();
            foreach (var line in dto.Lines)
            {
                Lines.Add(line);
            }

            Allocations.Clear();
            foreach (var allocation in dto.Allocations)
            {
                Allocations.Add(allocation);
            }

            Reminders.Clear();
            foreach (var reminder in dto.Reminders)
            {
                Reminders.Add(reminder);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"請求書詳細の取得に失敗しました。{Environment.NewLine}{ex.Message}";
            Lines.Clear();
            Allocations.Clear();
            Reminders.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task OpenPdfAsync()
    {
        try
        {
            var pdf = await _invoiceService.GetAdminInvoicePdfAsync(_invoiceId);
            var path = InvoiceService.SavePdfToTempFile(pdf);

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"PDFを開けませんでした。{Environment.NewLine}{ex.Message}",
                "PDF表示エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public string FormatCurrency(decimal value)
    {
        return value.ToString("C0", CultureInfo.GetCultureInfo("ja-JP"));
    }

    public string FormatDate(DateTime value)
    {
        return value.ToString("yyyy/MM/dd");
    }

    public string FormatMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
            return "—";

        var upper = method.Trim().ToUpperInvariant();

        if (upper.Contains("BANK") || method.Contains("振込"))
            return "銀行振込";
        if (upper.Contains("CASH") || method.Contains("現金"))
            return "現金";
        if (upper.Contains("CARD") || method.Contains("カード"))
            return "カード";

        return method;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }
}