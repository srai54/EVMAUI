namespace EVSwap.Mobile.Interfaces;

public interface IApiService
{
    Task<T?> GetAsync<T>(string endpoint);
    Task<T?> PostAsync<T>(string endpoint, object? data = null);
    Task<T?> PutAsync<T>(string endpoint, object? data = null);
    Task DeleteAsync(string endpoint);
    Task<T?> PostMultipartAsync<T>(string endpoint, MultipartFormDataContent content);
}
