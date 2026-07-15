using EVSwap.Mobile.Interfaces;

namespace EVSwap.Mobile.Services;

public class SecureStorageService : ISecureStorageService
{
    public async Task SaveAsync(string key, string value)
    {
        await SecureStorage.Default.SetAsync(key, value);
    }

    public async Task<string?> GetAsync(string key)
    {
        return await SecureStorage.Default.GetAsync(key);
    }

    public void Remove(string key)
    {
        SecureStorage.Default.Remove(key);
    }

    public void RemoveAll()
    {
        SecureStorage.Default.RemoveAll();
    }
}
