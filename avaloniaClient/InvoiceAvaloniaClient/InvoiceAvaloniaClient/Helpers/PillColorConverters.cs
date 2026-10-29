using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace InvoiceAvaloniaClient.Helpers;

/// <summary>会員ロール（1=管理者 / 2=一般会員 / 9=退会）をバッジの色名に変換する。</summary>
public sealed class MemberRoleColorConverter : IValueConverter
{
    public static readonly MemberRoleColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            1 => "sky",
            2 => "emerald",
            _ => "slate"
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>会員の有効 / 無効をバッジの色名に変換する。</summary>
public sealed class ActiveColorConverter : IValueConverter
{
    public static readonly ActiveColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "emerald" : "rose";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>売上の入金状況（PAID / PARTIAL / UNPAID）をバッジの色名に変換する。</summary>
public sealed class SalesStatusColorConverter : IValueConverter
{
    public static readonly SalesStatusColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (value?.ToString() ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "PAID" => "emerald",
            "PARTIAL" => "sky",
            "UNPAID" => "amber",
            _ => "slate"
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
