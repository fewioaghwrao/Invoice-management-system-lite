using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceSystem.Infrastructure.Data;

public static class AppDbInitializer
{
    private const string DemoSeedKey = "demo-seed-v1";

    public static void Initialize(IServiceProvider services, bool isDevelopment)
    {
        using var scope = services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Member>>();

        var seedDemo = Environment.GetEnvironmentVariable("SEED_DEMO_DATA") == "true";

        context.Database.Migrate();

        InvoiceStatusSeeder.Seed(context);

        if (!isDevelopment && !seedDemo)
        {
            return;
        }

        SeedLockService.EnsureTable(context);

        if (!SeedLockService.TryAcquire(context, DemoSeedKey))
        {
            Console.WriteLine($"[Seed] {DemoSeedKey} already applied. Skip.");
            return;
        }

        DemoDataSeeder.Seed(context, hasher);
    }
}