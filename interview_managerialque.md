# Managerial, Scenario, Optimization & Domain-Specific Interview Questions

> **Target Company:** Datakrew — EV Fleet Intelligence with IoT/AI solutions (OXRED Platform Suite)
> **Role:** .NET MAUI Freelancer / Cross-Platform Developer
> **Goal:** 1 million EVs in 5 years, touch a billion lives

---

## Table of Contents

1. [Datakrew Company & EV Domain](#1-datakrew-company--ev-domain)
2. [Scenario-Based Problem Solving](#2-scenario-based-problem-solving)
3. [Performance Optimization & Scaling](#3-performance-optimization--scaling)
4. [Managerial & Leadership](#4-managerial--leadership)
5. [CI/CD, Cloud & DevOps](#5-cicd-cloud--devops)
6. [IoT & Enterprise MAUI](#6-iot--enterprise-maui)
7. [Architecture & System Design](#7-architecture--system-design)
8. [Behavioral & Cultural Fit](#8-behavioral--cultural-fit)

---

## 1. Datakrew Company & EV Domain

**Q1: What do you understand about Datakrew's OXRED Platform Suite?**

**Answer:** OXRED is Datakrew's flagship EV fleet intelligence platform that provides deep insights into vehicle fleet performance and diagnostics using IoT sensors and AI analytics. It collects real-time telemetry from EVs — battery health, motor temperature, tire pressure, GPS location, energy consumption — and surfaces actionable insights through dashboards, alerts, and predictive maintenance reports. The MAUI app likely serves as the mobile interface for fleet managers and drivers to monitor this data on the go.

---

**Q2: How would you architect a MAUI app to display real-time EV telemetry data?**

**Answer:**

```csharp
public class TelemetryService
{
    // Option 1: SignalR for real-time push
    private HubConnection _hubConnection;

    public async Task ConnectAsync(string vehicleId)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{Constants.ApiBaseUrl}/hubs/telemetry?vehicleId={vehicleId}")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<TelemetryData>("TelemetryUpdate", data =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
                TelemetryReceived?.Invoke(this, data));
        });

        await _hubConnection.StartAsync();
    }

    // Option 2: Polling fallback when WebSocket isn't available
    private Timer _pollTimer;
    public void StartPolling(string vehicleId)
    {
        _pollTimer = new Timer(async _ =>
        {
            var data = await _api.GetAsync<TelemetryData>(
                $"/api/telemetry/{vehicleId}/latest");
            if (data is not null)
                TelemetryReceived?.Invoke(this, data);
        }, null, 0, 5000); // every 5 seconds
    }

    public event EventHandler<TelemetryData>? TelemetryReceived;
}
```

---

**Q3: You need to display a live map of 10,000+ EVs for a fleet manager. How do you avoid freezing the UI?**

**Answer:**

1. **Clustering** — Group nearby vehicles into clusters at lower zoom levels. Use `Map` control with clustering library.
2. **Virtualization** — Only render pins for vehicles in the current viewport. Subscribe to `MapSpanChanged` and update pins.
3. **Batching** — Receive telemetry in batches (every 5-10s) rather than per-vehicle updates.
4. **Lazy loading** — Load vehicle details only when a pin is tapped.
5. **Background processing** — Parse and filter telemetry data on a background thread, dispatch only the final pin collection to the UI thread.

```csharp
public partial class FleetMapViewModel : BaseViewModel
{
    [ObservableProperty] private ObservableCollection<VehiclePin> _visiblePins = new();
    private List<VehicleData> _allVehicles = new();
    private CancellationTokenSource? _updateCts;

    public void OnMapRegionChanged(GeoLocation center, double radiusKm)
    {
        _updateCts?.Cancel();
        _updateCts = new CancellationTokenSource();
        var token = _updateCts.Token;

        Task.Run(() =>
        {
            var visible = _allVehicles
                .Where(v => GeoUtils.Distance(center, v.Location) <= radiusKm)
                .Select(v => new VehiclePin(v))
                .ToList();

            if (token.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                VisiblePins = new ObservableCollection<VehiclePin>(visible);
            });
        }, token);
    }
}
```

---

**Q4: How would you handle offline mode for an EV fleet app where drivers may be in areas with poor connectivity?**

**Answer:**

```csharp
public class OfflineSyncService
{
    private readonly SQLiteAsyncConnection _localDb;
    private readonly Queue<SyncItem> _syncQueue = new();

    public OfflineSyncService()
    {
        _localDb = new SQLiteAsyncConnection("evswap_offline.db");
        _localDb.CreateTableAsync<SyncItem>().FireAndForget();
    }

    // 1. Always read from local cache first
    public async Task<T?> GetCachedOrFetchAsync<T>(string cacheKey, Func<Task<T?>> fetchFunc)
    {
        var cached = await _localDb.FindAsync<CacheEntry>(c => c.Key == cacheKey);
        if (cached is not null && cached.ExpiresAt > DateTime.UtcNow)
            return JsonSerializer.Deserialize<T>(cached.JsonData);

        // Try API
        try
        {
            var fresh = await fetchFunc();
            if (fresh is not null)
            {
                await _localDb.InsertOrReplaceAsync(new CacheEntry
                {
                    Key = cacheKey,
                    JsonData = JsonSerializer.Serialize(fresh),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5)
                });
            }
            return fresh;
        }
        catch (HttpRequestException)
        {
            return cached is not null
                ? JsonSerializer.Deserialize<T>(cached.JsonData)
                : default;
        }
    }

    // 2. Queue writes when offline
    public async Task QueueWriteAsync(string endpoint, object payload)
    {
        try
        {
            await _api.PostAsync(endpoint, payload);
        }
        catch (HttpRequestException)
        {
            await _localDb.InsertAsync(new SyncItem
            {
                Endpoint = endpoint,
                Payload = JsonSerializer.Serialize(payload),
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    // 3. Sync when connectivity returns
    public async Task SyncPendingAsync()
    {
        var pending = await _localDb.Table<SyncItem>().ToListAsync();
        foreach (var item in pending)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<object>(item.Payload);
                await _api.PostAsync(item.Endpoint, payload);
                await _localDb.DeleteAsync(item);
            }
            catch { /* will retry next sync cycle */ }
        }
    }
}
```

---

**Q5: Explain how you'd integrate IoT sensor data (battery voltage, motor temperature, GPS) into the MAUI app.**

**Answer:**

1. **Backend** — IoT devices send telemetry via MQTT/HTTP to Azure IoT Hub or similar.
2. **API Layer** — ASP.NET Core processes and stores telemetry, exposes REST endpoints.
3. **Real-time push** — SignalR hub pushes updates to connected MAUI clients.
4. **MAUI client** — `TelemetryService` subscribes to SignalR hub, deserializes data into `TelemetryData` model, raises events consumed by ViewModels.
5. **UI rendering** — Bind to `ObservableCollection<TelemetryData>`, use `DataTemplateSelector` for different sensor types (gauge for battery, chart for temperature over time).

```xml
<CollectionView ItemsSource="{Binding SensorReadings}">
    <CollectionView.ItemTemplate>
        <DataTemplate x:DataType="models:TelemetryData">
            <Grid ColumnDefinitions="*,Auto">
                <Label Text="{Binding SensorName}" />
                <Label Grid.Column="1" Text="{Binding Value, StringFormat='{0:F2}'}" />
            </Grid>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

---

**Q6: What security considerations are unique to an EV fleet management mobile app?**

**Answer:**

| Concern | Mitigation |
|---------|-----------|
| Vehicle control commands via app | Require secondary authentication (biometric + PIN) for critical actions |
| Telemetry data exposure | End-to-end encryption, token-scoped access (drivers see only their assigned vehicles) |
| GPS tracking privacy | Clear privacy policy, granular location permissions, data anonymization |
| Firmware OTA updates | Signed firmware packages, verify checksum before applying |
| Device theft | Remote wipe capability, PIN lock on app startup |
| API abuse | Rate limiting per API key/token, IP whitelisting for fleet APIs |

```csharp
// Critical action requires re-authentication
[RelayCommand]
async Task RemoteDisableVehicleAsync(string vehicleId)
{
    var authed = await Biometrics.Default.AuthenticateAsync(
        new AuthenticationRequest { Title = "Confirm Action" });
    if (authed.Status != BiometricAuthenticationStatus.Success) return;

    var pin = await Shell.Current.CurrentPage.DisplayPromptAsync(
        "Verify PIN", "Enter your security PIN", "Confirm", "Cancel", "*****");
    if (pin != _cachedPin) { await ShowAlertAsync("Invalid PIN"); return; }

    await _api.PostAsync($"/api/fleet/{vehicleId}/disable", new { });
}
```

---

**Q7: Datakrew aims to serve 1 million EVs in 5 years. How does your MAUI architecture scale?**

**Answer:**

1. **API Gateway** — Single entry point with load balancing, caching, rate limiting.
2. **Microservices** — Separate services for telemetry, fleet management, user management, reporting. Each scales independently.
3. **Database sharding** — Shard vehicle data by region or fleet ID.
4. **CDN for static assets** — App updates, images, firmware blobs served via CDN.
5. **Client-side caching** — `MemoryCache` + SQLite local DB to reduce API calls.
6. **Lazy loading & pagination** — Never load all vehicles at once. Paginate everything.
7. **Compiled bindings** — Use `x:DataType` for faster binding performance with 1000s of list items.
8. **Dependency injection** — All services registered as singletons (reused, not recreated).

```csharp
// API response pagination
public class PagedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNext => Page * PageSize < TotalCount;
}
```

---

**Q8: What metrics would you track in the MAUI app to monitor fleet health?**

**Answer:**

```csharp
public class FleetHealthDashboard
{
    // Battery metrics
    public double AvgBatteryHealth { get; set; }  // 0-100%
    public int CriticalBatteryCount { get; set; } // <20% health
    public int ChargingNow { get; set; }

    // Motor metrics
    public double AvgMotorTemp { get; set; }
    public int OverheatingCount { get; set; }     // >95°C

    // Fleet utilization
    public int ActiveVehicles { get; set; }
    public int IdleVehicles { get; set; }
    public double UtilizationRate { get; set; }

    // Maintenance
    public int DueForService { get; set; }
    public int ActiveAlerts { get; set; }

    // Efficiency
    public double AvgKmPerKwh { get; set; }
    public double TotalEnergyConsumed { get; set; } // MWh
}
```

---

**Q9: How would you implement push notifications for critical vehicle alerts (overheating, battery failure, unauthorized movement)?**

**Answer:**

```csharp
public class AlertNotificationService
{
    // 1. Register device for push
    public async Task RegisterForPushAsync()
    {
#if ANDROID
        // Firebase Cloud Messaging
        var fcmToken = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
        await _api.PostAsync("/api/notifications/register", new { Token = fcmToken, Platform = "android" });
#elif IOS
        // Apple Push Notification Service
        var apnsToken = await CrossPushNotification.Current.GetTokenAsync();
        await _api.PostAsync("/api/notifications/register", new { Token = apnsToken, Platform = "ios" });
#endif
    }

    // 2. Handle incoming notification
    public void OnNotificationReceived(NotificationData notification)
    {
        // Critical alerts override silent mode
        if (notification.Priority == AlertPriority.Critical)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("//alerts",
                    new Dictionary<string, object>
                    {
                        { "AlertId", notification.AlertId }
                    });
            });
        }
    }

    // 3. Local notification fallback (when push fails)
    public async Task ShowLocalAlertAsync(string title, string body)
    {
        await LocalNotificationCenter.Current.Show(new NotificationRequest
        {
            Title = title,
            Description = body,
            ScheduleType = NotificationScheduleType.Time,
            DeliveryTime = DateTime.Now.AddSeconds(1),
            CategoryType = NotificationCategoryType.Alarm
        });
    }
}
```

---

**Q10: How would you handle firmware version compatibility between the MAUI app and EV hardware?**

**Answer:**

```csharp
public class FirmwareCompatibilityService
{
    public async Task CheckCompatibilityAsync()
    {
        var appVersion = Version.Parse(AppInfo.Current.VersionString);
        var firmwareInfo = await _api.GetAsync<FirmwareRequirement>("/api/firmware/requirements");

        if (firmwareInfo is null) return;

        if (appVersion < firmwareInfo.MinAppVersion)
        {
            // Block access — force app update
            await ShowAlertAsync("Update Required",
                $"Firmware v{firmwareInfo.FirmwareVersion} requires app v{firmwareInfo.MinAppVersion}. " +
                $"You have v{appVersion}. Please update.");
            Application.Current?.Quit();
        }

        if (firmwareInfo.FirmwareVersion > firmwareInfo.CurrentFirmware)
        {
            // Suggest firmware update
            await ShowAlertAsync("Firmware Update Available",
                $"Vehicle firmware v{firmwareInfo.FirmwareVersion} is available.");
        }
    }
}
```

---

## 2. Scenario-Based Problem Solving

**Q11: The app crashes on Android but works fine on Windows. How do you debug this?**

**Answer:**

1. Check `Platforms/Android/AndroidManifest.xml` for missing permissions.
2. Connect Android device, enable USB debugging, run `adb logcat | FindStr "EVSwap"`.
3. Look for `Java.Lang.Exception`, `NullReferenceException`, or `MissingMethodException`.
4. Test with Android emulator using different API levels (28, 30, 33, 34).
5. Common Android-specific issues:
   - **Missing permissions:** `ACCESS_FINE_LOCATION`, `INTERNET`, `READ_EXTERNAL_STORAGE`
   - **Lifecycle:** Backgrounded app gets killed, ViewModel state lost
   - **UI thread:** Background thread updating UI without `MainThread.BeginInvokeOnMainThread`
   - **Fragment conflicts:** Shell navigation conflicting with Android back stack

```csharp
// Add logging specific to Android
#if ANDROID
Android.Util.Log.Debug("EVSwap", $"Crash at {DateTime.UtcNow}: {ex}");
#endif
```

---

**Q12: A user reports that battery data on the dashboard is 30 minutes stale. How do you investigate?**

**Answer:**

1. Check `_httpClient.Timeout` — default is 100s, but server-side caching might return stale data.
2. Check API response headers: `Cache-Control: max-age=1800` means 30-min cache.
3. Check if `TelemetryService` uses polling vs real-time push. Polling at 30-min intervals explains it.
4. Check if `MemoryCache` on the client has a 30-min expiration.
5. Fix:

```csharp
// Server-side: reduce cache duration
[HttpGet("latest")]
[ResponseCache(Duration = 10)]  // 10 seconds instead of 1800
public async Task<IActionResult> GetLatestTelemetry(string vehicleId) { ... }

// Client-side: reduce cache or use SignalR
services.AddMemoryCache(options =>
    options.ExpirationScanFrequency = TimeSpan.FromSeconds(10));
```

---

**Q13: The login page takes 8 seconds to appear on a low-end Android device. How do you optimize?**

**Answer:**

1. **Profile startup** — Use `dotnet trace` or Android Studio Profiler to identify bottlenecks.
2. **XAML parsing** — Enable compiled bindings (`x:DataType`) to reduce runtime reflection.
3. **Lazy loading** — Use `x:Load="False"` on non-critical UI elements, load them after the page appears.
4. **Reduce assembly size** — Remove unused NuGet packages, use LLVM optimization.
5. **Splash screen** — Show a branded splash screen while the app initializes.
6. **AOT compilation** — Enable `<PublishAot>true</PublishAot>` in .csproj for Android.

```xml
<!-- Lazy load heavy UI -->
<Frame x:Load="{Binding IsAdvancedMode}" IsVisible="False">
    <!-- complex dashboard charts -->
</Frame>
```

---

**Q14: The API returns 500 errors intermittently. Users see "Something went wrong" without details. How do you improve this?**

**Answer:**

```csharp
// 1. Categorize errors for better messaging
public static class ErrorMapper
{
    private static readonly Dictionary<HttpStatusCode, string> Messages = new()
    {
        [HttpStatusCode.Unauthorized] = "Session expired. Please login again.",
        [HttpStatusCode.Forbidden] = "You don't have permission for this action.",
        [HttpStatusCode.NotFound] = "The requested resource was not found.",
        [HttpStatusCode.InternalServerError] = "Server error. Our team has been notified.",
        [HttpStatusCode.ServiceUnavailable] = "Service is temporarily down. Retrying..."
    };

    public static string GetUserMessage(HttpStatusCode statusCode) =>
        Messages.TryGetValue(statusCode, out var msg) ? msg : "An unexpected error occurred.";
}

// 2. Auto-retry with exponential backoff
public async Task<T?> CallWithRetryAsync<T>(Func<Task<T?>> call, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try { return await call(); }
        catch (HttpRequestException ex) when (i < maxRetries - 1)
        {
            if (ex.StatusCode == HttpStatusCode.InternalServerError)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
                continue;
            }
            throw; // non-retryable
        }
    }
    return default;
}

// 3. Log the actual error server-side
// API: Add middleware to log all 500s with correlation ID
app.UseMiddleware<ErrorLoggingMiddleware>();
```

---

**Q15: You need to add a new screen to the app without breaking existing navigation. What's your approach?**

**Answer:**

1. Create the new page and ViewModel in appropriate folders.
2. Register in DI: `builder.Services.AddTransient<NewPage>(); builder.Services.AddTransient<NewViewModel>();`
3. Add route in `AppShell.xaml`:

```xml
<ShellContent Route="newfeature" ContentTemplate="{DataTemplate views:NewPage}" />
```

4. Option A: Add a new tab for the feature.
5. Option B: Add a button on an existing page that navigates to the new route.
6. Test all existing navigation flows — verify that back button, tab switching, and deep links still work.
7. Update any tests that assert on navigation calls.

---

**Q16: The CollectionView lags when scrolling through 500+ station items. How do you fix it?**

**Answer:**

1. **Enable virtualization** — Ensure `CollectionView` (not `ListView`) is used.
2. **Compiled bindings** — Add `x:DataType` to both page and `DataTemplate`.
3. **Reduce visual tree** — Minimize nested layouts in `ItemTemplate`.
4. **Async loading** — Load items in batches of 20 using `RemainingItemsThreshold`.
5. **Image optimization** — If items have images, ensure they're cached and resized.
6. **Use `SimpleStackLayout`** — Replace `Grid` with `VerticalStackLayout` in templates where possible.

```xml
<CollectionView ItemsSource="{Binding Stations}"
                RemainingItemsThreshold="5"
                RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}"
                x:DataType="vm:StationsViewModel">
    <CollectionView.ItemTemplate x:DataType="models:StationModel">
        <DataTemplate>
            <VerticalStackLayout Padding="10">
                <Label Text="{Binding Name}" />
                <Label Text="{Binding Distance, StringFormat='{0:F1} km'}" />
            </VerticalStackLayout>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

---

**Q17: The app needs to work in both online and offline mode. How do you design the data layer?**

**Answer:**

```csharp
public interface IDataService<T>
{
    Task<T?> GetAsync(string id);
    Task<List<T>> GetAllAsync();
    Task SaveAsync(T item);
    Task DeleteAsync(string id);
}

// Online implementation
public class ApiDataService<T> : IDataService<T>
{
    private readonly IApiService _api;
    private readonly string _endpoint;

    public async Task<T?> GetAsync(string id) =>
        await _api.GetAsync<T>($"{_endpoint}/{id}");

    public async Task SaveAsync(T item) =>
        await _api.PostAsync(_endpoint, item);
}

// Offline-capable implementation
public class CachedDataService<T> : IDataService<T>
{
    private readonly ApiDataService<T> _remote;
    private readonly SQLiteAsyncConnection _local;
    private readonly IConnectivityService _connectivity;

    public async Task<T?> GetAsync(string id)
    {
        if (_connectivity.IsConnected)
        {
            try
            {
                var remote = await _remote.GetAsync(id);
                if (remote is not null)
                    await _local.InsertOrReplaceAsync(remote);
                return remote;
            }
            catch { /* fall through to cache */ }
        }
        return await _local.FindAsync<T>(id);
    }

    public async Task SaveAsync(T item)
    {
        await _local.InsertOrReplaceAsync(item);
        if (_connectivity.IsConnected)
        {
            try { await _remote.SaveAsync(item); }
            catch { /* queued for sync */ }
        }
    }
}
```

---

**Q18: A customer reports that the app consumes 500MB of RAM after 30 minutes of use. How do you identify the leak?**

**Answer:**

1. **Reproduce** — Use the app normally for 30 min, monitor with Visual Studio Diagnostic Tools (Memory Usage).
2. **Snapshot comparison** — Take snapshots at t=0, t=15min, t=30min. Identify growing objects.
3. **Common MAUI memory leaks:**
   - **Event handlers** — ViewModel subscribes to `PropertyChanged` on a singleton, never unsubscribes
   - **Static collections** — `List<WeakReference>` or `ConcurrentDictionary` holding ViewModels
   - **Image cache** — Loading full-resolution photos without resizing
   - **Timer not disposed** — `DispatcherTimer` left running after page disappears

```csharp
// Fix: unsubscribe in OnDisappearing
protected override void OnDisappearing()
{
    base.OnDisappearing();
    _telemetryService.TelemetryReceived -= OnTelemetryUpdate;
    _timer?.Dispose();
}
```

4. **Use `WeakReferenceMessenger`** instead of strong-referenced event aggregators.
5. **Set large image `CacheValve`** — `Image.CacheValve = TimeSpan.FromMinutes(10)`.

---

**Q19: How do you handle a situation where the API contract changes (e.g., a field is renamed) and the mobile app isn't updated yet?**

**Answer:**

1. **Backward-compatible API changes** — Always add new fields, never remove or rename existing ones. Deprecate old fields with `[Obsolete]`.
2. **Versioned endpoints** — `api/v1/stations`, `api/v2/stations`. Old app uses v1, new app uses v2.
3. **API version header** — `Accept: application/vnd.evswap.v2+json`.
4. **Flexible deserialization** — Use `JsonSerializerOptions.UnmappedMemberHandling = Skip` and `[JsonExtensionData]` for extra fields.

```csharp
public class StationModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    [JsonPropertyName("location")]  // new name
    public string Address { get; set; } = "";  // old name

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraFields { get; set; }
}
```

5. **Feature flags** — Server-side toggle for new features so you can roll out gradually.

---

**Q20: The app needs to support both tablet and phone layouts. How do you approach responsive design?**

**Answer:**

```xml
<!-- Adaptive layout using OnIdiom and VisualStateManager -->
<ContentPage.Resources>
    <Style TargetType="Grid" x:Key="DashboardGrid">
        <Setter Property="ColumnDefinitions" Value="*" />
        <Setter Property="RowDefinitions" Value="Auto" />
        <Setter Property="VisualStateManager.VisualStateGroups">
            <VisualStateGroupList>
                <VisualState x:Name="Phone">
                    <VisualState.Setters>
                        <Setter Property="ColumnDefinitions" Value="*" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="Tablet">
                    <VisualState.Setters>
                        <Setter Property="ColumnDefinitions" Value="*,*" />
                    </VisualState.Setters>
                </VisualState>
            </VisualStateGroupList>
        </Setter>
    </Style>
</ContentPage.Resources>

<!-- Programmatic adaption -->
public partial class DashboardPage : ContentPage
{
    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (DeviceInfo.Idiom == DeviceIdiom.Tablet)
            VisualStateManager.GoToState(DashboardGrid, "Tablet");
        else
            VisualStateManager.GoToState(DashboardGrid, "Phone");
    }
}
```

---

## 3. Performance Optimization & Scaling

**Q21: The API response for fleet dashboard takes 15 seconds. How do you reduce it to under 2 seconds?**

**Answer:**

1. **Database indexing** — Ensure indexes on `VehicleId`, `CreatedAt`, `FleetId`, `Status`.
2. **Caching** — Implement Redis cache for dashboard aggregates. Cache for 30 seconds.
3. **Read replicas** — Move reporting queries to a read-only database replica.
4. **Asynchronous aggregation** — Pre-compute dashboard aggregates in a background job every 30 seconds. API just reads pre-computed results.
5. **Pagination** — Don't return all data. Return summary + paginated details.
6. **Projection** — Only select needed columns, not `SELECT *`.

```csharp
// Background job pre-computes dashboard data
public class DashboardAggregator : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDistributedCache _cache;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var dashboard = new FleetDashboardData
            {
                TotalVehicles = await db.Vehicles.CountAsync(ct),
                ActiveToday = await db.Vehicles.CountAsync(v => v.LastActive >= DateTime.UtcNow.AddHours(-24), ct),
                AvgBatteryHealth = await db.Vehicles.AverageAsync(v => v.BatteryHealth, ct),
                ActiveAlerts = await db.Alerts.CountAsync(a => !a.Resolved, ct)
            };

            await _cache.SetAsync("fleet_dashboard",
                JsonSerializer.SerializeToUtf8Bytes(dashboard),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) },
                ct);

            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
```

---

**Q22: How would you optimize SQLite queries in the MAUI app for 100,000+ locally cached records?**

**Answer:**

1. **Indexing** — Create indexes on frequently queried columns:

```sql
CREATE INDEX idx_vehicles_fleet ON Vehicles(FleetId);
CREATE INDEX idx_telemetry_vehicle_time ON Telemetry(VehicleId, Timestamp);
```

2. **Batch operations** — Use `InsertOrReplaceAllAsync` instead of looping `InsertAsync`.
3. **Pagination** — Use `Skip()`/`Take()` with `ORDER BY` — never load all records.
4. **Projection** — Select only needed columns.
5. **WAL mode** — Enable Write-Ahead Logging for concurrent reads/writes.

```csharp
await _localDb.ExecuteAsync("PRAGMA journal_mode=WAL");
await _localDb.ExecuteAsync("PRAGMA cache_size=-8000"); // 8MB cache

// Efficient paginated query
var page = await _localDb.Table<TelemetryData>()
    .Where(t => t.VehicleId == vehicleId)
    .OrderByDescending(t => t.Timestamp)
    .Skip(pageNumber * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

**Q23: The app has 20 NuGet packages and takes 2 minutes to restore. How do you speed up the build?**

**Answer:**

1. **NuGet cache** — `dotnet nuget locals all --clear` (if corrupted). Use global packages folder.
2. **NuGet.config** — Add a local cache feed: `<add key="cache" value="C:\nuget-cache" />`.
3. **Central package management** — Use `Directory.Packages.props` to unify versions across projects.
4. **Remove unused packages** — Audit and remove packages not directly referenced.
5. **Parallel restore** — `dotnet restore --parallel`.
6. **Build property optimizations**:

```xml
<PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <RestoreLockedMode>true</RestoreLockedMode>
</PropertyGroup>
```

---

**Q24: How do you reduce MAUI app binary size for distribution?**

**Answer:**

1. **Enable AOT** — `<PublishAot>true</PublishAot>` (significant size increase but better startup — balance needed).
2. **ILLinker** — Configure linker to remove unused code:

```xml
<PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
</PropertyGroup>
```

3. **Remove unused resources** — Delete unused fonts, images, and translations.
4. **Image compression** — Use WebP format instead of PNG. Resize to max display size.
5. **Conditional compilation** — Exclude debug-only code in Release.

```xml
<ItemGroup Condition="'$(Configuration)' == 'Release'">
    <MauiImage Remove="Resources/Images/debug_badge.png" />
</ItemGroup>
```

6. **Single assembly** — Merges all assemblies into one:

```xml
<PropertyGroup>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>false</SelfContained>
</PropertyGroup>
```

---

**Q25: The app sends 50 API requests when the dashboard loads. How do you reduce network calls?**

**Answer:**

1. **Batch endpoint** — Create a single `/api/dashboard` endpoint that returns all data:

```csharp
// Instead of 50 calls, make 1
var dashboard = await _api.GetAsync<FleetDashboardData>("/api/fleet/dashboard");
```

2. **GraphQL** — Use a single GraphQL endpoint with a query requesting only needed fields.
3. **HTTP/2 multiplexing** — Multiple requests over a single connection.
4. **Response caching** — Cache responses client-side with `ETag`.
5. **Preloading** — Predict user behavior and pre-fetch data in background.

```csharp
// ETag caching
public async Task<T?> GetWithEtagAsync<T>(string endpoint) where T : class
{
    var cachedEtag = await _secureStorage.GetAsync($"etag:{endpoint}");

    var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
    if (cachedEtag is not null)
        request.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(cachedEtag));

    var response = await _httpClient.SendAsync(request);
    if (response.StatusCode == HttpStatusCode.NotModified) // 304
        return JsonSerializer.Deserialize<T>(await _secureStorage.GetAsync($"cache:{endpoint}")!);

    var body = await response.Content.ReadAsStringAsync();
    var etag = response.Headers.ETag?.Tag;
    if (etag is not null)
    {
        await _secureStorage.SetAsync($"etag:{endpoint}", etag);
        await _secureStorage.SetAsync($"cache:{endpoint}", body);
    }

    return JsonSerializer.Deserialize<T>(body);
}
```

---

**Q26: How would you implement real-time collaboration between fleet managers viewing the same dashboard?**

**Answer:**

```csharp
// 1. SignalR group per fleet
public class FleetHub : Hub
{
    public async Task JoinFleetGroup(int fleetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"fleet_{fleetId}");
    }
}

