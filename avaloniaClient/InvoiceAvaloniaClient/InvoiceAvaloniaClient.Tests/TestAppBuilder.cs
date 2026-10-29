using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using InvoiceAvaloniaClient.Tests;

// すべてのテスト（[Fact] / [Theory]）を Avalonia の UI スレッド上で実行する。
// ViewModel は Dispatcher.UIThread 経由で変更通知を出すため、WPF 版の [StaFact] の代わりにこれを使う。
[assembly: AvaloniaTestFramework]
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace InvoiceAvaloniaClient.Tests;

/// <summary>
/// テスト用の Avalonia アプリ（画面を表示しないヘッドレス実行）。
/// 本番の App はログイン画面を開くため使わず、最小限の Application で起動する。
/// </summary>
public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<Application>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
