using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace InvoiceAvaloniaClient.Helpers;

/// <summary>
/// 入金方法のコード（BANK_TRANSFER など）を画面表示用の日本語に変換する。
/// AdminInvoiceDetailViewModel.FormatMethod と同じ変換規則。
/// </summary>
public sealed class PaymentMethodLabelConverter : IValueConverter
{
    public static readonly PaymentMethodLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var method = value as string;

        if (string.IsNullOrWhiteSpace(method))
            return "—";

        var upper = method.Trim().ToUpperInvariant();

        if (upper.Contains("BANK") || method.Contains("振込"))
            return "銀行振込";
        if (upper.Contains("CASH") || method.Contains("現金"))
            return "現金";
        if (upper.Contains("CARD") || method.Contains("カード"))
            return "カード";

        return method;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