// 2. Server pushes changes to all group members
public class FleetService
{
    private readonly IHubContext<FleetHub> _hubContext;

    public async Task NotifyFleetUpdate(int fleetId, FleetUpdate update)
    {
        await _hubContext.Clients.Group($"fleet_{fleetId}")
            .SendAsync("FleetUpdated", update);
    }
}

// 3. MAUI client receives updates
public class FleetViewModel
{
    private HubConnection _hub;

    public async Task ConnectAsync(int fleetId)
    {
        _hub = new HubConnectionBuilder()
            .WithUrl($"{Constants.ApiBaseUrl}/hubs/fleet")
            .Build();

        _hub.On<FleetUpdate>("FleetUpdated", update =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                // Apply delta update — don't refresh entire dashboard
                ApplyUpdate(update);
            });
        });

        await _hub.StartAsync();
        await _hub.InvokeAsync("JoinFleetGroup", fleetId);
    }

    private void ApplyUpdate(FleetUpdate update)
    {
        // Incremental update without re-query
        if (update.Type == "VehicleStatus")
        {
            var vehicle = Vehicles.FirstOrDefault(v => v.Id == update.VehicleId);
            if (vehicle is not null)
                vehicle.Status = update.NewStatus;
        }
    }
}
```

---

**Q27: The app crashes on iOS when receiving 1000+ telemetry updates per second. How do you throttle?**

**Answer:**

```csharp
public class ThrottledTelemetryService
{
    private Channel<TelemetryData> _channel = Channel.CreateBounded<TelemetryData>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest  // drop old data, keep latest
        });

    private readonly IDispatcherTimer _timer;

    public ThrottledTelemetryService()
    {
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(250); // 4 updates/sec max
        _timer.Tick += (s, e) => FlushBatch();
        _timer.Start();
    }

    public void OnTelemetryReceived(TelemetryData data)
    {
        _channel.Writer.TryWrite(data); // non-blocking
    }

    private void FlushBatch()
    {
        var batch = new List<TelemetryData>();
        while (_channel.Reader.TryRead(out var data))
            batch.Add(data);

        if (batch.Count > 0)
        {
            // Process batch on UI thread — 4 times per second max
            BatchReceived?.Invoke(this, batch);
        }
    }

    public event EventHandler<List<TelemetryData>>? BatchReceived;
}
```

---

**Q28: How do you handle 10,000+ concurrent users hitting the login API?**

**Answer:**

1. **Horizontal scaling** — Add more API server instances behind a load balancer.
2. **Stateless authentication** — Use JWT (already done) so any server can validate without session state.
3. **Redis caching** — Cache bcrypt/hashed password verification results (with TTL).
4. **Rate limiting** — Prevent brute force:

```csharp
// API: Rate limiting with ASP.NET Core
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Login", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

