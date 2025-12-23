// InvoiceSystem.Application/Common/Interfaces/IAppDbContext.cs
using InvoiceSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Member> Members { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }  // ★ 追加

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
