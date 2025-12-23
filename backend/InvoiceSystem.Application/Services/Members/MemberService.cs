using InvoiceSystem.Application.Commands.Members;
using InvoiceSystem.Application.Common.Interfaces;
using InvoiceSystem.Application.Dtos.Members;
using InvoiceSystem.Application.Queries.Members;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Application.Services.Members;

public sealed class MemberService : IMemberService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher<Member> _passwordHasher;

    public MemberService(IAppDbContext db, IPasswordHasher<Member> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// 会員登録（※Auth側で使う想定。管理画面で使わないならEndpointから呼ばなければOK）
    /// </summary>
    public async Task<MemberDto> RegisterAsync(RegisterMemberCommand command)
    {
        var email = command.Email.Trim();

        var exists = await _db.Members.AnyAsync(m => m.Email == email);
        if (exists)
            throw new InvalidOperationException("このメールアドレスは既に登録されています。");

        var now = DateTime.UtcNow;

        var member = new Member
        {
            Name = command.Name.Trim(),
            Email = email,
            PostalCode = string.IsNullOrWhiteSpace(command.PostalCode) ? null : command.PostalCode.Trim(),
            Address = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim(),
            Phone = string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone.Trim(),
            Role = command.Role ?? MemberRole.Customer,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,

            // メール認証は別フローで扱う前提
            IsEmailConfirmed = false,
            EmailVerificationToken = null,
            EmailVerificationTokenExpiresAt = null
        };

        member.PasswordHash = _passwordHasher.HashPassword(member, command.Password);

        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        return new MemberDto
        {
            Id = member.Id,
            Name = member.Name,
            Email = member.Email,
            PostalCode = member.PostalCode,
            Address = member.Address,
            Phone = member.Phone,
            Role = member.Role,
            IsActive = member.IsActive,
            CreatedAt = member.CreatedAt
        };
    }

    /// <summary>
    /// 会員詳細取得
    /// </summary>
    public async Task<MemberDto?> GetByIdAsync(long id)
    {
        var m = await _db.Members.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (m is null) return null;

        return new MemberDto
        {
            Id = m.Id,
            Name = m.Name,
            Email = m.Email,
            PostalCode = m.PostalCode,
            Address = m.Address,
            Phone = m.Phone,
            Role = m.Role,
            IsActive = m.IsActive,
            CreatedAt = m.CreatedAt
        };
    }

    /// <summary>
    /// 会員一覧検索
    /// </summary>
    public async Task<IReadOnlyList<MemberListItemDto>> SearchAsync(MemberSearchQuery query)
    {
        var q = _db.Members.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var kw = query.Keyword.Trim();
            q = q.Where(m => m.Name.Contains(kw) || m.Email.Contains(kw));
        }

        if (query.Role.HasValue)
        {
            q = q.Where(m => m.Role == query.Role.Value);
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(m => m.IsActive == query.IsActive.Value);
        }

        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 50 : query.PageSize;
        var skip = (page - 1) * pageSize;

        var list = await q
            .OrderByDescending(m => m.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(m => new MemberListItemDto
            {
                Id = m.Id,
                Name = m.Name,
                Email = m.Email,
                Role = m.Role,
                IsActive = m.IsActive
            })
            .ToListAsync();

        return list;
    }

    /// <summary>
    /// 退会（論理削除）
    /// </summary>
    public async Task DeactivateAsync(long id)
    {
        var m = await _db.Members.FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return;

        m.Role = MemberRole.Disabled;
        m.IsActive = false;
        m.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// 会員更新（管理画面の編集専用）
    /// </summary>
    public async Task<MemberDto?> UpdateAsync(long id, UpdateMemberCommand command)
    {
        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id);
        if (member is null) return null;

        var email = command.Email.Trim();

        var emailExists = await _db.Members.AnyAsync(m => m.Id != id && m.Email == email);
        if (emailExists)
            throw new InvalidOperationException("このメールアドレスは既に登録されています。");

        member.Name = command.Name.Trim();
        member.Email = email;
        member.PostalCode = string.IsNullOrWhiteSpace(command.PostalCode) ? null : command.PostalCode.Trim();
        member.Address = string.IsNullOrWhiteSpace(command.Address) ? null : command.Address.Trim();
        member.Phone = string.IsNullOrWhiteSpace(command.Phone) ? null : command.Phone.Trim();

        member.Role = command.Role;
        member.IsActive = command.IsActive;

        // 整合性：退会ロールなら必ず無効化
        if (member.Role == MemberRole.Disabled)
        {
            member.IsActive = false;
        }

        member.UpdatedAt = DateTime.UtcNow;

        // パスワード・メール認証系は触らない
        await _db.SaveChangesAsync();

        return new MemberDto
        {
            Id = member.Id,
            Name = member.Name,
            Email = member.Email,
            PostalCode = member.PostalCode,
            Address = member.Address,
            Phone = member.Phone,
            Role = member.Role,
            IsActive = member.IsActive,
            CreatedAt = member.CreatedAt
        };
    }

    public async Task<List<MemberOptionDto>> GetOptionsAsync()
    {
        return await _db.Members
            .AsNoTracking()
            .Where(m => m.IsActive && m.Role == MemberRole.Customer)
            .OrderBy(m => m.Name)
            .Select(m => new MemberOptionDto
            {
                Id = m.Id,
                Name = m.Name
            })
            .ToListAsync();
    }

}

