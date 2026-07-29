using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using OxyPlot.Wpf;

namespace InvoiceSystem.Wpf.ViewModels;

public class AdminDashboardViewModel : INotifyPropertyChanged
{
    private readonly IAdminService _adminService;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AdminDashboardViewModel(IAdminService adminService, CurrentUser? currentUser)
    {
        _adminService = adminService;
        CurrentUser = currentUser;

        var now = DateTime.Now;
        SelectedYear = now.Year;

        MonthlySalesPlotModel = new PlotModel();
    }

    public CurrentUser? CurrentUser { get; }

    private int _selectedYear;
    public int SelectedYear
    {
        get => _selectedYear;
        set
        {
            if (_selectedYear != value)
            {
                _selectedYear = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedYearText));
            }
        }
    }

    public string SelectedYearText => $"{SelectedYear} 年";

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    private decimal _totalSales;
    public decimal TotalSales
    {
        get => _totalSales;
        set
        {
            if (_totalSales != value)
            {
                _totalSales = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalSalesText));
            }
        }
    }

    public string TotalSalesText => FormatCurrency(TotalSales);

    private decimal _unpaidAmount;
    public decimal UnpaidAmount
    {
        get => _unpaidAmount;
        set
        {
            if (_unpaidAmount != value)
            {
                _unpaidAmount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UnpaidAmountText));
            }
        }
    }

    public string UnpaidAmountText => FormatCurrency(UnpaidAmount);

    private int _invoiceCount;
    public int InvoiceCount
    {
        get => _invoiceCount;
        set
        {
            if (_invoiceCount != value)
            {
                _invoiceCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InvoiceCountText));
            }
        }
    }

    public string InvoiceCountText => $"{InvoiceCount} 件";

    private int _paymentCount;
    public int PaymentCount
    {
        get => _paymentCount;
        set
        {
            if (_paymentCount != value)
            {
                _paymentCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PaymentCountText));
            }
        }
    }

    public string PaymentCountText => $"{PaymentCount} 件";

    private double _recoveryRate;
    public double RecoveryRate
    {
        get => _recoveryRate;
        set
        {
            if (Math.Abs(_recoveryRate - value) > 0.0001)
            {
                _recoveryRate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RecoveryRateText));
            }
        }
    }

    public string RecoveryRateText => $"{RecoveryRate:F1}%";

    private PlotModel _monthlySalesPlotModel = new();
    public PlotModel MonthlySalesPlotModel
    {
        get => _monthlySalesPlotModel;
        set
        {
            _monthlySalesPlotModel = value;
            OnPropertyChanged();
        }
    }

    public IPlotController FixedPlotController { get; } = CreateFixedPlotController();

    public ObservableCollection<AdminUnpaidInvoiceDto> UnpaidInvoices { get; } = new();
    public ObservableCollection<AdminMonthlySalesItemViewModel> MonthlySales { get; } = new();
    public ObservableCollection<WorstCustomerDto> WorstCustomers { get; } = new();
    public ObservableCollection<AdminOperationLogItemViewModel> RecentLogs { get; } = new();

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var summaryTask = _adminService.GetSummaryAsync(SelectedYear);
            var worstTask = _adminService.GetWorstTop5Async(SelectedYear);
            var logsTask = _adminService.GetRecentOperationLogsAsync(5);

            await Task.WhenAll(summaryTask, worstTask, logsTask);

            var summary = summaryTask.Result;
            var worst = worstTask.Result;
            var logs = logsTask.Result;

            TotalSales = summary.InvoiceTotal;
            UnpaidAmount = summary.RemainingTotal;
            InvoiceCount = summary.InvoiceCount;
            PaymentCount = summary.PaymentCount;
            RecoveryRate = summary.RecoveryRate;

            UnpaidInvoices.Clear();
            foreach (var item in summary.UnpaidTop5)
            {
                UnpaidInvoices.Add(item);
            }

            MonthlySales.Clear();
            foreach (var item in summary.MonthlySales)
            {
                MonthlySales.Add(new AdminMonthlySalesItemViewModel
                {
                    MonthLabel = $"{item.Month}月",
                    Amount = item.InvoiceTotal
                });
            }

            BuildMonthlySalesPlot(summary.MonthlySales);

            WorstCustomers.Clear();
            foreach (var item in worst.Rows)
            {
                WorstCustomers.Add(item);
            }

            RecentLogs.Clear();
            foreach (var item in logs)
            {
                RecentLogs.Add(new AdminOperationLogItemViewModel(item));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"管理者ダッシュボードの読込に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void MovePreviousYear()
    {
        SelectedYear--;
    }

    public void MoveNextYear()
    {
        SelectedYear++;
    }

    private void BuildMonthlySalesPlot(IEnumerable<AdminMonthlySalesDto> monthlySales)
    {
        var model = new PlotModel
        {
            Title = "月別売上（請求金額ベース）",
            Subtitle = $"{SelectedYear}年 1月〜12月",
            TextColor = OxyColor.Parse("#E5E7EB"),
            PlotAreaBorderColor = OxyColor.Parse("#334155"),
            Background = OxyColors.Transparent,
            TitleColor = OxyColor.Parse("#F8FAFC"),
            SubtitleColor = OxyColor.Parse("#94A3B8"),
            TitleFontSize = 14,
            SubtitleFontSize = 10,
            DefaultFontSize = 11,
            Padding = new OxyThickness(6, 6, 6, 6),
            IsLegendVisible = false
        };

        var monthlyMap = Enumerable.Range(1, 12)
            .ToDictionary(m => m, _ => 0m);

        foreach (var item in monthlySales ?? Enumerable.Empty<AdminMonthlySalesDto>())
        {
            if (item.Month >= 1 && item.Month <= 12)
            {
                monthlyMap[item.Month] = item.InvoiceTotal;
            }
        }

        var maxValue = monthlyMap.Values.DefaultIfEmpty(0m).Max();
        var axisMax = CalculateAxisMaximum(maxValue);

        // 横軸（月）
        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Minimum = 1,
            Maximum = 12,
            AbsoluteMinimum = 1,
            AbsoluteMaximum = 12,
            MajorStep = 1,
            MinorStep = 1,
            TextColor = OxyColor.Parse("#CBD5E1"),
            TicklineColor = OxyColor.Parse("#475569"),
            AxislineColor = OxyColor.Parse("#475569"),
            MajorGridlineStyle = LineStyle.None,
            MinorGridlineStyle = LineStyle.None,
            IsPanEnabled = false,
            IsZoomEnabled = false,
            LabelFormatter = value =>
            {
                var month = (int)Math.Round(value);
                return month >= 1 && month <= 12 ? $"{month}月" : string.Empty;
            }
        };

        // 縦軸（金額）
        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = 0,
            Maximum = axisMax,
            AbsoluteMinimum = 0,
            TextColor = OxyColor.Parse("#CBD5E1"),
            TicklineColor = OxyColor.Parse("#475569"),
            AxislineColor = OxyColor.Parse("#475569"),
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.None,
            MajorGridlineColor = OxyColor.Parse("#1E293B"),
            StringFormat = "#,0",
            Title = "円",
            TitleColor = OxyColor.Parse("#94A3B8"),
            FontSize = 11,
            IsPanEnabled = false,
            IsZoomEnabled = false
        };

        var series = new LineSeries
        {
            Title = "請求金額",
            Color = OxyColor.Parse("#38BDF8"),
            StrokeThickness = 2.5,
            MarkerType = MarkerType.Circle,
            MarkerSize = 4,
            MarkerFill = OxyColor.Parse("#7DD3FC"),
            MarkerStroke = OxyColor.Parse("#E0F2FE"),
            MarkerStrokeThickness = 1,
            CanTrackerInterpolatePoints = false,
            TrackerFormatString = "{0}\n月: {2:0}\n金額: {4:#,0} 円"
        };

        for (int month = 1; month <= 12; month++)
        {
            series.Points.Add(new DataPoint(month, (double)monthlyMap[month]));
        }

        model.Axes.Add(xAxis);
        model.Axes.Add(yAxis);
        model.Series.Add(series);

        MonthlySalesPlotModel = model;
    }

    private static double CalculateAxisMaximum(decimal maxValue)
    {
        if (maxValue <= 0)
        {
            return 100000;
        }

        var raw = (double)maxValue * 1.2;

        if (raw <= 100000) return RoundUp(raw, 10000);
        if (raw <= 500000) return RoundUp(raw, 50000);
        if (raw <= 1000000) return RoundUp(raw, 100000);

        return RoundUp(raw, 500000);
    }

    private static double RoundUp(double value, double unit)
    {
        return Math.Ceiling(value / unit) * unit;
    }

    private static IPlotController CreateFixedPlotController()
    {
        return new PlotController();
    }

    private static string FormatCurrency(decimal value)
    {
        return value.ToString("C0", CultureInfo.GetCultureInfo("ja-JP"));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }
}

