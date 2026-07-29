using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace InvoiceSystem.Wpf.ViewModels;

public class AdminCollectionViewModel : INotifyPropertyChanged
{
    private readonly IInvoiceService _invoiceService;
    private readonly long _invoiceId;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AdminCollectionViewModel(IInvoiceService invoiceService, long invoiceId)
    {
        _invoiceService = invoiceService;
        _invoiceId = invoiceId;

        ChannelOptions = new List<SelectionOption>
        {
            new("EMAIL", "メール"),
            new("PHONE", "電話"),
            new("LETTER", "書面")
        };

        ToneOptions = new List<SelectionOption>
        {
            new("SOFT", "ソフト"),
            new("NORMAL", "標準"),
            new("STRONG", "強め")
        };

        _selectedChannel = "EMAIL";
        _selectedTone = "NORMAL";
    }

    public ObservableCollection<DunningLogRowViewModel> Logs { get; } = new();

    public List<SelectionOption> ChannelOptions { get; }
    public List<SelectionOption> ToneOptions { get; }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRecord));
            }
        }
    }

    private bool _isSaving;
    public bool IsSaving
    {
        get => _isSaving;
        set
        {
            if (_isSaving != value)
            {
                _isSaving = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRecord));
                OnPropertyChanged(nameof(RecordButtonText));
            }
        }
    }

    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    private long _invoiceIdValue;
    public long InvoiceId
    {
        get => _invoiceIdValue;
        set
        {
            if (_invoiceIdValue != value)
            {
                _invoiceIdValue = value;
                OnPropertyChanged();
            }
        }
    }

    private string _invoiceNumber = string.Empty;
    public string InvoiceNumber
    {
        get => _invoiceNumber;
        set
        {
            if (_invoiceNumber != value)
            {
                _invoiceNumber = value;
                OnPropertyChanged();
                UpdateTemplate();
            }
        }
    }

    private string _memberName = string.Empty;
    public string MemberName
    {
        get => _memberName;
        set
        {
            if (_memberName != value)
            {
                _memberName = value;
                OnPropertyChanged();
                UpdateTemplate();
            }
        }
    }

    private string _memberEmail = string.Empty;
    public string MemberEmail
    {
        get => _memberEmail;
        set
        {
            if (_memberEmail != value)
            {
                _memberEmail = value;
                OnPropertyChanged();
            }
        }
    }

    private DateTime _invoiceDate;
    public DateTime InvoiceDate
    {
        get => _invoiceDate;
        set
        {
            if (_invoiceDate != value)
            {
                _invoiceDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(InvoiceDateText));
            }
        }
    }

    public string InvoiceDateText => InvoiceDate == default
        ? "-"
        : $"請求日：{InvoiceDate:yyyy/MM/dd}";

    private DateTime _dueDate;
    public DateTime DueDate
    {
        get => _dueDate;
        set
        {
            if (_dueDate != value)
            {
                _dueDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DueDateText));
                UpdateTemplate();
            }
        }
    }

    public string DueDateText => DueDate == default
        ? "-"
        : DueDate.ToString("yyyy/MM/dd");

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        set
        {
            if (_totalAmount != value)
            {
                _totalAmount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalText));
                OnPropertyChanged(nameof(RemainingAmount));
                OnPropertyChanged(nameof(RemainingText));
                OnPropertyChanged(nameof(CanRecord));
                UpdateTemplate();
            }
        }
    }

    public string TotalText => FormatCurrency(TotalAmount);

    private decimal _paidTotal;
    public decimal PaidTotal
    {
        get => _paidTotal;
        set
        {
            if (_paidTotal != value)
            {
                _paidTotal = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PaidTotalText));
                OnPropertyChanged(nameof(RemainingAmount));
                OnPropertyChanged(nameof(RemainingText));
                OnPropertyChanged(nameof(CanRecord));
                UpdateTemplate();
            }
        }
    }

    public string PaidTotalText => $"入金済：{FormatCurrency(PaidTotal)}";

    public decimal RemainingAmount => Math.Max(0, TotalAmount - PaidTotal);

    public string RemainingText => FormatCurrency(RemainingAmount);

    private string _selectedChannel;
    public string SelectedChannel
    {
        get => _selectedChannel;
        set
        {
            if (_selectedChannel != value)
            {
                _selectedChannel = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedChannelLabel));
            }
        }
    }

    public string SelectedChannelLabel => ToChannelLabel(SelectedChannel);

    private string _selectedTone;
    public string SelectedTone
    {
        get => _selectedTone;
        set
        {
            if (_selectedTone != value)
            {
                _selectedTone = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedToneLabel));
                UpdateTemplate();
            }
        }
    }

    public string SelectedToneLabel => ToToneLabel(SelectedTone);

    private DateTime? _nextActionDate;
    public DateTime? NextActionDate
    {
        get => _nextActionDate;
        set
        {
            if (_nextActionDate != value)
            {
                _nextActionDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NextActionDateTextForDialog));
            }
        }
    }

    public string NextActionDateTextForDialog =>
        NextActionDate.HasValue
            ? NextActionDate.Value.ToString("yyyy/MM/dd")
            : "-";

    private string _memo = "テンプレ送付。";
    public string Memo
    {
        get => _memo;
        set
        {
            if (_memo != value)
            {
                _memo = value;
                OnPropertyChanged();
            }
        }
    }

    private string _subjectPreview = string.Empty;
    public string SubjectPreview
    {
        get => _subjectPreview;
        set
        {
            if (_subjectPreview != value)
            {
                _subjectPreview = value;
                OnPropertyChanged();
            }
        }
    }

    private string _bodyPreview = string.Empty;
    public string BodyPreview
    {
        get => _bodyPreview;
        set
        {
            if (_bodyPreview != value)
            {
                _bodyPreview = value;
                OnPropertyChanged();
            }
        }
    }

    public bool CanRecord => !IsLoading && !IsSaving && RemainingAmount > 0;

    public string RecordButtonText => IsSaving ? "記録中..." : "送信完了として記録";

    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var snapshot = await _invoiceService.GetCollectionSnapshotAsync(_invoiceId);
            var logs = await _invoiceService.GetCollectionLogsAsync(_invoiceId);

            ApplySnapshot(snapshot);
            ApplyLogs(logs);

            if (!NextActionDate.HasValue)
            {
                var latestNext = logs
                    .Where(x => x.NextActionDate.HasValue)
                    .OrderByDescending(x => x.At)
                    .Select(x => x.NextActionDate)
                    .FirstOrDefault();

                NextActionDate = latestNext ?? DueDate;
            }

            UpdateTemplate();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"催促情報の取得に失敗しました。{Environment.NewLine}{ex.Message}";
            Logs.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task<bool> RecordAsync()
    {
        if (RemainingAmount <= 0)
        {
            ErrorMessage = "未回収残額が0円のため、催促記録は不要です。";
            return false;
        }

        try
        {
            IsSaving = true;
            ErrorMessage = string.Empty;

            var title = Logs.Count == 0
                ? $"初回督促（{SelectedToneLabel}）"
                : $"督促（{Logs.Count + 1}回目 / {SelectedToneLabel}）";

            var request = new CreateDunningLogRequestDto
            {
                Channel = SelectedChannel,
                Tone = SelectedTone,
                Title = title,
                Memo = string.IsNullOrWhiteSpace(Memo) ? "テンプレ送付。" : Memo,
                NextActionDate = NextActionDate,
                Subject = SubjectPreview,
                BodyText = BodyPreview
            };

            await _invoiceService.CreateCollectionLogAsync(_invoiceId, request);

            var logs = await _invoiceService.GetCollectionLogsAsync(_invoiceId);
            ApplyLogs(logs);

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"催促履歴の登録に失敗しました。{Environment.NewLine}{ex.Message}";
            return false;
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ApplySnapshot(CollectionSnapshotDto dto)
    {
        InvoiceId = dto.InvoiceId;
        InvoiceNumber = dto.InvoiceNumber;
        MemberName = dto.MemberName;
        MemberEmail = dto.MemberEmail;
        InvoiceDate = dto.InvoiceDate;
        DueDate = dto.DueDate;
        TotalAmount = dto.Total;
        PaidTotal = dto.PaidTotal;
    }

    private void ApplyLogs(IEnumerable<DunningLogDto> logs)
    {
        Logs.Clear();

        foreach (var log in logs.OrderByDescending(x => x.At))
        {
            Logs.Add(new DunningLogRowViewModel
            {
                Id = log.Id,
                At = log.At,
                AtText = log.At.ToString("yyyy/MM/dd HH:mm"),
                Channel = log.Channel,
                ChannelLabel = ToChannelLabel(log.Channel),
                Tone = log.Tone,
                ToneLabel = ToToneLabel(log.Tone),
                Title = log.Title,
                Memo = string.IsNullOrWhiteSpace(log.Memo) ? "—" : log.Memo!,
                NextActionDate = log.NextActionDate,
                NextActionDateText = log.NextActionDate.HasValue
                    ? log.NextActionDate.Value.ToString("yyyy/MM/dd")
                    : "—"
            });
        }
    }

    private void UpdateTemplate()
    {
        var dueDateText = DueDate == default
            ? "-"
            : DueDate.ToString("yyyy/MM/dd");

        string subject = SelectedTone.ToUpperInvariant() switch
        {
            "SOFT" => $"【ご確認】お支払い状況のご確認のお願い（{InvoiceNumber}）",
            "STRONG" => $"【至急】お支払いのお願い（{InvoiceNumber}）",
            _ => $"【重要】お支払いのお願い（{InvoiceNumber}）"
        };

        string intro = SelectedTone.ToUpperInvariant() switch
        {
            "SOFT" => $"{MemberName} 様{Environment.NewLine}{Environment.NewLine}いつもお世話になっております。",
            "STRONG" => $"{MemberName} 様{Environment.NewLine}{Environment.NewLine}恐れ入りますが、至急ご確認ください。",
            _ => $"{MemberName} 様{Environment.NewLine}{Environment.NewLine}お世話になっております。"
        };

        string body = SelectedTone.ToUpperInvariant() switch
        {
            "SOFT" =>
                $"下記請求書につきまして、支払期限（{dueDateText}）を過ぎている可能性がございます。{Environment.NewLine}" +
                "行き違いがございましたら失礼いたしますが、ご入金状況をご確認いただけますでしょうか。",
            "STRONG" =>
                $"下記請求書につきまして、支払期限（{dueDateText}）を過ぎております。{Environment.NewLine}" +
                "至急ご対応をお願いいたします。",
            _ =>
                $"下記請求書につきまして、支払期限（{dueDateText}）を過ぎております。{Environment.NewLine}" +
                "恐れ入りますが、ご入金状況をご確認のうえ、ご対応をお願いいたします。"
        };

        var footer =
            Environment.NewLine + Environment.NewLine +
            $"【請求書番号】{InvoiceNumber}{Environment.NewLine}" +
            $"【未回収残額】{FormatCurrency(RemainingAmount)}{Environment.NewLine}{Environment.NewLine}" +
            "本メールと行き違いでご入金済みの場合は、ご容赦ください。" + Environment.NewLine +
            "よろしくお願いいたします。";

        SubjectPreview = subject;
        BodyPreview = intro + Environment.NewLine + Environment.NewLine + body + footer;
    }

    private string ToChannelLabel(string? channel)
    {
        return (channel ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "EMAIL" => "メール",
            "PHONE" => "電話",
            "LETTER" => "書面",
            _ => string.IsNullOrWhiteSpace(channel) ? "—" : channel
        };
    }

    private string ToToneLabel(string? tone)
    {
        return (tone ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "SOFT" => "ソフト",
            "NORMAL" => "標準",
            "STRONG" => "強め",
            _ => string.IsNullOrWhiteSpace(tone) ? "—" : tone
        };
    }

    private string FormatCurrency(decimal value)
    {
        return value.ToString("C0", CultureInfo.GetCultureInfo("ja-JP"));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }
}

public class SelectionOption
{
    public SelectionOption(string value, string label)
    {
        Value = value;
        Label = label;
    }

    public string Value { get; }
    public string Label { get; }
}

public class DunningLogRowViewModel
{
    public long Id { get; set; }
    public DateTime At { get; set; }
    public string AtText { get; set; } = string.Empty;

    public string Channel { get; set; } = string.Empty;
    public string ChannelLabel { get; set; } = string.Empty;

    public string? Tone { get; set; }
    public string ToneLabel { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Memo { get; set; } = string.Empty;

    public DateTime? NextActionDate { get; set; }
    public string NextActionDateText { get; set; } = string.Empty;
}