[HttpPost("login")]
[EnableRateLimiting("Login")]
public async Task<IActionResult> Login(LoginRequest request) { ... }
```

5. **CDN for static content** — Login page assets served via CDN.
6. **Database connection pooling** — Use `Max Pool Size=200` in connection string.

---

**Q29: How would you migrate from Xamarin.Forms to .NET MAUI for an existing EV fleet app?**

**Answer:**

1. **Inventory** — List all NuGet packages, custom renderers, and platform-specific code.
2. **Replace renderers with handlers** — Xamarin `ViewRenderer` → MAUI `Handler` pattern.
3. **Update namespaces** — `Xamarin.Forms` → `Microsoft.Maui`, `Xamarin.Essentials` → `Microsoft.Maui.Essentials`.
4. **Replace DependencyService with DI** — `DependencyService.Get<T>()` → constructor injection.
5. **Update XAML** — Replace `x:Arguments` with MAUI equivalents, update `ContentPage` base class.
6. **Test on all platforms** — Windows (new!), iOS, Android, macOS.
7. **Gradual rollout** — Ship MAUI version alongside Xamarin version, migrate users gradually.

```csharp
// Migration helper
#if XAMARIN
using Xamarin.Forms;
#elif MAUI
using Microsoft.Maui;
using Microsoft.Maui.Controls;
#endif
```

---

**Q30: How do you implement feature flags for gradual rollout of new features?**

**Answer:**

```csharp
public class FeatureFlagService
{
    private Dictionary<string, bool> _flags = new();
    private readonly HttpClient _httpClient;

