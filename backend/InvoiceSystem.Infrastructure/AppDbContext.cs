using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;
using System.Security.AccessControl;

namespace InvoiceSystem.Infrastructure;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceStatus> InvoiceStatuses => Set<InvoiceStatus>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<PaymentImportBatch> PaymentImportBatches => Set<PaymentImportBatch>();
    public DbSet<ReminderHistory> ReminderHistories => Set<ReminderHistory>();

    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimesToUtc();
        return base.SaveChangesAsync(cancellationToken);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // リレーション・精度設定（ここは今までどおり）
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Status)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.StatusId);

        modelBuilder.Entity<Member>()
            .HasMany(m => m.Invoices)
            .WithOne(i => i.Member)
            .HasForeignKey(i => i.MemberId);

        modelBuilder.Entity<Member>()
            .HasMany(m => m.Payments)
            .WithOne(p => p.Member)
            .HasForeignKey(p => p.MemberId);

        modelBuilder.Entity<PaymentAllocation>()
            .HasOne(pa => pa.Payment)
            .WithMany(p => p.PaymentAllocations)
            .HasForeignKey(pa => pa.PaymentId);

        modelBuilder.Entity<PaymentAllocation>()
            .HasOne(pa => pa.Invoice)
            .WithMany(i => i.PaymentAllocations)
            .HasForeignKey(pa => pa.InvoiceId);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<PaymentAllocation>(e =>
        {
            // 金額の精度
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");

            // Index
            e.HasIndex(x => x.InvoiceId);
            e.HasIndex(x => x.PaymentId);

            // 1入金×1請求を1行に制限するなら（運用次第）
            e.HasIndex(x => new { x.PaymentId, x.InvoiceId }).IsUnique();

            // FK
            e.HasOne(x => x.Payment)
             .WithMany(p => p.PaymentAllocations)
             .HasForeignKey(x => x.PaymentId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Invoice)
             .WithMany(i => i.PaymentAllocations)
             .HasForeignKey(x => x.InvoiceId)
             .OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<ReminderHistory>()
    .HasOne(r => r.Invoice)
    .WithMany(i => i.ReminderHistories)
    .HasForeignKey(r => r.InvoiceId)
    .OnDelete(DeleteBehavior.Cascade);

        // ★ InvoiceStatus だけ Seed（ここは今のままでOK）
        modelBuilder.Entity<InvoiceStatus>().HasData(
            new InvoiceStatus { Id = 1, Code = "UNPAID", Name = "未入金", IsOverdue = false, IsClosed = false, SortOrder = 10 },
            new InvoiceStatus { Id = 2, Code = "PARTIAL", Name = "一部入金", IsOverdue = false, IsClosed = false, SortOrder = 20 },
            new InvoiceStatus { Id = 3, Code = "PAID", Name = "入金済み", IsOverdue = false, IsClosed = true, SortOrder = 30 },
            new InvoiceStatus { Id = 4, Code = "OVERDUE", Name = "期限超過", IsOverdue = true, IsClosed = false, SortOrder = 40 },
            new InvoiceStatus { Id = 5, Code = "CANCELLED", Name = "キャンセル", IsOverdue = false, IsClosed = true, SortOrder = 50 }
        );

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.HasIndex(x => x.Token)
                  .IsUnique();

            entity.HasOne(x => x.Member)
                  .WithMany()               // Member 側にナビゲーションを持たない場合
                  .HasForeignKey(x => x.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- 追加推奨: 制約・インデックス ---
        modelBuilder.Entity<InvoiceStatus>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<Payment>()
            .HasIndex(x => new { x.MemberId, x.PaymentDate });

        modelBuilder.Entity<PaymentAllocation>()
            .HasIndex(x => new { x.PaymentId, x.InvoiceId })
            .IsUnique();

        modelBuilder.Entity<PaymentAllocation>()
            .HasIndex(x => x.InvoiceId);

        modelBuilder.Entity<InvoiceLine>(e =>
        {
            e.ToTable("InvoiceLines");
            e.HasKey(x => x.Id);

            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Qty).IsRequired();
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");

            e.HasOne(x => x.Invoice)
              .WithMany(i => i.Lines)
              .HasForeignKey(x => x.InvoiceId)
              .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.InvoiceId, x.LineNo }).IsUnique();
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("AuditLogs");
            e.HasKey(x => x.Id);

            e.Property(x => x.ActorUserId).IsRequired();
            e.Property(x => x.ActorRole).HasMaxLength(50);

            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.Entity).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(50);

            e.Property(x => x.Summary).HasMaxLength(400);
            e.Property(x => x.DataJson).HasColumnType("text");

            e.Property(x => x.CorrelationId).HasMaxLength(100);
            e.Property(x => x.IpAddress).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(300);

            e.Property(x => x.CreatedAt).IsRequired();

            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => new { x.Entity, x.EntityId });
            e.HasIndex(x => x.Action);
            e.HasIndex(x => x.ActorUserId);
        });

    }

public override int SaveChanges()
{
    NormalizeDateTimesToUtc();
    return base.SaveChanges();
}



private void NormalizeDateTimesToUtc()
{
    foreach (var entry in ChangeTracker.Entries())
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;

        foreach (var prop in entry.Properties)
        {
            // DateTime
            if (prop.Metadata.ClrType == typeof(DateTime))
            {
                var dt = (DateTime)prop.CurrentValue!;
                prop.CurrentValue = EnsureUtc(dt);
            }

            // DateTime?
            if (prop.Metadata.ClrType == typeof(DateTime?))
            {
                var dt = (DateTime?)prop.CurrentValue;
                if (dt.HasValue)
                    prop.CurrentValue = EnsureUtc(dt.Value);
            }
        }
    }

    static DateTime EnsureUtc(DateTime dt)
    {
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
        };
    }
}


}



