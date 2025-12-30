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
using Microsoft.OpenApi.Models;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// ★起動時にフォント登録（必須）
var fontRegularPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "NotoSansJP-Regular.ttf");
var fontBoldPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "NotoSansJP-Bold.ttf");

FontManager.RegisterFont(File.OpenRead(fontRegularPath));
FontManager.RegisterFont(File.OpenRead(fontBoldPath));

var builder = WebApplication.CreateBuilder(args);

// ================================
// PDF用 日本語フォント登録（重要）
// ================================
RegisterPdfFonts();

static void RegisterPdfFonts()
{
    var baseDir = AppContext.BaseDirectory;

    var regularPath = Path.Combine(baseDir, "Assets", "Fonts", "NotoSansJP-Regular.ttf");
    var boldPath = Path.Combine(baseDir, "Assets", "Fonts", "NotoSansJP-Bold.ttf");

    if (File.Exists(regularPath))
    {
        FontManager.RegisterFont(File.OpenRead(regularPath));
        Console.WriteLine($"[PDF] Font registered: {regularPath}");
    }
    else
    {
        Console.WriteLine($"[PDF] Font NOT found: {regularPath}");
    }

    if (File.Exists(boldPath))
    {
        FontManager.RegisterFont(File.OpenRead(boldPath));
        Console.WriteLine($"[PDF] Font registered: {boldPath}");
    }
    else
    {
        Console.WriteLine($"[PDF] Font NOT found: {boldPath} (optional)");
    }
}

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

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InvoiceSystem.Api",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});


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

// Audit Logger
builder.Services.AddScoped<IAuditLogger, AuditLogger>();

// Admin Operation Log Service
builder.Services.AddScoped<IAdminOperationLogService, AdminOperationLogService>();

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
app.MapAdminOperationLogEndpoints();


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
    void Upsert(string code, string name, bool isOverdue, bool isClosed, int sort)
    {
        var st = context.InvoiceStatuses.SingleOrDefault(x => x.Code == code);
        if (st == null)
        {
            context.InvoiceStatuses.Add(new InvoiceStatus
            {
                Code = code,
                Name = name,
                IsOverdue = isOverdue,
                IsClosed = isClosed,
                SortOrder = sort
            });
        }
        else
        {
            // 既存があっても不足/変更を補正する（安全）
            st.Name = name;
            st.IsOverdue = isOverdue;
            st.IsClosed = isClosed;
            st.SortOrder = sort;
        }
    }

    Upsert("UNPAID", "未入金", false, false, 1);
    Upsert("PARTIAL", "一部入金", false, false, 2);
    Upsert("PAID", "入金済み", false, true, 3);
    Upsert("OVERDUE", "期限超過", true, false, 4);
    Upsert("DUNNING", "催促中", true, false, 5);

    context.SaveChanges();
}


static InvoiceStatus MustGetStatus(AppDbContext context, string code)
{
    return context.InvoiceStatuses.SingleOrDefault(x => x.Code == code)
        ?? throw new InvalidOperationException($"InvoiceStatus not found. Code={code}");
}

