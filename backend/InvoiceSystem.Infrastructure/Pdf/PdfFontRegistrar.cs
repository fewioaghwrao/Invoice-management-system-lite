using QuestPDF.Drawing;

namespace InvoiceSystem.Infrastructure.Pdf;

public static class PdfFontRegistrar
{
    public static void Register()
    {
        var baseDir = AppContext.BaseDirectory;

        RegisterFont(
            Path.Combine(baseDir, "Assets", "Fonts", "NotoSansJP-Regular.ttf"),
            required: true);

        RegisterFont(
            Path.Combine(baseDir, "Assets", "Fonts", "NotoSansJP-Bold.ttf"),
            required: false);
    }

    private static void RegisterFont(string path, bool required)
    {
        if (File.Exists(path))
        {
            FontManager.RegisterFont(File.OpenRead(path));
            Console.WriteLine($"[PDF] Font registered: {path}");
            return;
        }

        var suffix = required ? "" : " (optional)";
        Console.WriteLine($"[PDF] Font NOT found: {path}{suffix}");
    }
}