using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;
using System.Windows;

namespace InvoiceSystem.Wpf.Views;

public partial class MemberListWindow : Window
{
    private readonly MemberListViewModel _viewModel;

    public MemberListWindow(MemberService memberService)
    {
        InitializeComponent();

        _viewModel = new MemberListViewModel(memberService);
        DataContext = _viewModel;

        Loaded += MemberListWindow_Loaded;
    }

    private async void MemberListWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }
}