public class AdminMonthlySalesItemViewModel
{
    public string MonthLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountText => Amount.ToString("C0", CultureInfo.GetCultureInfo("ja-JP"));
}

public class AdminOperationLogItemViewModel
{
    public AdminOperationLogItemViewModel(AdminOperationLogDto dto)
    {
        Id = dto.Id;
        At = dto.At;
        ActorUserId = dto.ActorUserId;
        Action = dto.Action;
        Entity = dto.Entity;
        EntityId = dto.EntityId;
        Summary = dto.Summary;
    }

    public int Id { get; }
    public DateTime At { get; }
    public int ActorUserId { get; }
    public string Action { get; }
    public string Entity { get; }
    public string? EntityId { get; }
    public string Summary { get; }

    public string AtText => At.ToString("yyyy/MM/dd HH:mm");
    public string ActionLabel => FormatActionLabel(Action);
    public string TargetLabel => FormatTarget(Entity, EntityId);
    public string ActorText => ActorUserId.ToString();

    private static string FormatActionLabel(string code)
    {
        var x = (code ?? string.Empty).Trim().ToUpperInvariant();

        return x switch
        {
            "PAYMENT_ALLOCATION_ADDED" => "割当追加",
            "PAYMENT_ALLOCATION_DELETED" => "割当削除",
            "PAYMENT_ALLOCATIONS_REPLACED" => "割当保存（置換）",
            "PAYMENT_ALLOCATIONS_CLEARED" => "割当クリア",
            _ => code
        };
    }

    private static string FormatTarget(string entity, string? entityId)
    {
        var e = (entity ?? string.Empty).Trim().ToUpperInvariant();

        var label = e switch
        {
            "PAYMENT" => "入金",
            "INVOICE" => "請求書",
            "MEMBER" => "会員",
            _ => entity
        };

        return string.IsNullOrWhiteSpace(entityId) ? label : $"{label} #{entityId}";
    }

    private static double CalculateAxisMaximum(decimal maxValue)
    {
        if (maxValue <= 0)
        {
            return 100000;
        }

        var raw = (double)maxValue * 1.2;

        if (raw <= 100000) return RoundUp(raw, 10000);
        if (raw <= 500000) return RoundUp(raw, 50000);
        if (raw <= 1000000) return RoundUp(raw, 100000);

        return RoundUp(raw, 500000);
    }

    private static double RoundUp(double value, double unit)
    {
        return Math.Ceiling(value / unit) * unit;
    }
}