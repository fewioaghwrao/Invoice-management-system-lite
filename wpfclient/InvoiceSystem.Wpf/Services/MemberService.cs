using InvoiceSystem.Wpf.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace InvoiceSystem.Wpf.Services;

public class MemberService : IMemberService
{
    private readonly HttpClient _httpClient;

    public MemberService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<MemberListItemDto>> GetMembersAsync(
        string? keyword,
        int? role,
        bool? isActive,
        int page,
        int pageSize)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        if (!string.IsNullOrWhiteSpace(keyword))
            query["Keyword"] = keyword;

        if (role.HasValue)
            query["RoleId"] = role.Value.ToString();

        if (isActive.HasValue)
            query["IsActive"] = isActive.Value.ToString().ToLowerInvariant();

        query["Page"] = page.ToString();
        query["PageSize"] = pageSize.ToString();

        var url = "/api/members";
        var qs = query.ToString();
        if (!string.IsNullOrWhiteSpace(qs))
        {
            url += "?" + qs;
        }

        var result = await _httpClient.GetFromJsonAsync<List<MemberListItemDto>>(url);
        return result ?? new List<MemberListItemDto>();
    }

    public async Task DisableMemberAsync(int memberId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/members/{memberId}/disable");
        using var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(text) ? "退会（無効化）に失敗しました。" : text);
        }
    }

    public async Task<MemberDetailDto?> GetByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"/api/members/{id}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<MemberDetailDto>();
    }

    public async Task UpdateAsync(int id, MemberUpdateRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"/bff/members/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisableAsync(int id)
    {
        var response = await _httpClient.PutAsync($"/bff/members/{id}/disable", null);
        response.EnsureSuccessStatusCode();
    }
}