    public async Task LoadFlagsAsync()
    {
        try
        {
            _flags = await _httpClient.GetFromJsonAsync<Dictionary<string, bool>>(
                $"{Constants.ApiBaseUrl}/api/features")
                ?? new Dictionary<string, bool>();
        }
        catch
        {
            // Default flags when offline
            _flags = new Dictionary<string, bool>
            {
                ["new_dashboard"] = false,
                ["biometric_login"] = true,
                ["offline_mode"] = false
            };
        }
    }

    public bool IsEnabled(string feature) =>
        _flags.TryGetValue(feature, out var enabled) && enabled;
}

// Usage in XAML
public class FeatureFlagConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var flagService = IPlatformApplication.Current!.Services.GetRequiredService<FeatureFlagService>();
        return flagService.IsEnabled(parameter as string ?? "");
    }
}

// <Entry IsVisible="{Binding Converter={StaticResource FeatureFlagConverter}, ConverterParameter='new_dashboard'}" />
```

---

## 4. Managerial & Leadership

**Q31: You're leading a team of 3 developers on a MAUI app. How do you divide work?**

**Answer:**

1. **Feature-based split** — Assign entire features (not layers): Dev A = Fleet Dashboard, Dev B = Swap Station Management, Dev C = User Profile & Settings.
2. **Contract-first** — Define API contracts (`IOpenAPI`/Swagger) before any coding starts. Teams work against the same contract.
3. **Shared components** — Identify shared services (ApiService, AuthService, BaseViewModel) and assign to one person.
4. **Code reviews** — Every PR reviewed by at least one other team member. Focus on architecture, error handling, and test coverage.
5. **Daily standups** — 15min sync. Blockers raised immediately. Demo every Friday.
6. **Branch strategy** — Feature branches from `develop`, PR → `develop`, release branches from `main`.

---

**Q32: A junior developer on your team submits a PR with no error handling, hardcoded URLs, and empty catch blocks. How do you handle the code review?**

**Answer:**

1. **Be specific and constructive** — Don't say "this is wrong." Say "This API call can throw `HttpRequestException`. Let's wrap it in try-catch and show a user-friendly message."
2. **Explain the why** — "Hardcoded URLs mean we need to rebuild the app to change the server address. Let's move this to `Constants.cs`."
3. **Provide examples** — Show the corrected code pattern.
4. **Set coding standards** — Create `CONTRIBUTING.md` with:
   - All API calls must have error handling
   - No hardcoded values (use constants or configuration)
   - No empty `catch` blocks
   - All ViewModels must extend `BaseViewModel`
   - Code must compile with 0 warnings
5. **Follow up** — Check the next PR to see if the feedback was applied. If not, pair-program to reinforce.

---

**Q33: How do you estimate delivery time for a new MAUI feature?**

**Answer:**

1. **Break down** — Split into: API contract, ViewModel, XAML UI, navigation, testing, deployment.
2. **T-shirt sizing** — Small (1-2 days), Medium (3-5 days), Large (1-2 weeks), XL (2-4 weeks).
3. **Historical data** — "Last time we added a similar list screen, it took 3 days."
4. **Risk factor** — Add 20-50% buffer for unknowns (third-party SDK quirks, platform-specific bugs).
5. **Estimation formula**: `(Optimistic + 4*MostLikely + Pessimistic) / 6` (PERT method).
6. **Communicate confidence** — "I'm 70% confident this ships in 2 weeks. The risk is the Bluetooth SDK — if there are issues, add 3-5 days."

---

**Q34: The client wants to add a feature that requires a database schema change. How do you manage the rollout?**

**Answer:**

1. **Backward-compatible migration** — Add columns as nullable. Old app version ignores new columns.
2. **Feature flag** — Keep the feature hidden until the migration is verified.
3. **Deploy migration first** — Apply DB migration during low-traffic window. Verify data integrity.
4. **Deploy API update** — New endpoints that use the new columns.
5. **Deploy MAUI app** — Submit to app stores. Wait for 50% adoption.
6. **Cleanup** — After migration period, make columns non-nullable, remove old code paths.

```csharp
// Migration: Add column with nullable first
migrationBuilder.AddColumn<string>(
    name: "SerialNumber",
    table: "Batteries",
    type: "nvarchar(100)",
    nullable: true);  // ← old app still works

