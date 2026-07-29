using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using InvoiceSystem.Wpf.Helpers;
using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;

namespace InvoiceSystem.Wpf.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly IAuthService _authService;

    private string _email = "";
    private string _password = "";
    private string _errorMessage = "";
    private bool _isBusy;
    private CurrentUser? _currentUser;

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => CanLogin());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action<string>? LoginSucceeded;

    public ICommand LoginCommand { get; }

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value) return;
            _email = value;
            OnPropertyChanged();
            RaiseLoginCanExecuteChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password == value) return;
            _password = value;
            OnPropertyChanged();
            RaiseLoginCanExecuteChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value) return;
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            RaiseLoginCanExecuteChanged();
        }
    }

    public CurrentUser? CurrentUser
    {
        get => _currentUser;
        private set
        {
            _currentUser = value;
            OnPropertyChanged();
        }
    }

    private bool CanLogin()
    {
        return !IsBusy
               && !string.IsNullOrWhiteSpace(Email)
               && !string.IsNullOrWhiteSpace(Password);
    }

    private async Task LoginAsync()
    {
        try
        {
            ErrorMessage = "";
            IsBusy = true;

            var response = await _authService.LoginAsync(new LoginRequest
            {
                Email = Email.Trim(),
                Password = Password
            });

            CurrentUser = new CurrentUser
            {
                Id = response.Id,
                Name = response.Name,
                Email = response.Email,
                Role = response.Role,
                Token = response.Token
            };

            LoginSucceeded?.Invoke(response.Role);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RaiseLoginCanExecuteChanged()
    {
        if (LoginCommand is RelayCommand relay)
        {
            relay.RaiseCanExecuteChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
