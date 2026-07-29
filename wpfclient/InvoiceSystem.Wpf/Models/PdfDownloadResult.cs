namespace InvoiceSystem.Wpf.Models
{
    public class PdfDownloadResult
    {
        public byte[] Content { get; set; } = System.Array.Empty<byte>();
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/pdf";
    }
}