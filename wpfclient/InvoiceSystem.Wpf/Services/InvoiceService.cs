using InvoiceSystem.Wpf.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvoiceSystem.Wpf.Services;

public class InvoiceService : IInvoiceService
{
    private readonly HttpClient _httpClient;

    public InvoiceService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AccountInvoiceListDto> GetMemberInvoicesAsync(
        int year,
        string month,
        string status,
        string q,
        int page,
        int pageSize = 10)
    {
        var requestUri =
            $"/api/members/me/invoices" +
            $"?year={year}" +
            $"&month={Uri.EscapeDataString(month ?? "all")}" +
            $"&status={Uri.EscapeDataString(status ?? "all")}" +
            $"&q={Uri.EscapeDataString(q ?? string.Empty)}" +
            $"&page={page}" +
            $"&pageSize={pageSize}";

        using var response = await _httpClient.GetAsync(requestUri);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AccountInvoiceListDto>();

            if (result == null)
            {
                throw new Exception("請求書一覧応答の解析に失敗しました。");
            }

            return result;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この請求書情報を参照する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "請求書一覧の取得条件が不正です。");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("請求書一覧データが見つかりませんでした。");
        }

        throw new Exception(message ?? "請求書一覧の取得に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task<InvoiceDetailDto> GetMemberInvoiceDetailAsync(long invoiceId)
    {
        using var response = await _httpClient.GetAsync($"/api/members/me/invoices/{invoiceId}");

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<InvoiceDetailDto>();

            if (result == null)
            {
                throw new Exception("請求書詳細応答の解析に失敗しました。");
            }

            return result;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この請求書を参照する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("請求書が見つかりませんでした。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "請求書詳細の取得条件が不正です。");
        }

        throw new Exception(message ?? "請求書詳細の取得に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task<PdfDownloadResult> GetMemberInvoicePdfAsync(long invoiceId)
    {
        using var response = await _httpClient.GetAsync($"/api/members/me/invoices/{invoiceId}/pdf");

        if (response.IsSuccessStatusCode)
        {
            var contentBytes = await response.Content.ReadAsByteArrayAsync();

            if (contentBytes == null || contentBytes.Length == 0)
            {
                throw new Exception("PDFデータが空です。");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
            var fileName = GetFileNameFromResponse(response) ?? $"invoice-{invoiceId}.pdf";

            if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".pdf";
            }

            return new PdfDownloadResult
            {
                Content = contentBytes,
                FileName = fileName,
                ContentType = contentType
            };
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この請求書PDFを参照する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("請求書PDFが見つかりませんでした。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "請求書PDFの取得条件が不正です。");
        }

        throw new Exception(message ?? "請求書PDFの取得に失敗しました。時間をおいて再度お試しください。");
    }

    public static string SavePdfToTempFile(PdfDownloadResult pdf)
    {
        if (pdf.Content == null || pdf.Content.Length == 0)
        {
            throw new Exception("保存対象のPDFデータが空です。");
        }

        var fileName = string.IsNullOrWhiteSpace(pdf.FileName)
            ? $"invoice-{DateTime.Now:yyyyMMddHHmmssfff}.pdf"
            : pdf.FileName;

        foreach (var c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c, '_');
        }

        var tempDirectory = Path.Combine(Path.GetTempPath(), "InvoiceSystem.Wpf", "Pdf");
        Directory.CreateDirectory(tempDirectory);

        var fullPath = Path.Combine(
            tempDirectory,
            $"{DateTime.Now:yyyyMMddHHmmssfff}_{fileName}");

        File.WriteAllBytes(fullPath, pdf.Content);

        return fullPath;
    }

    private static string? GetFileNameFromResponse(HttpResponseMessage response)
    {
        var contentDisposition = response.Content.Headers.ContentDisposition;

        if (contentDisposition != null)
        {
            if (!string.IsNullOrWhiteSpace(contentDisposition.FileNameStar))
            {
                return contentDisposition.FileNameStar.Trim('"');
            }

            if (!string.IsNullOrWhiteSpace(contentDisposition.FileName))
            {
                return contentDisposition.FileName.Trim('"');
            }
        }

        if (response.Content.Headers.TryGetValues("Content-Disposition", out var values))
        {
            var raw = values.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(raw))
            {
                const string marker = "filename=";
                var index = raw.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

                if (index >= 0)
                {
                    var fileName = raw[(index + marker.Length)..].Trim().Trim('"');

                    if (!string.IsNullOrWhiteSpace(fileName))
                    {
                        return fileName;
                    }
                }
            }
        }

        return null;
    }

    private static string? TryReadMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("message", out var messageElement))
            {
                return messageElement.GetString();
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    public async Task<AccountInvoiceListDto> GetMemberInvoicesWithBalanceAsync(
    int year,
    string month,
    string status,
    string q,
    int page,
    int pageSize = 50)
    {
        var requestUri =
            $"/api/members/me/invoices/with-balance" +
            $"?year={year}" +
            $"&month={Uri.EscapeDataString(month ?? "all")}" +
            $"&status={Uri.EscapeDataString(status ?? "all")}" +
            $"&q={Uri.EscapeDataString(q ?? string.Empty)}" +
            $"&page={page}" +
            $"&pageSize={pageSize}";

        using var response = await _httpClient.GetAsync(requestUri);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<AccountInvoiceListDto>();

            if (result == null)
            {
                throw new Exception("入金確認応答の解析に失敗しました。");
            }

            return result;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この入金確認情報を参照する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "入金確認の取得条件が不正です。");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("入金確認データが見つかりませんでした。");
        }

        throw new Exception(message ?? "入金確認データの取得に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task<List<InvoiceListItemDto>> SearchInvoicesAsync(InvoiceSearchRequest request)
    {
        var queryParams = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                queryParams.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
            }
        }

        Add("InvoiceNumber", request.InvoiceNumber);
        Add("MemberName", request.MemberName);
        Add("StatusId", request.StatusId?.ToString());
        Add("FromInvoiceDate", request.FromInvoiceDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add("ToInvoiceDate", request.ToInvoiceDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Add("Page", request.Page.ToString());
        Add("PageSize", request.PageSize.ToString());

        var url = "/api/invoices";
        if (queryParams.Count > 0)
        {
            url += "?" + string.Join("&", queryParams);
        }

        var result = await _httpClient.GetFromJsonAsync<List<InvoiceListItemDto>>(url);
        return result ?? new List<InvoiceListItemDto>();
    }

    public async Task<InvoiceDetailDto> GetAdminInvoiceDetailAsync(long invoiceId)
    {
        using var response = await _httpClient.GetAsync($"/api/invoices/{invoiceId}");

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<InvoiceDetailDto>();

            if (result == null)
            {
                throw new Exception("請求書詳細応答の解析に失敗しました。");
            }

            return result;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この請求書を参照する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("請求書が見つかりませんでした。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "請求書詳細の取得条件が不正です。");
        }

        throw new Exception(message ?? "請求書詳細の取得に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task<PdfDownloadResult> GetAdminInvoicePdfAsync(long invoiceId)
    {
        using var response = await _httpClient.GetAsync($"/api/invoices/{invoiceId}/pdf");

        if (response.IsSuccessStatusCode)
        {
            var contentBytes = await response.Content.ReadAsByteArrayAsync();

            if (contentBytes == null || contentBytes.Length == 0)
            {
                throw new Exception("PDFデータが空です。");
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/pdf";
            var fileName = GetFileNameFromResponse(response) ?? $"invoice-{invoiceId}.pdf";

            if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".pdf";
            }

            return new PdfDownloadResult
            {
                Content = contentBytes,
                FileName = fileName,
                ContentType = contentType
            };
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この請求書PDFを参照する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("請求書PDFが見つかりませんでした。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "請求書PDFの取得条件が不正です。");
        }

        throw new Exception(message ?? "請求書PDFの取得に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task UpdateAdminInvoiceAsync(long invoiceId, InvoiceUpsertRequestDto request)
    {
        using var response = await _httpClient.PutAsJsonAsync($"/api/invoices/{invoiceId}", request);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この請求書を更新する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("更新対象の請求書が見つかりませんでした。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "請求書更新の入力内容が不正です。");
        }

        throw new Exception(message ?? "請求書更新に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task<long> CreateAdminInvoiceAsync(InvoiceUpsertRequestDto request)
    {
        using var response = await _httpClient.PostAsJsonAsync("/api/invoices", request);

        if (response.IsSuccessStatusCode)
        {
            // Created: 本文に DTO が返る想定
            var created = await response.Content.ReadFromJsonAsync<InvoiceDetailDto>();
            if (created != null)
            {
                return created.Id;
            }

            // DTOが読めなくても Location ヘッダから拾えれば拾う
            var location = response.Headers.Location?.ToString();
            if (!string.IsNullOrWhiteSpace(location))
            {
                var last = location.Split('/').LastOrDefault();
                if (long.TryParse(last, out var id))
                {
                    return id;
                }
            }

            return 0;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この請求書を作成する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "請求書作成の入力内容が不正です。");
        }

        throw new Exception(message ?? "請求書作成に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task<List<MemberOptionDto>> GetMemberOptionsAsync()
    {
        using var response = await _httpClient.GetAsync("/api/members/options");

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<List<MemberOptionDto>>();
            return result ?? new List<MemberOptionDto>();
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("会員選択肢を参照する権限がありません。");
        }

        throw new Exception(message ?? "会員一覧の取得に失敗しました。");
    }

    public async Task<CollectionSnapshotDto> GetCollectionSnapshotAsync(long invoiceId)
    {
        using var response = await _httpClient.GetAsync($"/api/collections/{invoiceId}/snapshot");

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<CollectionSnapshotDto>();
            if (result == null)
            {
                throw new Exception("督促対象情報の解析に失敗しました。");
            }

            return result;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この督促情報を参照する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("対象の請求情報が見つかりませんでした。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "督促対象情報の取得条件が不正です。");
        }

        throw new Exception(message ?? "督促対象情報の取得に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task<List<DunningLogDto>> GetCollectionLogsAsync(long invoiceId)
    {
        using var response = await _httpClient.GetAsync($"/api/collections/{invoiceId}/logs");

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<List<DunningLogDto>>();
            return result ?? new List<DunningLogDto>();
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この督促履歴を参照する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("督促履歴が見つかりませんでした。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "督促履歴の取得条件が不正です。");
        }

        throw new Exception(message ?? "督促履歴の取得に失敗しました。時間をおいて再度お試しください。");
    }

    public async Task<long> CreateCollectionLogAsync(long invoiceId, CreateDunningLogRequestDto request)
    {
        using var response = await _httpClient.PostAsJsonAsync($"/api/collections/{invoiceId}/logs", request);

        if (response.IsSuccessStatusCode)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("id", out var idElement)
                && idElement.TryGetInt64(out var id))
            {
                return id;
            }

            return 0;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new Exception("この督促履歴を登録する権限がありません。");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("対象の請求情報が見つかりませんでした。");
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "督促登録の入力内容が不正です。");
        }

        throw new Exception(message ?? "督促登録に失敗しました。時間をおいて再度お試しください。");
    }
}