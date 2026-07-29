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

public class PaymentDetailViewModel : INotifyPropertyChanged
{
    private readonly IPaymentService _paymentService;
    private readonly IInvoiceService _invoiceService;
    private readonly long _paymentId;

    private bool _isLoading;
    private bool _isSaving;
    private bool _hasError;
    private string _errorMessage = string.Empty;

    private long _id;
    private DateTime _paymentDate;
    private string _payerName = string.Empty;
    private string _method = string.Empty;
    private decimal _amount;
    private decimal _allocatedAmount;
    private decimal _unallocatedAmount;
    private string _status = "UNALLOCATED";

    private PaymentAllocationEditRow? _selectedAllocationRow;
    private InvoiceListItemDto? _selectedCandidate;

    private string _searchInvoiceNumber = string.Empty;
    private string _searchMemberName = string.Empty;

    public PaymentDetailViewModel(
        long paymentId,
        IPaymentService paymentService,
        IInvoiceService invoiceService)
    {
        _paymentId = paymentId;
        _paymentService = paymentService;
        _invoiceService = invoiceService;

        AllocationRows = new ObservableCollection<PaymentAllocationEditRow>();
        CandidateInvoices = new ObservableCollection<InvoiceListItemDto>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PaymentAllocationEditRow> AllocationRows { get; }
    public ObservableCollection<InvoiceListItemDto> CandidateInvoices { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsSaving
    {
        get => _isSaving;
        set => SetProperty(ref _isSaving, value);
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

    public long Id
    {
        get => _id;
        set
        {
            if (SetProperty(ref _id, value))
            {
                OnPropertyChanged(nameof(PaymentIdText));
            }
        }
    }

    public string PaymentIdText => $"PAY-{Id:D3}";

    public DateTime PaymentDate
    {
        get => _paymentDate;
        set
        {
            if (SetProperty(ref _paymentDate, value))
            {
                OnPropertyChanged(nameof(PaymentDateText));
            }
        }
    }

    public string PaymentDateText => PaymentDate == default ? "-" : PaymentDate.ToString("yyyy/MM/dd");

    public string PayerName
    {
        get => _payerName;
        set => SetProperty(ref _payerName, value);
    }

    public string Method
    {
        get => _method;
        set => SetProperty(ref _method, value);
    }

    public decimal Amount
    {
        get => _amount;
        set
        {
            if (SetProperty(ref _amount, value))
            {
                OnPropertyChanged(nameof(AmountText));
            }
        }
    }

    public string AmountText => FormatCurrency(Amount);

    public decimal AllocatedAmount
    {
        get => _allocatedAmount;
        set
        {
            if (SetProperty(ref _allocatedAmount, value))
            {
                OnPropertyChanged(nameof(AllocatedAmountText));
            }
        }
    }

    public string AllocatedAmountText => FormatCurrency(AllocatedAmount);

    public decimal UnallocatedAmount
    {
        get => _unallocatedAmount;
        set
        {
            if (SetProperty(ref _unallocatedAmount, value))
            {
                OnPropertyChanged(nameof(UnallocatedAmountText));
            }
        }
    }

    public string UnallocatedAmountText => FormatCurrency(UnallocatedAmount);

    public string Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string StatusText => Status?.ToUpperInvariant() switch
    {
        "ALLOCATED" => "割当済",
        "PARTIAL" => "一部割当",
        _ => "未割当"
    };

    public PaymentAllocationEditRow? SelectedAllocationRow
    {
        get => _selectedAllocationRow;
        set => SetProperty(ref _selectedAllocationRow, value);
    }

    public InvoiceListItemDto? SelectedCandidate
    {
        get => _selectedCandidate;
        set => SetProperty(ref _selectedCandidate, value);
    }

    public string SearchInvoiceNumber
    {
        get => _searchInvoiceNumber;
        set => SetProperty(ref _searchInvoiceNumber, value);
    }

    public string SearchMemberName
    {
        get => _searchMemberName;
        set => SetProperty(ref _searchMemberName, value);
    }

    public decimal InputAllocatedSum
    {
        get
        {
            decimal sum = 0m;
            foreach (var row in AllocationRows)
            {
                if (decimal.TryParse(row.AmountText, out var amount) && amount > 0)
                {
                    sum += amount;
                }
            }
            return sum;
        }
    }

    public string InputAllocatedSumText => FormatCurrency(InputAllocatedSum);

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            var dto = await _paymentService.GetByIdAsync(_paymentId);

            Id = dto.Id;
            PaymentDate = dto.PaymentDate;
            PayerName = dto.PayerName ?? string.Empty;
            Method = dto.Method ?? string.Empty;
            Amount = dto.Amount;
            AllocatedAmount = dto.AllocatedAmount;
            UnallocatedAmount = dto.UnallocatedAmount;
            Status = dto.Status ?? "UNALLOCATED";

            AllocationRows.Clear();
            foreach (var item in dto.Allocations ?? new List<PaymentAllocationDto>())
            {
                AllocationRows.Add(new PaymentAllocationEditRow
                {
                    InvoiceId = item.InvoiceId,
                    InvoiceNumber = item.InvoiceNumber ?? string.Empty,
                    AmountText = item.Amount.ToString(CultureInfo.InvariantCulture)
                });
            }

            if (AllocationRows.Count == 0)
            {
                AddRow();
            }

            CandidateInvoices.Clear();
            NotifyAllocationSummaryChanged();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void AddRow()
    {
        AllocationRows.Add(new PaymentAllocationEditRow());
        NotifyAllocationSummaryChanged();
    }

    public void RemoveRow(PaymentAllocationEditRow? row)
    {
        if (row is null)
            return;

        AllocationRows.Remove(row);

        if (AllocationRows.Count == 0)
        {
            AddRow();
        }

        NotifyAllocationSummaryChanged();
    }

    public void ClearRowInvoice(PaymentAllocationEditRow? row)
    {
        if (row is null)
            return;

        row.InvoiceId = null;
        row.InvoiceNumber = string.Empty;
        row.MemberName = string.Empty;
        OnPropertyChanged(nameof(AllocationRows));
    }

    public async Task SearchCandidatesAsync()
    {
        try
        {
            HasError = false;
            ErrorMessage = string.Empty;

            var request = new InvoiceSearchRequest
            {
                InvoiceNumber = string.IsNullOrWhiteSpace(SearchInvoiceNumber) ? null : SearchInvoiceNumber.Trim(),
                MemberName = string.IsNullOrWhiteSpace(SearchMemberName) ? null : SearchMemberName.Trim(),
                Page = 1,
                PageSize = 50
            };

            var result = await _invoiceService.SearchInvoicesAsync(request);

            CandidateInvoices.Clear();
            foreach (var item in result)
            {
                CandidateInvoices.Add(item);
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            CandidateInvoices.Clear();
        }
    }

    public void ApplyCandidateToSelectedRow()
    {
        if (SelectedAllocationRow is null || SelectedCandidate is null)
            return;

        SelectedAllocationRow.InvoiceId = SelectedCandidate.Id;
        SelectedAllocationRow.InvoiceNumber = SelectedCandidate.InvoiceNumber ?? string.Empty;

        var memberNameProperty = SelectedCandidate.GetType().GetProperty("MemberName");
        if (memberNameProperty != null)
        {
            SelectedAllocationRow.MemberName = memberNameProperty.GetValue(SelectedCandidate)?.ToString() ?? string.Empty;
        }

        OnPropertyChanged(nameof(AllocationRows));
    }

    public string? Validate()
    {
        var errors = new List<string>();

        var usedRows = AllocationRows
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.InvoiceNumber) ||
                !string.IsNullOrWhiteSpace(x.AmountText))
            .ToList();

        if (usedRows.Count == 0)
        {
            errors.Add("割当行を1件以上入力してください。");
        }

        for (var i = 0; i < usedRows.Count; i++)
        {
            var row = usedRows[i];

            if (!row.InvoiceId.HasValue || row.InvoiceId.Value <= 0)
            {
                errors.Add($"割当行{i + 1}: 請求書を選択してください。");
            }

            if (string.IsNullOrWhiteSpace(row.AmountText))
            {
                errors.Add($"割当行{i + 1}: 金額は必須です。");
            }
            else if (!decimal.TryParse(row.AmountText, out var amount) || amount <= 0)
            {
                errors.Add($"割当行{i + 1}: 金額は1以上の数値で入力してください。");
            }
        }

        var duplicateIds = usedRows
            .Where(x => x.InvoiceId.HasValue)
            .GroupBy(x => x.InvoiceId!.Value)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            errors.Add("同じ請求書が複数行で選択されています。1行にまとめてください。");
        }

        if (InputAllocatedSum > Amount)
        {
            errors.Add("割当合計が入金額を超えています。");
        }

        return errors.Count == 0 ? null : string.Join(Environment.NewLine, errors);
    }

    public async Task SaveAsync()
    {
        var validation = Validate();
        if (!string.IsNullOrWhiteSpace(validation))
        {
            throw new Exception(validation);
        }

        try
        {
            IsSaving = true;
            HasError = false;
            ErrorMessage = string.Empty;

            var lines = AllocationRows
                .Where(x => x.InvoiceId.HasValue &&
                            !string.IsNullOrWhiteSpace(x.AmountText) &&
                            decimal.TryParse(x.AmountText, out var amount) &&
                            amount > 0)
                .Select(x => new PaymentAllocationLineDto
                {
                    InvoiceId = x.InvoiceId!.Value,
                    Amount = decimal.Parse(x.AmountText)
                })
                .ToList();

            await _paymentService.SaveAllocationsAsync(_paymentId, lines);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            throw;
        }
        finally
        {
            IsSaving = false;
        }
    }

    public void NotifyAllocationSummaryChanged()
    {
        OnPropertyChanged(nameof(InputAllocatedSum));
        OnPropertyChanged(nameof(InputAllocatedSumText));
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