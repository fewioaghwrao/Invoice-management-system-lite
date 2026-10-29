using Avalonia.Controls;
using Avalonia.Interactivity;
using InvoiceAvaloniaClient.ViewModels;

namespace InvoiceAvaloniaClient.Views;

/// <summary>
/// 会員用の登録情報（会員用シェル内に表示）。
/// </summary>
public partial class MemberProfileView : UserControl
{
    private readonly MemberProfileViewModel? _viewModel;
    private bool _initialized;

    /// <summary>デザイナー用。</summary>
    public MemberProfileView()
    {
        InitializeComponent();
    }

    public MemberProfileView(MemberProfileViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += View_OnLoaded;
    }

    private async void View_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (_initialized || _viewModel is null)
        {
            return;
        }

        _initialized = true;
        await _viewModel.InitializeAsync();
    }
}
