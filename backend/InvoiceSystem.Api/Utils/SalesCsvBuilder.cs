namespace InvoiceSystem.Api.Utils;

using System.Globalization;
using System.Text;
using InvoiceSystem.Application.Services.Sales; // ★これが重要

public static class SalesCsvBuilder
{
    public static string Build(IEnumerable<SalesExportRow> rows)
    {
        var nl = "\r\n";
        var sb = new StringBuilder();

        sb.Append("請求番号,顧客名,発行日,期限,ステータス,請求金額,入金済,残額,最終入金日").Append(nl);

        foreach (var r in rows)
        {
            sb
                .Append(E(r.InvoiceNumber)).Append(',')
                .Append(E(r.ClientName)).Append(',')
                .Append(E(r.IssuedAt.ToString("yyyy/MM/dd"))).Append(',')
                .Append(E(r.DueAt.ToString("yyyy/MM/dd"))).Append(',')
                .Append(E(r.Status)).Append(',')
                .Append(E(r.InvoiceAmount.ToString("0", CultureInfo.InvariantCulture))).Append(',')
                .Append(E(r.PaidAmount.ToString("0", CultureInfo.InvariantCulture))).Append(',')
                .Append(E(r.RemainingAmount.ToString("0", CultureInfo.InvariantCulture))).Append(',')
                .Append(E(r.LastPaidAt?.ToString("yyyy/MM/dd") ?? ""))
                .Append(nl);
        }

        return sb.ToString();
    }

    private static string E(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}

