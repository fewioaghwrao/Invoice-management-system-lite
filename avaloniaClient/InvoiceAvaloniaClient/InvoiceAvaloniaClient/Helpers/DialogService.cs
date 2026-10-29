using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using InvoiceAvaloniaClient.Views;

namespace InvoiceAvaloniaClient.Helpers;

/// <summary>
/// WPF の MessageBox / ConfirmDialogWindow.ShowDialog / SaveFileDialog の置き換え。
/// Avalonia のダイアログは非同期なので、呼び出し側は await する。
/// </summary>
public static class DialogService
{
    /// <summary>今アクティブなウィンドウ（なければメインウィンドウ）。</summary>
    public static Window? GetActiveWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.Windows.FirstOrDefault(w => w.IsActive)
                   ?? desktop.MainWindow
                   ?? desktop.Windows.FirstOrDefault();
        }

        return null;
    }

    /// <summary>確認ダイアログを表示し、確定ボタンが押されたら true を返す。</summary>
    public static async Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText = "OK",
        ConfirmDialogWindow.DialogVisualType visualType = ConfirmDialogWindow.DialogVisualType.Default,
        string? subMessage = null,
        string cancelText = "キャンセル",
        Window? owner = null)
    {
        var dialog = new ConfirmDialogWindow(title, message, confirmText, cancelText, visualType, subMessage);
        await ShowDialogCoreAsync(dialog, owner);
        return dialog.IsConfirmed;
    }

    /// <summary>お知らせ（完了メッセージなど）を表示する。</summary>
    public static Task ShowInfoAsync(string message, string title = "お知らせ", Window? owner = null)
        => ShowMessageAsync(message, title, ConfirmDialogWindow.DialogVisualType.Information, owner);

    /// <summary>エラー・警告を表示する。</summary>
    public static Task ShowErrorAsync(string message, string title = "エラー", Window? owner = null)
        => ShowMessageAsync(message, title, ConfirmDialogWindow.DialogVisualType.Error, owner);

    private static async Task ShowMessageAsync(
        string message,
        string title,
        ConfirmDialogWindow.DialogVisualType visualType,
        Window? owner)
    {
        var dialog = new ConfirmDialogWindow(title, message, "OK", "キャンセル", visualType);
        await ShowDialogCoreAsync(dialog, owner);
    }

    private static async Task ShowDialogCoreAsync(Window dialog, Window? owner)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowDialogCoreAsync(dialog, owner));
            return;
        }

        owner ??= GetActiveWindow();

        // デスクトップ以外（テストのヘッドレス実行など）でウィンドウが無い場合は表示しない
        // （表示すると閉じる人がいないため、処理が止まってしまう）
        if (owner is null
            && Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime)
        {
            return;
        }

        if (owner is not null && owner.IsVisible)
        {
            await dialog.ShowDialog(owner);
            return;
        }

        // 親ウィンドウが無い場合は普通に表示して閉じるまで待つ
        var closed = new TaskCompletionSource();
        dialog.Closed += (_, _) => closed.TrySetResult();
        dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        dialog.Show();
        await closed.Task;
    }

    /// <summary>
    /// 保存先を選ぶダイアログ（WPF の SaveFileDialog の代わり）。キャンセル時は null。
    /// </summary>
    public static async Task<string?> PickSaveFilePathAsync(
        Visual? anchor,
        string title,
        string suggestedFileName,
        string fileTypeName,
        string extension)
    {
        var topLevel = (anchor is null ? null : TopLevel.GetTopLevel(anchor)) ?? GetActiveWindow();
        if (topLevel is null)
        {
            return null;
        }

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            ShowOverwritePrompt = true,
            FileTypeChoices = new[]
            {
                new FilePickerFileType(fileTypeName) { Patterns = new[] { "*." + extension } }
            }
        });

        return file?.TryGetLocalPath();
    }
}
