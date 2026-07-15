using EVSwap.Mobile.LocalDatabase;

namespace EVSwap.Mobile.Interfaces;

public interface ILocalDatabaseService
{
    Task InitializeAsync();
    Task<T?> GetItemAsync<T>(int id) where T : new();
    Task SaveItemAsync<T>(T item) where T : new();
    Task DeleteItemAsync<T>(T item) where T : new();
    Task<List<T>> GetItemsAsync<T>() where T : new();
    Task<List<PendingSyncItem>> GetPendingSyncItemsAsync();
    Task SavePendingSyncItemAsync(PendingSyncItem item);
    Task DeletePendingSyncItemAsync(PendingSyncItem item);
}