// After all clients updated:
migrationBuilder.AlterColumn<string>(
    name: "SerialNumber",
    table: "Batteries",
    nullable: false);
```

---

**Q35: A production bug causes crashes for 10% of users. Walk through your incident response.**

**Answer:**

1. **Triage** (0-15 min): Identify affected version, platform, and severity. Check crash logs (App Center, Sentry).
2. **Mitigate** (15-60 min): If possible, disable the feature via feature flag. If not, prepare a hotfix.
3. **Root cause** (1-4 hrs): Reproduce the crash, identify the code path. Common suspects: null reference, platform API mismatch, threading issue.
4. **Fix** — Write the fix with a regression test.
5. **Deploy** — Hotfix build → TestFlight/Internal Track → 100% rollout after verification.
6. **Post-mortem** — Within 24 hrs: What happened? Why wasn't it caught? How do we prevent recurrence?

```csharp
// Hotfix checklist
// 1. git checkout -b hotfix/crash-on-null-telemetry
// 2. Fix the bug
// 3. dotnet test
// 4. git commit -m "fix: crash when telemetry data is null"
// 5. git push && create PR
// 6. Merge to main, tag release
// 7. Build and deploy
```

---

**Q36: You're interviewing a candidate for a MAUI developer role. What 3 questions do you ask?**

**Answer:**

1. **Practical MVVM** — "Walk me through how you'd implement a login screen in MAUI. From XAML through ViewModel to API call."
   - Looks for: understanding of `[ObservableProperty]`, `[RelayCommand]`, DI, error handling, navigation.
2. **Debugging** — "The CollectionView on Android scrolls smoothly, but on iOS it stutters. How do you diagnose and fix it?"
   - Looks for: platform-specific knowledge, profiling tools, understanding of UI virtualization.
3. **Architecture** — "Design a system where the MAUI app needs to show real-time locations of 500 EVs on a map."
   - Looks for: SignalR, clustering, viewport filtering, batching.

---

**Q37: How do you ensure code quality across a team of MAUI developers?**

**Answer:**

1. **StyleCop / .editorconfig** — Enforce consistent coding style.
2. **SonarQube** — Static analysis for code smells, security issues, and technical debt.
3. **PR gate** — Build + test + lint must pass before merge. Minimum 1 reviewer.
4. **Test coverage** — Require 70%+ coverage on ViewModels and Services. No coverage drop on new code.
5. **Architecture tests** — Verify that ViewModels don't reference Views, services don't reference ViewModels.
6. **Pair programming** — Complex features (offline sync, real-time telemetry) developed in pairs.
7. **Knowledge sharing** — Monthly brown-bag sessions where developers present a feature they built.

```csharp
// Architecture test example (using NetArchTest)
[Test]
public void ViewModels_ShouldNot_ReferenceViews()
{
    var result = Types.InAssembly(typeof(BaseViewModel).Assembly)
        .That().ResideInNamespace("EVSwap.Mobile.ViewModels")
        .ShouldNot()
        .HaveDependencyOn("EVSwap.Mobile.Views")
        .GetResult();

    Assert.IsTrue(result.IsSuccessful);
}
```

---

**Q38: How do you handle a situation where a developer is blocked waiting for an API that isn't ready yet?**

**Answer:**

1. **Contract-first approach** — Define API interface/DTOs first. Developer codes against `IApiService` with a mock implementation.
2. **Mock API service** — Create `MockApiService` that returns realistic dummy data:

```csharp
#if DEBUG
builder.Services.AddSingleton<IApiService, MockApiService>();
#else
builder.Services.AddSingleton<IApiService, ApiService>();
#endif
```

3. **Parallel workstreams** — Frontend team works on UI with mock data. Backend team builds real API. Integration happens when both are ready.
4. **WireMock / Postman mock server** — Set up a mock server that returns sample responses.
5. **Sprint buffer** — Reserve 20% of sprint capacity for integration work.

---

**Q39: How do you prioritize technical debt vs new features?**

**Answer:**

1. **Rule of thumb** — 20% of each sprint for tech debt / refactoring.
2. **Business impact matrix**:

| Debt | User Impact | Dev Velocity Impact | Priority |
|------|-------------|-------------------|----------|
| Monolithic API | None today | Slowing every sprint | High |
| Hardcoded strings | None | Low | Low |
| Missing error handling | Crashes | Medium | Critical |
| No tests | Regressions | Slow releases | High |

3. **Boy scout rule** — "Leave the code cleaner than you found it." Every PR includes some refactoring.
4. **Debt sprint** — Every 4th sprint is dedicated to tech debt and infrastructure improvements.

---

**Q40: Your app needs to support both Android and iOS. The client wants to ship in 4 weeks but you estimate 8. How do you handle this?**

**Answer:**

1. **Don't say yes immediately** — Explain the trade-offs.
2. **Scope negotiation** — "We can ship in 4 weeks if we reduce scope to: login, dashboard (read-only), and 1 fleet list. No offline mode, no push notifications, no tablet layout."
3. **Phased delivery** — "Week 4: MVP with core features. Week 6: Phase 2 with notifications and offline. Week 8: Full feature set."
4. **Platform priority** — "Ship Android-only in week 4 (80% of users), iOS in week 6."
5. **Resource adjustment** — "With 2 more developers, we could hit week 6 for both platforms."
6. **Risk buffer** — Always add 20% buffer for app store review times, unexpected platform bugs.

---

## 5. CI/CD, Cloud & DevOps

**Q41: Design a CI/CD pipeline for a MAUI app targeting Android, iOS, and Windows.**

**Answer:**

```yaml
# GitHub Actions example
name: Build & Deploy MAUI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    strategy:
      matrix:
        target: [android, ios, windows]
    runs-on: ${{ matrix.target == 'ios' && 'macos-14' || 'windows-2025' }}

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build ${{ matrix.target }}
        run: |
          dotnet build -f net10.0-${{ matrix.target }} -c Release

      - name: Run Tests
        run: dotnet test

      - name: Publish ${{ matrix.target }}
        if: github.ref == 'refs/heads/main'
        run: |
          dotnet publish -f net10.0-${{ matrix.target }} -c Release

      - name: Upload Artifact
        if: github.ref == 'refs/heads/main'
        uses: actions/upload-artifact@v4
        with:
          name: evswap-${{ matrix.target }}
          path: publish/
