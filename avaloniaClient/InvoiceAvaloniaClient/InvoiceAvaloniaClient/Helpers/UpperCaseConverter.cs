using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace InvoiceAvaloniaClient.Helpers;

/// <summary>
/// 文字列を大文字にする（割当状態 ALLOCATED / PARTIAL / UNALLOCATED の表示切り替えで、大小文字の違いを吸収するため）。
/// </summary>
public sealed class UpperCaseConverter : IValueConverter
{
    public static readonly UpperCaseConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value?.ToString() ?? string.Empty).Trim().ToUpperInvariant();

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
