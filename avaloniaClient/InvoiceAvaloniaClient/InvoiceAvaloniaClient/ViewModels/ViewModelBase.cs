using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace InvoiceAvaloniaClient.ViewModels;

/// <summary>
/// ViewModel の基底クラス。
/// CommunityToolkit.Mvvm の ObservableObject（SetProperty / OnPropertyChanged）に、
/// WPF 版から移植した ViewModel が使う RaisePropertyChanged を加えたもの。
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        OnPropertyChanged(propertyName);
    }
}
