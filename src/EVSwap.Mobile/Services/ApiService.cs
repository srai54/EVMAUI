using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EVSwap.Mobile.Helpers;
using EVSwap.Mobile.Interfaces;
using EVSwap.Mobile.Models;

namespace EVSwap.Mobile.Services;

public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _secureStorage;
    private readonly IServiceProvider _serviceProvider;
    private IAuthService? _authService;

    public ApiService(ISecureStorageService secureStorage, IServiceProvider serviceProvider)
    {
        _secureStorage = secureStorage;
        _serviceProvider = serviceProvider;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(Constants.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private IAuthService AuthService => _authService ??= _serviceProvider.GetRequiredService<IAuthService>();

    private async Task AttachTokenAsync()
    {
        var token = await _secureStorage.GetAsync(Constants.StorageKeys.AuthToken);
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private async Task<T?> SendAsync<T>(HttpRequestMessage request)
    {
        await AttachTokenAsync();

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshResult = await AuthService.RefreshTokenAsync();
            if (refreshResult is not null)
            {
                await AttachTokenAsync();
                var retryRequest = await CloneRequestAsync(request);
                response = await _httpClient.SendAsync(retryRequest);
            }
        }

        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = string.IsNullOrWhiteSpace(content)
                ? $"Request failed with status {(int)response.StatusCode} {response.ReasonPhrase}"
                : content;
            throw new HttpRequestException(errorMsg, null, response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(content))
            return default;

        var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (result is null && content is not null)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize response into {typeof(T).Name}. Status: {(int)response.StatusCode}, Body: {content[..Math.Min(content.Length, 500)]}");
        }

        return result;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);
            if (request.Content.Headers.ContentType is not null)
                clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
        }
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        return await SendAsync<T>(request);
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (data is not null)
            request.Content = JsonContent.Create(data);
        return await SendAsync<T>(request);
    }

    public async Task<T?> PutAsync<T>(string endpoint, object? data = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, endpoint);
        if (data is not null)
            request.Content = JsonContent.Create(data);
        return await SendAsync<T>(request);
    }

    public async Task DeleteAsync(string endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, endpoint);
        await AttachTokenAsync();
        await _httpClient.SendAsync(request);
    }

    public async Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content)
    {
        await AttachTokenAsync();
        var response = await _httpClient.PostAsync(endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
