using System;
using System.Linq;
using System.Threading.Tasks;
using InvoiceSystem.Application.Commands.Members;
using InvoiceSystem.Application.Queries.Members;
using InvoiceSystem.Application.Services.Members;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InvoiceSystem.Tests.Services.Members;

public sealed class MemberServiceTests
{
    [Fact]
    public async Task RegisterAsync_NewEmail_CreatesMember()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var command = new RegisterMemberCommand
        {
            Name = "  テスト太郎  ",
            Email = "  test@example.com  ",
            Password = "Password123!",
            PostalCode = " 100-0001 ",
            Address = " 東京都千代田区 ",
            Phone = " 090-0000-0000 ",
            Role = null
        };

        // Act
        var result = await service.RegisterAsync(command);

        // Assert
        Assert.True(result.Id > 0);
        Assert.Equal("テスト太郎", result.Name);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("100-0001", result.PostalCode);
        Assert.Equal("東京都千代田区", result.Address);
        Assert.Equal("090-0000-0000", result.Phone);
        Assert.Equal(MemberRole.Customer, result.Role);
        Assert.True(result.IsActive);

        var member = await db.Members.SingleAsync();

        Assert.False(string.IsNullOrWhiteSpace(member.PasswordHash));
        Assert.NotEqual("Password123!", member.PasswordHash);

        Assert.False(member.IsEmailConfirmed);
        Assert.Null(member.EmailVerificationToken);
        Assert.Null(member.EmailVerificationTokenExpiresAt);

        var verifyResult = new PasswordHasher<Member>()
            .VerifyHashedPassword(member, member.PasswordHash, "Password123!");

        Assert.Equal(PasswordVerificationResult.Success, verifyResult);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.Add(CreateMember(
            id: 1,
            name: "既存ユーザー",
            email: "duplicate@example.com"
        ));

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var command = new RegisterMemberCommand
        {
            Name = "新規ユーザー",
            Email = "duplicate@example.com",
            Password = "Password123!",
            Role = MemberRole.Customer
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RegisterAsync(command)
        );

        Assert.Equal("このメールアドレスは既に登録されています。", ex.Message);
        Assert.Equal(1, await db.Members.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ExistingMember_ReturnsMemberDto()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.Add(CreateMember(
            id: 1,
            name: "テスト太郎",
            email: "test@example.com",
            role: MemberRole.Customer
        ));

        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetByIdAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("テスト太郎", result.Name);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal(MemberRole.Customer, result.Role);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchAsync_ByKeyword_ReturnsMatchedMembers()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.AddRange(
            CreateMember(1, "山田太郎", "yamada@example.com"),
            CreateMember(2, "佐藤花子", "sato@example.com"),
            CreateMember(3, "鈴木一郎", "suzuki@example.com")
        );

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var query = new MemberSearchQuery
        {
            Keyword = "佐藤",
            Page = 1,
            PageSize = 50
        };

        // Act
        var result = await service.SearchAsync(query);

        // Assert
        Assert.Single(result);
        Assert.Equal("佐藤花子", result.Single().Name);
    }

    [Fact]
    public async Task SearchAsync_ByRoleAndIsActive_ReturnsFilteredMembers()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.AddRange(
            CreateMember(1, "顧客A", "a@example.com", MemberRole.Customer, isActive: true),
            CreateMember(2, "管理者", "admin@example.com", MemberRole.Admin, isActive: true),
            CreateMember(3, "無効顧客", "disabled@example.com", MemberRole.Customer, isActive: false)
        );

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var query = new MemberSearchQuery
        {
            Role = MemberRole.Customer,
            IsActive = true,
            Page = 1,
            PageSize = 50
        };

        // Act
        var result = await service.SearchAsync(query);

