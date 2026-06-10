using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Infrastructure.Data.Seed;

public static class SeedLockService
{
    public static void EnsureTable(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ""__SeedLocks"" (
                ""Key"" text PRIMARY KEY,
                ""CreatedAt"" timestamptz NOT NULL DEFAULT now()
            );
        ");
    }

    public static bool TryAcquire(AppDbContext context, string key)
    {
        var rows = context.Database.ExecuteSqlRaw(@"
            INSERT INTO ""__SeedLocks"" (""Key"") VALUES ({0})
            ON CONFLICT (""Key"") DO NOTHING;
        ", key);

        return rows == 1;
    }
}