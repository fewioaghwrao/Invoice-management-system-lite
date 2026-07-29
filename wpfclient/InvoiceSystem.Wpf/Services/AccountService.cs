using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InvoiceSystem.Wpf.Models;

namespace InvoiceSystem.Wpf.Services;

public class AccountService : IAccountService
{
    private readonly HttpClient _httpClient;

    public AccountService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AccountProfileDto> GetMyProfileAsync()
    {
        using var response = await _httpClient.GetAsync("/api/members/me");
        var responseText = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var result = System.Text.Json.JsonSerializer.Deserialize<AccountProfileDto>(
                responseText,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result == null)
            {
                throw new Exception("プロフィール応答の解析に失敗しました。 response=" + responseText);
            }

            return result;
        }

        throw new Exception(
            $"プロフィール取得失敗 Status={(int)response.StatusCode} {response.StatusCode}\n" +
            $"Response={responseText}");
    }

    public async Task UpdateMyProfileAsync(AccountProfileDto profile)
    {
        using var response = await _httpClient.PutAsJsonAsync("/api/members/me", new
        {
            name = profile.Name,
            email = profile.Email,
            postalCode = profile.PostalCode,
            address = profile.Address,
            phone = profile.Phone
        });

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var responseText = await response.Content.ReadAsStringAsync();
        var message = TryReadMessage(responseText);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new Exception(message ?? "入力内容が不正です。");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new Exception("ログイン情報の有効期限が切れています。再度ログインしてください。");
        }

        throw new Exception(message ?? "プロフィールの保存に失敗しました。");
    }

    public async Task DeleteMyAccountAsync()
    {
        using var response = await _httpClient.DeleteAsync("/api/members/me");

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

        throw new Exception(message ?? "退会処理に失敗しました。");
    }

    public static bool IsEmailLike(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Regex.IsMatch(value, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
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
        }

        return null;
    }
}