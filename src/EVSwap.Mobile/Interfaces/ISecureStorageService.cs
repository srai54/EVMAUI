namespace EVSwap.Mobile.Interfaces;

public interface ISecureStorageService
{
    Task SaveAsync(string key, string value);
    Task<string?> GetAsync(string key);
    void Remove(string key);
    void RemoveAll();
}
