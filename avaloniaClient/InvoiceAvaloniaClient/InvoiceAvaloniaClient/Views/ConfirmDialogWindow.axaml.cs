using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 確認ダイアログ。WPF 版の ConfirmDialogWindow に加え、MessageBox の代わり（ボタン 1 つ）にも使う。
/// 表示は <see cref="Helpers.DialogService"/> から行う。
/// </summary>
public partial class ConfirmDialogWindow : Window
{
    public enum DialogVisualType
    {
        Default,
        SaveConfirm,
        DangerConfirm,

        /// <summary>お知らせ（完了など）。ボタンは 1 つ。</summary>
        Information,

        /// <summary>エラー・警告。ボタンは 1 つ。</summary>
        Error
    }

    /// <summary>デザイナー用。</summary>
    public ConfirmDialogWindow()
        : this("確認", "実行してもよろしいですか？")
    {
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

        Title = title;
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;
        CancelButton.Content = cancelText;

        if (!string.IsNullOrWhiteSpace(subMessage))
        {
            SubMessageText.Text = subMessage;
            SubMessageText.IsVisible = true;
        }

        ApplyVariant(visualType);

        Opened += (_, _) => ConfirmButton.Focus();
        KeyDown += OnKeyDown;
    }

    public bool IsConfirmed { get; private set; }

    private void ApplyVariant(DialogVisualType visualType)
    {
        switch (visualType)
        {
            case DialogVisualType.SaveConfirm:
            case DialogVisualType.Information:
                SetIcon("✓", "#0B2A1E", "#166534", "#86EFAC");
                SetConfirmColor("#0EA5E9");
                break;

            case DialogVisualType.DangerConfirm:
                SetIcon("!", "#3F1D1D", "#7F1D1D", "#FCA5A5");
                SetConfirmColor("#991B1B");

                if (!SubMessageText.IsVisible)
                {
                    SubMessageText.Text = "この操作は元に戻せません。内容を確認してから実行してください。";
                    SubMessageText.IsVisible = true;
                }

                SubMessageText.Foreground = Brush("#FCA5A5");
                break;

            case DialogVisualType.Error:
                SetIcon("!", "#3F1D1D", "#7F1D1D", "#FCA5A5");
                SetConfirmColor("#0EA5E9");
                break;

            default:
                SetIcon("?", "#132032", "#155E75", "#7DD3FC");
                SetConfirmColor("#0EA5E9");
                break;
        }

        // お知らせ・エラーは OK ボタンだけにする
        if (visualType is DialogVisualType.Information or DialogVisualType.Error)
        {
            CancelButton.IsVisible = false;
        }
    }

    private void SetIcon(string text, string background, string border, string foreground)
    {
        IconText.Text = text;
        IconBadge.Background = Brush(background);
        IconBadge.BorderBrush = Brush(border);
        IconText.Foreground = Brush(foreground);
    }

    private void SetConfirmColor(string hex)
    {
        ConfirmButton.Background = Brush(hex);
        ConfirmButton.BorderBrush = Brush(hex);
    }

    private static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            IsConfirmed = false;
            Close(false);
            e.Handled = true;
        }
    }

    private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        Close(true);
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close(false);
    }
}
