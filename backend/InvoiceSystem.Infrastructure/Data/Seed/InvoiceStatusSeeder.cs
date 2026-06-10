using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Infrastructure.Data.Seed;

public static class InvoiceStatusSeeder
{
    public static void Seed(AppDbContext context)
    {
        context.Database.ExecuteSqlRaw(@"
            INSERT INTO ""InvoiceStatuses"" (""Code"", ""Name"", ""IsOverdue"", ""IsClosed"", ""SortOrder"")
            VALUES
                ('UNPAID',  '未入金',   FALSE, FALSE, 1),
                ('PARTIAL', '一部入金', FALSE, FALSE, 2),
                ('PAID',    '入金済み', FALSE, TRUE,  3),
                ('OVERDUE', '期限超過', TRUE,  FALSE, 4),
                ('DUNNING', '催促中',   TRUE,  FALSE, 5)
            ON CONFLICT (""Code"") DO UPDATE
            SET
                ""Name"" = EXCLUDED.""Name"",
                ""IsOverdue"" = EXCLUDED.""IsOverdue"",
                ""IsClosed"" = EXCLUDED.""IsClosed"",
                ""SortOrder"" = EXCLUDED.""SortOrder"";
        ");
    }
}