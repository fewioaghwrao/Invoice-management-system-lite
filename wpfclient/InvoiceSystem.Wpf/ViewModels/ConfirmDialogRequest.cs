using InvoiceSystem.Wpf.Views;

namespace InvoiceSystem.Wpf.ViewModels;

public sealed class ConfirmDialogRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ConfirmText { get; set; } = "OK";
    public string CancelText { get; set; } = "キャンセル";
    public string? SubMessage { get; set; }
    public ConfirmDialogWindow.DialogVisualType VisualType { get; set; }
}
