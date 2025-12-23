using InvoiceSystem.Api.Endpoints;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Application.Services.Auth;
using InvoiceSystem.Application.Services.Members;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Infrastructure.Email;
using InvoiceSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.Text;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// CORS
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ORIGINS");
// 例: "http://localhost:3000,https://xxxx.herokuapp.com,https://xxxx.vercel.app"

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (!string.IsNullOrWhiteSpace(corsOrigins))
        {
            var origins = corsOrigins
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            policy.WithOrigins(origins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            Console.WriteLine("[WARN] CORS_ORIGINS not set. Fallback to localhost only.");

            policy.WithOrigins("http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});



// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext (Postgres + Heroku DATABASE_URL 対応)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        // postgres:// or postgresql:// のときだけ変換（堅牢化）
        if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            options.UseNpgsql(ToNpgsqlConnectionString(databaseUrl));
        }
        else
        {
            // Host=... 形式が来る環境もある
            options.UseNpgsql(databaseUrl);
        }
    }
    else
    {
        var cs = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(cs);
    }
});

// Infrastructure 層 DI
builder.Services.AddInfrastructureServices();

// Services
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<IPasswordHasher<Member>, PasswordHasher<Member>>();
builder.Services.AddScoped<MemberRegistrationService>();
builder.Services.AddScoped<PasswordResetService>();
builder.Services.AddScoped<IEmailSender, MailtrapEmailSender>();
builder.Services.AddScoped<IMemberService, InvoiceSystem.Application.Services.Members.MemberService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();

// JWT
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("MemberOnly", p => p.RequireRole("Member"));
});

var app = builder.Build();

// 起動時：Migrate + Seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var hasher = services.GetRequiredService<IPasswordHasher<Member>>();
    // ② デモデータ（Development もしくは明示許可した時）
    var seedDemo = Environment.GetEnvironmentVariable("SEED_DEMO_DATA") == "true";

    context.Database.Migrate();

    // ① マスタ（全環境OK）
    SeedInvoiceStatuses(context);

    // ② デモデータ（Development のみ）
    if (app.Environment.IsDevelopment() || seedDemo)
    {
        SeedDemoData(context, hasher);
    }
}

app.UseCors();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapMemberEndpoints();
app.MapInvoiceEndpoints();
app.MapPaymentEndpoints();
app.MapCollectionEndpoints();
app.MapAuthEndpoints();
app.MapSalesEndpoints();
app.MapAdminEndpoints();
app.MapMyAccountEndpoints();

app.MapGet("/health", () => "OK");

app.Run();


// ===== Helpers =====

// Heroku DATABASE_URL -> Npgsql connection string
static string ToNpgsqlConnectionString(string databaseUrl)
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

static void SeedInvoiceStatuses(AppDbContext context)
{
    if (context.InvoiceStatuses.Any()) return;

    context.InvoiceStatuses.AddRange(
        new InvoiceStatus { Code = "UNPAID", Name = "未入金", IsOverdue = false, IsClosed = false, SortOrder = 1 },
        new InvoiceStatus { Code = "PARTIAL", Name = "一部入金", IsOverdue = false, IsClosed = false, SortOrder = 2 },
        new InvoiceStatus { Code = "PAID", Name = "入金済み", IsOverdue = false, IsClosed = true, SortOrder = 3 },
        new InvoiceStatus { Code = "OVERDUE", Name = "期限超過", IsOverdue = true, IsClosed = false, SortOrder = 4 },
        new InvoiceStatus { Code = "DUNNING", Name = "催促中", IsOverdue = true, IsClosed = false, SortOrder = 5 }
    );

    context.SaveChanges();
}

static InvoiceStatus MustGetStatus(AppDbContext context, string code)
{
    return context.InvoiceStatuses.SingleOrDefault(x => x.Code == code)
        ?? throw new InvalidOperationException($"InvoiceStatus not found. Code={code}");
}

