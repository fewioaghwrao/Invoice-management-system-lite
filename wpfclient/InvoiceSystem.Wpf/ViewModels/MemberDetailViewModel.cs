using InvoiceSystem.Wpf.Infrastructure;
using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.Views;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace InvoiceSystem.Wpf.ViewModels;

public sealed class MemberDetailViewModel : ViewModelBase
{
    private readonly IMemberService _memberService;
    private readonly int _memberId;
    private readonly Window _window;

    private int _id;
    private string _name = "";
    private string _email = "";
    private string? _postalCode;
    private string? _address;
    private string? _phone;
    private int _role;
    private bool _isActive;
    private DateTime _createdAt;

    private bool _isLoading;
    private bool _isSaving;
    private bool _isDisabling;
    private string? _errorMessage;

    public MemberDetailViewModel(IMemberService memberService, int memberId, Window window)
    {
        _memberService = memberService;
        _memberId = memberId;
        _window = window;

        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
        DisableCommand = new RelayCommand(async _ => await DisableAsync(), _ => CanDisable());
        CloseCommand = new RelayCommand(_ => _window.Close(), _ => !IsBusy);
    }

    public int Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
                RefreshCommands();
        }
    }

    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
                RefreshCommands();
        }
    }

    public string? PostalCode
    {
        get => _postalCode;
        set => SetProperty(ref _postalCode, value);
    }

    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string? Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public int Role
    {
        get => _role;
        set
        {
            if (SetProperty(ref _role, value))
            {
                RaisePropertyChanged(nameof(RoleText));
                RaisePropertyChanged(nameof(IsAdmin));
                RaisePropertyChanged(nameof(IsDisabledMember));
                RefreshCommands();
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetProperty(ref _isActive, value))
            {
                RaisePropertyChanged(nameof(StatusText));
                RaisePropertyChanged(nameof(IsDisabledMember));
                RefreshCommands();
            }
        }
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (SetProperty(ref _isLoading, value))
            {
                RaisePropertyChanged(nameof(IsBusy));
                RefreshCommands();
            }
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            if (SetProperty(ref _isSaving, value))
            {
                RaisePropertyChanged(nameof(IsBusy));
                RefreshCommands();
            }
        }
    }

    public bool IsDisabling
    {
        get => _isDisabling;
        set
        {
            if (SetProperty(ref _isDisabling, value))
            {
                RaisePropertyChanged(nameof(IsBusy));
                RefreshCommands();
            }
        }
    }

    public bool IsBusy => IsLoading || IsSaving || IsDisabling;

    public string? ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
                RaisePropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public string RoleText => Role switch
    {
        1 => "管理者",
        2 => "一般会員",
        9 => "退会",
        _ => $"不明({Role})"
    };

    public string StatusText => IsActive ? "有効" : "無効";

    public bool IsAdmin => Role == 1;
    public bool IsDisabledMember => Role == 9 || !IsActive;

    public ICommand SaveCommand { get; }
    public ICommand DisableCommand { get; }
    public ICommand CloseCommand { get; }

    public async Task LoadAsync()
    {
        ErrorMessage = null;
        IsLoading = true;

        try
        {
            var member = await _memberService.GetByIdAsync(_memberId);
            if (member == null)
            {
                ErrorMessage = "会員情報の取得に失敗しました。";
                return;
            }

            Id = member.Id;
            Name = member.Name;
            Email = member.Email;
            PostalCode = member.PostalCode;
            Address = member.Address;
            Phone = member.Phone;
            Role = member.Role;
            IsActive = member.IsActive;
            CreatedAt = member.CreatedAt;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"会員情報の取得に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanSave()
    {
        return !IsBusy && Validate().Count == 0;
    }

    private bool CanDisable()
    {
        return !IsBusy && !IsAdmin && !IsDisabledMember && Id > 0;
    }

    private List<string> Validate()
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            issues.Add("名前は必須です。");

        if (string.IsNullOrWhiteSpace(Email))
            issues.Add("メールアドレスは必須です。");
        else if (!IsEmailLike(Email))
            issues.Add("メールアドレスの形式が正しくありません。");

        if (Role == 1)
            issues.Add("管理者ロールはこの画面では編集できません。");

        if (Role == 9 || !IsActive)
            issues.Add("この会員は無効（退会）です。編集はできません。");

        return issues;
    }

    private async Task SaveAsync()
    {
        ErrorMessage = null;

        var issues = Validate();
        if (issues.Count > 0)
        {
            ErrorMessage = string.Join(Environment.NewLine, issues);
            return;
        }

        IsSaving = true;
        try
        {
            var request = new MemberUpdateRequest
            {
                Name = Name.Trim(),
                Email = Email.Trim(),
                PostalCode = string.IsNullOrWhiteSpace(PostalCode) ? null : PostalCode.Trim(),
                Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                Phone = string.IsNullOrWhiteSpace(Phone) ? null : Phone.Trim(),
                RoleId = Role,
                IsActive = IsActive
            };

            await _memberService.UpdateAsync(Id, request);

            MessageBox.Show("会員情報を保存しました。", "保存完了", MessageBoxButton.OK, MessageBoxImage.Information);
            _window.DialogResult = true;
            _window.Close();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"保存に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task DisableAsync()
    {
        ErrorMessage = null;

        var confirmed = ShowConfirmDialog(
            title: "退会確認",
            message: "本当に退会（無効化）しますか？",
            confirmText: "退会する",
            visualType: ConfirmDialogWindow.DialogVisualType.DangerConfirm,
            subMessage:
                $"対象: {Name}（{Email}）{Environment.NewLine}{Environment.NewLine}" +
                "この操作は元に戻せません。内容を確認してから実行してください。");

        if (!confirmed)
            return;

        IsDisabling = true;
        try
        {
            await _memberService.DisableAsync(Id);

            MessageBox.Show("退会（無効化）を実行しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            _window.DialogResult = true;
            _window.Close();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"退会（無効化）に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            IsDisabling = false;
        }
    }

    private static bool IsEmailLike(string value)
    {
        return Regex.IsMatch(value, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
    }

    private void RefreshCommands()
    {
        (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DisableCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (CloseCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private bool ShowConfirmDialog(
    string title,
    string message,
    string confirmText,
    ConfirmDialogWindow.DialogVisualType visualType = ConfirmDialogWindow.DialogVisualType.Default,
    string? subMessage = null)
    {
        var owner = Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(w => w.IsActive) ?? _window;

        var dialog = new ConfirmDialogWindow(
            title: title,
            message: message,
            confirmText: confirmText,
            cancelText: "キャンセル",
            visualType: visualType,
            subMessage: subMessage)
        {
            Owner = owner
        };

        var result = dialog.ShowDialog();
        return result == true && dialog.IsConfirmed;
    }
}