using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

public partial class RegisterWindow : Window
{
    private readonly RegisterViewModel _viewModel;
    private bool _isPasswordVisible;

    public RegisterWindow(AuthService authService)
    {
        InitializeComponent();

        _viewModel = new RegisterViewModel(authService);
        _viewModel.RegisterSucceeded += OnRegisterSucceeded;

        DataContext = _viewModel;
    }

    private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!_isPasswordVisible)
        {
            _viewModel.Password = PasswordBox.Password;
        }
    }

    private void VisiblePasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isPasswordVisible)
        {
            _viewModel.Password = VisiblePasswordTextBox.Text;
        }
    }

    private void TogglePasswordButton_OnClick(object sender, RoutedEventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;

        if (_isPasswordVisible)
        {
            VisiblePasswordTextBox.Text = PasswordBox.Password;
            PasswordBoxBorder.Visibility = Visibility.Collapsed;
            VisiblePasswordBorder.Visibility = Visibility.Visible;
            TogglePasswordButton.Content = "非表示";
            VisiblePasswordTextBox.Focus();
            VisiblePasswordTextBox.CaretIndex = VisiblePasswordTextBox.Text.Length;
        }
        else
        {
            PasswordBox.Password = VisiblePasswordTextBox.Text;
            PasswordBoxBorder.Visibility = Visibility.Visible;
            VisiblePasswordBorder.Visibility = Visibility.Collapsed;
            TogglePasswordButton.Content = "表示";
            PasswordBox.Focus();
        }
    }

    private void OnRegisterSucceeded()
    {
        MessageBox.Show(
            "登録が完了しました。ログイン画面からサインインしてください。",
            "登録完了",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OpenTermsButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new TermsWindow
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void OpenPrivacyPolicyButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = new PrivacyPolicyWindow
        {
            Owner = this
        };

        window.ShowDialog();
    }
}