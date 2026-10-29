using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace InvoiceAvaloniaClient.Controls;

/// <summary>
/// 月別売上グラフの表示データ。
/// </summary>
/// <param name="FirstMonth">表示する最初の月（1〜12）。</param>
/// <param name="LastMonth">表示する最後の月（1〜12）。</param>
/// <param name="AxisMaximum">縦軸の最大値。</param>
/// <param name="Values">1月〜12月の値（12 要素）。</param>
/// <param name="AxisLabelFormatter">縦軸の目盛りの表示。</param>
public sealed record MonthlyChartData(
    int FirstMonth,
    int LastMonth,
    double AxisMaximum,
    IReadOnlyList<double> Values,
    Func<double, string> AxisLabelFormatter)
{
    public static MonthlyChartData Empty(int firstMonth, int lastMonth)
        => new(firstMonth, lastMonth, 100000, new double[12], v => v.ToString("#,0", CultureInfo.InvariantCulture));
}

/// <summary>
/// 月別売上の折れ線グラフ。WPF 版で使っていた OxyPlot の代わりに、必要な表示だけを自前で描く。
/// （横軸 = 月 / 縦軸 = 金額、破線のグリッド、マーカー、マウスを乗せると金額を表示）
/// </summary>
public sealed class MonthlyLineChart : Control
{
    public static readonly StyledProperty<MonthlyChartData?> DataProperty =
        AvaloniaProperty.Register<MonthlyLineChart, MonthlyChartData?>(nameof(Data));

    private const double PlotLeft = 56;
    private const double PlotRight = 18;
    private const double PlotTop = 14;
    private const double PlotBottom = 30;
    private const int YSteps = 5;

    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#CBD5E1"));
    private static readonly IBrush LineBrush = new SolidColorBrush(Color.Parse("#38BDF8"));
    private static readonly IBrush MarkerFill = new SolidColorBrush(Color.Parse("#7DD3FC"));
    private static readonly IBrush MarkerStroke = new SolidColorBrush(Color.Parse("#E0F2FE"));
    private static readonly IBrush TooltipBackground = new SolidColorBrush(Color.Parse("#F20F172A"));
    private static readonly IBrush TooltipBorder = new SolidColorBrush(Color.Parse("#334155"));
    private static readonly IBrush TooltipText = new SolidColorBrush(Color.Parse("#F1F5F9"));

    private static readonly IPen AxisPen = new Pen(new SolidColorBrush(Color.Parse("#475569")), 1);
    private static readonly IPen GridPen = new Pen(new SolidColorBrush(Color.Parse("#1E293B")), 1, new DashStyle(new double[] { 4, 4 }, 0));
    private static readonly IPen SeriesPen = new Pen(LineBrush, 2.5, lineJoin: PenLineJoin.Round);
    private static readonly IPen MarkerPen = new Pen(MarkerStroke, 1);
    private static readonly IPen TooltipPen = new Pen(TooltipBorder, 1);

    private static readonly Typeface LabelTypeface = new(FontFamily.Default);

    private int? _hoverMonth;

    static MonthlyLineChart()
    {
        AffectsRender<MonthlyLineChart>(DataProperty);
    }

