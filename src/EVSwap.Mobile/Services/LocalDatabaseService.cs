using EVSwap.Mobile.Interfaces;

namespace EVSwap.Mobile.Services;

public class LocalDatabaseService : ILocalDatabaseService
{
    private readonly LocalDatabase.LocalDatabaseService _inner;

    public LocalDatabaseService()
    {
        _inner = new LocalDatabase.LocalDatabaseService();
    }

    public Task InitializeAsync() => _inner.InitializeAsync();
    public Task<T?> GetItemAsync<T>(int id) where T : new() => _inner.GetItemAsync<T>(id);
    public Task SaveItemAsync<T>(T item) where T : new() => _inner.SaveItemAsync(item);
    public Task DeleteItemAsync<T>(T item) where T : new() => _inner.DeleteItemAsync(item);
    public Task<List<T>> GetItemsAsync<T>() where T : new() => _inner.GetItemsAsync<T>();
    public Task<List<LocalDatabase.PendingSyncItem>> GetPendingSyncItemsAsync() => _inner.GetPendingSyncItemsAsync();
    public Task SavePendingSyncItemAsync(LocalDatabase.PendingSyncItem item) => _inner.SavePendingSyncItemAsync(item);
    public Task DeletePendingSyncItemAsync(LocalDatabase.PendingSyncItem item) => _inner.DeletePendingSyncItemAsync(item);
}