```

---

**Q42: How do you manage app store deployment for both Play Store and App Store?**

**Answer:**

| Step | Play Store (Android) | App Store (iOS) |
|------|--------------------|----------------|
| Signing | Upload keystore to CI | Use App Store Connect API key |
| Build | `dotnet publish -f net10.0-android` | `dotnet publish -f net10.0-ios` |
| Upload | Google Play Developer API | Transporter / altool |
| Testing | Internal Track → Closed Track → Production | TestFlight → App Review → Production |
| Review time | 2-24 hours | 24-48 hours |
| Rollback | Instant (via Play Console) | Requires new submission |

```bash
# Android: Publish to Play Store
dotnet publish -f net10.0-android -c Release \
  -p:AndroidKeyStore=true \
  -p:AndroidSigningKeyStore=evswap.keystore \
  -p:AndroidSigningKeyAlias=evswap \
  -p:AndroidSigningKeyPass=$KEY_PASS

# iOS: Publish to App Store
dotnet publish -f net10.0-ios -c Release \
  -p:ArchiveOnBuild=true \
  -p:CodesignKey="Apple Distribution" \
  -p:CodesignProvision="EVSwap Distribution"
```

---

**Q43: How do you implement automatic updates for a MAUI Windows app without the Microsoft Store?**

**Answer:**

```csharp
public class AutoUpdateService
{
    private readonly IApiService _api;

    public async Task CheckForUpdatesAsync()
    {
        var updateInfo = await _api.GetAsync<UpdateInfo>("/api/app/update");
        if (updateInfo is null) return;

        var currentVersion = Version.Parse(AppInfo.Current.VersionString);
        if (Version.Parse(updateInfo.LatestVersion) <= currentVersion) return;

        // Download and install
        var installerPath = Path.Combine(FileSystem.CacheDirectory, "EVSwap.Update.msix");
        using var client = new HttpClient();
        var bytes = await client.GetByteArrayAsync(updateInfo.DownloadUrl);
        await File.WriteAllBytesAsync(installerPath, bytes);

        // Launch installer
        var process = new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true,
            Verb = "runas"  // admin privileges for install
        };
        Process.Start(process);

        // Close current app
        Application.Current?.Quit();
    }
}
```

---

**Q44: How do you integrate Azure services with the MAUI app?**

**Answer:**

```csharp
// 1. Azure App Center — crash reporting & analytics
AppCenter.Start("android=<key>;ios=<key>",
    typeof(Analytics), typeof(Crashes));

// 2. Azure SignalR Service — real-time telemetry
builder.Services.AddSingleton(_ =>
    new HubConnectionBuilder()
        .WithUrl("https://evswap-signalr.azurewebsites.net/hubs/telemetry")
        .WithAutomaticReconnect()
        .Build());

// 3. Azure Blob Storage — firmware & asset downloads
public async Task<string> GetFirmwareDownloadUrlAsync(string fileName)
{
    var container = new BlobContainerClient(
        "DefaultEndpointsProtocol=https;AccountName=evswap",
        "firmware");
    var blob = container.GetBlobClient(fileName);
    return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1))
        .ToString();
}

// 4. Azure App Configuration — feature flags
builder.Configuration.AddAzureAppConfiguration(options =>
    options.Connect(Environment.GetEnvironmentVariable("AZURE_APP_CONFIG_CONNECTION")));
```

---

**Q45: How do you monitor app health and errors in production?**

**Answer:**

```csharp
public class TelemetryLogger
{
    // 1. App Center Crashes — automatic crash reporting
    // 2. Custom events
    public void TrackEvent(string eventName, Dictionary<string, string>? properties = null)
    {
        Analytics.TrackEvent(eventName, properties);
    }

    // 3. API call tracking
    public async Task<T?> TrackApiCall<T>(Func<Task<T?>> apiCall, string endpoint)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var result = await apiCall();
            TrackEvent("ApiSuccess", new Dictionary<string, string>
            {
                ["endpoint"] = endpoint,
                ["duration_ms"] = ((int)(DateTime.UtcNow - startTime).TotalMilliseconds).ToString()
            });
            return result;
        }
        catch (Exception ex)
        {
            TrackEvent("ApiFailure", new Dictionary<string, string>
            {
                ["endpoint"] = endpoint,
                ["error"] = ex.Message
            });
            throw;
        }
    }

    // 4. Health check endpoint (API)
    // GET /health — returns 200 if app is healthy, 503 if degraded
}
```

---

## 6. IoT & Enterprise MAUI

**Q46: How do you handle Bluetooth Low Energy (BLE) communication for EV diagnostics?**

**Answer:**

```csharp
public class BleDiagnosticService
{
    private IBluetoothLE _ble;
    private IDevice _connectedDevice;

