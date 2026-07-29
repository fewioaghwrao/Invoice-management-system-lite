using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace InvoiceSystem.Wpf.Models;

public class EditableInvoiceLineItem : INotifyPropertyChanged
{
    private long? _id;
    private int _lineNo;
    private string _name = string.Empty;
    private decimal _qty = 1;
    private decimal _unitPrice;

    public event PropertyChangedEventHandler? PropertyChanged;

    public long? Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    public int LineNo
    {
        get => _lineNo;
        set
        {
            _lineNo = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public decimal Qty
    {
        get => _qty;
        set
        {
            _qty = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Amount));
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            _unitPrice = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Amount));
        }
    }

    public decimal Amount => Qty * UnitPrice;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}