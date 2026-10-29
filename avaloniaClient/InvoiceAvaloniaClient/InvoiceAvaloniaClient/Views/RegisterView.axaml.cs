using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.Services;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 新規会員登録画面。LoginWindow 内に差し込んで表示する。
/// </summary>
public partial class RegisterView : UserControl
{
    private readonly RegisterViewModel _viewModel;

    /// <summary>ログイン画面へ戻る操作がされた。</summary>
    public event EventHandler? BackRequested;

    /// <summary>登録が完了した。引数は API から返った完了メッセージ。</summary>
    public event EventHandler<string>? Registered;

    /// <summary>デザイナー用。</summary>
    public RegisterView()
    {
        InitializeComponent();
        _viewModel = null!;
    }

    public RegisterView(AuthService authService)
    {
        InitializeComponent();

        _viewModel = new RegisterViewModel(authService);
        _viewModel.RegisterSucceeded += OnRegisterSucceeded;

        DataContext = _viewModel;

        Loaded += (_, _) => NameTextBox.Focus();
    }

    private void OnRegisterSucceeded()
    {
        Registered?.Invoke(this, _viewModel.SuccessMessage);
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    // ===== パスワード入力 =====

    private void TogglePasswordButton_OnClick(object? sender, RoutedEventArgs e)
    {
        PasswordBox.RevealPassword = !PasswordBox.RevealPassword;
        TogglePasswordButton.Content = PasswordBox.RevealPassword ? "非表示" : "表示";

        PasswordBox.Focus();
        PasswordBox.CaretIndex = PasswordBox.Text?.Length ?? 0;
    }
}
