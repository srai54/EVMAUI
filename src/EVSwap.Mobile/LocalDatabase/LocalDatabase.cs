using SQLite;

namespace EVSwap.Mobile.LocalDatabase;

public class LocalUser
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class CachedStation
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public double? DistanceKm { get; set; }
}

public class CachedBattery
{
    [PrimaryKey]
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double ChargeLevel { get; set; }
    public double Temperature { get; set; }
    public double Voltage { get; set; }
}

public class PendingSyncItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string JsonPayload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class LocalDatabaseService : Interfaces.ILocalDatabaseService
{
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;

    public LocalDatabaseService()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "evswap.db3");
    }

    public async Task InitializeAsync()
    {
        if (_database is not null) return;

        _database = new SQLiteAsyncConnection(_dbPath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _database.CreateTableAsync<LocalUser>();
        await _database.CreateTableAsync<CachedStation>();
        await _database.CreateTableAsync<CachedBattery>();
        await _database.CreateTableAsync<PendingSyncItem>();
    }

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database is null)
            await InitializeAsync();
        return _database!;
    }

    public async Task<T?> GetItemAsync<T>(int id) where T : new()
    {
        var db = await GetDatabaseAsync();
        return await db.FindAsync<T>(id);
    }

    public async Task SaveItemAsync<T>(T item) where T : new()
    {
        var db = await GetDatabaseAsync();
        await db.InsertOrReplaceAsync(item);
    }

    public async Task DeleteItemAsync<T>(T item) where T : new()
    {
        var db = await GetDatabaseAsync();
        await db.DeleteAsync(item);
    }

    public async Task<List<T>> GetItemsAsync<T>() where T : new()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<T>().ToListAsync();
    }

    public async Task<List<PendingSyncItem>> GetPendingSyncItemsAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<PendingSyncItem>().OrderBy(p => p.CreatedAt).ToListAsync();
    }

    public async Task SavePendingSyncItemAsync(PendingSyncItem item)
    {
        var db = await GetDatabaseAsync();
        await db.InsertAsync(item);
    }

    public async Task DeletePendingSyncItemAsync(PendingSyncItem item)
    {
        var db = await GetDatabaseAsync();
        await db.DeleteAsync(item);
    }
}