    public MonthlyChartData? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DataProperty)
        {
            _hoverMonth = null;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var data = Data;
        if (data is null)
        {
            return;
        }

        var plot = GetPlotRect();
        var position = e.GetPosition(this);
        int? month = null;

        if (plot.Width > 0 && position.X >= plot.Left - 12 && position.X <= plot.Right + 12)
        {
            var span = Math.Max(1, data.LastMonth - data.FirstMonth);
            var raw = data.FirstMonth + (position.X - plot.Left) / plot.Width * span;
            month = Math.Clamp((int)Math.Round(raw), data.FirstMonth, data.LastMonth);
        }

        if (month != _hoverMonth)
        {
            _hoverMonth = month;
            InvalidateVisual();
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (_hoverMonth is not null)
        {
            _hoverMonth = null;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // ヒットテスト用に透明で塗っておく（マウスを乗せたときの金額表示のため）
        context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));

        var data = Data;
        var plot = GetPlotRect();
        if (data is null || plot.Width <= 0 || plot.Height <= 0)
        {
            return;
        }

        var axisMax = data.AxisMaximum <= 0 ? 1 : data.AxisMaximum;
        var span = Math.Max(1, data.LastMonth - data.FirstMonth);

        double X(int month) => plot.Left + (month - data.FirstMonth) / (double)span * plot.Width;
        double Y(double value) => plot.Bottom - Math.Clamp(value / axisMax, 0, 1) * plot.Height;

        // 縦軸の目盛りと横方向のグリッド
        for (var i = 0; i <= YSteps; i++)
        {
            var value = axisMax / YSteps * i;
            var y = Y(value);

            if (i > 0)
            {
                context.DrawLine(GridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            }

            var label = CreateText(data.AxisLabelFormatter(value), 11, TextBrush);
            context.DrawText(label, new Point(plot.Left - 8 - label.Width, y - label.Height / 2));
        }

        // 横軸の目盛り（月）と縦方向のグリッド
        for (var month = data.FirstMonth; month <= data.LastMonth; month++)
        {
            var x = X(month);

            if (month > data.FirstMonth)
            {
                context.DrawLine(GridPen, new Point(x, plot.Top), new Point(x, plot.Bottom));
            }

            var label = CreateText($"{month}月", 11, TextBrush);
            context.DrawText(label, new Point(x - label.Width / 2, plot.Bottom + 8));
        }

        // 軸線
        context.DrawLine(AxisPen, new Point(plot.Left, plot.Top), new Point(plot.Left, plot.Bottom));
        context.DrawLine(AxisPen, new Point(plot.Left, plot.Bottom), new Point(plot.Right, plot.Bottom));

        // 折れ線
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            for (var month = data.FirstMonth; month <= data.LastMonth; month++)
            {
                var point = new Point(X(month), Y(GetValue(data, month)));

                if (month == data.FirstMonth)
                {
                    g.BeginFigure(point, false);
                }
                else
                {
                    g.LineTo(point);
                }
            }

            g.EndFigure(false);
        }

        using (context.PushClip(new Rect(plot.Left - 6, plot.Top - 6, plot.Width + 12, plot.Height + 12)))
        {
            context.DrawGeometry(null, SeriesPen, geometry);

            for (var month = data.FirstMonth; month <= data.LastMonth; month++)
            {
                var center = new Point(X(month), Y(GetValue(data, month)));
                var radius = month == _hoverMonth ? 6 : 4;
                context.DrawEllipse(MarkerFill, MarkerPen, center, radius, radius);
            }
        }

        // マウスを乗せた月の金額
        if (_hoverMonth is int hover)
        {
            var value = GetValue(data, hover);
            var text = CreateText($"{hover}月\n金額: {value.ToString("#,0", CultureInfo.InvariantCulture)} 円", 12, TooltipText);

            var anchor = new Point(X(hover), Y(value));
            var box = new Rect(anchor.X + 12, anchor.Y - text.Height - 16, text.Width + 20, text.Height + 12);

            if (box.Right > Bounds.Width - 4)
            {
                box = box.WithX(anchor.X - 12 - box.Width);
            }

            if (box.Top < 4)
            {
                box = box.WithY(anchor.Y + 12);
            }

            context.DrawRectangle(TooltipBackground, TooltipPen, box, 8, 8);
            context.DrawText(text, new Point(box.X + 10, box.Y + 6));
        }
    }

    private Rect GetPlotRect()
    {
        var width = Bounds.Width - PlotLeft - PlotRight;
        var height = Bounds.Height - PlotTop - PlotBottom;
        return new Rect(PlotLeft, PlotTop, Math.Max(0, width), Math.Max(0, height));
    }

    private static double GetValue(MonthlyChartData data, int month)
    {
        var index = month - 1;
        return index >= 0 && index < data.Values.Count ? data.Values[index] : 0;
    }

    private static FormattedText CreateText(string text, double size, IBrush brush)
        => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, LabelTypeface, size, brush);
}
