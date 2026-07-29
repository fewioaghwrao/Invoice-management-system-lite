using InvoiceSystem.Wpf.Models;
using InvoiceSystem.Wpf.Services;

namespace InvoiceSystem.Wpf.Tests.Fakes;

public sealed class FakeMemberService : IMemberService
{
    public List<MemberListItemDto> MembersToReturn { get; set; } = new();
    public Exception? GetMembersException { get; set; }

    public string? LastKeyword { get; private set; }
    public int? LastRole { get; private set; }
    public bool? LastIsActive { get; private set; }
    public int LastPage { get; private set; }
    public int LastPageSize { get; private set; }

    public int? DisabledMemberId { get; private set; }

    public MemberDetailDto? MemberToReturn { get; set; }
    public Exception? GetByIdException { get; set; }

    public int? UpdatedId { get; private set; }
    public MemberUpdateRequest? UpdatedRequest { get; private set; }

    public int? DisabledId { get; private set; }

    public Task<List<MemberListItemDto>> GetMembersAsync(
        string? keyword,
        int? role,
        bool? isActive,
        int page,
        int pageSize)
    {
        if (GetMembersException is not null)
            throw GetMembersException;

        LastKeyword = keyword;
        LastRole = role;
        LastIsActive = isActive;
        LastPage = page;
        LastPageSize = pageSize;

        return Task.FromResult(MembersToReturn);
    }

    public Task DisableMemberAsync(int memberId)
    {
        DisabledMemberId = memberId;
        return Task.CompletedTask;
    }

    public Task<MemberDetailDto?> GetByIdAsync(int id)
    {
        if (GetByIdException is not null)
            throw GetByIdException;

        return Task.FromResult(MemberToReturn);
    }

    public Task UpdateAsync(int id, MemberUpdateRequest request)
    {
        UpdatedId = id;
        UpdatedRequest = request;
        return Task.CompletedTask;
    }

    public Task DisableAsync(int id)
    {
        DisabledId = id;
        return Task.CompletedTask;
    }
}