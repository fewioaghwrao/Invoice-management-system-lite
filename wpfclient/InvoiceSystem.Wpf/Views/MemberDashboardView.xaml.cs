using System.Windows.Controls;

namespace InvoiceSystem.Wpf.Views;

/// <summary>
/// マイページ（会員用シェル内に表示）。DataContext は MemberDashboardViewModel。
/// </summary>
public partial class MemberDashboardView : UserControl
{
    public MemberDashboardView()
    {
        InitializeComponent();
    }
}