static void SeedDemoData(AppDbContext context, IPasswordHasher<Member> hasher)
{
    var now = new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc);

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
    // Status master（5種を取得）
    // -----------------------------
    var stUnpaid = MustGetStatus(context, "UNPAID");
    var stPartial = MustGetStatus(context, "PARTIAL");
    var stPaid = MustGetStatus(context, "PAID");
    var stOverdue = MustGetStatus(context, "OVERDUE");
    var stDunning = MustGetStatus(context, "DUNNING"); // 今回は作るだけで未使用

    // -----------------------------
    // Helper: Invoice + Lines (idempotent by InvoiceNumber)
    // Helper: Payment (idempotent by MemberId + PaymentDate + Amount) 
    // Helper: Allocation ※既存のままでOK
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
                InvoiceDate = invoiceDateUtc,
                DueDate = dueDateUtc,
                TotalAmount = total,
                StatusId = stUnpaid.Id, // ★後で再計算
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
            inv.MemberId = m.Id;
            inv.InvoiceDate = invoiceDateUtc;
            inv.DueDate = dueDateUtc;
            inv.TotalAmount = total;
            inv.UpdatedAt = now;

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
                PaymentDate = paymentDateUtc,
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

    // UTC helper
    DateTime Utc(int y, int m, int d) => new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc);

    // PaymentDate を衝突しないようにズラす
    DateTime PayAt(int y, int m, int day, int memberNo, int invSeq, int splitNo)
    {
        var baseUtc = Utc(y, m, day);

        // ★月(m)も混ぜて衝突をさらに避ける
        var minutes = (memberNo * 300) + (m * 20) + (invSeq * 5) + (splitNo - 1);

        return baseUtc.AddMinutes(minutes);
    }


    // ==========================================================
    // ① 2025年11月（完全固定：過去・完結 + overdue）
    // ==========================================================
    // 会員1：入金済み（1回）
    {
        var y = 2025; var m = 11;
        var inv = UpsertInvoice(member1, $"INV-{y}-{m:00}-FIX-M1-001", Utc(y, m, 5), Utc(y, m, 30),
            (1, $"月額利用料（{y}/{m:00}）", 1, 120000m));

        var p = UpsertPayment(member1, PayAt(y, m, 20, 1, 1, 1), 120000m);
        EnsureAllocation(p, inv, 120000m);
    }

    // 会員2：分割入金（2回で完了）
    {
        var y = 2025; var m = 11;
        var inv = UpsertInvoice(member2, $"INV-{y}-{m:00}-FIX-M2-001", Utc(y, m, 7), Utc(y, m, 30),
            (1, $"月額利用料（{y}/{m:00}）", 1, 200000m));

        var p1 = UpsertPayment(member2, PayAt(y, m, 15, 2, 1, 1), 100000m);
        var p2 = UpsertPayment(member2, PayAt(y, m, 28, 2, 1, 2), 100000m);
        EnsureAllocation(p1, inv, 100000m);
        EnsureAllocation(p2, inv, 100000m);
    }

    // 会員3：未入金（期限超過＝OVERDUEの見せ場）
    {
        var y = 2025; var m = 11;
        _ = UpsertInvoice(member3, $"INV-{y}-{m:00}-FIX-M3-001", Utc(y, m, 10), Utc(y, m, 25),
            (1, $"月額利用料（{y}/{m:00}）", 1, 80000m));
        // 入金なし
    }

    // 会員4：入金済み
    {
        var y = 2025; var m = 11;
        var inv = UpsertInvoice(member4, $"INV-{y}-{m:00}-FIX-M4-001", Utc(y, m, 12), Utc(y, m, 30),
            (1, $"月額利用料（{y}/{m:00}）", 1, 50000m));

        var p = UpsertPayment(member4, PayAt(y, m, 29, 4, 1, 1), 50000m);

        EnsureAllocation(p, inv, 50000m);
    }
    // （追加）会員4：催促中（DUNNING）
    {
        var y = 2025; var m = 11;
        var inv = UpsertInvoice(member4, $"INV-{y}-{m:00}-FIX-M4-002-DUNNING", Utc(y, m, 2), Utc(y, m, 10),
            (1, $"月額利用料（{y}/{m:00}）", 1, 50000m));

        // 入金なし・ステータスは明示で DUNNING
        inv.StatusId = stDunning.Id;
        inv.UpdatedAt = now;
        context.SaveChanges();
    }

    // ==========================================================
    // ②// ② 2025年12月（完全固定：説明用サンプル（UNPAID/PARTIAL/PAID）
    // ==========================================================
    // 会員1：一部入金（PARTIAL）
    {
        var y = 2025; var m = 12;
        var inv = UpsertInvoice(member1, $"INV-{y}-{m:00}-FIX-M1-001", Utc(y, m, 1), Utc(y, m, 31),
            (1, $"月額利用料（{y}/{m:00}）", 1, 150000m));

        var p = UpsertPayment(member1, PayAt(y, m, 10, 1, 1, 1), 50000m);
        EnsureAllocation(p, inv, 50000m);
    }

    // 会員2：未入金（期限未来）
    {
        var y = 2025; var m = 12;
        _ = UpsertInvoice(member2, $"INV-{y}-{m:00}-FIX-M2-001", Utc(y, m, 3), Utc(y, m, 28),
            (1, $"月額利用料（{y}/{m:00}）", 1, 90000m));
    }

    // 会員3：入金済み
    {
        var y = 2025; var m = 12;
        var inv = UpsertInvoice(member3, $"INV-{y}-{m:00}-FIX-M3-001", Utc(y, m, 5), Utc(y, m, 25),
            (1, $"月額利用料（{y}/{m:00}）", 1, 60000m));

        var p = UpsertPayment(member3, PayAt(y, m, 20, 3, 1, 1), 60000m);
        EnsureAllocation(p, inv, 60000m);
    }

    // 会員4：分割予定（1回目のみ＝PARTIAL）
    {
        var y = 2025; var m = 12;
        var inv = UpsertInvoice(member4, $"INV-{y}-{m:00}-FIX-M4-001", Utc(y, m, 8), Utc(y, m, 31),
            (1, $"月額利用料（{y}/{m:00}）", 1, 100000m));

        var p1 = UpsertPayment(member4, PayAt(y, m, 18, 4, 1, 1), 40000m);
        EnsureAllocation(p1, inv, 40000m);
    }

    // ==========================================================
    // ③ 2026年（1年分）: 毎月 会員1〜4 各1枚（=48件）
    //    状態が偏らない固定ルール
    // ==========================================================
    for (int month = 1; month <= 12; month++)
    {
        int y = 2026;
        int m = month;

        // 会員1：偶数月は入金済み、奇数月は未入金
        {
            var inv = UpsertInvoice(member1, $"INV-{y}-{m:00}-M1-001", Utc(y, m, 5), Utc(y, m, 25),
                (1, $"月額利用料（{y}/{m:00}）", 1, 120000m));

            if (m % 2 == 0)
            {
                var p = UpsertPayment(member1, PayAt(y, m, 20, 1, 1, 1), 120000m);
                EnsureAllocation(p, inv, 120000m);
            }
        }

        // 会員2：毎月分割2回で完了
        {
            var inv = UpsertInvoice(member2, $"INV-{y}-{m:00}-M2-001", Utc(y, m, 6), Utc(y, m, 25),
                (1, $"月額利用料（{y}/{m:00}）", 1, 200000m));

            var p1 = UpsertPayment(member2, PayAt(y, m, 15, 2, 1, 1), 100000m);
            var p2 = UpsertPayment(member2, PayAt(y, m, 24, 2, 1, 2), 100000m);
            EnsureAllocation(p1, inv, 100000m);
            EnsureAllocation(p2, inv, 100000m);
        }

        // 会員3：四半期末(3,6,9,12)は未入金、他は一部入金（PARTIAL）
        {
            var inv = UpsertInvoice(member3, $"INV-{y}-{m:00}-M3-001", Utc(y, m, 7), Utc(y, m, 25),
                (1, $"月額利用料（{y}/{m:00}）", 1, 80000m));

            bool isQuarterEnd = (m % 3 == 0);
            if (!isQuarterEnd)
            {
                var p = UpsertPayment(member3, PayAt(y, m, 18, 3, 1, 1), 30000m);
                EnsureAllocation(p, inv, 30000m);
            }
        }

        // 会員4：毎月入金済み
        {
            var inv = UpsertInvoice(member4, $"INV-{y}-{m:00}-M4-001", Utc(y, m, 8), Utc(y, m, 25),
                (1, $"月額利用料（{y}/{m:00}）", 1, 50000m));

            var p = UpsertPayment(member4, PayAt(y, m, 22, 4, 1, 1), 50000m);
            EnsureAllocation(p, inv, 50000m);
        }
    }

    // ==========================================================
    // ④ Status 再計算（UNPAID / PARTIAL / PAID / OVERDUE）
    //    ★ここが一番重要：OVERDUE を“使う”
    // ==========================================================
    var targets = context.Invoices
        .Include(i => i.Status)
        .Include(i => i.PaymentAllocations)
        .Where(i =>
            i.InvoiceNumber.StartsWith("INV-2025-11-") ||
            i.InvoiceNumber.StartsWith("INV-2025-12-") ||
            i.InvoiceNumber.StartsWith("INV-2026-"))
        .ToList();

    foreach (var inv in targets)
    {
        // ★催促中は固定表示したいので再計算で上書きしない
        if (inv.Status?.Code == "DUNNING")
            continue;

        var paid = inv.PaymentAllocations.Sum(a => a.Amount);
        var total = inv.TotalAmount;

        bool isOverdue = inv.DueDate.Date < now.Date && paid < total;

        long newStatusId =
            isOverdue ? stOverdue.Id :
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

    // ==========================================================
    // ⑤ AuditLog（管理者トップ表示用：直近5件）
    //    2026-01-07〜08（UTC）
    // ==========================================================

    // すでに AuditLog が入っているなら何もしない（二重防止）
    if (!context.AuditLogs.Any())
    {
        var t1 = new DateTime(2026, 1, 7, 9, 30, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 1, 7, 11, 15, 0, DateTimeKind.Utc);
        var t3 = new DateTime(2026, 1, 8, 9, 10, 0, DateTimeKind.Utc);
        var t4 = new DateTime(2026, 1, 8, 10, 45, 0, DateTimeKind.Utc);
        var t5 = new DateTime(2026, 1, 8, 14, 20, 0, DateTimeKind.Utc);

        context.AuditLogs.AddRange(
            new AuditLog
            {
                ActorUserId = admin.Id,
                ActorRole = "Admin",
                Action = "PAYMENT_CREATED",
                Entity = "PAYMENT",
                EntityId = "2026-01-M1-001",
                Summary = "入金を登録しました（手動登録）",
                CreatedAt = t1
            },
            new AuditLog
            {
                ActorUserId = admin.Id,
                ActorRole = "Admin",
                Action = "PAYMENT_ALLOCATION_ADDED",
                Entity = "INVOICE",
                EntityId = "INV-2025-12-FIX-M1-001",
                Summary = "請求書に入金を割り当てました",
                CreatedAt = t2
            },
            new AuditLog
            {
                ActorUserId = admin.Id,
                ActorRole = "Admin",
                Action = "PAYMENT_ALLOCATIONS_REPLACED",
                Entity = "PAYMENT",
                EntityId = "2026-01-M2-001",
                Summary = "入金割当を保存しました（再割当）",
                CreatedAt = t3
            },
            new AuditLog
            {
                ActorUserId = admin.Id,
                ActorRole = "Admin",
                Action = "DUNNING_LOG_CREATED",
                Entity = "INVOICE",
                EntityId = "INV-2025-11-FIX-M4-002-DUNNING",
                Summary = "催促履歴を追加しました",
                CreatedAt = t4
            },
            new AuditLog
            {
                ActorUserId = admin.Id,
                ActorRole = "Admin",
                Action = "INVOICE_STATUS_UPDATED",
                Entity = "INVOICE",
                EntityId = "INV-2025-11-FIX-M3-001",
                Summary = "期限超過のためステータスを更新しました",
                CreatedAt = t5
            }
        );

        context.SaveChanges();
    }

}





