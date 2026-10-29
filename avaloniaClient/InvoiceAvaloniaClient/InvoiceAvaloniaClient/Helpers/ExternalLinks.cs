using System.Diagnostics;
using Avalonia.Controls;

namespace InvoiceAvaloniaClient.Helpers;

/// <summary>
/// 利用規約・プライバシーポリシーなど、Web 版のページを既定のブラウザで開く。
/// </summary>
public static class ExternalLinks
{
    public const string TermsUrl = "https://invoice-management-system-lite.vercel.app/terms";
    public const string PrivacyPolicyUrl = "https://invoice-management-system-lite.vercel.app/privacy";

    public static void Open(string url, Window? owner = null)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            var message = $"ブラウザを開けませんでした。次の URL を直接開いてください。\n{url}";
            _ = DialogService.ShowErrorAsync(message, "リンクを開けません", owner);
        }
    }

    /// <summary>ファイル（PDF など）を既定のアプリで開く。</summary>
    public static void OpenFile(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