static void SeedDemoData(AppDbContext context, IPasswordHasher<Member> hasher)
{
    var now = DateTime.UtcNow;

    // -----------------------------
    // Members (Upsert by Email)
    // -----------------------------
    Member UpsertMember(string name, string email, MemberRole role, string rawPassword,
        string? postal = null, string? address = null, string? phone = null)
    {
        var m = context.Members.SingleOrDefault(x => x.Email == email);

        if (m == null)
        {
            m = new Member
            {
                Name = name,
                Email = email,
                PostalCode = postal,
                Address = address,
                Phone = phone,
                IsActive = true,
                Role = role,
                CreatedAt = now,
                UpdatedAt = now,
                IsEmailConfirmed = true
            };
            m.PasswordHash = hasher.HashPassword(m, rawPassword);
            context.Members.Add(m);
            context.SaveChanges();
        }
        else
        {
            // 最低限の補正（必要なら）
            m.Name = name;
            m.Role = role;
            m.IsActive = true;
            m.IsEmailConfirmed = true;
            m.PostalCode ??= postal;
            m.Address ??= address;
            m.Phone ??= phone;
            m.UpdatedAt = now;

            // パスワードを固定で上書きしたいなら有効化（任意）
            // m.PasswordHash = hasher.HashPassword(m, rawPassword);

            context.SaveChanges();
        }
        return m;
    }

    var admin = UpsertMember("デモ管理者", "admin@example.com", MemberRole.Admin, "Admin1234!",
        "1000001", "東京都千代田区テスト1-1-1", "090-1111-2222");

    var member1 = UpsertMember("デモ会員1", "member@example.com", MemberRole.Customer, "Member1234!",
        "1000002", "東京都千代田区テスト2-2-2", "090-2222-3333");

    var member2 = UpsertMember("デモ会員2", "member2@example.com", MemberRole.Customer, "Test1234!",
        "1000003", "東京都千代田区テスト3-3-3", "090-3333-4444");

    var member3 = UpsertMember("デモ会員3", "member3@example.com", MemberRole.Customer, "Test1234!",
        "1000004", "東京都千代田区テスト4-4-4", "090-4444-5555");

    var member4 = UpsertMember("デモ会員4", "member4@example.com", MemberRole.Customer, "Test1234!",
        "1000005", "東京都千代田区テスト5-5-5", "090-5555-6666");

    // -----------------------------
    // Status master
    // -----------------------------
    var stUnpaid = MustGetStatus(context, "UNPAID");
    var stPartial = MustGetStatus(context, "PARTIAL");
    var stPaid = MustGetStatus(context, "PAID");

    // -----------------------------
    // Helper: Invoice + Lines (idempotent by InvoiceNumber)
    // ※ Status は “最初はUNPAIDで入れる” → 後で allocation から再計算
    // -----------------------------
    Invoice UpsertInvoice(
        Member m,
        string invoiceNo,
        DateTime invoiceDateUtc,
        DateTime dueDateUtc,
        params (int lineNo, string name, int qty, decimal unitPrice)[] lines)
    {
        var inv = context.Invoices
            .Include(x => x.Lines)
            .SingleOrDefault(x => x.InvoiceNumber == invoiceNo);

        var total = lines.Sum(x => x.qty * x.unitPrice);

        if (inv == null)
        {
            inv = new Invoice
            {
                MemberId = m.Id,
                InvoiceNumber = invoiceNo,
                InvoiceDate = invoiceDateUtc, // ★UTC
                DueDate = dueDateUtc,         // ★UTC
                TotalAmount = total,
                StatusId = stUnpaid.Id,       // ★後で再計算
                CreatedAt = now,
                UpdatedAt = now,
                Lines = new List<InvoiceLine>()
            };

            foreach (var (lineNo, name, qty, unitPrice) in lines)
            {
                inv.Lines.Add(new InvoiceLine
                {
                    LineNo = lineNo,
                    Name = name,
                    Qty = qty,
                    UnitPrice = unitPrice,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            context.Invoices.Add(inv);
            context.SaveChanges();
        }
        else
        {
            // “Seedで確実に揃える”ために更新（増殖しない）
            inv.MemberId = m.Id;
            inv.InvoiceDate = invoiceDateUtc;
            inv.DueDate = dueDateUtc;
            inv.TotalAmount = total;
            inv.UpdatedAt = now;

            // Linesが空なら入れる（既にあるなら触らない）
            if (inv.Lines == null || inv.Lines.Count == 0)
            {
                inv.Lines = new List<InvoiceLine>();
                foreach (var (lineNo, name, qty, unitPrice) in lines)
                {
                    inv.Lines.Add(new InvoiceLine
                    {
                        LineNo = lineNo,
                        Name = name,
                        Qty = qty,
                        UnitPrice = unitPrice,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }

            context.SaveChanges();
        }

        return inv;
    }

    // -----------------------------
    // Helper: Payment (idempotent by MemberId + PaymentDate + Amount)
    // -----------------------------
    Payment UpsertPayment(Member m, DateTime paymentDateUtc, decimal amount, string method = "銀行振込", string? payerName = null)
    {
        var p = context.Payments.SingleOrDefault(x =>
            x.MemberId == m.Id &&
            x.PaymentDate == paymentDateUtc &&
            x.Amount == amount);

        if (p == null)
        {
            p = new Payment
            {
                MemberId = m.Id,
                PaymentDate = paymentDateUtc, // ★UTC
                Amount = amount,
                Method = method,
                PayerName = payerName ?? m.Name,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.Payments.Add(p);
            context.SaveChanges();
        }

        return p;
    }

    // -----------------------------
    // Helper: Allocation (idempotent-ish by PaymentId + InvoiceId + Amount)
    // -----------------------------
    void EnsureAllocation(Payment p, Invoice inv, decimal amount)
    {
        var exists = context.PaymentAllocations.Any(x =>
            x.PaymentId == p.Id &&
            x.InvoiceId == inv.Id &&
            x.Amount == amount);

        if (!exists)
        {
            context.PaymentAllocations.Add(new PaymentAllocation
            {
                PaymentId = p.Id,
                InvoiceId = inv.Id,
                Amount = amount
            });
            context.SaveChanges();
        }
    }

    // ==========================================================
    // Bulk demo data: 2025-01 .. 2026-12
    //  - each member: 20 invoices per month
    //  - mix statuses every month: UNPAID / PARTIAL(keep) / PARTIAL->PAID
    //  - member1 is heavier (more PARTIAL->PAID)
    //  - UpsertPayment key is weak (MemberId+PaymentDate+Amount), so make PaymentDate unique
    // ==========================================================

    // UTC helper (seedは0時固定でOK、Paymentはズラす)
    DateTime Utc(int y, int m, int d) => new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    // PaymentDateを必ずユニークにする（UpsertPayment衝突回避）
    // baseDayUtc は 00:00:00 を想定。memberNo(1..4), monthIndex(0..), invoiceSeq(1..20), splitNo(1..2)でズラす
    DateTime UniquePayAt(DateTime baseDayUtc, int memberNo, int monthIndex, int invoiceSeq, int splitNo)
    {
        // 分がかぶらないように大きめのオフセット
        // 例：会員ごとに 300分ブロック、月ごとに 30分、請求書ごとに 2分、splitで+0/+1
        var minutes =
            (memberNo * 300) +
            (monthIndex * 30) +
            (invoiceSeq * 2) +
            (splitNo - 1);

        return baseDayUtc.AddMinutes(minutes);
    }

    // 金額の規則（単純すぎると金額重複が増えるので、月/会員/連番で揺らす）
    decimal CalcAmount(int y, int m, int memberNo, int seq)
    {
        // 20,000 ～ 79,000 の範囲でゆらす
        var v = 20000 + ((y * 37 + m * 911 + memberNo * 701 + seq * 1337) % 60000);
        // 1000円単位に丸め
        v = (v / 1000) * 1000;
        return v;
    }

    // 支払パターン配分（会員1を厚め）
    (int unpaid, int partialKeep, int partialToPaid) GetMix(int memberNo)
    {
        if (memberNo == 1)
        {
            // 会員1：毎月20件のうち “一部→完了” を多め
            return (unpaid: 4, partialKeep: 4, partialToPaid: 12);
        }
        // 会員2〜4：バランス型（必ず一部→完了あり）
        return (unpaid: 8, partialKeep: 6, partialToPaid: 6);
    }

    var members = new (Member mem, int memberNo)[]
    {
    (member1, 1),
    (member2, 2),
    (member3, 3),
    (member4, 4),
    };

    var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    var end = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);

    int monthIndex = 0;
    for (var dt = start; dt <= end; dt = dt.AddMonths(1), monthIndex++)
    {
        int y = dt.Year;
        int m = dt.Month;

        foreach (var (mem, memberNo) in members)
        {
            var (unpaidCount, partialKeepCount, partialToPaidCount) = GetMix(memberNo);

            // 月の請求書20件を作る
            var invList = new List<(Invoice inv, decimal total, int seq, DateTime issuedUtc)>(20);

            for (int seq = 1; seq <= 20; seq++)
            {
                var invoiceNo = $"INV-{y}-{m:00}-M{memberNo}-{seq:000}";

                // 発行日：月初～中旬でばらす（1..20日、月末超えない）
                var issueDay = Math.Min(1 + (seq % 20), DateTime.DaysInMonth(y, m));
                var issuedUtc = Utc(y, m, issueDay);

                // 期限：原則月末（※あなたの既存に合わせやすい）
                var dueUtc = Utc(y, m, DateTime.DaysInMonth(y, m));

                var amount = CalcAmount(y, m, memberNo, seq);

                var inv = UpsertInvoice(
                    mem,
                    invoiceNo,
                    issuedUtc,
                    dueUtc,
                    (1, $"月額利用料（{y}/{m:00}）#{seq:00}", 1, amount)
                );

                invList.Add((inv, amount, seq, issuedUtc));
            }

            // 20件を並び順のまま「混在」させる
            int idx = 0;

            // 1) UNPAID：何もしない
            idx += unpaidCount;

            // 2) PARTIAL（残す）：1回だけ入金して止める
            for (int k = 0; k < partialKeepCount; k++)
            {
                var (inv, total, seq, issuedUtc) = invList[idx++];

                // 例：40%入金（最低1000）
                var pay1 = Math.Max(1000m, Math.Floor(total * 0.4m / 1000m) * 1000m);

                // 入金日は「発行日+10日」基準、ユニーク化
                var basePayDay = issuedUtc.AddDays(10);
                var payAt = UniquePayAt(basePayDay, memberNo, monthIndex, seq, splitNo: 1);

                var p1 = UpsertPayment(mem, payAt, pay1);
                EnsureAllocation(p1, inv, pay1);
            }

            // 3) PARTIAL -> PAID：2回に分けて完了
            for (int k = 0; k < partialToPaidCount; k++)
            {
                var (inv, total, seq, issuedUtc) = invList[idx++];

                // 例：1回目 30%（最低1000）＋2回目 残額
                var payA = Math.Max(1000m, Math.Floor(total * 0.3m / 1000m) * 1000m);
                if (payA >= total) payA = total - 1000m; // 念のため
                var payB = total - payA;

                var basePayDayA = issuedUtc.AddDays(8);
                var payAtA = UniquePayAt(basePayDayA, memberNo, monthIndex, seq, splitNo: 1);

                var basePayDayB = issuedUtc.AddDays(18);
                var payAtB = UniquePayAt(basePayDayB, memberNo, monthIndex, seq, splitNo: 2);

                var pA = UpsertPayment(mem, payAtA, payA);
                EnsureAllocation(pA, inv, payA);

                var pB = UpsertPayment(mem, payAtB, payB);
                EnsureAllocation(pB, inv, payB);
            }
        }
    }

    // ==========================================================
    // Recalculate Status from allocations (UNPAID/PARTIAL/PAID)
    // 対象：2025-2026 の INV に拡張
    // ==========================================================
    var targetInvoices = context.Invoices
        .Include(i => i.PaymentAllocations)
        .Where(i =>
            i.InvoiceNumber.StartsWith("INV-2025-") ||
            i.InvoiceNumber.StartsWith("INV-2026-"))
        .ToList();

    foreach (var inv in targetInvoices)
    {
        var paid = inv.PaymentAllocations.Sum(a => a.Amount);
        var total = inv.TotalAmount;

        long newStatusId =
            paid <= 0m ? stUnpaid.Id :
            paid < total ? stPartial.Id :
            stPaid.Id;

        if (inv.StatusId != newStatusId)
        {
            inv.StatusId = newStatusId;
            inv.UpdatedAt = now;
        }
    }
    context.SaveChanges();
}





