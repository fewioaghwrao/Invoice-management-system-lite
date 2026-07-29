using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace InvoiceSystem.Wpf.Views;

public partial class ConfirmDialogWindow : Window
{
    public enum DialogVisualType
    {
        Default,
        SaveConfirm,
        DangerConfirm
    }

    public ConfirmDialogWindow(
        string title,
        string message,
        string confirmText = "OK",
        string cancelText = "キャンセル",
        DialogVisualType visualType = DialogVisualType.Default,
        string? subMessage = null)
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;
        CancelButton.Content = cancelText;

        if (!string.IsNullOrWhiteSpace(subMessage))
        {
            SubMessageText.Text = subMessage;
            SubMessageText.Visibility = Visibility.Visible;
        }
        else
        {
            SubMessageText.Text = string.Empty;
            SubMessageText.Visibility = Visibility.Collapsed;
        }

        ApplyVariant(visualType);

        Loaded += (_, _) =>
        {
            ConfirmButton.Focus();
            Keyboard.Focus(ConfirmButton);
        };
    }

    public bool IsConfirmed { get; private set; }

    private void ApplyVariant(DialogVisualType visualType)
    {
        switch (visualType)
        {
            case DialogVisualType.SaveConfirm:
                IconBadge.Background = CreateBrush("#0B2A1E");
                IconBadge.BorderBrush = CreateBrush("#166534");
                IconText.Text = "✓";
                IconText.Foreground = CreateBrush("#86EFAC");

                ConfirmButton.Background = CreateBrush("#0EA5E9");
                ConfirmButton.BorderBrush = CreateBrush("#0EA5E9");
                break;

            case DialogVisualType.DangerConfirm:
                IconBadge.Background = CreateBrush("#3F1D1D");
                IconBadge.BorderBrush = CreateBrush("#7F1D1D");
                IconText.Text = "!";
                IconText.Foreground = CreateBrush("#FCA5A5");

                ConfirmButton.Background = CreateBrush("#991B1B");
                ConfirmButton.BorderBrush = CreateBrush("#991B1B");

                if (SubMessageText.Visibility == Visibility.Collapsed)
                {
                    SubMessageText.Text = "この操作は元に戻せません。内容を確認してから実行してください。";
                    SubMessageText.Visibility = Visibility.Visible;
                }

                SubMessageText.Foreground = CreateBrush("#FCA5A5");
                break;

            default:
                IconBadge.Background = CreateBrush("#132032");
                IconBadge.BorderBrush = CreateBrush("#155E75");
                IconText.Text = "?";
                IconText.Foreground = CreateBrush("#7DD3FC");

                ConfirmButton.Background = CreateBrush("#0EA5E9");
                ConfirmButton.BorderBrush = CreateBrush("#0EA5E9");
                break;
        }
    }

    private static SolidColorBrush CreateBrush(string hex)
    {
        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        DialogResult = true;
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        DialogResult = false;
        Close();
    }
}