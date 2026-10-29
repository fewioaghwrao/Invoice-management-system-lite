using System.Globalization;
using Avalonia.Data.Converters;

namespace InvoiceAvaloniaClient.Helpers;

/// <summary>
/// bool の値で 2 つの文字列を切り替える（WPF 版の DataTrigger で Content を差し替えていた箇所の代わり）。
/// ConverterParameter に「true のときの文字列|false のときの文字列」を指定する。
/// 例: {Binding IsBusy, Converter={x:Static helpers:BoolTextConverter.Instance}, ConverterParameter='ログイン中...|ログイン'}
/// 数値の場合は 0 より大きければ true として扱う（ID が選択済みかどうかなど）。
/// </summary>
public sealed class BoolTextConverter : IValueConverter
{
    public static readonly BoolTextConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var parts = (parameter?.ToString() ?? string.Empty).Split('|');
        var whenTrue = parts.Length > 0 ? parts[0] : string.Empty;
        var whenFalse = parts.Length > 1 ? parts[1] : string.Empty;

        var isTrue = value is true || PositiveConverter.Instance.Convert(value, typeof(bool), null, culture) is true;
        return isTrue ? whenTrue : whenFalse;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// 値の文字列表現が ConverterParameter と一致するか（大文字小文字は区別しない）。
/// 「|」区切りで複数指定した場合は、どれかと一致すれば true。
/// </summary>
public sealed class EqualsConverter : IValueConverter
{
    public static readonly EqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = (value?.ToString() ?? string.Empty).Trim();
        var candidates = (parameter?.ToString() ?? string.Empty).Split('|');

        return candidates.Any(c => string.Equals(text, c.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>EqualsConverter の否定。</summary>
public sealed class NotEqualsConverter : IValueConverter
{
    public static readonly NotEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => EqualsConverter.Instance.Convert(value, targetType, parameter, culture) is not true;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// int の値と ConverterParameter の数値が一致するかを bool で返す（ラジオボタンの選択状態用）。
/// </summary>
public sealed class IntEqualsConverter : IValueConverter
{
    public static readonly IntEqualsConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int number
           && int.TryParse(parameter?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var expected)
           && number == expected;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>数値が 0 より大きいか。</summary>
public sealed class PositiveConverter : IValueConverter
{
    public static readonly PositiveConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value switch
        {
            int i => i > 0,
            long l => l > 0,
            decimal m => m > 0,
            double d => d > 0,
            _ => false
        };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// 選択欄（年など）の一覧を入れ替えた瞬間に SelectedItem が null になっても、
/// int などの ViewModel 側の値を書き換えない（InvalidCastException の表示を防ぐ）。
/// </summary>
public sealed class IgnoreNullConverter : IValueConverter
{
    public static readonly IgnoreNullConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value ?? Avalonia.Data.BindingOperations.DoNothing;
}
