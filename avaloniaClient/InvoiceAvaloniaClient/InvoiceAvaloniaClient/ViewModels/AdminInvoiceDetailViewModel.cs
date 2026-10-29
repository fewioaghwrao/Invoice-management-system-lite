using InvoiceAvaloniaClient.Models;
using InvoiceAvaloniaClient.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia.Threading;
using Avalonia.Media;

using InvoiceAvaloniaClient.Helpers;

namespace InvoiceAvaloniaClient.ViewModels;

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

    public IBrush StatusBackground => StatusKey switch
    {
        "未入金" => new SolidColorBrush(Color.Parse("#3F1D1D")),
        "一部入金" => new SolidColorBrush(Color.Parse("#3B2A12")),
        "入金済み" => new SolidColorBrush(Color.Parse("#0F2F24")),
        "期限超過" => new SolidColorBrush(Color.Parse("#4C0519")),
        "キャンセル" => new SolidColorBrush(Color.Parse("#1F2937")),
        _ => new SolidColorBrush(Color.Parse("#1E293B"))
    };

    public IBrush StatusForeground => StatusKey switch
    {
        "未入金" => new SolidColorBrush(Color.Parse("#FCA5A5")),
        "一部入金" => new SolidColorBrush(Color.Parse("#FCD34D")),
        "入金済み" => new SolidColorBrush(Color.Parse("#86EFAC")),
        "期限超過" => new SolidColorBrush(Color.Parse("#FDA4AF")),
        "キャンセル" => new SolidColorBrush(Color.Parse("#CBD5E1")),
        _ => Brushes.White
    };

    public IBrush StatusBorderBrush => StatusKey switch
    {
        "未入金" => new SolidColorBrush(Color.Parse("#B91C1C")),
        "一部入金" => new SolidColorBrush(Color.Parse("#D97706")),
        "入金済み" => new SolidColorBrush(Color.Parse("#059669")),
        "期限超過" => new SolidColorBrush(Color.Parse("#E11D48")),
        "キャンセル" => new SolidColorBrush(Color.Parse("#475569")),
        _ => new SolidColorBrush(Color.Parse("#334155"))
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
            await DialogService.ShowErrorAsync($"PDFを開けませんでした。{Environment.NewLine}{ex.Message}",
                "PDF表示エラー");
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
        Dispatcher.UIThread.Invoke(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }
}