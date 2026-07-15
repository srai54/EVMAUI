using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;

namespace EVSwap.Mobile.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _storage;

    public ApiService(ISecureStorageService storage)
    {
        _storage = storage;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(Constants.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private async Task AttachTokenAsync()
    {
        var token = await _storage.GetAsync(Constants.StorageKeys.AuthToken);
        if (!string.IsNullOrEmpty(token))
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        await AttachTokenAsync();
        var response = await _httpClient.GetAsync(endpoint);
        return await HandleResponse<T>(response);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        await AttachTokenAsync();
        var response = data is null
            ? await _httpClient.PostAsync(endpoint, null)
            : await _httpClient.PostAsJsonAsync(endpoint, data);
        return await HandleResponse<T>(response);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object? data = null)
    {
        await AttachTokenAsync();
        var response = data is null
            ? await _httpClient.PutAsync(endpoint, null)
            : await _httpClient.PutAsJsonAsync(endpoint, data);
        return await HandleResponse<T>(response);
    }

    public async Task DeleteAsync(string endpoint)
    {
        await AttachTokenAsync();
        await _httpClient.DeleteAsync(endpoint);
    }

    public async Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content)
    {
        await AttachTokenAsync();
        var response = await _httpClient.PostAsync(endpoint, content);
        var body = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static async Task<T?> HandleResponse<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var msg = string.IsNullOrWhiteSpace(body)
                ? $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}"
                : body;
            throw new HttpRequestException(msg, null, response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(body))
            return default;

        var result = JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result is null)
            throw new InvalidOperationException(
                $"Failed to parse response into {typeof(T).Name}. Body: {body[..Math.Min(body.Length, 300)]}");

        return result;
    }
}