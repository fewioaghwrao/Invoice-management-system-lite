using InvoiceSystem.Api.Endpoints;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Services;
using InvoiceSystem.Application.Services.Auth;
using InvoiceSystem.Application.Services.Members;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Infrastructure;
using InvoiceSystem.Infrastructure.Email;
using InvoiceSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using InvoiceSystem.Infrastructure.Pdf;
using InvoiceSystem.Infrastructure.Data;
using InvoiceSystem.Infrastructure.Database;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json;
using System.Text;



//PDF生成ライブラリ QuestPDF のライセンスタイプを設定
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
PdfFontRegistrar.Register();

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
builder.Services.AddHealthChecks();

// DbContext (Postgres + Heroku DATABASE_URL 対応)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        options.UseNpgsql(PostgresConnectionStringFactory.Create(databaseUrl));
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


// ===============================
// 起動時：Migrate + Seed
// ===============================
AppDbInitializer.Initialize(app.Services, app.Environment.IsDevelopment());

app.UseCors();

var enableSwagger = builder.Configuration.GetValue<bool>("ENABLE_SWAGGER");

if (app.Environment.IsDevelopment() || enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
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

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                duration = x.Value.Duration.TotalMilliseconds,
                error = x.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

app.MapGet("/", () => Results.Text(
@"Invoice System API

- Swagger UI: /swagger
- Health: /health
", "text/plain"));

app.Run();


public partial class Program { }