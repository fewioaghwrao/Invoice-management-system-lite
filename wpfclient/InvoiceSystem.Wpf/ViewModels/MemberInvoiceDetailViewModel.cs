using InvoiceSystem.Wpf.Infrastructure;
using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace InvoiceSystem.Wpf.ViewModels;

public sealed class MemberInvoiceDetailViewModel : ViewModelBase
{
    private readonly long _invoiceId;
    private readonly IInvoiceService _invoiceService;
    private readonly Action _openInvoiceList;

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

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    private string _invoiceNumber = "---";
    public string InvoiceNumber
    {
        get => _invoiceNumber;
        set => SetProperty(ref _invoiceNumber, value);
    }

    private string _statusName = "---";
    public string StatusName
    {
        get => _statusName;
        set => SetProperty(ref _statusName, value);
    }

    private string _invoiceDateText = "---";
    public string InvoiceDateText
    {
        get => _invoiceDateText;
        set => SetProperty(ref _invoiceDateText, value);
    }

    private string _dueDateText = "---";
    public string DueDateText
    {
        get => _dueDateText;
        set => SetProperty(ref _dueDateText, value);
    }

    private string _totalAmountText = "---";
    public string TotalAmountText
    {
        get => _totalAmountText;
        set => SetProperty(ref _totalAmountText, value);
    }

    private string _paidAmountText = "---";
    public string PaidAmountText
    {
        get => _paidAmountText;
        set => SetProperty(ref _paidAmountText, value);
    }

    private string _remainingAmountText = "---";
    public string RemainingAmountText
    {
        get => _remainingAmountText;
        set => SetProperty(ref _remainingAmountText, value);
    }

    private string _remarks = "備考はありません。";
    public string Remarks
    {
        get => _remarks;
        set => SetProperty(ref _remarks, value);
    }

    private bool _isOverdue;
    public bool IsOverdue
    {
        get => _isOverdue;
        set
        {
            if (SetProperty(ref _isOverdue, value))
            {
                RaisePropertyChanged(nameof(OverdueVisibility));
            }
        }
    }

    public Visibility OverdueVisibility => IsOverdue ? Visibility.Visible : Visibility.Collapsed;

    private bool _isPdfEnabled;
    public bool IsPdfEnabled
    {
        get => _isPdfEnabled;
        set => SetProperty(ref _isPdfEnabled, value);
    }

    private Brush _statusBackground = CreateBrush("#132032");
    public Brush StatusBackground
    {
        get => _statusBackground;
        set => SetProperty(ref _statusBackground, value);
    }

    private Brush _statusBorderBrush = CreateBrush("#334155");
    public Brush StatusBorderBrush
    {
        get => _statusBorderBrush;
        set => SetProperty(ref _statusBorderBrush, value);
    }

    private Brush _statusForeground = CreateBrush("#E2E8F0");
    public Brush StatusForeground
    {
        get => _statusForeground;
        set => SetProperty(ref _statusForeground, value);
    }

    private string _footerText = "Invoice Detail / Member";
    public string FooterText
    {
        get => _footerText;
        set => SetProperty(ref _footerText, value);
    }

    public ICommand BackCommand { get; }
    public ICommand OpenPdfCommand { get; }

    public MemberInvoiceDetailViewModel(
        long invoiceId,
        IInvoiceService invoiceService,
        Action openInvoiceList)
    {
        _invoiceId = invoiceId;
        _invoiceService = invoiceService;
        _openInvoiceList = openInvoiceList;

        BackCommand = new RelayCommand(BackToList);
        OpenPdfCommand = new RelayCommand(async () => await OpenPdfAsync(), () => IsPdfEnabled && !IsLoading);

        IsPdfEnabled = false;
    }

    public async Task InitializeAsync()
    {
        await LoadDetailAsync();
    }

    private async Task LoadDetailAsync()
    {
        try
        {
            ToggleLoading(true, "読み込み中...");
            ErrorMessage = string.Empty;
            IsPdfEnabled = false;
            RaiseCommandStates();

            var detail = await _invoiceService.GetMemberInvoiceDetailAsync(_invoiceId);
            BindDetail(detail);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsPdfEnabled = false;
            RaiseCommandStates();
        }
        finally
        {
            ToggleLoading(false);
            RaiseCommandStates();
        }
    }

    private void BindDetail(InvoiceDetailDto detail)
    {
        InvoiceNumber = detail.InvoiceNumber;
        StatusName = detail.StatusName;
        InvoiceDateText = FormatDate(detail.InvoiceDate);
        DueDateText = FormatDate(detail.DueDate);
        TotalAmountText = FormatMoney(detail.TotalAmount);
        PaidAmountText = FormatMoney(detail.PaidAmount);
        RemainingAmountText = FormatMoney(detail.RemainingAmount);
        Remarks = string.IsNullOrWhiteSpace(detail.Remarks) ? "備考はありません。" : detail.Remarks;

        IsOverdue =
            detail.DueDate.Date < DateTime.Today &&
            detail.RemainingAmount > 0;

        ApplyStatusStyle(detail.StatusName);

        IsPdfEnabled = true;
        RaiseCommandStates();
    }

    private void ApplyStatusStyle(string statusName)
    {
        if (statusName.Contains("入金済"))
        {
            StatusBackground = CreateBrush("#0B2A1E");
            StatusBorderBrush = CreateBrush("#166534");
            StatusForeground = CreateBrush("#86EFAC");
            return;
        }

        if (statusName.Contains("一部"))
        {
            StatusBackground = CreateBrush("#3B2506");
            StatusBorderBrush = CreateBrush("#92400E");
            StatusForeground = CreateBrush("#FCD34D");
            return;
        }

        if (statusName.Contains("未入金"))
        {
            StatusBackground = CreateBrush("#3A1118");
            StatusBorderBrush = CreateBrush("#9F1239");
            StatusForeground = CreateBrush("#FDA4AF");
            return;
        }

        StatusBackground = CreateBrush("#132032");
        StatusBorderBrush = CreateBrush("#334155");
        StatusForeground = CreateBrush("#E2E8F0");
    }

    private async Task OpenPdfAsync()
    {
        try
        {
            IsPdfEnabled = false;
            ToggleLoading(true, "PDFを取得しています...");
            ErrorMessage = string.Empty;
            RaiseCommandStates();

            var pdf = await _invoiceService.GetMemberInvoicePdfAsync(_invoiceId);

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
            ErrorMessage = ex.Message;

            MessageBox.Show(
                $"PDFを開けませんでした。\n\n{ex.Message}",
                "PDF表示エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ToggleLoading(false);
            IsPdfEnabled = true;
            RaiseCommandStates();
        }
    }

    private void BackToList()
    {
        _openInvoiceList();
    }

    private void ToggleLoading(bool isLoading, string message = "")
    {
        LoadingMessage = isLoading ? message : string.Empty;
    }

    private void RaiseCommandStates()
    {
        if (OpenPdfCommand is RelayCommand pdfCommand)
        {
            pdfCommand.RaiseCanExecuteChanged();
        }

        if (BackCommand is RelayCommand backCommand)
        {
            backCommand.RaiseCanExecuteChanged();
        }
    }

    private static string FormatDate(DateTime value)
    {
        if (value == default)
            return "---";

        return value.ToString("yyyy/MM/dd");
    }

    private static string FormatMoney(decimal value)
    {
        return $"{value:N0} 円";
    }

    private static Brush CreateBrush(string hex)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }
}
