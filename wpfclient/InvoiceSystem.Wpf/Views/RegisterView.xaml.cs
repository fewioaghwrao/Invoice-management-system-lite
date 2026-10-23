using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

/// <summary>
/// 新規会員登録画面。LoginWindow 内に差し込んで表示する。
/// </summary>
public partial class RegisterView : UserControl
{
    private readonly RegisterViewModel _viewModel;
    private bool _isPasswordVisible;

    /// <summary>ログイン画面へ戻る操作がされた。</summary>
    public event EventHandler? BackRequested;

    /// <summary>登録が完了した。引数は API から返った完了メッセージ。</summary>
    public event EventHandler<string>? Registered;

    public RegisterView(AuthService authService)
    {
        InitializeComponent();

        _viewModel = new RegisterViewModel(authService);
        _viewModel.RegisterSucceeded += OnRegisterSucceeded;

        DataContext = _viewModel;
    }

    private void RegisterView_OnLoaded(object sender, RoutedEventArgs e)
    {
        NameTextBox.Focus();
    }

    private void OnRegisterSucceeded()
    {
        Registered?.Invoke(this, _viewModel.SuccessMessage);
    }

    private void BackButton_OnClick(object sender, RoutedEventArgs e)
    {
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    // ===== パスワード入力 =====

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!_isPasswordVisible)
        {
            _viewModel.Password = PasswordBox.Password;
        }

        UpdatePasswordPlaceholder();
    }

    private void VisiblePasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isPasswordVisible)
        {
            _viewModel.Password = VisiblePasswordTextBox.Text;
        }

        UpdatePasswordPlaceholder();
    }

    private void UpdatePasswordPlaceholder()
    {
        if (PasswordPlaceholder is null)
        {
            return;
        }

        var current = _isPasswordVisible ? VisiblePasswordTextBox.Text : PasswordBox.Password;
        PasswordPlaceholder.Visibility = string.IsNullOrEmpty(current)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void TogglePasswordButton_OnClick(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;

        if (_isPasswordVisible)
        {
            VisiblePasswordTextBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            VisiblePasswordTextBox.Visibility = Visibility.Visible;
            TogglePasswordButton.Content = "非表示";
            VisiblePasswordTextBox.Focus();
            VisiblePasswordTextBox.CaretIndex = VisiblePasswordTextBox.Text.Length;
        }
        else
        {
            PasswordBox.Password = VisiblePasswordTextBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            VisiblePasswordTextBox.Visibility = Visibility.Collapsed;
            TogglePasswordButton.Content = "表示";
            PasswordBox.Focus();
        }

        UpdatePasswordPlaceholder();
    }
}
