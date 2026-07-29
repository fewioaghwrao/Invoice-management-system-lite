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

public class PaymentCreateViewModel : INotifyPropertyChanged
{
    private readonly IPaymentService _paymentService;

    private bool _isLoading;
    private bool _isSaving;
    private bool _hasError;
    private string _errorMessage = string.Empty;

    private MemberOptionDto? _selectedMember;
    private DateTime _paymentDate = DateTime.Today;
    private string _payerName = string.Empty;
    private bool _payerNameTouched;
    private string _amountText = string.Empty;
    private PaymentMethodOption? _selectedMethod;

    public PaymentCreateViewModel(IPaymentService paymentService)
    {
        _paymentService = paymentService;

        Members = new ObservableCollection<MemberOptionDto>();
        MethodOptions = new ObservableCollection<PaymentMethodOption>
        {
            new PaymentMethodOption { Value = "BANK_TRANSFER", Label = "振込" },
            new PaymentMethodOption { Value = "CASH", Label = "現金" },
            new PaymentMethodOption { Value = "CARD", Label = "クレジットカード" },
            new PaymentMethodOption { Value = "OTHER", Label = "その他" }
        };

        SelectedMethod = MethodOptions.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MemberOptionDto> Members { get; }
    public ObservableCollection<PaymentMethodOption> MethodOptions { get; }

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

    public MemberOptionDto? SelectedMember
    {
        get => _selectedMember;
        set
        {
            if (SetProperty(ref _selectedMember, value))
            {
                if (!_payerNameTouched)
                {
                    PayerName = value?.Name ?? string.Empty;
                }
            }
        }
    }

    public DateTime PaymentDate
    {
        get => _paymentDate;
        set => SetProperty(ref _paymentDate, value);
    }

    public string PayerName
    {
        get => _payerName;
        set => SetProperty(ref _payerName, value);
    }

    public string AmountText
    {
        get => _amountText;
        set => SetProperty(ref _amountText, value);
    }

    public PaymentMethodOption? SelectedMethod
    {
        get => _selectedMethod;
        set => SetProperty(ref _selectedMethod, value);
    }

    public void MarkPayerNameTouched()
    {
        _payerNameTouched = true;
    }

    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;

            Members.Clear();
            var members = await _paymentService.GetMemberOptionsAsync();

            foreach (var member in members)
            {
                Members.Add(member);
            }

            if (SelectedMethod is null && MethodOptions.Count > 0)
            {
                SelectedMethod = MethodOptions[0];
            }
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

    public string? Validate()
    {
        var errors = new List<string>();

        if (IsLoading)
        {
            errors.Add("会員一覧を読み込み中です。");
        }

        if (Members.Count == 0)
        {
            errors.Add("有効な会員が存在しません。会員を作成してから入金登録してください。");
        }

        if (SelectedMember is null)
        {
            errors.Add("会員（入金元）を選択してください。");
        }

        if (PaymentDate == default)
        {
            errors.Add("入金日は必須です。");
        }

        if (string.IsNullOrWhiteSpace(PayerName))
        {
            errors.Add("入金名義は必須です。");
        }

        if (string.IsNullOrWhiteSpace(AmountText))
        {
            errors.Add("入金額は必須です。");
        }
        else if (!decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            if (!decimal.TryParse(AmountText, out amount) || amount <= 0)
            {
                errors.Add("入金額は 1 以上の数値で入力してください。");
            }
        }

        return errors.Count == 0 ? null : string.Join(Environment.NewLine, errors);
    }

    public async Task<long> SaveAsync()
    {
        var validationError = Validate();
        if (!string.IsNullOrWhiteSpace(validationError))
        {
            throw new Exception(validationError);
        }

        try
        {
            IsSaving = true;
            HasError = false;
            ErrorMessage = string.Empty;

            decimal amount;
            if (!decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
            {
                amount = decimal.Parse(AmountText);
            }

            var request = new CreatePaymentRequestDto
            {
                MemberId = SelectedMember!.Id,
                PaymentDate = PaymentDate.Date,
                Amount = amount,
                PayerName = PayerName.Trim(),
                Method = SelectedMethod?.Value
            };

            var id = await _paymentService.CreateAsync(request);
            return id;
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
