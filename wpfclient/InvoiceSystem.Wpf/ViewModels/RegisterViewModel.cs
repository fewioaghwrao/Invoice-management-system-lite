using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using InvoiceSystem.Wpf.Commands;
using System.Text.RegularExpressions;

namespace InvoiceSystem.Wpf.ViewModels;

public class RegisterViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _authService;

    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _postalCode = string.Empty;
    private string _address = string.Empty;
    private string _phone = string.Empty;
    private bool _isLoading;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private bool _hasSuccess;
    private string _successMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RegisterSucceeded;

    public RegisterViewModel(IAuthService authService)
    {
        _authService = authService;
        RegisterCommand = new AsyncRelayCommand(RegisterAsync, () => !IsLoading);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string PostalCode
    {
        get => _postalCode;
        set => SetProperty(ref _postalCode, value);
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                (RegisterCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            }
        }
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

    public bool HasSuccess
    {
        get => _hasSuccess;
        set => SetProperty(ref _hasSuccess, value);
    }

    public string SuccessMessage
    {
        get => _successMessage;
        set => SetProperty(ref _successMessage, value);
    }

    public ICommand RegisterCommand { get; }

    public async Task RegisterAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;
        HasSuccess = false;
        SuccessMessage = string.Empty;

        Name = Name?.Trim() ?? string.Empty;
        Email = Email?.Trim() ?? string.Empty;
        PostalCode = PostalCode?.Trim() ?? string.Empty;
        Address = Address?.Trim() ?? string.Empty;
        Phone = Phone?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(Name))
        {
            HasError = true;
            ErrorMessage = "氏名を入力してください。";
            return;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            HasError = true;
            ErrorMessage = "メールアドレスを入力してください。";
            return;
        }

        if (!IsValidEmail(Email))
        {
            HasError = true;
            ErrorMessage = "メールアドレスの形式が正しくありません。";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            HasError = true;
            ErrorMessage = "パスワードを入力してください。";
            return;
        }

        if (Password.Length < 8)
        {
            HasError = true;
            ErrorMessage = "パスワードは8文字以上を推奨します。";
            return;
        }

        try
        {
            IsLoading = true;

            var request = new RegisterRequest
            {
                Name = Name,
                Email = Email,
                Password = Password,
                PostalCode = PostalCode,
                Address = Address,
                Phone = Phone
            };

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                HasError = true;
                ErrorMessage = result.Message;
                return;
            }

            HasSuccess = true;
            SuccessMessage = result.Message;

            Name = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            PostalCode = string.Empty;
            Address = string.Empty;
            Phone = string.Empty;

            RegisterSucceeded?.Invoke();
        }
        catch
        {
            HasError = true;
            ErrorMessage = "通信エラーが発生しました。時間をおいて再度お試しください。";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase);
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}