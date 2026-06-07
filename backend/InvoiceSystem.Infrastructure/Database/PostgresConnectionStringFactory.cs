using Npgsql;

namespace InvoiceSystem.Infrastructure.Database;

public static class PostgresConnectionStringFactory
{
    public static string Create(string databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            throw new ArgumentException("Database URL is empty.", nameof(databaseUrl));
        }

        if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return ConvertHerokuDatabaseUrl(databaseUrl);
        }

        // Host=...;Username=...;Password=...;Database=... 形式はそのまま返す
        return databaseUrl;
    }

    private static string ConvertHerokuDatabaseUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : "",
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require,
            TrustServerCertificate = true,
        }.ConnectionString;
    }
}