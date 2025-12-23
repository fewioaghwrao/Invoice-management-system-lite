using InvoiceSystem.Application.Commands.Members;
using InvoiceSystem.Application.Dtos.Members;
using InvoiceSystem.Application.Queries.Members;

namespace InvoiceSystem.Application.Services;

public interface IMemberService
{
    Task<MemberDto?> GetByIdAsync(long id);
    Task<MemberDto?> UpdateAsync(long id, UpdateMemberCommand command);
    Task<IReadOnlyList<MemberListItemDto>> SearchAsync(MemberSearchQuery query);
    Task DeactivateAsync(long id);

    Task<List<MemberOptionDto>> GetOptionsAsync();
}

