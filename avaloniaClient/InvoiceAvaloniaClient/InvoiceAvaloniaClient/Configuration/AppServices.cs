using System.Text.Json;
using InvoiceAvaloniaClient.Services;

namespace InvoiceAvaloniaClient.Configuration;

/// <summary>
/// API の接続先（appsettings.json）と、画面間で共有するサービスをまとめたもの。
/// WPF 版の LoginWindow コンストラクタで行っていた生成処理をここに移した。
/// </summary>
public sealed class AppServices
{
    private AppServices(HttpClient httpClient)
    {
        HttpClient = httpClient;
        AuthService = new AuthService(httpClient);
        InvoiceService = new InvoiceService(httpClient);
        AccountService = new AccountService(httpClient);
        AdminService = new AdminService(httpClient);
        MemberService = new MemberService(httpClient);
        SalesService = new SalesService(httpClient);
        PaymentService = new PaymentService(httpClient);
    }

    public HttpClient HttpClient { get; }
    public AuthService AuthService { get; }
    public InvoiceService InvoiceService { get; }
    public AccountService AccountService { get; }
    public AdminService AdminService { get; }
    public MemberService MemberService { get; }
    public SalesService SalesService { get; }
    public PaymentService PaymentService { get; }

    /// <summary>
    /// appsettings.json を読み込んでサービス一式を作る。
    /// ログアウト後は作り直す（認証ヘッダーを持ち越さないため。WPF 版と同じ動き）。
    /// </summary>
    public static AppServices Create()
    {
        var apiSettings = LoadApiSettings();

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiSettings.BaseUrl)
        };

        return new AppServices(httpClient);
    }

    private static ApiSettings LoadApiSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"appsettings.json が見つかりません。（{path}）");
        }

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        if (!document.RootElement.TryGetProperty("ApiSettings", out var section))
        {
            throw new InvalidOperationException("ApiSettings の読み込みに失敗しました。");
        }

        var settings = section.Deserialize<ApiSettings>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (settings is null || string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            throw new InvalidOperationException("ApiSettings:BaseUrl が設定されていません。");
        }

        return settings;
    }
}