        // Assert
        Assert.Single(result);
        Assert.Equal("顧客A", result.Single().Name);
    }

    [Fact]
    public async Task SearchAsync_DefaultPaging_ReturnsLatest50OrderedByIdDescending()
    {
        // Arrange
        await using var db = CreateDbContext();

        for (var i = 1; i <= 60; i++)
        {
            db.Members.Add(CreateMember(
                id: i,
                name: $"会員{i}",
                email: $"member{i}@example.com"
            ));
        }

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var query = new MemberSearchQuery
        {
            Page = 0,
            PageSize = 0
        };

        // Act
        var result = await service.SearchAsync(query);

        // Assert
        Assert.Equal(50, result.Count);
        Assert.Equal(60, result.First().Id);
        Assert.Equal(11, result.Last().Id);
    }

    [Fact]
    public async Task DeactivateAsync_ExistingMember_DisablesMember()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.Add(CreateMember(
            id: 1,
            name: "テスト太郎",
            email: "test@example.com",
            role: MemberRole.Customer,
            isActive: true
        ));

        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        await service.DeactivateAsync(1);

        // Assert
        var member = await db.Members.SingleAsync();

        Assert.Equal(MemberRole.Disabled, member.Role);
        Assert.False(member.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_NotFound_DoesNothing()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        // Act
        await service.DeactivateAsync(999);

        // Assert
        Assert.Empty(db.Members);
    }

    [Fact]
    public async Task UpdateAsync_ExistingMember_UpdatesMember()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.Add(CreateMember(
            id: 1,
            name: "変更前",
            email: "before@example.com",
            role: MemberRole.Customer,
            isActive: true
        ));

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var command = new UpdateMemberCommand
        {
            Name = "  変更後  ",
            Email = "  after@example.com  ",
            PostalCode = " 530-0001 ",
            Address = " 大阪府大阪市 ",
            Phone = " 080-0000-0000 ",
            Role = MemberRole.Admin,
            IsActive = true
        };

        // Act
        var result = await service.UpdateAsync(1, command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("変更後", result.Name);
        Assert.Equal("after@example.com", result.Email);
        Assert.Equal("530-0001", result.PostalCode);
        Assert.Equal("大阪府大阪市", result.Address);
        Assert.Equal("080-0000-0000", result.Phone);
        Assert.Equal(MemberRole.Admin, result.Role);
        Assert.True(result.IsActive);

        var member = await db.Members.SingleAsync();
        Assert.Equal("変更後", member.Name);
        Assert.Equal("after@example.com", member.Email);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNull()
    {
        // Arrange
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var command = new UpdateMemberCommand
        {
            Name = "存在しない",
            Email = "notfound@example.com",
            Role = MemberRole.Customer,
            IsActive = true
        };

        // Act
        var result = await service.UpdateAsync(999, command);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.AddRange(
            CreateMember(1, "会員1", "one@example.com"),
            CreateMember(2, "会員2", "two@example.com")
        );

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var command = new UpdateMemberCommand
        {
            Name = "会員1更新",
            Email = "two@example.com",
            Role = MemberRole.Customer,
            IsActive = true
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateAsync(1, command)
        );

        Assert.Equal("このメールアドレスは既に登録されています。", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_RoleDisabled_ForcesIsActiveFalse()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.Add(CreateMember(
            id: 1,
            name: "退会予定",
            email: "disabled@example.com",
            role: MemberRole.Customer,
            isActive: true
        ));

        await db.SaveChangesAsync();

        var service = CreateService(db);

        var command = new UpdateMemberCommand
        {
            Name = "退会予定",
            Email = "disabled@example.com",
            Role = MemberRole.Disabled,
            IsActive = true
        };

        // Act
        var result = await service.UpdateAsync(1, command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MemberRole.Disabled, result.Role);
        Assert.False(result.IsActive);

        var member = await db.Members.SingleAsync();
        Assert.Equal(MemberRole.Disabled, member.Role);
        Assert.False(member.IsActive);
    }

    [Fact]
    public async Task GetOptionsAsync_ReturnsOnlyActiveCustomersOrderedByName()
    {
        // Arrange
        await using var db = CreateDbContext();

        db.Members.AddRange(
            CreateMember(1, "Sato", "sato@example.com", MemberRole.Customer, isActive: true),
            CreateMember(2, "Admin", "admin@example.com", MemberRole.Admin, isActive: true),
            CreateMember(3, "Disabled", "disabled@example.com", MemberRole.Customer, isActive: false),
            CreateMember(4, "Abe", "abe@example.com", MemberRole.Customer, isActive: true)
        );

        await db.SaveChangesAsync();

        var service = CreateService(db);

        // Act
        var result = await service.GetOptionsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Abe", result[0].Name);
        Assert.Equal("Sato", result[1].Name);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static MemberService CreateService(AppDbContext db)
    {
        return new MemberService(
            db,
            new PasswordHasher<Member>()
        );
    }

    private static Member CreateMember(
        long id,
        string name,
        string email,
        MemberRole role = MemberRole.Customer,
        bool isActive = true)
    {
        var now = DateTime.UtcNow;

        return new Member
        {
            Id = id,
            Name = name,
            Email = email,
            PostalCode = null,
            Address = null,
            Phone = null,
            PasswordHash = "hash",
            Role = role,
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now,
            IsEmailConfirmed = true,
            EmailVerificationToken = null,
            EmailVerificationTokenExpiresAt = null
        };
    }
}
