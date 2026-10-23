using System;
using System.Diagnostics;
using System.Windows;

namespace InvoiceSystem.Wpf.Helpers;

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

            if (owner is null)
            {
                MessageBox.Show(message, "リンクを開けません", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show(owner, message, "リンクを開けません", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