    public async Task ConnectToVehicleAsync(string vehicleId)
    {
        _ble = CrossBluetoothLE.Current;
        var adapter = _ble.Adapter;

        adapter.DeviceDiscovered += (s, args) =>
        {
            if (args.Device.Name?.Contains(vehicleId) == true)
            {
                _connectedDevice = args.Device;
                adapter.StopScanning();
            }
        };

        await adapter.StartScanningForDevicesAsync();
        await _connectedDevice.ConnectAsync();

        // Read diagnostic data
        var service = await _connectedDevice.GetServiceAsync(Guid.Parse("180D")); // Battery Service
        var characteristic = await service.GetCharacteristicAsync(Guid.Parse("2A1C")); // Battery Level
        var data = await characteristic.ReadAsync();
        var batteryLevel = BitConverter.ToInt16(data.data, 0);
    }
}
// Requires Plugin.BLE NuGet package
```

---

**Q47: How do you manage MAUI app configuration for different environments (dev, staging, production)?**

**Answer:**

```csharp
// appsettings.json for each environment
// appsettings.Development.json
// appsettings.Staging.json
// appsettings.Production.json

// MauiProgram.cs
var appSettingsStream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
var config = new ConfigurationBuilder()
    .AddJsonStream(appSettingsStream)
#if DEBUG
    .AddJsonStream(await FileSystem.OpenAppPackageFileAsync("appsettings.Development.json"))
#endif
    .Build();

builder.Configuration.AddConfiguration(config);

// Usage
public class ApiService
{
    public ApiService(IConfiguration config)
    {
        var baseUrl = config["Api:BaseUrl"];
        _httpClient.BaseAddress = new Uri(baseUrl);
    }
}

// appsettings.Development.json
{
  "Api": {
    "BaseUrl": "http://localhost:5238",
    "TimeoutSeconds": 60
  }
}

// appsettings.Production.json
{
  "Api": {
    "BaseUrl": "https://api.evswap.datakrew.com",
    "TimeoutSeconds": 15
  }
}
```

---

**Q48: How do you implement QR code scanning for vehicle identification?**

**Answer:**

```csharp
public class QrScanService
{
    public async Task<string?> ScanVehicleQrAsync()
    {
        var options = new CameraBarcodeReaderOptions
        {
            Formats = BarcodeFormat.QrCode,
            AutoRotate = true,
            Multiple = false
        };

        // Using ZXing.Net.Maui
        var result = await BarcodeReader.Default.ReadAsync(options);
        return result?.Value;  // returns vehicle VIN or ID
    }
}

// XAML:
// <barcode:CameraBarcodeReaderView
//     BarcodesDetected="OnBarcodeDetected"
//     IsDetecting="True" />
```

---

**Q49: How do you implement report generation (PDF) in the MAUI app?**

**Answer:**

```csharp
public class ReportService
{
    public async Task<string> GenerateFleetReportAsync(int fleetId)
    {
        // Option 1: Server-generated PDF
        var pdfBytes = await _api.GetAsync<byte[]>($"/api/report/fleet/{fleetId}/pdf");
        var path = Path.Combine(FileSystem.CacheDirectory, $"fleet_{fleetId}_report.pdf");
        await File.WriteAllBytesAsync(path, pdfBytes);
        return path;

        // Option 2: Client-generated (using QuestPDF)
        // Document.Create(container =>
        // {
        //     container.Page(page =>
        //     {
        //         page.Content().Column(col =>
        //         {
        //             col.Item().Text("Fleet Report").FontSize(20);
        //             col.Item().Table(table => { ... });
        //         });
        //     });
        // }).GeneratePdf(path);
    }

    public async Task ShareReportAsync(string filePath)
    {
        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "Fleet Report",
            File = new ShareFile(filePath)
        });
    }
}
```

---

**Q50: How do you handle driver shift management and authentication in the app?**

**Answer:**

```csharp
public class ShiftManagementService
{
    public async Task<bool> StartShiftAsync()
    {
        var user = _auth.CurrentUser;
        if (user is null) return false;

        // 1. Record shift start
        var shift = await _api.PostAsync<ShiftRecord>("/api/shift/start", new
        {
            UserId = user.Id,
            VehicleId = user.AssignedVehicleId,
            StartedAt = DateTime.UtcNow,
            OdometerStart = await ReadOdometerAsync()
        });

        // 2. Enable vehicle controls
        if (shift is not null)
        {
            await _api.PostAsync($"/api/vehicle/{user.AssignedVehicleId}/unlock", new { });
            Preferences.Default.Set("active_shift_id", shift.Id);
            Preferences.Default.Set("shift_started_at", DateTime.UtcNow.ToString("O"));
            return true;
        }
        return false;
    }

    public async Task<bool> EndShiftAsync()
    {
        var shiftId = Preferences.Default.Get("active_shift_id", 0);
        if (shiftId == 0) return false;

        await _api.PostAsync($"/api/shift/{shiftId}/end", new
        {
            EndedAt = DateTime.UtcNow,
            OdometerEnd = await ReadOdometerAsync()
        });

        Preferences.Default.Remove("active_shift_id");
        Preferences.Default.Remove("shift_started_at");

        // Lock vehicle
        await _api.PostAsync($"/api/vehicle/{(await _auth.CurrentUser)?.AssignedVehicleId}/lock", new { });
        return true;
    }
}
```

---

## 7. Architecture & System Design

**Q51: Design a system where 10,000 EVs send telemetry every 5 seconds. How does the backend and app handle this?**

**Answer:**

```
[10,000 EVs] → MQTT Broker (Mosquitto)
                    ↓
            [IoT Hub / Azure Event Hub]
                    ↓
            [Stream Processor] (Azure Stream Analytics / Kafka)
                    ↓
        ┌───────────┴───────────┐
        ↓                       ↓
  [Time-Series DB]        [Alert Engine]
  (InfluxDB/TimescaleDB)   (Rules Engine)
        ↓                       ↓
  [API Server]            [SignalR Hub]
        ↓                       ↓
  [MAUI App] ← ← ← ← ← ← ← ← ←┘
  (REST + WebSocket)
```

**Key design decisions:**
- **MQTT** — Lightweight protocol designed for IoT. Much lower overhead than HTTP for telemetry.
- **Event Hub** — Buffers the data stream, handles 10K+ messages/sec.
- **Time-Series DB** — Optimized for append-heavy telemetry data (InfluxDB).
- **Alert Engine** — Evaluates rules (e.g., "battery > 95°C") and triggers notifications.
- **MAUI app** — Uses SignalR for real-time updates, falls back to REST polling.

---

**Q52: How would you refactor a monolithic MAUI app into a modular architecture?**

**Answer:**

```csharp
// 1. Identify modules by feature:
//    - EVSwap.Shared (Models, Interfaces, Constants)
//    - EVSwap.FleetModule (Fleet management pages, VMs, services)
//    - EVSwap.SwapModule (Swap station pages)
//    - EVSwap.AuthModule (Login, registration)
//    - EVSwap.Core (BaseViewModel, ApiService, AuthService)

// 2. Each module is a class library project:
//    EVSwap.FleetModule.csproj
//    <ProjectReference Include="..\EVSwap.Core\EVSwap.Core.csproj" />

// 3. Modules register their own services:
public static class FleetModuleRegistration
{
    public static MauiAppBuilder AddFleetModule(this MauiAppBuilder builder)
    {
        builder.Services.AddTransient<FleetDashboardPage>();
        builder.Services.AddTransient<FleetDashboardViewModel>();
        builder.Services.AddSingleton<IFleetService, FleetService>();
        return builder;
    }
}

// 4. Main app composes modules:
// MauiProgram.cs
builder.AddFleetModule();
builder.AddSwapModule();
builder.AddAuthModule();
```

---

**Q53: How do you handle database migrations when the MAUI app uses local SQLite?**

**Answer:**

```csharp
public class DatabaseMigrator
{
    private readonly SQLiteAsyncConnection _db;
    private int _currentVersion;

    public DatabaseMigrator(SQLiteAsyncConnection db)
    {
        _db = db;
    }

