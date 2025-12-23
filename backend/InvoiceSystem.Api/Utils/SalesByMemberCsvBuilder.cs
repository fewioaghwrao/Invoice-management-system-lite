namespace InvoiceSystem.Api.Utils;

using System.Globalization;
using System.Text;
using InvoiceSystem.Application.Dtos.Sales;

public static class SalesByMemberCsvBuilder
{
    public static string Build(
        IEnumerable<SalesByMemberRowDto> rows,
        SalesSummaryDto? summary = null   // ★追加
    )
    {
        var nl = "\r\n";
        var sb = new StringBuilder();

        sb.Append("顧客名,請求合計,入金済,未回収,回収率").Append(nl);

        foreach (var r in rows)
        {
            sb
                .Append(E(r.MemberName)).Append(',')
                .Append(E(r.InvoiceTotal.ToString("0", CultureInfo.InvariantCulture))).Append(',')
                .Append(E(r.PaidTotal.ToString("0", CultureInfo.InvariantCulture))).Append(',')
                .Append(E(r.RemainingTotal.ToString("0", CultureInfo.InvariantCulture))).Append(',')
                .Append(E(r.RecoveryRate.ToString("0.0", CultureInfo.InvariantCulture)))
                .Append(nl);
        }

        // ★合計行（ここから）
        if (summary is not null)
        {
            sb
                .Append("合計").Append(',')
                .Append(E(summary.InvoiceTotal.ToString("0", CultureInfo.InvariantCulture))).Append(',')
                .Append(E(summary.PaidTotal.ToString("0", CultureInfo.InvariantCulture))).Append(',')
                .Append(E(summary.RemainingTotal.ToString("0", CultureInfo.InvariantCulture))).Append(',')
                .Append(E(summary.RecoveryRate.ToString("0.0", CultureInfo.InvariantCulture)))
                .Append(nl);
        }
        // ★ここまで

        return sb.ToString();
    }

    private static string E(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
