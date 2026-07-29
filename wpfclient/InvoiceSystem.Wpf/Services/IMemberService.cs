using InvoiceSystem.Wpf.Models;

namespace InvoiceSystem.Wpf.Services;

public interface IMemberService
{
    Task<List<MemberListItemDto>> GetMembersAsync(
    string? keyword,
    int? role,
    bool? isActive,
    int page,
    int pageSize);

    Task DisableMemberAsync(int memberId);

    Task<MemberDetailDto?> GetByIdAsync(int id);
    Task UpdateAsync(int id, MemberUpdateRequest request);
    Task DisableAsync(int id);
}