    public async Task MigrateAsync()
    {
        _currentVersion = await GetSchemaVersionAsync();

        if (_currentVersion < 1)
            await MigrateToV1Async();
        if (_currentVersion < 2)
            await MigrateToV2Async();
        // ... chain migrations

        await SetSchemaVersionAsync(2); // latest version
    }

    private async Task MigrateToV2Async()
    {
        await _db.ExecuteAsync("ALTER TABLE Vehicles ADD COLUMN FirmwareVersion TEXT DEFAULT ''");
        await _db.ExecuteAsync("ALTER TABLE Telemetry ADD COLUMN MotorTemp REAL DEFAULT 0");
    }

    private async Task<int> GetSchemaVersionAsync()
    {
        try
        {
            var result = await _db.ExecuteScalarAsync<int>(
                "PRAGMA user_version");
            return result;
        }
        catch { return 0; }
    }

    private async Task SetSchemaVersionAsync(int version)
    {
        await _db.ExecuteAsync($"PRAGMA user_version = {version}");
    }
}
```

---

**Q54: Design an offline-first architecture for the fleet management app.**

**Answer:**

```
┌─────────────────────────────────────┐
│          MAUI App                    │
│  ┌─────────────┐  ┌──────────────┐  │
│  │ ViewModel    │  │ Sync Service │  │
│  │ (reads local │  │ (background) │  │
│  │  first)      │  │              │  │
│  └──────┬───────┘  └──────┬───────┘  │
│         │                 │          │
│  ┌──────▼─────────────────▼───────┐  │
│  │      Data Service Layer        │  │
│  │  ┌──────────┐ ┌─────────────┐  │  │
│  │  │ Local DB │ │ API Caller  │  │  │
│  │  │ (SQLite) │ │ (HttpClient)│  │  │
│  │  └──────────┘ └─────────────┘  │  │
│  └────────────────────────────────┘  │
└─────────────────────────────────────┘
```

**Read path:** ViewModel → DataService → Local DB first → return cached data → background refresh from API → update UI.

**Write path:** ViewModel → DataService → Save to Local DB → queue API call → when online, flush queue.

---

**Q55: How would you implement role-based access control (RBAC) in the MAUI app?**

**Answer:**

```csharp
public class AuthorizationService
{
    private readonly IAuthService _auth;

    public bool CanAccess(string feature)
    {
        var roles = _auth.CurrentUser?.Roles ?? new List<string>();

        return feature switch
        {
            "FleetDashboard" => roles.Contains("FleetManager") || roles.Contains("Admin"),
            "DriverShift" => roles.Contains("Driver"),
            "BillingReports" => roles.Contains("Finance") || roles.Contains("Admin"),
            "VehicleControl" => roles.Contains("Admin"), // sensitive!
            "UserManagement" => roles.Contains("Admin"),
            _ => false
        };
    }

    // XAML usage via converter
    public class RoleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var authService = IPlatformApplication.Current!.Services
                .GetRequiredService<IAuthorizationService>();
            return authService.CanAccess(parameter as string ?? "");
        }
    }
}

// Usage:
// <Button Text="Disable Vehicle" IsVisible="{Binding Converter={StaticResource RoleToVisibility}, ConverterParameter='VehicleControl'}" />
```

---

**Q56: Design a notification system that works across push, in-app, and email channels.**

**Answer:**

```csharp
public class UnifiedNotificationService
{
    public async Task SendNotificationAsync(Notification notif)
    {
        // 1. Save notification to database
        var saved = await _api.PostAsync<Notification>("/api/notifications", notif);

        // 2. Determine delivery channel
        switch (notif.Priority)
        {
            case AlertPriority.Critical:
                // All channels
                await SendPushAsync(notif);
                await ShowInAppAlertAsync(notif);
                await SendEmailAsync(notif);
                break;

            case AlertPriority.High:
                await SendPushAsync(notif);
                await ShowInAppBadgeAsync(notif);
                break;

            case AlertPriority.Info:
                // In-app notification center only
                break;
        }
    }

    private async Task ShowInAppAlertAsync(Notification notif)
    {
        // Custom toast/banner overlay
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Shell.Current.CurrentPage.DisplayAlert(
                notif.Title, notif.Body, "View");
        });
    }

    private async Task ShowInAppBadgeAsync(Notification notif)
    {
        UnreadCount++;
        // Badge appears on tab icon
    }
}
```

---

## 8. Behavioral & Cultural Fit

**Q57: Tell me about a time you had to debug a difficult production issue in a mobile app.**

**Answer:** *(Sample answer structure)*
- **Situation:** Users reported the app crashing on Android after the latest update.
- **Task:** Identify the root cause and ship a fix within 24 hours.
- **Action:** 
  1. Checked App Center crashes — saw `NullReferenceException` in `StationDetailPage`.
  2. Reproduced on a Pixel 6 running Android 14.
  3. Found that a new API field `opening_hours` was null for some stations.
  4. The old code assumed it was never null and called `.Split(",")` on it.
- **Result:** Fixed with a null check. Shipped hotfix within 6 hours. Added a unit test that asserts null `opening_hours` doesn't crash. Added `[CanBeNull]` annotation.
- **Prevention:** Added nullable reference type analysis to CI pipeline.

---

**Q58: How do you handle disagreements with a backend developer about API contract design?**

**Answer:**

1. **Understand their perspective** — "You're designing for server performance. I'm designing for mobile UX. Let's find the middle ground."
2. **Propose trade-offs** — "You want a separate endpoint per field. That means the app makes 10 API calls. How about a single `/api/fleet/summary` that returns everything in one response?"
3. **Use data** — "Loading the dashboard currently takes 8 seconds with 10 calls. If we batch into 1 call, it takes 1.5 seconds. Here are the profiler screenshots."
4. **Escalate if needed** — If it's blocking the release, involve the tech lead for a decision.
5. **Document the decision** — "We agreed on `/api/fleet/summary` for now. If performance becomes an issue, we can split it later."

---

**Q59: Describe a project where you had to learn a new technology quickly. How did you approach it?**

**Answer:** *(Sample answer)*
- **Situation:** Client needed a MAUI app but I had only Xamarin.Forms experience.
- **Action:**
  1. **Read** — Microsoft's MAUI migration guide and release notes.
  2. **Prototype** — Built a small proof-of-concept app in 3 days (login → list → detail).
  3. **Community** — Followed MAUI GitHub issues, StackOverflow, .NET MAUI Discord.
  4. **Pair** — Had a senior MAUI dev review my first pull request.
  5. **Iterate** — First sprint was slower (learning curve), but by sprint 2, velocity matched Xamarin.
- **Result:** Delivered the MAUI app on time. Now I'm faster in MAUI than I was in Xamarin.

---

**Q60: Why do you want to work at Datakrew specifically?**

**Answer:** *(Tailor to your situation)*
- "I'm passionate about the intersection of mobile technology and sustainability. EV adoption is critical for our planet's future, and Datakrew's mission to serve 1 million EVs directly contributes to that."
- "The technical challenges excite me — real-time telemetry at scale, offline-first architecture, IoT integration. These are hard problems that require thoughtful engineering."
- "I see the OXRED Platform Suite as the nerve center for EV fleet operations. Building the mobile interface for that system means my work directly impacts fleet managers' daily efficiency."
- "The 4-6 year experience requirement in the job description matches my skill level perfectly. I've built and shipped MAUI/Xamarin apps to production and I'm ready to apply that experience to Datakrew's scale challenges."

---

> **Quick Tips for Datakrew Interview:**
> 
> 1. **Know the numbers** — 1 million EVs target, OXRED Platform Suite, IoT/AI focus.
> 2. **Emphasize scale** — Every answer should consider "how does this work for 10K+ vehicles / 1M+ users?"
> 3. **Show real-time awareness** — EV telemetry is all about real-time. Mention SignalR, WebSocket, MQTT.
> 4. **Offline-first mindset** — Fleet drivers may have poor connectivity. Offline support is critical.
> 5. **Security consciousness** — Vehicle control from a phone requires rigorous security. Mention biometrics, PIN, remote wipe.
> 6. **Performance obsession** — Battery efficiency, memory usage, startup time — EV app should be as efficient as the vehicles it monitors.
