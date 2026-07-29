using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class PrivacyPolicyWindow : Window
{
    public PrivacyPolicyWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}