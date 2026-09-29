using System.IO.Compression;
using System.Text;
using InvoiceSystem.Application.Dtos.AccessExport;

namespace InvoiceSystem.Api.Utils;

public static class AccessExportZipBuilder
{
    public static byte[] Build(
        AccessExportDataDto data)
    {
        var invoicesCsv =
            AccessExportCsvBuilder.BuildInvoices(data.Invoices);

        var paymentsCsv =
            AccessExportCsvBuilder.BuildPayments(data.Payments);

        var allocationsCsv =
            AccessExportCsvBuilder.BuildAllocations(data.Allocations);

        var ym = $"{data.Year}{data.Month:00}";

        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(
            stream,
            ZipArchiveMode.Create,
            leaveOpen: true))
        {
            WriteEntry(
                archive,
                $"invoices_{ym}.csv",
                invoicesCsv);

            WriteEntry(
                archive,
                $"payments_{ym}.csv",
                paymentsCsv);

            WriteEntry(
                archive,
                $"allocations_{ym}.csv",
                allocationsCsv);
        }

        return stream.ToArray();
    }

    private static void WriteEntry(
        ZipArchive archive,
        string fileName,
        string content)
    {
        var entry = archive.CreateEntry(
            fileName,
            CompressionLevel.Optimal);

        using var entryStream = entry.Open();

        var bom = Encoding.UTF8.GetPreamble();
        entryStream.Write(bom, 0, bom.Length);

        var body = Encoding.UTF8.GetBytes(content);
        entryStream.Write(body, 0, body.Length);
    }
}