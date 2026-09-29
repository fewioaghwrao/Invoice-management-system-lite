using System.Globalization;
using System.Text;
using InvoiceSystem.Application.Dtos.AccessExport;

namespace InvoiceSystem.Api.Utils;

public static class AccessExportCsvBuilder
{
    private const string NewLine = "\r\n";

    public static string BuildInvoices(
        IReadOnlyList<AccessInvoiceExportRow> rows)
    {
        var sb = new StringBuilder();

        sb.Append(
            "InvoiceId,MemberId,InvoiceNumber,InvoiceDate,DueDate," +
            "TotalAmount,StatusCode,StatusName,IsOverdue,IsClosed,MemberName");
        sb.Append(NewLine);

        foreach (var row in rows)
        {
            sb.Append(row.InvoiceId);
            sb.Append(',');
            sb.Append(row.MemberId);
            sb.Append(',');
            sb.Append(Escape(row.InvoiceNumber));
            sb.Append(',');
            sb.Append(row.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(row.DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(row.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Escape(row.StatusCode));
            sb.Append(',');
            sb.Append(Escape(row.StatusName));
            sb.Append(',');
            sb.Append(row.IsOverdue ? "true" : "false");
            sb.Append(',');
            sb.Append(row.IsClosed ? "true" : "false");
            sb.Append(',');
            sb.Append(Escape(row.MemberName));
            sb.Append(NewLine);
        }

        return sb.ToString();
    }

    public static string BuildPayments(
        IReadOnlyList<AccessPaymentExportRow> rows)
    {
        var sb = new StringBuilder();

        sb.Append(
            "PaymentId,MemberId,PaymentDate,Amount,PayerName,Method");
        sb.Append(NewLine);

        foreach (var row in rows)
        {
            sb.Append(row.PaymentId);
            sb.Append(',');
            sb.Append(row.MemberId);
            sb.Append(',');
            sb.Append(row.PaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(row.Amount.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(',');
            sb.Append(Escape(row.PayerName));
            sb.Append(',');
            sb.Append(Escape(row.Method));
            sb.Append(NewLine);
        }

        return sb.ToString();
    }

    public static string BuildAllocations(
        IReadOnlyList<AccessAllocationExportRow> rows)
    {
        var sb = new StringBuilder();

        sb.Append(
            "AllocationId,PaymentId,InvoiceId,Amount");
        sb.Append(NewLine);

        foreach (var row in rows)
        {
            sb.Append(row.AllocationId);
            sb.Append(',');
            sb.Append(row.PaymentId);
            sb.Append(',');
            sb.Append(row.InvoiceId);
            sb.Append(',');
            sb.Append(row.Amount.ToString("0.00", CultureInfo.InvariantCulture));
            sb.Append(NewLine);
        }

        return sb.ToString();
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var escaped = value.Replace("\"", "\"\"");

        if (escaped.Contains(',') ||
            escaped.Contains('"') ||
            escaped.Contains('\r') ||
            escaped.Contains('\n'))
        {
            return $"\"{escaped}\"";
        }

        return escaped;
    }
}
