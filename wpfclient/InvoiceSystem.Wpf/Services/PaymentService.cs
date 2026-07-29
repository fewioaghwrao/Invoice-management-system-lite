using InvoiceSystem.Wpf.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvoiceSystem.Wpf.Services;

public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;

    public PaymentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentListResultDto> SearchAsync(PaymentSearchRequest request)
    {
        var year = request.Year <= 0 ? DateTime.Today.Year : request.Year;
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
        var month = string.IsNullOrWhiteSpace(request.Month) ? "all" : request.Month.Trim();
        var status = string.IsNullOrWhiteSpace(request.Status) ? "all" : request.Status.Trim();
        var q = request.Q ?? string.Empty;

        var url =
            "/api/payments" +
            $"?year={year}" +
            $"&month={Uri.EscapeDataString(month)}" +
            $"&q={Uri.EscapeDataString(q)}" +
            $"&status={Uri.EscapeDataString(status)}" +
            $"&page={page}" +
            $"&pageSize={pageSize}";

        using var response = await _httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<PaymentListResultDto>(text, options);

            if (result == null)
            {
                throw new Exception("入金一覧応答の解析に失敗しました。");
            }

            result.Rows ??= new List<PaymentListItemDto>();
            result.Summary ??= new PaymentSummaryDto();
            result.Month ??= "all";
            result.Keyword ??= string.Empty;
            result.Status ??= "all";

            return result;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new Exception("この入金情報を参照する権限がありません。");

        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new Exception(message ?? "入金一覧の取得条件が不正です。");

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new Exception("入金一覧データが見つかりませんでした。");

        throw new Exception(message ?? "入金一覧の取得に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task<PaymentDetailDto> GetByIdAsync(long paymentId)
    {
        using var response = await _httpClient.GetAsync($"/api/payments/{paymentId}");

        if (response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<PaymentDetailDto>(text, options);

            if (result == null)
            {
                throw new Exception("入金詳細応答の解析に失敗しました。");
            }

            result.Allocations ??= new List<PaymentAllocationDto>();
            return result;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new Exception("この入金詳細を参照する権限がありません。");

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new Exception("入金データが見つかりませんでした。");

        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new Exception(message ?? "入金詳細の取得条件が不正です。");

        throw new Exception(message ?? "入金詳細の取得に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task<List<MemberOptionDto>> GetMemberOptionsAsync()
    {
        using var response = await _httpClient.GetAsync("/api/members?RoleId=2&IsActive=true&Page=1&PageSize=200");

        if (response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(text);

            var result = new List<MemberOptionDto>();

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    result.Add(new MemberOptionDto
                    {
                        Id = item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                        Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty
                    });
                }
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                     doc.RootElement.TryGetProperty("rows", out var rows) &&
                     rows.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in rows.EnumerateArray())
                {
                    result.Add(new MemberOptionDto
                    {
                        Id = item.TryGetProperty("id", out var id) ? id.GetInt64() : 0,
                        Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty
                    });
                }
            }

            return result
                .Where(x => x.Id > 0 && !string.IsNullOrWhiteSpace(x.Name))
                .ToList();
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new Exception("会員一覧を参照する権限がありません。");

        throw new Exception(message ?? "会員一覧の取得に失敗しました。");
    }

    public async Task<long> CreateAsync(CreatePaymentRequestDto request)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/payments", request);

        if (response.IsSuccessStatusCode)
        {
            var text = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(text))
                return 0;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var result = JsonSerializer.Deserialize<CreatePaymentResponseEnvelope>(text, options);
            if (result != null && result.Id > 0)
            {
                return result.Id;
            }

            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.TryGetProperty("id", out var idElement) &&
                idElement.TryGetInt64(out var id))
            {
                return id;
            }

            return 0;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new Exception("この入金を登録する権限がありません。");

        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw new Exception(message ?? "入金登録の入力内容が不正です。");

        throw new Exception(message ?? "入金登録に失敗しました。時間をおいて再度お試しください。");
    }

    private static string? TryReadMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                return doc.RootElement.GetString();
            }

            if (doc.RootElement.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString();
            }
        }
        catch
        {
        }

        return null;
    }
    public async Task SaveAllocationsAsync(long paymentId, IEnumerable<PaymentAllocationLineDto> lines)
    {
    var request = new SavePaymentAllocationsRequestDto
    {
        Lines = lines?.ToList() ?? new List<PaymentAllocationLineDto>()
    };

    using var response = await _httpClient.PutAsJsonAsync($"/api/payments/{paymentId}/allocations", request);

    if (response.IsSuccessStatusCode)
    {
        return;
    }

    var responseText = await response.Content.ReadAsStringAsync();
    var message = TryReadMessage(responseText);

    if (response.StatusCode == HttpStatusCode.Unauthorized)
        throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");

    if (response.StatusCode == HttpStatusCode.Forbidden)
        throw new Exception("この入金割当を更新する権限がありません。");

    if (response.StatusCode == HttpStatusCode.BadRequest)
        throw new Exception(message ?? "割当保存の入力内容が不正です。");

    if (response.StatusCode == HttpStatusCode.NotFound)
        throw new Exception("対象の入金データが見つかりませんでした。");

    throw new Exception(message ?? "割当保存に失敗しました。時間をおいて再度お試しください。");
    }
}