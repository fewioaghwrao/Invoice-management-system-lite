using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;

namespace InvoiceSystem.Wpf.ViewModels;

public class AdminInvoiceEditViewModel : INotifyPropertyChanged
{
    private readonly IInvoiceService _invoiceService;
    private readonly InvoiceEditorMode _mode;
    private readonly long? _invoiceId;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AdminInvoiceEditViewModel(
        IInvoiceService invoiceService,
        InvoiceEditorMode mode,
        long? invoiceId = null)
    {
        _invoiceService = invoiceService;
        _mode = mode;
        _invoiceId = invoiceId;

        StatusOptions = new ObservableCollection<InvoiceStatusOption>
        {
            new() { Id = 1, Name = "未入金" },
            new() { Id = 2, Name = "一部入金" },
            new() { Id = 3, Name = "入金済み" },
            new() { Id = 4, Name = "期限超過" },
            new() { Id = 5, Name = "キャンセル" }
        };

        Lines.CollectionChanged += (_, _) => Recalculate();
        Members.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasMembers));
    }

    public ObservableCollection<EditableInvoiceLineItem> Lines { get; } = new();
    public ObservableCollection<InvoiceStatusOption> StatusOptions { get; }
    public ObservableCollection<MemberOptionDto> Members { get; } = new();

    public bool IsNewMode => _mode == InvoiceEditorMode.New;
    public bool IsEditMode => _mode == InvoiceEditorMode.Edit;

    public string ScreenTitle => IsNewMode ? "請求書作成" : "請求書編集";
    public string SaveButtonText => IsNewMode ? "作成" : "保存";
    public bool HasMembers => Members.Count > 0;

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    private bool _isSaving;
    public bool IsSaving
    {
        get => _isSaving;
        set { _isSaving = value; OnPropertyChanged(); }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    private long _memberId;
    public long MemberId
    {
        get => _memberId;
        set { _memberId = value; OnPropertyChanged(); }
    }

    private string _memberName = string.Empty;
    public string MemberName
    {
        get => _memberName;
        set { _memberName = value; OnPropertyChanged(); }
    }

    private string _invoiceNumber = string.Empty;
    public string InvoiceNumber
    {
        get => _invoiceNumber;
        set { _invoiceNumber = value; OnPropertyChanged(); }
    }

    private DateTime _invoiceDate = DateTime.Today;
    public DateTime InvoiceDate
    {
        get => _invoiceDate;
        set { _invoiceDate = value; OnPropertyChanged(); }
    }

    private DateTime _dueDate = DateTime.Today;
    public DateTime DueDate
    {
        get => _dueDate;
        set { _dueDate = value; OnPropertyChanged(); }
    }

    private string? _remarks;
    public string? Remarks
    {
        get => _remarks;
        set { _remarks = value; OnPropertyChanged(); }
    }

    private InvoiceStatusOption? _selectedStatus;
    public InvoiceStatusOption? SelectedStatus
    {
        get => _selectedStatus;
        set { _selectedStatus = value; OnPropertyChanged(); }
    }

    private EditableInvoiceLineItem? _selectedLine;
    public EditableInvoiceLineItem? SelectedLine
    {
        get => _selectedLine;
        set { _selectedLine = value; OnPropertyChanged(); }
    }

    private MemberOptionDto? _selectedMember;
    public MemberOptionDto? SelectedMember
    {
        get => _selectedMember;
        set
        {
            _selectedMember = value;
            if (_selectedMember != null)
            {
                MemberId = _selectedMember.Id;
                MemberName = _selectedMember.Name;
            }
            OnPropertyChanged();
        }
    }

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        set
        {
            _totalAmount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TotalAmountText));
        }
    }

    public string TotalAmountText => TotalAmount.ToString("C0", CultureInfo.GetCultureInfo("ja-JP"));

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            await LoadMembersAsync();

            if (IsNewMode)
            {
                InitializeForNew();
                return;
            }

            if (!_invoiceId.HasValue)
                throw new Exception("編集対象の請求書IDが指定されていません。");

            var dto = await _invoiceService.GetAdminInvoiceDetailAsync(_invoiceId.Value);

            MemberId = dto.MemberId;
            MemberName = dto.MemberName;
            InvoiceNumber = dto.InvoiceNumber;
            InvoiceDate = dto.InvoiceDate;
            DueDate = dto.DueDate;
            Remarks = dto.Remarks;

            SelectedStatus = StatusOptions.FirstOrDefault(x => x.Id == dto.StatusId);
            SelectedMember = Members.FirstOrDefault(x => x.Id == dto.MemberId);

            Lines.Clear();

            if (dto.Lines?.Count > 0)
            {
                foreach (var line in dto.Lines.OrderBy(x => x.LineNo))
                {
                    var item = new EditableInvoiceLineItem
                    {
                        Id = line.Id,
                        LineNo = line.LineNo,
                        Name = line.Name,
                        Qty = line.Qty,
                        UnitPrice = line.UnitPrice
                    };
                    item.PropertyChanged += Line_PropertyChanged;
                    Lines.Add(item);
                }
            }
            else
            {
                AddLine();
            }

            RenumberLines();
            Recalculate();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{ScreenTitle}データの取得に失敗しました。{Environment.NewLine}{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadMembersAsync()
    {
        Members.Clear();
        var members = await _invoiceService.GetMemberOptionsAsync();

        foreach (var member in members)
        {
            Members.Add(member);
        }
    }

    private void InitializeForNew()
    {
        InvoiceNumber = "INV-NEW";
        InvoiceDate = DateTime.Today;
        DueDate = DateTime.Today;
        Remarks = string.Empty;

        SelectedStatus = StatusOptions.FirstOrDefault(x => x.Id == 1);

        if (Members.Count > 0)
        {
            SelectedMember = Members[0];
        }

        Lines.Clear();
        AddLine();

        RenumberLines();
        Recalculate();
    }

    public void AddLine()
    {
        var item = new EditableInvoiceLineItem
        {
            LineNo = Lines.Count + 1,
            Name = string.Empty,
            Qty = 1,
            UnitPrice = 0
        };

        item.PropertyChanged += Line_PropertyChanged;
        Lines.Add(item);
        SelectedLine = item;

        RenumberLines();
        Recalculate();
    }

    public void RemoveSelectedLine()
    {
        if (SelectedLine is null)
            return;

        SelectedLine.PropertyChanged -= Line_PropertyChanged;
        Lines.Remove(SelectedLine);

        if (Lines.Count == 0)
        {
            AddLine();
        }

        RenumberLines();
        Recalculate();
    }

    public void MoveSelectedLineUp()
    {
        if (SelectedLine is null) return;

        var index = Lines.IndexOf(SelectedLine);
        if (index <= 0) return;

        Lines.Move(index, index - 1);
        RenumberLines();
        Recalculate();
    }

    public void MoveSelectedLineDown()
    {
        if (SelectedLine is null) return;

        var index = Lines.IndexOf(SelectedLine);
        if (index < 0 || index >= Lines.Count - 1) return;

        Lines.Move(index, index + 1);
        RenumberLines();
        Recalculate();
    }

    public async Task<bool> SaveAsync()
    {
        try
        {
            ErrorMessage = string.Empty;

            Validate();

            IsSaving = true;

            var request = new InvoiceUpsertRequestDto
            {
                MemberId = SelectedMember!.Id,
                InvoiceNumber = InvoiceNumber.Trim(),
                InvoiceDate = InvoiceDate,
                DueDate = DueDate,
                StatusId = SelectedStatus!.Id!.Value,
                Remarks = string.IsNullOrWhiteSpace(Remarks) ? null : Remarks.Trim(),
                Lines = Lines.Select(x => new InvoiceUpsertLineDto
                {
                    Id = IsNewMode ? null : x.Id,
                    LineNo = x.LineNo,
                    Name = x.Name.Trim(),
                    Qty = x.Qty,
                    UnitPrice = x.UnitPrice
                }).ToList()
            };

            if (IsNewMode)
            {
                await _invoiceService.CreateAdminInvoiceAsync(request);
            }
            else
            {
                if (!_invoiceId.HasValue)
                    throw new Exception("更新対象の請求書IDがありません。");

                await _invoiceService.UpdateAdminInvoiceAsync(_invoiceId.Value, request);
            }

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"請求書の{(IsNewMode ? "作成" : "更新")}に失敗しました。{Environment.NewLine}{ex.Message}";
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(InvoiceNumber))
            throw new Exception("請求番号を入力してください。");

        if (SelectedMember is null || SelectedMember.Id <= 0)
            throw new Exception("会員を選択してください。");

        if (SelectedStatus?.Id is null)
            throw new Exception("ステータスを選択してください。");

        if (DueDate < InvoiceDate)
            throw new Exception("支払期限は請求日以降にしてください。");

        if (Lines.Count == 0)
            throw new Exception("明細を1行以上入力してください。");

        foreach (var line in Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Name))
                throw new Exception("明細名を入力してください。");

            if (line.Qty <= 0)
                throw new Exception("数量は 0 より大きい値にしてください。");

            if (line.UnitPrice < 0)
                throw new Exception("単価は 0 以上にしてください。");
        }
    }

    private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(EditableInvoiceLineItem.Qty) or nameof(EditableInvoiceLineItem.UnitPrice))
        {
            Recalculate();
        }
    }

    private void RenumberLines()
    {
        for (int i = 0; i < Lines.Count; i++)
        {
            Lines[i].LineNo = i + 1;
        }
    }

    private void Recalculate()
    {
        TotalAmount = Lines.Sum(x => x.Amount);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }
}