using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace InvoiceAvaloniaClient.Helpers;

/// <summary>
/// ウィンドウの切り替え（ログイン → 管理者 / 会員、ログアウト → ログイン）。
/// WPF 版の「次のウィンドウを Show して自分を Close」と同じ動きにする。
/// </summary>
public static class WindowNavigation
{
    public static void Replace(Window current, Window next)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = next;
        }

        next.Show();
        current.Close();
    }
}
