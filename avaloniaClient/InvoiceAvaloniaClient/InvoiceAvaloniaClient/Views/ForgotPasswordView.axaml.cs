using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.Models;
using InvoiceAvaloniaClient.Services;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// パスワード再設定メールの送信画面。LoginWindow 内に差し込んで表示する。
/// </summary>
public partial class ForgotPasswordView : UserControl
{
    private readonly AuthService? _authService;

    /// <summary>ログイン画面へ戻る操作がされた。</summary>
    public event EventHandler? BackRequested;

    /// <summary>デザイナー用。</summary>
    public ForgotPasswordView()
    {
        InitializeComponent();
    }

    public ForgotPasswordView(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;

        Loaded += (_, _) => EmailTextBox.Focus();
    }

    private async void SendButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_authService is null)
        {
            return;
        }

        HideError();

        var email = (EmailTextBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("メールアドレスを入力してください。");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowError("メールアドレスの形式が正しくありません。");
            return;
        }

        try
        {
            SetLoadingState(true);

            await _authService.ForgotPasswordAsync(new ForgotPasswordRequest
            {
                Email = email
            });

            FormPanel.IsVisible = false;
            SentPanel.IsVisible = true;
            SendButton.IsDefault = false;
            BackToLoginButton.Focus();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SetLoadingState(false);
        }
    }

    private void BackButton_OnClick(object? sender, RoutedEventArgs e)
    {
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SetLoadingState(bool isLoading)
    {
        SendButton.IsEnabled = !isLoading;
        SendButton.Content = isLoading ? "送信中..." : "再設定メールを送信";
        EmailTextBox.IsEnabled = !isLoading;
    }

    private void ShowError(string message)
    {
        ErrorTextBlock.Text = message;
        ErrorBorder.IsVisible = true;
    }

    private void HideError()
    {
        ErrorTextBlock.Text = string.Empty;
        ErrorBorder.IsVisible = false;
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase);
    }
}
