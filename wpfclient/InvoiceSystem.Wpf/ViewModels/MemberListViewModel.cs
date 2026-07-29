using InvoiceSystem.Wpf.Infrastructure;
using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace InvoiceSystem.Wpf.ViewModels;

public class MemberListViewModel : INotifyPropertyChanged
{
    private readonly IMemberService _memberService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MemberListViewModel(IMemberService memberService)
    {
        _memberService = memberService;

        SearchCommand = new RelayCommand(async _ => await SearchAsync(), _ => !IsLoading);
        ResetCommand = new RelayCommand(async _ => await ResetAsync(), _ => !IsLoading);
        PrevPageCommand = new RelayCommand(async _ => await MovePrevPageAsync(), _ => HasPrevPage && !IsLoading);
        NextPageCommand = new RelayCommand(async _ => await MoveNextPageAsync(), _ => HasNextPage && !IsLoading);
        OpenDetailCommand = new RelayCommand(OpenDetail);
        DisableMemberCommand = new RelayCommand(async x => await DisableMemberAsync(x as MemberRowViewModel), CanDisableMember);

        RoleOptions.Add(new FilterOption<int?>("すべて", null));
        RoleOptions.Add(new FilterOption<int?>("管理者", 1));
        RoleOptions.Add(new FilterOption<int?>("一般会員", 2));
        RoleOptions.Add(new FilterOption<int?>("退会", 9));
        SelectedRole = RoleOptions.First();

        StatusOptions.Add(new FilterOption<bool?>("すべて", null));
        StatusOptions.Add(new FilterOption<bool?>("有効", true));
        StatusOptions.Add(new FilterOption<bool?>("無効（退会含む）", false));
        SelectedStatus = StatusOptions.First();
    }

    public ObservableCollection<MemberRowViewModel> Members { get; } = new();
    public ObservableCollection<FilterOption<int?>> RoleOptions { get; } = new();
    public ObservableCollection<FilterOption<bool?>> StatusOptions { get; } = new();

    private string _keyword = string.Empty;
    public string Keyword
    {
        get => _keyword;
        set
        {
            if (_keyword != value)
            {
                _keyword = value;
                OnPropertyChanged();
            }
        }
    }

    private FilterOption<int?>? _selectedRole;
    public FilterOption<int?>? SelectedRole
    {
        get => _selectedRole;
        set
        {
            if (_selectedRole != value)
            {
                _selectedRole = value;
                OnPropertyChanged();
            }
        }
    }

    private FilterOption<bool?>? _selectedStatus;
    public FilterOption<bool?>? SelectedStatus
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
                RaiseCommandStates();
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
                OnPropertyChanged(nameof(HasPrevPage));
                OnPropertyChanged(nameof(PageInfoText));
                RaiseCommandStates();
            }
        }
    }

    public int PageSize { get; } = 5;

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
                RaiseCommandStates();
            }
        }
    }

    public bool HasPrevPage => CurrentPage > 1;

    public string PageInfoText
    {
        get
        {
            if (Members.Count == 0) return "0件";
            var from = (CurrentPage - 1) * PageSize + 1;
            var to = (CurrentPage - 1) * PageSize + Members.Count;
            return $"{from}–{to}件を表示（1ページあたり {PageSize}件）";
        }
    }

    public ICommand SearchCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand PrevPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand OpenDetailCommand { get; }
    public ICommand DisableMemberCommand { get; }

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var rows = await _memberService.GetMembersAsync(
                Keyword,
                SelectedRole?.Value,
                SelectedStatus?.Value,
                CurrentPage,
                PageSize);

            Members.Clear();
            foreach (var item in rows)
            {
                Members.Add(new MemberRowViewModel(item));
            }

            HasNextPage = rows.Count == PageSize;
            OnPropertyChanged(nameof(PageInfoText));
        }
        catch (Exception ex)
        {
            ErrorMessage = $"会員一覧の読込に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    private async Task ResetAsync()
    {
        Keyword = string.Empty;
        SelectedRole = RoleOptions.FirstOrDefault();
        SelectedStatus = StatusOptions.FirstOrDefault();
        CurrentPage = 1;
        await LoadAsync();
    }

    private async Task MovePrevPageAsync()
    {
        if (!HasPrevPage) return;
        CurrentPage--;
        await LoadAsync();
    }

    private async Task MoveNextPageAsync()
    {
        if (!HasNextPage) return;
        CurrentPage++;
        await LoadAsync();
    }

    private async void OpenDetail(object? parameter)
    {
        if (parameter is not MemberRowViewModel member)
            return;

        var window = new MemberDetailWindow(_memberService, member.Id);
        var result = window.ShowDialog();

        if (result == true)
        {
            await LoadAsync();
        }
    }

    private bool CanDisableMember(object? parameter)
    {
        if (parameter is not MemberRowViewModel member) return false;
        return !IsLoading && member.CanDisable;
    }

    private async Task DisableMemberAsync(MemberRowViewModel? member)
    {
        if (member is null || !member.CanDisable) return;

        var confirmed = ShowConfirmDialog(
            title: "退会確認",
            message: $"「{member.Name}」を退会（無効化）しますか？",
            confirmText: "退会する",
            visualType: ConfirmDialogWindow.DialogVisualType.DangerConfirm,
            subMessage: "この操作は元に戻せません。内容を確認してから実行してください。");

        if (!confirmed) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            await _memberService.DisableMemberAsync(member.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"退会（無効化）に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RaiseCommandStates()
    {
        (SearchCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (ResetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (PrevPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (DisableMemberCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
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
            .FirstOrDefault(w => w.IsActive);

        var dialog = new ConfirmDialogWindow(
            title: title,
            message: message,
            confirmText: confirmText,
            cancelText: "キャンセル",
            visualType: visualType,
            subMessage: subMessage);

        if (owner != null)
        {
            dialog.Owner = owner;
        }

        var result = dialog.ShowDialog();
        return result == true && dialog.IsConfirmed;
    }
}

public class MemberRowViewModel
{
    public MemberRowViewModel(MemberListItemDto dto)
    {
        Id = dto.Id;
        Name = dto.Name;
        Email = dto.Email;
        Role = dto.Role;
        IsActive = dto.IsActive;
    }

    public int Id { get; }
    public string Name { get; }
    public string Email { get; }
    public int Role { get; }
    public bool IsActive { get; }

    public string RoleText => Role switch
    {
        1 => "管理者",
        2 => "一般会員",
        9 => "退会",
        _ => $"不明({Role})"
    };

    public string StatusText => IsActive ? "有効" : "無効";

    public bool IsAdmin => Role == 1;
    public bool IsWithdrawn => Role == 9;
    public bool CanDisable => !IsAdmin && IsActive && !IsWithdrawn;
}

public class FilterOption<T>
{
    public FilterOption(string label, T value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public T Value { get; }

    public override string ToString() => Label;
}