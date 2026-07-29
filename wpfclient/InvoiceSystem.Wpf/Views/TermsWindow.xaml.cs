using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class TermsWindow : Window
{
    public TermsWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}