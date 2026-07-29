using System;
using System.Text.RegularExpressions;
using System.Windows;
using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;

namespace InvoiceSystem.Wpf.Views;

public partial class ForgotPasswordWindow : Window
{
    private readonly AuthService _authService;

    public ForgotPasswordWindow(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void SendButton_OnClick(object sender, RoutedEventArgs e)
    {
        ClearMessage();

        var email = EmailTextBox.Text.Trim();

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

            ShowSuccess("再設定リンクをメールに送信しました。メールをご確認ください。");
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

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SetLoadingState(bool isLoading)
    {
        SendButton.IsEnabled = !isLoading;
        SendButton.Content = isLoading ? "送信中..." : "再設定メールを送信";
        EmailTextBox.IsEnabled = !isLoading;
    }

    private void ShowError(string message)
    {
        MessageBorder.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3F1D1D"));
        MessageBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7F1D1D"));
        MessageTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FCA5A5"));
        MessageTextBlock.Text = message;
        MessageBorder.Visibility = Visibility.Visible;
    }

    private void ShowSuccess(string message)
    {
        MessageBorder.Background = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#082F49"));
        MessageBorder.BorderBrush = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0369A1"));
        MessageTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#7DD3FC"));
        MessageTextBlock.Text = message;
        MessageBorder.Visibility = Visibility.Visible;
    }

    private void ClearMessage()
    {
        MessageTextBlock.Text = string.Empty;
        MessageBorder.Visibility = Visibility.Collapsed;
    }

    private static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(
            email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase);
    }
}