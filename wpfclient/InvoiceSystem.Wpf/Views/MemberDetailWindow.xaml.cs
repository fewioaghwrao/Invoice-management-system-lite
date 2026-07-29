using System.Windows;
using InvoiceSystem.Wpf.Services;
using InvoiceSystem.Wpf.ViewModels;

namespace InvoiceSystem.Wpf.Views;

public partial class MemberDetailWindow : Window
{
    private readonly IMemberService _memberService;
    private readonly MemberDetailViewModel _viewModel;

    public MemberDetailWindow(IMemberService memberService, int memberId)
    {
        InitializeComponent();

        _memberService = memberService;

        _viewModel = new MemberDetailViewModel(
            memberService,
            memberId,
            this);

        DataContext = _viewModel;

        Loaded += MemberDetailWindow_Loaded;
    }

    private async void MemberDetailWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadAsync();
    }
}