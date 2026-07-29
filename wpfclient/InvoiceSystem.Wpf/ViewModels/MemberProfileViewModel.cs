using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using InvoiceSystem.Wpf.Infrastructure;

namespace InvoiceSystem.Wpf.ViewModels;

public sealed class MemberProfileViewModel : ViewModelBase
{
    private readonly IAccountService _accountService;
    private readonly Action _openDashboard;
    private readonly Action _openLogin;
    private readonly Func<ConfirmDialogRequest, bool> _showConfirmDialog;

    private AccountProfileDto? _profile;
    private string _originalEmail = string.Empty;

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
                RaiseCommandStates();
            }
        }
    }

    public bool IsLoading => !string.IsNullOrWhiteSpace(LoadingMessage);

    public Visibility LoadingVisibility =>
        IsLoading ? Visibility.Visible : Visibility.Collapsed;

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                RaisePropertyChanged(nameof(HasError));
                RaisePropertyChanged(nameof(ErrorMessageVisibility));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public Visibility ErrorMessageVisibility =>
        HasError ? Visibility.Visible : Visibility.Collapsed;

    private string _infoMessage = string.Empty;
    public string InfoMessage
    {
        get => _infoMessage;
        set
        {
            if (SetProperty(ref _infoMessage, value))
            {
                RaisePropertyChanged(nameof(HasInfo));
                RaisePropertyChanged(nameof(InfoMessageVisibility));
            }
        }
    }

    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);

    public Visibility InfoMessageVisibility =>
        HasInfo ? Visibility.Visible : Visibility.Collapsed;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                RaisePropertyChanged(nameof(IsEmailChanged));
                RaisePropertyChanged(nameof(EmailChangedText));
                RaisePropertyChanged(nameof(EmailChangedNoticeVisibility));
            }
        }
    }

    private string _phone = string.Empty;
    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    private string _postalCode = string.Empty;
    public string PostalCode
    {
        get => _postalCode;
        set => SetProperty(ref _postalCode, value);
    }

    private string _address = string.Empty;
    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public bool IsEmailChanged =>
        !string.IsNullOrWhiteSpace(_originalEmail) &&
        !string.Equals(_originalEmail, Email?.Trim() ?? string.Empty, StringComparison.Ordinal);

    public string EmailChangedText =>
        IsEmailChanged
            ? $"変更前：{_originalEmail}\n変更後：{Email?.Trim() ?? string.Empty}"
            : string.Empty;

    public Visibility EmailChangedNoticeVisibility =>
        IsEmailChanged ? Visibility.Visible : Visibility.Collapsed;

    public ICommand SaveCommand { get; }
    public ICommand WithdrawCommand { get; }
    public ICommand BackCommand { get; }

    public MemberProfileViewModel(
        IAccountService accountService,
        Action openDashboard,
        Action openLogin,
        Func<ConfirmDialogRequest, bool> showConfirmDialog)
    {
        _accountService = accountService;
        _openDashboard = openDashboard;
        _openLogin = openLogin;
        _showConfirmDialog = showConfirmDialog;

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => !IsLoading);
        WithdrawCommand = new RelayCommand(async () => await WithdrawAsync(), () => !IsLoading);
        BackCommand = new RelayCommand(Back, () => !IsLoading);
    }

    public async Task InitializeAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            ToggleLoading(true, "処理中...");
            ClearMessages();

            var profile = await _accountService.GetMyProfileAsync();
            _profile = profile;
            _originalEmail = profile.Email ?? string.Empty;

            BindProfile(profile);

            RaisePropertyChanged(nameof(IsEmailChanged));
            RaisePropertyChanged(nameof(EmailChangedText));
            RaisePropertyChanged(nameof(EmailChangedNoticeVisibility));
        }
        catch (Exception ex)
        {
            InfoMessage = string.Empty;
            ErrorMessage = $"プロフィールの取得に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            ToggleLoading(false);
        }
    }

    private void BindProfile(AccountProfileDto profile)
    {
        Name = profile.Name ?? string.Empty;
        Email = profile.Email ?? string.Empty;
        Phone = profile.Phone ?? string.Empty;
        PostalCode = profile.PostalCode ?? string.Empty;
        Address = profile.Address ?? string.Empty;
    }

    private AccountProfileDto BuildProfileFromForm()
    {
        return new AccountProfileDto
        {
            Id = _profile?.Id ?? 0,
            Name = Name?.Trim() ?? string.Empty,
            Email = Email?.Trim() ?? string.Empty,
            Phone = NormalizeNullable(Phone),
            PostalCode = NormalizeNullable(PostalCode),
            Address = NormalizeNullable(Address)
        };
    }

    private string[] Validate(AccountProfileDto profile)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            issues.Add("氏名は必須です。");
        }

        if (string.IsNullOrWhiteSpace(profile.Email))
        {
            issues.Add("メールアドレスは必須です。");
        }
        else if (!AccountService.IsEmailLike(profile.Email))
        {
            issues.Add("メールアドレスの形式が正しくありません。");
        }

        return issues.ToArray();
    }

    private async Task SaveAsync()
    {
        var profile = BuildProfileFromForm();
        var issues = Validate(profile);

        ClearMessages();

        if (issues.Length > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, issues);
            return;
        }

        var emailChanged = !string.Equals(_originalEmail, profile.Email, StringComparison.Ordinal);

        var confirmMessage =
            "次の内容で保存します。よろしいですか？\n\n" +
            $"氏名：{profile.Name}\n" +
            $"メール：{profile.Email}\n" +
            $"電話：{profile.Phone ?? "—"}\n" +
            $"郵便番号：{profile.PostalCode ?? "—"}\n" +
            $"住所：{profile.Address ?? "—"}";

        if (emailChanged)
        {
            confirmMessage +=
                $"\n\nメールアドレスが変更されます。\n変更前：{_originalEmail}\n変更後：{profile.Email}\n\n※ Lite版のため、確認メールは送信されません。";
        }

        var confirmed = _showConfirmDialog(new ConfirmDialogRequest
        {
            Title = "保存内容の確認",
            Message = "入力内容を保存します。よろしいですか？",
            ConfirmText = "保存する",
            CancelText = "キャンセル",
            VisualType = ConfirmDialogWindow.DialogVisualType.SaveConfirm,
            SubMessage = confirmMessage
        });

        if (!confirmed)
            return;

        try
        {
            ToggleLoading(true, "処理中...");

            await _accountService.UpdateMyProfileAsync(profile);

            _profile = profile;
            _originalEmail = profile.Email ?? string.Empty;

            RaisePropertyChanged(nameof(IsEmailChanged));
            RaisePropertyChanged(nameof(EmailChangedText));
            RaisePropertyChanged(nameof(EmailChangedNoticeVisibility));

            ErrorMessage = string.Empty;
            InfoMessage = "プロフィールを保存しました。";
        }
        catch (Exception ex)
        {
            InfoMessage = string.Empty;
            ErrorMessage = $"プロフィールの保存に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            ToggleLoading(false);
        }
    }

    private async Task WithdrawAsync()
    {
        ClearMessages();

        var confirmed = _showConfirmDialog(new ConfirmDialogRequest
        {
            Title = "退会確認",
            Message = "本当に退会しますか？",
            ConfirmText = "退会する",
            CancelText = "キャンセル",
            VisualType = ConfirmDialogWindow.DialogVisualType.DangerConfirm,
            SubMessage = "退会後はログインできなくなります。この操作は取り消せません。"
        });

        if (!confirmed)
            return;

        try
        {
            ToggleLoading(true, "処理中...");

            await _accountService.DeleteMyAccountAsync();

            ErrorMessage = string.Empty;
            InfoMessage = "退会処理が完了しました。";

            _openLogin();
        }
        catch (Exception ex)
        {
            InfoMessage = string.Empty;
            ErrorMessage = $"退会処理に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            ToggleLoading(false);
        }
    }

    private void Back()
    {
        _openDashboard();
    }

    private void ToggleLoading(bool isLoading, string message = "")
    {
        LoadingMessage = isLoading ? message : string.Empty;
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
    }

    private void RaiseCommandStates()
    {
        if (SaveCommand is RelayCommand saveCommand)
            saveCommand.RaiseCanExecuteChanged();

        if (WithdrawCommand is RelayCommand withdrawCommand)
            withdrawCommand.RaiseCanExecuteChanged();

        if (BackCommand is RelayCommand backCommand)
            backCommand.RaiseCanExecuteChanged();
    }

    private static string? NormalizeNullable(string? value)
    {
        var v = value?.Trim();
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }
}