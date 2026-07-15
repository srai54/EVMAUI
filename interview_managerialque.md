# Managerial, Scenario, Optimization & Domain-Specific Interview Questions

> **Target Company:** Datakrew — EV Fleet Intelligence with IoT/AI solutions (OXRED Platform Suite)
> **Role:** .NET MAUI Freelancer / Cross-Platform Developer
> **Goal:** 1 million EVs in 5 years, touch a billion lives

---

## How to Use This Guide

Each question has:
- **Theory** — The conceptual foundation. WHY this matters, WHEN to use it, WHAT problem it solves.
- **Code Example** — Practical implementation of the theory in real MAUI/C# code.

Learn the theory first to build understanding, then study the code to see it in action.

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

**Answer:**

**Theory:** This is a company-awareness question that tests whether you've researched Datakrew before the interview. Understanding the company's mission (1 million EVs in 5 years), product (OXRED Platform Suite), and technology stack (IoT/AI for EV fleet intelligence) shows genuine interest and initiative. Interviewers want to see that you can connect your technical skills to their business domain.

**Code Example:** OXRED is Datakrew's flagship EV fleet intelligence platform that provides deep insights into vehicle fleet performance and diagnostics using IoT sensors and AI analytics. It collects real-time telemetry from EVs — battery health, motor temperature, tire pressure, GPS location, energy consumption — and surfaces actionable insights through dashboards, alerts, and predictive maintenance reports. The MAUI app likely serves as the mobile interface for fleet managers and drivers to monitor this data on the go.

---

**Q2: How would you architect a MAUI app to display real-time EV telemetry data?**

**Answer:**

**Theory:** Real-time data requires a push-based architecture — the server initiates data delivery, not the client. The two main approaches are: (1) **SignalR** (WebSocket-based, real-time bidirectional communication) — best for live updates, server pushes data as it arrives. (2) **Polling** (HTTP GET on a timer) — simpler but less efficient, suitable for data that changes infrequently. A hybrid approach uses SignalR as primary and falls back to polling when WebSocket connections fail (corporate firewalls, poor connectivity). The choice depends on your data volume and latency requirements: SignalR for sub-second updates (battery temperature), polling for minute-level updates (daily mileage report).

**Code Example:**
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

**Theory:** Rendering 10,000+ pins on a map simultaneously will freeze ANY mobile UI because of two bottlenecks: (1) **Memory** — each pin is a UIView/ViewGroup with its own lifecycle; 10,000 of them consume hundreds of MB. (2) **UI thread** — updating all pins on every telemetry batch blocks the UI thread. The solution combines multiple strategies: **clustering** (group nearby pins at low zoom, show individuals at high zoom), **viewport filtering** (only render pins visible on screen — thousands off-screen are invisible), **batching** (process telemetry in batches, not per-vehicle), and **background processing** (parse and filter data off the UI thread). The cancellation token pattern is essential — when the user pans the map, cancel the previous viewport computation and start anew.

**Code Example:**
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

**Theory:** Offline support follows the **local-first** pattern: always read from local storage first (instant, works offline), then sync with the server when connectivity is available. The three pillars are: (1) **Local cache** — SQLite database on the device stores recent data with expiration times. Reads hit the cache first. (2) **Write queue** — when the user creates data (swap request, report issue), save it locally AND queue it for server sync. (3) **Background sync** — when connectivity restores, process the queue: send queued writes, refresh stale cache entries. Conflict resolution is critical — if the same record was modified both locally and on the server, you need a strategy (last-write-wins, server-wins, or manual merge). For EV fleets, where drivers operate in remote areas with intermittent connectivity, offline support is not optional — it's a core requirement.

**Code Example:**
```csharp
public class OfflineSyncService
{
    private readonly SQLiteAsyncConnection _localDb;

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
        try { await _api.PostAsync(endpoint, payload); }
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

**Theory:** IoT integration follows a data pipeline: **Device → Gateway → Cloud → API → Mobile App**. The EV's sensors (battery management system, motor controller, GPS module) send telemetry via lightweight protocols like MQTT (optimized for IoT — low bandwidth, pub/sub model). Azure IoT Hub or AWS IoT Core ingests this data, processes it (filtering, aggregation, anomaly detection), and stores it. The ASP.NET API exposes REST endpoints for historical data and SignalR hubs for real-time data. The MAUI app connects to both: REST for initial load and pull-to-refresh, SignalR for live updates. The key design decision is WHERE to do data processing: on the device (SDK), in the cloud (IoT Hub), or on the API server. For EV telemetry, raw sensor data should be processed in the cloud (scales better), and the API/Mobile layer should receive already-aggregated, meaningful metrics.

**Code Example:**
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

**Theory:** EV fleet apps have a UNIQUE threat model compared to typical consumer apps because the app can control PHYSICAL VEHICLES (remote disable, unlock, start). A compromised fleet manager account could disable an entire fleet or unlock vehicles for theft. The security principles: (1) **Defense in depth** — multiple layers of security. Biometric + PIN for critical actions, not just token-based auth. (2) **Least privilege** — a driver should only see their assigned vehicle, not the entire fleet. (3) **Audit trail** — every vehicle control action must be logged with timestamp, user, and vehicle ID. (4) **Short-lived sessions** — access tokens expire in 15 minutes, refresh tokens in 24 hours. (5) **Remote wipe** — if a device is stolen, the admin can revoke all tokens and force logout. The app must also handle data privacy regulations (GDPR, CCPA) for GPS tracking data — clear privacy policy, opt-in location sharing, data retention limits.

**Code Example:**
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

**Theory:** Scaling a MAUI app for 1 million vehicles means thinking about EVERY layer. The mobile app itself is just the tip of the iceberg — the real scaling challenge is the BACKEND. Key principles: (1) **Stateless API** — every API request must be independently processable by any server instance. JWT tokens contain all session info; no server-side session storage. (2) **Horizontal scaling** — add more API server instances behind a load balancer. The API must be stateless for this to work. (3) **Caching everywhere** — Redis for API response caching, CDN for static assets, client-side SQLite for offline resilience. (4) **Database sharding** — split vehicle data by region or fleet ID so no single database becomes a bottleneck. (5) **Rate limiting** — protect the API from abuse (malicious or accidental). (6) **Pagination** — never return all vehicles in one response. The MAUI client must handle pagination gracefully with `RemainingItemsThreshold` for infinite scroll.

**Code Example:**
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

**Theory:** Fleet health monitoring is about converting raw telemetry data into ACTIONABLE business metrics. The key categories: (1) **Battery** — state of health (degradation over time), state of charge, temperature. Battery is the most expensive EV component; tracking its health predicts replacement needs. (2) **Motor** — temperature, efficiency, vibration patterns. Abnormal patterns indicate pending failure. (3) **Utilization** — active vs idle vehicles, distance traveled, energy consumed. Low utilization means fleet is over-provisioned. (4) **Maintenance** — due-for-service count, active alerts, mean time between failures. Predictive maintenance reduces downtime. (5) **Efficiency** — km/kWh, cost per km. These measure the fleet's operational efficiency. The MAUI dashboard should surface the TOP metrics visually (gauges, sparklines), with drill-down capability for details.

**Code Example:**
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

**Theory:** Push notifications require a three-layer architecture: (1) **Platform-specific push services** — Firebase Cloud Messaging (FCM) for Android, Apple Push Notification Service (APNS) for iOS. Each has its own API, registration flow, and payload format. (2) **Backend notification service** — a service that receives alerts from the telemetry pipeline, determines the target users (fleet manager assigned to that vehicle), and sends push notifications via FCM/APNS. (3) **MAUI client** — registers for push on startup, handles incoming notifications (foreground: show in-app banner, background: OS notification). Critical alerts (overheating, battery fire risk) need special handling: they should bypass silent mode, play a distinctive sound, and navigate the user to the alert details. Local notifications serve as a fallback when the push service is unreachable.

**Code Example:**
```csharp
public class AlertNotificationService
{
    // 1. Register device for push
    public async Task RegisterForPushAsync()
    {
#if ANDROID
        var fcmToken = await CrossFirebaseCloudMessaging.Current.GetTokenAsync();
        await _api.PostAsync("/api/notifications/register", new { Token = fcmToken, Platform = "android" });
#elif IOS
        var apnsToken = await CrossPushNotification.Current.GetTokenAsync();
        await _api.PostAsync("/api/notifications/register", new { Token = apnsToken, Platform = "ios" });
#endif
    }

    // 2. Handle incoming notification
    public void OnNotificationReceived(NotificationData notification)
    {
        if (notification.Priority == AlertPriority.Critical)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync("//alerts",
                    new Dictionary<string, object> { { "AlertId", notification.AlertId } });
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

**Theory:** Firmware compatibility is a critical concern in EV/IoT apps because the app controls hardware. If the app sends a command that the vehicle's firmware doesn't understand, it could cause undefined behavior or safety issues. The solution is **version negotiation** — the app checks its own version against the vehicle's minimum required version before allowing any control commands. This is typically done on app startup AND before each critical action (because firmware could be updated OTA while the app is running). The API should expose a version compatibility endpoint that returns: (1) minimum app version required for current firmware, (2) latest available firmware version, (3) release notes. If the app is too old, BLOCK access and force an update. If the firmware is outdated, suggest an update but don't block.

**Code Example:**
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
            await ShowAlertAsync("Update Required",
                $"Firmware v{firmwareInfo.FirmwareVersion} requires app v{firmwareInfo.MinAppVersion}. " +
                $"You have v{appVersion}. Please update.");
            Application.Current?.Quit();
        }

        if (firmwareInfo.FirmwareVersion > firmwareInfo.CurrentFirmware)
        {
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

**Theory:** Cross-platform crashes that occur on one platform but not another are almost always caused by: (1) **Missing permissions** — Android requires explicit manifest permissions for camera, location, Bluetooth, etc. Windows is permissive by default. (2) **Platform API differences** — Android's `OnBackPressed()` vs Windows' back button. (3) **Threading** — Android is strict about UI thread access; Windows is more forgiving. (4) **Lifecycle** — Android destroys and recreates activities on rotation. (5) **JNI/Interop** — Java/Kotlin interop issues that don't exist on Windows. The debugging approach is systematic: check platform-specific files first (`AndroidManifest.xml`), then use platform-specific logging (`adb logcat`), then test on multiple Android API levels. The worst offenders are usually null references from platform APIs that return null on Android but not on Windows.

**Code Example:**
```csharp
// Add logging specific to Android
#if ANDROID
Android.Util.Log.Debug("EVSwap", $"Crash at {DateTime.UtcNow}: {ex}");
#endif
```

---

**Q12: A user reports that battery data on the dashboard is 30 minutes stale. How do you investigate?**

**Answer:**

**Theory:** Stale data has three common causes: (1) **Server-side caching** — `[ResponseCache(Duration = 1800)]` on the API endpoint caches for 30 minutes. (2) **Client-side caching** — `MemoryCache` or `SQLite` cache with a 30-minute expiration. (3) **Polling interval** — the telemetry service polls every 30 minutes instead of every few seconds. The investigation follows the data flow backward: UI → ViewModel → Service → API → Database. Each layer could be adding the staleness. Check HTTP response headers (`Cache-Control: max-age=1800`) first — that tells you the API is caching. If the API is fine, check the client's telemetry service polling interval and local cache TTL. The fix depends on the root cause: reduce cache duration, switch to SignalR for real-time push, or add ETag-based conditional requests.

**Code Example:**
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

**Theory:** Slow page appearance is a **startup performance** problem, not a network problem. The 8 seconds is likely spent on: (1) **XAML parsing** — MAUI parses XAML at runtime by default. Without compiled bindings, every `{Binding}` is resolved via reflection. (2) **Assembly loading** — all referenced assemblies are loaded during startup. (3) **DI container initialization** — singleton services are created on first request. (4) **Expensive constructors** — ViewModel constructors that make API calls or load data synchronously. The fixes: enable compiled bindings (`x:DataType`), use `x:Load="False"` for non-critical UI, configure AOT compilation (`<PublishAot>true</PublishAot>`), make ViewModel constructors cheap (move initialization to `OnAppearing`), and show a splash screen immediately so the user perceives faster startup even if actual load time is the same.

**Code Example:**
```xml
<!-- Lazy load heavy UI -->
<Frame x:Load="{Binding IsAdvancedMode}" IsVisible="False">
    <!-- complex dashboard charts -->
</Frame>
```

---

**Q14: The API returns 500 errors intermittently. Users see "Something went wrong" without details. How do you improve this?**

**Answer:**

**Theory:** The problem has TWO parts: (1) **User experience** — a generic "Something went wrong" is unhelpful. Users need actionable information: "Network error, retrying..." vs "Server error, try again later" vs "Session expired, please login." (2) **Debugging** — without details, developers can't diagnose the issue. The fix combines: (a) **Categorize errors** — 4xx (client error, don't retry) vs 5xx (server error, retry with backoff) vs network errors (retry immediately). (b) **Auto-retry** — transient failures (5xx, timeouts) should be retried with exponential backoff. (c) **Correlation IDs** — the API should return a unique ID for every 500 error; the client logs it so support can trace the exact request. (d) **Logging middleware** — the API should log ALL 500s with full request details. The principle: users should never see raw error messages, but the app should always capture them for debugging.

**Code Example:**
```csharp
// Categorize errors for better messaging
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

// Auto-retry with exponential backoff
public async Task<T?> CallWithRetryAsync<T>(Func<Task<T?>> call, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try { return await call(); }
        catch (HttpRequestException ex) when (i < maxRetries - 1 && ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
        }
    }
    return await call();
}
```

---

**Q15: You need to add a new screen to the app without breaking existing navigation. What's your approach?**

**Answer:**

**Theory:** Adding a new screen to a MAUI app follows the **Open/Closed Principle** — the app should be OPEN for extension (adding screens) but CLOSED for modification (existing screens shouldn't change). Shell navigation supports this naturally: you add a new route in `AppShell.xaml`, register the page and ViewModel in DI, and you're done. The key is to NEVER modify existing routes or remove old ones. If you need to change navigation flow (e.g., add a step in the middle), use Shell's URI-based navigation with query parameters rather than pushing/poping on a NavigationPage stack. Test ALL existing navigation paths after adding a new screen: back button, tab switching, deep links, and programmatic navigation. Navigation bugs are among the hardest to catch because they only manifest in specific user flows.

**Code Example:**
```xml
<!-- Add new route without touching existing ones -->
<ShellContent Route="newfeature" ContentTemplate="{DataTemplate views:NewPage}" />

<!-- Register in DI -->
<!-- builder.Services.AddTransient<NewPage>(); builder.Services.AddTransient<NewViewModel>(); -->

<!-- Navigate to new screen -->
<!-- await Shell.Current.GoToAsync("newfeature"); -->
```

---

**Q16: The CollectionView lags when scrolling through 500+ station items. How do you fix it?**

**Answer:**

**Theory:** Scrolling lag in `CollectionView` is caused by the UI thread being too busy to handle scroll events. The root causes: (1) **Complex visual tree** — deeply nested layouts in `ItemTemplate` take too long to measure and arrange. (2) **Reflection-based bindings** — without `x:DataType`, each binding uses reflection. (3) **No virtualization** — if you accidentally use `BindableLayout` on a `StackLayout` instead of `CollectionView`, there's NO virtualization. (4) **Expensive item templates** — images, converters, or behaviors that do heavy work during scrolling. The fixes: (1) Use `CollectionView` (not `ListView` or `BindableLayout`). (2) Add `x:DataType` to page and DataTemplate for compiled bindings (5-10x faster). (3) Keep the template simple — one `VerticalStackLayout` with 2-3 labels, no nested grids. (4) Use `RemainingItemsThreshold` for incremental loading instead of loading all 500 at once. (5) Cache and resize images.

**Code Example:**
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

**Theory:** The **Repository pattern** with a **strategy** for online/offline is the cleanest approach. Define an `IDataService<T>` interface with standard CRUD operations. Implement TWO versions: `ApiDataService` (online, calls HTTP) and `CachedDataService` (offline-first, uses SQLite + API fallback). The offline implementation follows the **cache-aside** pattern: check local cache first → return if fresh → try API → update cache → return. For writes: save locally immediately (user gets instant feedback), then sync to server asynchronously. The abstraction (`IDataService<T>`) means ViewModels don't know or care about online/offline — they just call `GetAsync()` or `SaveAsync()`. This is the **Strategy pattern** in action: the data access strategy is swapped based on connectivity without changing the ViewModel.

**Code Example:**
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

**Theory:** Memory leaks in MAUI are almost always caused by **event handler references preventing garbage collection**. When ViewModel A subscribes to an event on a Singleton service B, B holds a REFERENCE to A. Even when the user navigates away from A's page, A can't be garbage collected because B's event delegate still points to A. Over time, every navigation creates a new ViewModel that never gets collected → memory grows unbounded. Common culprits: (1) `DispatcherTimer.Tick` event subscribed in `OnAppearing` but never unsubscribed in `OnDisappearing`. (2) MessagingCenter/WeakReferenceMessenger subscriptions without explicit unsubscription (WeakReference helps but the GC pressure remains). (3) Static collections (ConcurrentDictionary, List) that accumulate ViewModels. (4) Large bitmaps not disposed after use. The diagnostic approach: Visual Studio Memory Snapshot comparison at t=0, t=15min, t=30min. Look for growing counts of your ViewModel types.

**Code Example:**
```csharp
// Fix: unsubscribe in OnDisappearing
protected override void OnDisappearing()
{
    base.OnDisappearing();
    _telemetryService.TelemetryReceived -= OnTelemetryUpdate;
    _timer?.Dispose();
}
```

---

**Q19: How do you handle a situation where the API contract changes and the mobile app isn't updated yet?**

**Answer:**

**Theory:** API versioning prevents this problem. The principle: **never break backward compatibility**. When adding a new field, add it as nullable/optional — old app versions ignore unknown fields. When renaming a field, keep the old name and add the new name as an alias — both work. When changing behavior, create a NEW endpoint version (`/api/v2/stations`) and keep the old one (`/api/v1/stations`). The mobile app specifies the version via URL or `Accept` header. Server-side feature flags allow gradual rollout: enable the new behavior for 10% of users, monitor for issues, then ramp up. `[JsonExtensionData]` is the safety net — it captures any JSON properties that don't map to C# properties, preventing deserialization crashes when the server sends unexpected data.

**Code Example:**
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

---

**Q20: The app needs to support both tablet and phone layouts. How do you approach responsive design?**

**Answer:**

**Theory:** Responsive MAUI design uses **device idiom** (Phone vs Tablet) and **visual state** to adapt layouts. The core approach: (1) **Fluid layouts** — use `Grid` with star-sized columns (`*`) rather than fixed widths. (2) **Idiom adaptation** — phone shows single-column layout, tablet shows two-column grid. (3) **VisualStateManager** — define different visual states for phone and tablet, and switch based on `DeviceInfo.Idiom`. (4) **OnIdiom markup extension** — set different values for different idioms directly in XAML: `FontSize="{OnIdiom Phone=14, Tablet=20}"`. (5) **Separate pages** — if the layouts are radically different, create `StationPage_Phone.xaml` and `StationPage_Tablet.xaml` and select at runtime. The key insight: don't try to make ONE layout work for both. Design for phone first (constraints are tighter), then enhance for tablet.

**Code Example:**
```xml
<Style TargetType="Grid" x:Key="DashboardGrid">
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
```

---

## 3. Performance Optimization & Scaling

**Q21: The API response for fleet dashboard takes 15 seconds. How do you reduce it to under 2 seconds?**

**Answer:**

**Theory:** Slow API responses are a BACKEND problem, not a mobile problem. The fix must happen on the server. The principle is **pre-computation**: instead of calculating dashboard aggregates on every request (which requires scanning millions of telemetry records), pre-compute the aggregates in a BACKGROUND JOB every 30 seconds and cache the result. The API just reads the cached value. This is the **Command Query Responsibility Segregation (CQRS)** pattern — writes go through one path (telemetry ingestion), reads go through a different path (cached pre-computed results). Complementary optimizations: (1) Database indexing on WHERE clause columns. (2) Read replicas for reporting queries. (3) Pagination instead of returning all data. (4) Projection (`SELECT column1, column2` instead of `SELECT *`). The 15-second problem is almost always a missing index or an un-batched aggregation query.

**Code Example:**
```csharp
public class DashboardAggregator : BackgroundService
{
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
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) }, ct);

            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}
```

---

**Q22: How would you optimize SQLite queries in the MAUI app for 100,000+ locally cached records?**

**Answer:**

**Theory:** SQLite performance degrades with large datasets if you don't follow database best practices. The principles: (1) **Indexing** — `SELECT * FROM Telemetry WHERE VehicleId = @id ORDER BY Timestamp DESC` without an index on `(VehicleId, Timestamp)` requires a FULL TABLE SCAN (100K records read). With the index, it reads 10-100 records. (2) **WAL mode** — Write-Ahead Logging allows concurrent reads while writing, preventing "database is locked" errors. (3) **Batching** — `INSERT` 100 records at once instead of 100 individual inserts (100x faster). (4) **Pagination** — never `SELECT * FROM Telemetry` — use `LIMIT 20 OFFSET @page`. (5) **Projection** — `SELECT Timestamp, Value` instead of `SELECT *` if you only need two columns. (6) **Cache size** — `PRAGMA cache_size = -8000` allocates 8MB of cache (on by default, but good to verify).

**Code Example:**
```csharp
await _localDb.ExecuteAsync("PRAGMA journal_mode=WAL");
await _localDb.ExecuteAsync("PRAGMA cache_size=-8000"); // 8MB cache

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

**Theory:** NuGet restore time grows with (1) the number of packages, (2) the number of projects, (3) package size, and (4) feed latency. Optimization strategies: (1) **Central Package Management** — `Directory.Packages.props` ensures all projects use the same package version, reducing redundant downloads. (2) **Lock file** — `RestorePackagesWithLockFile` pins exact versions so restore checks the lock file before querying the feed. (3) **Local cache** — NuGet caches packages in `%USERPROFILE%\.nuget\packages`; a full restore only downloads packages NOT in the cache. (4) **Parallel restore** — `dotnet restore --parallel`. (5) **Remove unused packages** — audit with `dotnet list package` and remove references not directly used. (6) **NuGet.config** — add a local feed for CI/CD to avoid external feed latency.

**Code Example:**
```xml
<PropertyGroup>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
    <RestoreLockedMode>true</RestoreLockedMode>
</PropertyGroup>
```

---

**Q24: How do you reduce MAUI app binary size for distribution?**

**Answer:**

**Theory:** MAUI binary size affects download speed (app store), install time, and device storage. The main contributors: (1) **IL code** — your compiled C# assemblies. (2) **Resources** — images, fonts, audio files. (3) **.NET runtime** — the base class library. (4) **Native libraries** — platform-specific dependencies. Optimization strategies: (1) **PublishTrimmed** — removes unused IL code (can reduce size by 30-50%). (2) **PublishAot** — compiles to native code (INCREASES size but improves startup — use AOT for performance-critical sections only). (3) **Image compression** — use WebP instead of PNG, resize to max display size (don't include 2K resolution images). (4) **Conditional compilation** — exclude debug tools and test code from Release builds. (5) **Single assembly** — `PublishSingleFile` merges all assemblies into one, reducing overhead.

**Code Example:**
```xml
<PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>link</TrimMode>
</PropertyGroup>

<ItemGroup Condition="'$(Configuration)' == 'Release'">
    <MauiImage Remove="Resources/Images/debug_badge.png" />
</ItemGroup>
```

---

**Q25: The app sends 50 API requests when the dashboard loads. How do you reduce network calls?**

**Answer:**

**Theory:** 50 API requests for one screen is a **chatty API** anti-pattern — each request has HTTP overhead (DNS, TCP, TLS, headers) regardless of payload size. The fix is **batching** — combine multiple data requirements into a single API response. The extreme approach: a single `/api/dashboard` endpoint that returns ALL data needed for the dashboard screen. The moderate approach: 3-5 batch endpoints (profile, fleet summary, notifications). Caching reduces repeat requests: (1) **ETag/304** — the server returns a hash of the response; the client sends the hash on subsequent requests; if unchanged, server returns 304 (empty body, no processing). (2) **Client-side cache** — cache responses in MemoryCache for the session duration. (3) **Preloading** — predict what the user will need (e.g., while they view dashboard, preload stations list) and fetch in background.

**Code Example:**
```csharp
// Instead of 50 calls, make 1
var dashboard = await _api.GetAsync<FleetDashboardData>("/api/fleet/dashboard");

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

**Theory:** Real-time collaboration (multiple users seeing the same data update simultaneously) requires a **pub/sub** architecture. SignalR Groups are the ideal mechanism: each fleet has a SignalR Group, all fleet managers join their fleet's group, and any server-side data change is broadcast to all group members. The MAUI client receives **delta updates** — not the entire dashboard, just the fields that changed. This minimizes bandwidth and UI work. The alternative approach is **polling** — every client polls every N seconds — but this doesn't scale (10 fleet managers × 1-second polls = 10 requests/second × 60 = 600 requests/minute). SignalR pushes updates only when data changes, making it O(changes) instead of O(clients × interval).

**Code Example:**
```csharp
// SignalR group per fleet
public class FleetHub : Hub
{
    public async Task JoinFleetGroup(int fleetId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"fleet_{fleetId}");
    }
}

// Server pushes changes to all group members
public class FleetService
{
    private readonly IHubContext<FleetHub> _hubContext;

    public async Task NotifyFleetUpdate(int fleetId, FleetUpdate update)
    {
        await _hubContext.Clients.Group($"fleet_{fleetId}")
            .SendAsync("FleetUpdated", update);
    }
}

// MAUI client receives updates
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
                if (update.Type == "VehicleStatus")
                {
                    var vehicle = Vehicles.FirstOrDefault(v => v.Id == update.VehicleId);
                    if (vehicle is not null)
                        vehicle.Status = update.NewStatus;
                }
            });
        });

        await _hub.StartAsync();
        await _hub.InvokeAsync("JoinFleetGroup", fleetId);
    }
}
```

---

**Q27: The app crashes on iOS when receiving 1000+ telemetry updates per second. How do you throttle?**

**Answer:**

**Theory:** 1000 updates/second overwhelms the UI thread — even if each update is lightweight, the context switching overhead (receiving → deserializing → dispatching → updating ObservableCollection) exceeds what the UI thread can handle. The solution is **backpressure** and **batching**: (1) Don't process every update individually. Use a `Channel<T>` (thread-safe producer/consumer queue) with `DropOldest` policy — if the queue is full, drop the oldest update and keep the latest. (2) Use a `IDispatcherTimer` that flushes the queue 4 times per second (250ms interval) — process all accumulated updates in ONE batch. (3) The UI thread sees at most 4 updates/second, not 1000. This is the **Observer pattern** with throttling. The trade-off is latency — updates are delayed by up to 250ms — but for EV telemetry, 250ms is acceptable (the driver won't notice a 250ms delay in battery percentage display).

**Code Example:**
```csharp
public class ThrottledTelemetryService
{
    private Channel<TelemetryData> _channel = Channel.CreateBounded<TelemetryData>(
        new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

    private readonly IDispatcherTimer _timer;

    public ThrottledTelemetryService()
    {
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(250);
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
            BatchReceived?.Invoke(this, batch);
    }

    public event EventHandler<List<TelemetryData>>? BatchReceived;
}
```

---

**Q28: How do you handle 10,000+ concurrent users hitting the login API?**

**Answer:**

**Theory:** Authentication is the most-hit endpoint in any app. 10,000 concurrent login attempts requires the API to be STATELESS and horizontally scalable. (1) **JWT** — stateless by design. No server-side session storage needed. Any server instance can validate any token. (2) **Horizontal scaling** — add more API server instances behind a load balancer. Each instance independently handles login. (3) **Redis caching** — hashing passwords (bcrypt) is CPU-intensive (designed to be slow). Cache the bcrypt hash result per user for 60 seconds — if the same user attempts login 10 times in a minute, only the first attempt does the bcrypt work. (4) **Rate limiting** — 5 attempts per minute per IP prevents brute force attacks. (5) **Connection pooling** — `Max Pool Size=200` in the database connection string prevents connection exhaustion.

**Code Example:**
```csharp
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

---

**Q29: How would you migrate from Xamarin.Forms to .NET MAUI for an existing EV fleet app?**

**Answer:**

**Theory:** Migration is a multi-step process that should be done incrementally, not as a "big bang" rewrite. The phases: (1) **Assessment** — inventory every NuGet package, custom renderer, platform-specific file, and Xamarin-specific API (`DependencyService`, `MessagingCenter`). (2) **Project conversion** — create a new MAUI project and copy files over. Update `.csproj` to use MAUI SDK, update `TargetFrameworks`. (3) **API migration** — replace `DependencyService.Get<T>()` with constructor DI. Replace `Xamarin.Forms` namespaces with `Microsoft.Maui`. (4) **Renderer migration** — convert `ViewRenderer` to `Handler` with mapper. (5) **XAML updates** — remove Xamarin-specific markup, add `x:DataType` for compiled bindings. (6) **Testing** — test EVERY screen on all platforms. The risk is highest in custom renderers and platform-specific code. Ship the MAUI version alongside the Xamarin version initially (side-by-side), migrate users gradually, and remove the Xamarin version when adoption reaches 95%.

**Code Example:**
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

**Theory:** Feature flags allow you to TOGGLE features on/off WITHOUT deploying a new app version. This is essential for: (1) **Gradual rollout** — enable the feature for 10% of users, monitor for crashes, ramp up to 100%. (2) **Kill switch** — if a bug is found in production, disable the feature instantly via a server-side flag toggle, no hotfix needed. (3) **A/B testing** — give different user groups different experiences and measure which performs better. The architecture: the API returns a dictionary of feature flags (feature name → boolean). The MAUI app loads this on startup and BEFORE every API call that might depend on a feature flag. The feature flags are cached in memory for the session (they rarely change mid-session). In XAML, a converter checks if a feature is enabled to show/hide UI elements. The fallback: if the server is unreachable, use a local default flags file bundled with the app.

**Code Example:**
```csharp
public class FeatureFlagService
{
    private Dictionary<string, bool> _flags = new();

    public async Task LoadFlagsAsync()
    {
        try
        {
            _flags = await _httpClient.GetFromJsonAsync<Dictionary<string, bool>>(
                $"{Constants.ApiBaseUrl}/api/features") ?? new Dictionary<string, bool>();
        }
        catch
        {
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
```

---

## 4. Managerial & Leadership

**Q31: You're leading a team of 3 developers on a MAUI app. How do you divide work?**

**Answer:**

**Theory:** Software project management is about maximizing THROUGHPUT while minimizing BLOCKERS. Feature-based assignment is superior to layer-based for MAUI apps: Dev A owns Fleet Dashboard (XAML + ViewModel + Service), Dev B owns Swap Stations, Dev C owns Profile/Settings. Each developer owns a VERTICAL SLICE — they see their feature from UI to database. This reduces handoff delays and context switching. **Contract-first** is essential: define the API contracts (Swagger/OpenAPI) BEFORE any coding starts. All developers agree on the API shape, then work independently against mock data. Daily standups (15 min) surface blockers. The bottleneck is usually the API team or designer — schedule around their availability. Branch strategy: feature branches from `develop`, PR → `develop` (with code review), release branches from `main`.

**Code Example:**
```
Sprint structure:
- Dev A: Fleet Dashboard (FEATURE: XAML + VM + SVC)
- Dev B: Station Management (FEATURE: XAML + VM + SVC)  
- Dev C: Shared Components (Auth, Navigation, HTTP) + User Profile
- All: Code review each other's PRs
- Weekly: Demo to stakeholders
```

---

**Q32: A junior developer submits a PR with no error handling, hardcoded URLs, and empty catch blocks. How do you handle the code review?**

**Answer:**

**Theory:** Code review is TEACHING, not policing. The approach: (1) **Be specific** — "This API call on line 42 can throw `HttpRequestException`. Let's wrap it in try-catch and show an alert." (2) **Explain WHY** — "Hardcoded URLs mean we rebuild the app to change the server. Let's move this to Constants.cs so it's configurable." (3) **Show the pattern** — provide the CORRECT code snippet in the review comment. (4) **Set standards** — create a CONTRIBUTING.md with non-negotiable rules (all API calls need error handling, no hardcoded values, no empty catch blocks). (5) **Follow up** — check the next PR. If the same issues appear, switch to pair programming until the patterns stick. The goal is not to "catch" mistakes but to BUILD the junior's skills. A well-handled code review builds trust; a poorly-handled one demoralizes the developer.

**Code Example:**
```markdown
# CONTRIBUTING.md — Non-negotiable Standards
1. All API calls must have try-catch with user-facing error messages
2. No hardcoded strings — use Constants.cs or appsettings.json
3. No empty catch blocks — log the exception 
4. All ViewModels must extend BaseViewModel
5. Code must compile with 0 warnings
```

---

**Q33: How do you estimate delivery time for a new MAUI feature?**

**Answer:**

**Theory:** Estimation is about RISK ASSESSMENT, not precise prediction. The technique: (1) **Break down** the feature into granular tasks (API contract, ViewModel, XAML, navigation, testing, deployment). (2) **T-shirt size** each task (Small=1-2d, Medium=3-5d, Large=1-2w). (3) **Reference historical data** — "Last time we added a list screen, it took 3 days." (4) **Identify risk factors** — third-party SDK, new platform API, team member on leave. (5) **Apply PERT** — `(Optimistic + 4×MostLikely + Pessimistic) / 6`. (6) **Add buffer** — 20% for unknowns, 50% if new technology. (7) **Communicate confidence** — "70% confidence for 2 weeks. The Bluetooth SDK is the risk — if it has issues, add 3-5 days." Never give a single number; always give a range. When a manager asks "when will it be done?", respond with "it depends on X, Y, and Z — here are the scenarios."

**Code Example:**
```
Task breakdown for "Add Station List Screen":
- API contract definition: 0.5d
- StationService + IStationService: 1d
- StationViewModel with commands: 1.5d
- StationPage XAML with CollectionView: 1.5d
- Navigation integration: 0.5d
- Unit tests: 1d
- UI tests: 0.5d
TOTAL: 6.5d → with 20% buffer → 8d (1.6 weeks)
```

---

**Q34: The client wants to add a feature that requires a database schema change. How do you manage the rollout?**

**Answer:**

**Theory:** Database schema changes are HIGH RISK because OLD app versions can't handle NEW columns (crash on deserialization). The safe rollout sequence: (1) **Add columns as NULLABLE** — old app ignores unknown columns, new app uses them. (2) **Update API** — new endpoints use new columns, old endpoints still work. (3) **Deploy API first** — server-side changes only, no client impact. (4) **Deploy MAUI update** — submit to app stores. (5) **Wait for adoption** — monitor app version distribution, wait until 80%+ users are on new version. (6) **Make columns non-nullable** — only after old clients are gone. This process takes 2-4 weeks. If the feature needs non-nullable columns from day one, you need a SERVICE GATEWAY pattern: the API detects the app version and serves appropriate responses. Never change the database before the API and client are ready.

**Code Example:**
```csharp
// Migration: Add column with nullable first (old app still works)
migrationBuilder.AddColumn<string>(
    name: "SerialNumber",
    table: "Batteries",
    type: "nvarchar(100)",
    nullable: true);

// After all clients updated:
migrationBuilder.AlterColumn<string>(
    name: "SerialNumber",
    table: "Batteries",
    nullable: false);
```

---

**Q35: A production bug causes crashes for 10% of users. Walk through your incident response.**

**Answer:**

**Theory:** Incident response follows the **Triage → Mitigate → Root Cause → Fix → Post-mortem** cycle. (1) **Triage** (0-15 min) — identify affected version, platform, OS version, and user segment. Check crash logs in App Center/Sentry. (2) **Mitigate** (15-60 min) — the PRIORITY is stopping the damage. If the feature has a flag, DISABLE it for all users. If not, prepare a hotfix. (3) **Root cause** (1-4 hrs) — reproduce the crash, identify the exact code path. Common causes: null reference, platform API version mismatch, threading issue. (4) **Fix** — write the fix with a regression test that reproduces the crash. (5) **Deploy** — hotfix → internal test track → 10% rollout → 100% rollout after verification. (6) **Post-mortem** (within 24 hrs) — document: What happened? Why wasn't it caught? How do we prevent recurrence? The post-mortem should be BLAMELESS — focus on process improvements, not individual mistakes.

**Code Example:**
```
Hotfix checklist:
1. git checkout -b hotfix/crash-on-null-telemetry
2. Fix the bug + add regression test
3. dotnet test
4. git commit -m "fix: crash when telemetry data is null"
5. git push && create PR
6. Merge to main, tag release
7. Build and deploy
```

---

**Q36: You're interviewing a candidate for a MAUI developer role. What 3 questions do you ask?**

**Answer:**

**Theory:** Good interview questions test (1) PRACTICAL knowledge (can they build?), (2) DEBUGGING skill (can they fix?), and (3) ARCHITECTURE thinking (can they design?). Avoid trivia questions ("What's the difference between `virtual` and `abstract`?" — Google answers that). Instead, ask: (1) **Practical MVVM** — "Walk me through implementing a login screen from XAML to ViewModel to API call." This tests end-to-end understanding. (2) **Debugging** — "CollectionView is smooth on Android but stutters on iOS. How do you diagnose?" This tests platform-specific awareness. (3) **Architecture** — "Design a system to show 500 EVs on a map in real-time." This tests SignalR, clustering, viewport filtering. The best signal is when the candidate admits what they DON'T know and explains HOW they'd figure it out.

**Code Example:**
```
3 Questions for MAUI Interviews:
1. "Walk through building a login screen — XAML → ViewModel → Service → API → response."
2. "CollectionView lags on iOS but not Android. How do you find and fix it?"
3. "Design real-time tracking for 500 EVs on a map. What technologies and patterns?"
```

---

**Q37: How do you ensure code quality across a team of MAUI developers?**

**Answer:**

**Theory:** Code quality is a SYSTEM, not a person. Automate what you can, review what you can't. (1) **StyleCop/.editorconfig** — enforce naming, formatting, and file structure automatically. (2) **SonarQube** — static analysis for bugs, security issues, code smells. (3) **PR gate** — build + test + lint must pass before merge. (4) **Architecture tests** — automated tests that verify ViewModels don't reference Views, services don't depend on ViewModels, etc. These are written with NetArchTest. (5) **Test coverage gate** — 70%+ on ViewModels and Services, no coverage drop on new code. (6) **Pair programming** — complex features (offline sync, real-time telemetry) are developed in pairs. (7) **Knowledge sharing** — monthly sessions where a developer presents a feature they built. The goal is CONSISTENCY — every developer follows the same patterns so any developer can work on any part of the codebase.

**Code Example:**
```csharp
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

**Theory:** Blocked developers are the #1 productivity killer in mobile development. The solution is **parallel workstreams** enabled by mock services. Define the API contracts FIRST (Swagger/OpenAPI), generate a mock server, and let the mobile team code against the mock. The actual API team builds the real implementation. Integration happens when both are ready. This is the **Contract-First** approach: (1) API team publishes the OpenAPI spec. (2) Mobile team generates a `MockApiService` that returns realistic data matching the spec. (3) DI switches between mock and real: `#if DEBUG` → `MockApiService`, `#else` → `RealApiService`. (4) Both teams work in parallel without blocking each other. The sprint should reserve 20% capacity for integration testing when both sides are ready.

**Code Example:**
```csharp
#if DEBUG
builder.Services.AddSingleton<IApiService, MockApiService>();
#else
builder.Services.AddSingleton<IApiService, ApiService>();
#endif
```

---

**Q39: How do you prioritize technical debt vs new features?**

**Answer:**

**Theory:** Technical debt is like financial debt — some debt is productive (bought a house), some is destructive (credit card interest). Prioritize debt by BUSINESS IMPACT. A matrix approach: (1) **Critical** — missing error handling causes crashes (fix NOW). (2) **High** — monolithic API slows every sprint (dedicate 20% of each sprint). (3) **Medium** — no tests causes regressions (fix during feature work — boy scout rule). (4) **Low** — hardcoded strings, minor code smells (fix when touching the area). The 80/20 rule: 20% of the debt causes 80% of the pain. Target those 20%. The **boy scout rule** applies: "Leave the code cleaner than you found it." Every PR should include some refactoring of the area being modified. Additionally, dedicate every 4th sprint to tech debt and infrastructure — this prevents debt from accumulating to crisis levels.

**Code Example:**
```
Business Impact Matrix:
| Debt Type            | User Impact | Dev Velocity Impact | Priority |
|----------------------|-------------|-------------------|----------|
| Missing error handling | Crashes    | Low               | Critical |
| Monolithic API        | None        | Slowing every sprint | High  |
| No tests              | Regressions | Slow releases     | High    |
| Hardcoded strings     | None        | Low               | Low     |
```

---

**Q40: Your app needs to support both Android and iOS. The client wants to ship in 4 weeks but you estimate 8. How do you handle this?**

**Answer:**

**Theory:** This is a **scope vs time** negotiation. You can't change the laws of physics — 8 weeks of work takes 8 weeks. But you can change WHAT is delivered in 4 weeks. Options: (1) **Reduce scope** — ship an MVP with login, read-only dashboard, and 1 list screen. No offline, no push notifications, no tablet layout. (2) **Single platform first** — ship Android-only (80% of users) in 4 weeks, iOS in week 6. (3) **Phased delivery** — week 4: core features, week 6: secondary features, week 8: complete. (4) **Add resources** — with 2 more developers, you might hit 6 weeks. (5) **Accept delay** — explain the risks: "Ship in 4 weeks with full scope would require cutting testing and quality, resulting in crashes and bad reviews." Always present MULTIPLE options with trade-offs. Let the client decide which trade-off they prefer. Never say "no" — say "yes, but here's what it costs."

**Code Example:**
```
4-week MVP scope:
✅ Login/Registration
✅ Dashboard (read-only, no real-time)
✅ Station List (read-only)
❌ Offline mode
❌ Push notifications
❌ Tablet layout
❌ Swap Request (write)
```

---

## 5. CI/CD, Cloud & DevOps

**Q41: Design a CI/CD pipeline for a MAUI app targeting Android, iOS, and Windows.**

**Answer:**

**Theory:** CI/CD for MAUI must handle platform-specific build requirements: (1) **Android** — builds on Windows or Linux, needs Android SDK + keystore signing. (2) **iOS** — ONLY builds on macOS (Apple restriction), needs Apple Developer account + provisioning profiles. (3) **Windows** — builds on Windows, needs signing certificate for MSIX. A matrix strategy runs builds in parallel on different OS agents. The pipeline stages: (1) **Restore** — `dotnet restore`. (2) **Build** — `dotnet build -f net10.0-{platform}`. (3) **Test** — `dotnet test` (runs on one platform, tests ViewModels and Services). (4) **Publish** — `dotnet publish -f net10.0-{platform} -c Release`. (5) **Sign** — code signing with platform-specific certificates. (6) **Upload** — distribute to App Center for testing, then to stores for release. Git tags trigger the release pipeline; PRs trigger only the build + test pipeline.

**Code Example:**
```yaml
jobs:
  build:
    strategy:
      matrix:
        target: [android, ios, windows]
    runs-on: ${{ matrix.target == 'ios' && 'macos-14' || 'windows-2025' }}
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet restore
      - run: dotnet build -f net10.0-${{ matrix.target }} -c Release
      - run: dotnet test
      - run: dotnet publish -f net10.0-${{ matrix.target }} -c Release
```

---

**Q42: How do you manage app store deployment for both Play Store and App Store?**

**Answer:**

**Theory:** App store deployment is the FINAL mile of mobile development, and each store has different rules. **Play Store** (Android): faster review (2-24 hours), allows staged rollouts (10% → 50% → 100%), instant rollback. No review for updates that don't change permissions. **App Store** (iOS): slower review (24-48 hours), requires Xcode build + notarization, staged rollouts via TestFlight (external testers), rollback requires a new submission (24-48 hour delay). The deployment process: (1) Internal Test Track (developers + QA) — instant, no review. (2) Closed Track (beta testers) — hours. (3) Open Track (production) — reviewed. Always submit to BOTH stores on the SAME day to maintain parity. Use App Center or fastlane to automate the upload process. Store credentials (API keys, certificates) must be stored securely in CI/CD secrets.

**Code Example:**
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

**Theory:** Without the Microsoft Store, Windows app updates must be SELF-MANAGED. The pattern: (1) The app checks for updates on startup (or periodically): `GET /api/app/update` returns the latest version and download URL. (2) If a newer version exists, download the MSIX installer to the cache directory. (3) Launch the installer with admin privileges (`runas` verb) — MSIX requires admin for installation. (4) The app QUITS immediately after launching the installer. (5) The installer runs, replaces the app files, and launches the new version. The user experience should include: a progress bar during download, a "Update Available" notification (not forced, user can postpone), and background download (app continues working while update downloads). Forced updates (security patches) should display a mandatory dialog with no postpone option. The update URL should point to a CDN (Azure CDN, CloudFront) to handle large download loads.

**Code Example:**
```csharp
public async Task CheckForUpdatesAsync()
{
    var updateInfo = await _api.GetAsync<UpdateInfo>("/api/app/update");
    if (updateInfo is null) return;

    var currentVersion = Version.Parse(AppInfo.Current.VersionString);
    if (Version.Parse(updateInfo.LatestVersion) <= currentVersion) return;

    var installerPath = Path.Combine(FileSystem.CacheDirectory, "EVSwap.Update.msix");
    using var client = new HttpClient();
    var bytes = await client.GetByteArrayAsync(updateInfo.DownloadUrl);
    await File.WriteAllBytesAsync(installerPath, bytes);

    Process.Start(new ProcessStartInfo
    {
        FileName = installerPath,
        UseShellExecute = true,
        Verb = "runas"
    });
    Application.Current?.Quit();
}
```

---

**Q44: How do you integrate Azure services with the MAUI app?**

**Answer:**

**Theory:** Azure provides a comprehensive cloud platform for MAUI apps. The key services: (1) **App Center** — crash reporting, analytics, distribution (beta testing). Must be initialized on app startup with platform-specific keys. (2) **SignalR Service** — managed WebSocket for real-time telemetry. Higher scale than self-hosted SignalR, no server management. (3) **Blob Storage** — firmware updates, user avatars, report files. Generate SAS tokens for secure, time-limited access (don't expose storage keys in the app). (4) **App Configuration** — feature flags and configuration values managed in Azure, served via REST. (5) **Cosmos DB** — NoSQL database for high-volume telemetry data. (6) **Functions** — serverless compute for background processing (alert evaluation, report generation). The MAUI app doesn't connect directly to most Azure services — it goes through the API server, which is hosted on Azure. Only App Center (SDK) and SignalR (WebSocket) have direct client connections.

**Code Example:**
```csharp
// Azure App Center — crash reporting & analytics
AppCenter.Start("android=<key>;ios=<key>",
    typeof(Analytics), typeof(Crashes));

// Azure SignalR Service — real-time telemetry
builder.Services.AddSingleton(_ =>
    new HubConnectionBuilder()
        .WithUrl("https://evswap-signalr.azurewebsites.net/hubs/telemetry")
        .WithAutomaticReconnect()
        .Build());

// Azure Blob Storage — firmware & asset downloads
public async Task<string> GetFirmwareDownloadUrlAsync(string fileName)
{
    var container = new BlobContainerClient(
        "DefaultEndpointsProtocol=https;AccountName=evswap",
        "firmware");
    var blob = container.GetBlobClient(fileName);
    return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1))
        .ToString();
}
```

---

**Q45: How do you monitor app health and errors in production?**

**Answer:**

**Theory:** Production monitoring has three pillars: (1) **Crash reporting** — App Center/Sentry automatically captures unhandled exceptions with stack traces, device info, and app version. This is passive monitoring (user crashes → you get notified). (2) **Custom events** — track business metrics explicitly: "LoginSuccess", "SwapRequested", "DashboardLoaded". This is active monitoring (you define what matters). Track duration for API calls to detect performance regression. (3) **Health endpoint** — the API exposes `GET /health` that checks database connectivity, cache connectivity, and dependency health. A load balancer uses this to remove unhealthy instances. For the MAUI app specifically, monitor: app startup time (regression indicates a problem), API call success rate, and memory usage (leak detection). Set up ALERTS for anomalies: crash rate > 0.1%, API success rate < 99.5%, startup time > 5 seconds.

**Code Example:**
```csharp
public class TelemetryLogger
{
    public void TrackEvent(string eventName, Dictionary<string, string>? properties = null)
    {
        Analytics.TrackEvent(eventName, properties);
    }

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
}
```

---

## 6. IoT & Enterprise MAUI

**Q46: How do you handle Bluetooth Low Energy (BLE) communication for EV diagnostics?**

**Answer:**

**Theory:** BLE allows the MAUI app to communicate DIRECTLY with the vehicle's onboard diagnostics system without internet connectivity. The use case: a driver walks up to the vehicle, the app connects via BLE, reads battery health, motor temperature, and tire pressure — all while the vehicle is offline. The BLE communication pattern: (1) **Scan** — discover nearby BLE devices filtering by name or service UUID. (2) **Connect** — establish a GATT connection. (3) **Discover services** — the vehicle exposes services (Battery Service `0x180F`, Vehicle Diagnostics Service). (4) **Read characteristics** — read data from specific characteristics (Battery Level `0x2A19`). (5) **Subscribe to notifications** — receive real-time updates when data changes. Challenges: BLE range (~10m), pairing requirements, Android/iOS permission differences (Android requires location permission for BLE scanning), and battery drain. The `Plugin.BLE` NuGet package provides a cross-platform API.

**Code Example:**
```csharp
public async Task ConnectToVehicleAsync(string vehicleId)
{
    var ble = CrossBluetoothLE.Current;
    var adapter = ble.Adapter;

    adapter.DeviceDiscovered += (s, args) =>
    {
        if (args.Device.Name?.Contains(vehicleId) == true)
        {
            adapter.StopScanning();
        }
    };

    await adapter.StartScanningForDevicesAsync();
    // Connect and read diagnostic data
}
```

---

**Q47: How do you manage MAUI app configuration for different environments (dev, staging, production)?**

**Answer:**

**Theory:** Configuration management ensures the app connects to the RIGHT server with the RIGHT settings for each environment. The approach: (1) **appsettings.json** — base configuration shared across all environments. (2) **appsettings.{Environment}.json** — environment-specific overrides (Development/Staging/Production). (3) **Conditional loading** — `#if DEBUG` loads Development settings. (4) **IConfiguration** — access configuration via the standard .NET configuration abstraction. The key settings to manage: API base URL, timeout, log level, feature flags, and third-party API keys. NEVER hardcode environment-specific values. For CI/CD, use environment variables or Azure App Configuration to inject settings during build. The `appsettings.json` files are bundled with the app but API URLs should point to a configurable endpoint so the server can redirect the client. Mobile apps can't change configuration after deployment without an app update — use feature flags for runtime configuration changes.

**Code Example:**
```csharp
// appsettings.Development.json
{ "Api": { "BaseUrl": "http://localhost:5238", "TimeoutSeconds": 60 } }

// appsettings.Production.json
{ "Api": { "BaseUrl": "https://api.evswap.datakrew.com", "TimeoutSeconds": 15 } }

// MauiProgram.cs
var config = new ConfigurationBuilder()
    .AddJsonStream(await FileSystem.OpenAppPackageFileAsync("appsettings.json"))
#if DEBUG
    .AddJsonStream(await FileSystem.OpenAppPackageFileAsync("appsettings.Development.json"))
#endif
    .Build();
```

---

**Q48: How do you implement QR code scanning for vehicle identification?**

**Answer:**

**Theory:** QR code scanning allows a driver to identify a vehicle by scanning its VIN QR code sticker — faster than typing a 17-character VIN. The implementation uses the device camera to detect and decode QR codes in real-time. The `ZXing.Net.Maui` NuGet package provides a `CameraBarcodeReaderView` control that overlays a live camera feed with barcode detection. The flow: (1) Navigate to the QR scanning page. (2) Camera activates, shows a viewfinder. (3) When a QR code is detected, the `BarcodesDetected` event fires with the decoded value. (4) The app looks up the vehicle by the scanned QR code value. (5) Navigate to the vehicle detail page. Permissions required: `CAMERA` on Android (must be declared in `AndroidManifest.xml`) and `NSCameraUsageDescription` on iOS. Test with various lighting conditions, angles, and QR code sizes.

**Code Example:**
```csharp
public async Task<string?> ScanVehicleQrAsync()
{
    var options = new CameraBarcodeReaderOptions
    {
        Formats = BarcodeFormat.QrCode,
        AutoRotate = true,
        Multiple = false
    };
    var result = await BarcodeReader.Default.ReadAsync(options);
    return result?.Value;  // returns vehicle VIN or ID
}
```

---

**Q49: How do you implement report generation (PDF) in the MAUI app?**

**Answer:**

**Theory:** PDF reports serve as official records (fleet performance reports, maintenance history, trip summaries). Two approaches: (1) **Server-side PDF** — the API generates the PDF using a library like QuestPDF or DinkToPdf, returns the byte array. This is simpler (no PDF library needed on the client) and ensures consistent formatting. The MAUI app downloads the bytes, saves to cache, and opens with the system PDF viewer. (2) **Client-side PDF** — the MAUI app generates the PDF locally using QuestPDF (open source, MIT license). Useful when offline PDF generation is needed. After generation, use `Share.Default.RequestAsync()` to share via email, messaging, or any system share sheet. For security reports, consider adding a password to the PDF.

**Code Example:**
```csharp
public async Task<string> GenerateFleetReportAsync(int fleetId)
{
    var pdfBytes = await _api.GetAsync<byte[]>($"/api/report/fleet/{fleetId}/pdf");
    var path = Path.Combine(FileSystem.CacheDirectory, $"fleet_{fleetId}_report.pdf");
    await File.WriteAllBytesAsync(path, pdfBytes);
    return path;
}

public async Task ShareReportAsync(string filePath)
{
    await Share.Default.RequestAsync(new ShareFileRequest
    {
        Title = "Fleet Report",
        File = new ShareFile(filePath)
    });
}
```

---

**Q50: How do you handle driver shift management and authentication in the app?**

**Answer:**

**Theory:** Shift management is a CRITICAL feature for EV fleet apps — it ensures only authorized drivers operate vehicles, tracks hours for compliance (DOT regulations), and handles vehicle check-in/check-out. The pattern: (1) **Shift start** — driver authenticates, scans vehicle QR, records odometer reading (from BLE or manual entry). The API records the shift start, updates vehicle status to "In Use". (2) **Active shift** — the app periodically (every 5 min) sends a heartbeat with GPS location. This handles compliance tracking and alerts if a driver goes off-route. (3) **Shift end** — driver records end odometer, the API calculates distance driven, updates vehicle status to "Available". If the driver forgets to end a shift (closes app), a background job auto-ends shifts after 2 hours of inactivity. The shift is stored in `Preferences` on the device so the app can resume state if the app is killed.

**Code Example:**
```csharp
public async Task<bool> StartShiftAsync()
{
    var user = _auth.CurrentUser;
    if (user is null) return false;

    var shift = await _api.PostAsync<ShiftRecord>("/api/shift/start", new
    {
        UserId = user.Id,
        VehicleId = user.AssignedVehicleId,
        StartedAt = DateTime.UtcNow,
        OdometerStart = await ReadOdometerAsync()
    });

    if (shift is not null)
    {
        Preferences.Default.Set("active_shift_id", shift.Id);
        Preferences.Default.Set("shift_started_at", DateTime.UtcNow.ToString("O"));
        return true;
    }
    return false;
}
```

---

## 7. Architecture & System Design

**Q51: Design a system where 10,000 EVs send telemetry every 5 seconds. How does the backend and app handle this?**

**Answer:**

**Theory:** 10,000 EVs × 12 telemetry messages/minute = 120,000 messages/minute = 2,000/second. This is a HIGH-VOLUME data pipeline that requires specialized infrastructure. HTTP REST CANNOT handle this volume efficiently (too much overhead per request). The correct architecture: (1) **MQTT** — lightweight pub/sub protocol designed for IoT. Each message has ~2 bytes of overhead vs HTTP's ~800 bytes. (2) **IoT Hub/Event Hub** — managed service that ingests millions of messages/second. Buffers data, handles spikes, scales automatically. (3) **Stream processor** — Azure Stream Analytics or Kafka processes the stream in real-time: filtering, aggregation, anomaly detection. (4) **Time-series database** — InfluxDB or TimescaleDB optimized for append-heavy telemetry data. Standard SQL databases struggle with 2,000 inserts/second. (5) **SignalR** — pushes processed data to MAUI clients. Only meaningful aggregates and alerts reach the app, not raw telemetry. The MAUI app NEVER connects to the telemetry pipeline directly — it connects through the API and SignalR hub, which provide a human-readable view of the processed data.

**Code Example:**
```
Data Pipeline:
[10,000 EVs] → MQTT → IoT Hub → Stream Analytics → InfluxDB → API → SignalR → MAUI App
                   ↓
              Alert Engine (if battery > 95°C → notify fleet manager)
```

---

**Q52: How would you refactor a monolithic MAUI app into a modular architecture?**

**Answer:**

**Theory:** Modular architecture splits the app into feature-specific class library projects. Each module has its OWN pages, ViewModels, services, and models. The main app project is just a SHELL that composes modules. Benefits: (1) **Separation of concerns** — Fleet Module can't accidentally reference Swap Module's internals. (2) **Independent development** — teams own modules, fewer merge conflicts. (3) **Lazy loading** — modules are loaded on demand (startup loads only the auth module, fleet module loads when the fleet manager logs in). (4) **Reusability** — Auth Module can be reused across MAUI apps. The approach: (1) Extract shared code into `Core` project (BaseViewModel, ApiService, AuthService). (2) Extract feature code into module projects (`FleetModule`, `SwapModule`). (3) Each module has a registration extension method (`builder.AddFleetModule()`). (4) The main app calls these extensions. The challenge is managing cross-module dependencies (Fleet Module needs to navigate to Swap Module).

**Code Example:**
```csharp
// Module registration
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

// Main app composes modules
builder.AddFleetModule();
builder.AddSwapModule();
builder.AddAuthModule();
```

---

**Q53: How do you handle database migrations when the MAUI app uses local SQLite?**

**Answer:**

**Theory:** Local SQLite migrations are different from server-side EF Core migrations because you can't run `dotnet ef database update` on the user's device. Instead, you need a **programmatic migration runner** that checks the current schema version and applies pending migrations sequentially. The approach: (1) Use `PRAGMA user_version` to store the current schema version in the SQLite file. (2) On app startup, read the version. (3) If version < latest, apply each pending migration in order (ALTER TABLE ADD COLUMN, CREATE INDEX, etc.). (4) Update `user_version` after each migration. Key rules: (a) Never rename or delete columns (old code might reference them). (b) Only ADD columns as nullable. (c) Test migration FROM every previous version — users might skip versions. (d) Handle migration failure gracefully — show an error and offer to restore from backup. SQLite has limited ALTER TABLE support (can't drop columns), so forward-planning is essential.

**Code Example:**
```csharp
public async Task MigrateAsync()
{
    var currentVersion = await _db.ExecuteScalarAsync<int>("PRAGMA user_version");
    if (currentVersion < 1) await _db.ExecuteAsync("CREATE TABLE Vehicles (Id INT, Name TEXT)");
    if (currentVersion < 2) await _db.ExecuteAsync("ALTER TABLE Vehicles ADD COLUMN FirmwareVersion TEXT DEFAULT ''");
    if (currentVersion < 3) await _db.ExecuteAsync("CREATE INDEX idx_vehicles_name ON Vehicles(Name)");
    await _db.ExecuteAsync($"PRAGMA user_version = 3");
}
```

---

**Q54: Design an offline-first architecture for the fleet management app.**

**Answer:**

**Theory:** Offline-first means the LOCAL database is the SOURCE OF TRUTH, not the server. The app works completely offline, and syncs changes to the server when connectivity is available. The architecture has three layers: (1) **Local DB** (SQLite) — stores cached data AND pending writes. (2) **Sync Service** — background service that manages the synchronization. (3) **Conflict Resolver** — handles conflicts when the same record was modified on both client and server. The data flow: **Read** → check local cache → return immediately (sub-millisecond). Trigger background refresh → if online, fetch latest from API → update cache → UI updates automatically via binding. **Write** → save to local DB instantly → show success to user → queue sync request → when online, send to API. This gives users INSTANT feedback (no loading spinners for writes) and works completely offline. The trade-off is complexity in conflict resolution: if a driver updates their profile both on the app (offline) and on the web, which version wins?

**Code Example:**
```
┌─────────────────────────────────────┐
│  ViewModel → DataService → Local DB │ (instant, always works)
│                 ↓                    │
│              Sync Service → API     │ (background, when online)
└─────────────────────────────────────┘
```

---

**Q55: How would you implement role-based access control (RBAC) in the MAUI app?**

**Answer:**

**Theory:** RBAC restricts what users can SEE and DO based on their role. The server stores the role (Admin, FleetManager, Driver, Finance), and the MAUI app enforces UI-level restrictions. The approach: (1) **Server-side enforcement** — the API checks permissions on EVERY request. A driver can't delete vehicles even if they try to call the API directly. (2) **Client-side enforcement** — the app reads the user's roles from the JWT claims and hides/shows UI elements accordingly. This is PURELY UX — a malicious user could bypass client checks, which is why server-side enforcement is mandatory. (3) **Feature-based permissions** — define permissions per feature, not per role. "Feature is a dictionary of permissions: `{ "canDeleteVehicle": false, "canAssignDriver": true }`. Roles map to permission sets: Admin has all permissions, Driver has view-only. (4) **UI patterns** — disable buttons (user sees them but can't click) vs hide buttons (user doesn't know the feature exists). For security-sensitive apps, hide admin features from non-admin users.

**Code Example:**
```csharp
public bool CanAccess(string feature)
{
    var roles = _auth.CurrentUser?.Roles ?? new List<string>();
    return feature switch
    {
        "FleetDashboard" => roles.Contains("FleetManager") || roles.Contains("Admin"),
        "DriverShift" => roles.Contains("Driver"),
        "BillingReports" => roles.Contains("Finance") || roles.Contains("Admin"),
        "VehicleControl" => roles.Contains("Admin"),
        _ => false
    };
}
```

---

**Q56: Design a notification system that works across push, in-app, and email channels.**

**Answer:**

**Theory:** A unified notification system delivers the RIGHT message through the RIGHT channel based on priority and user preference. The architecture: (1) **Notification service** (server) — evaluates alert rules, creates notification records, routes to appropriate channels. (2) **Delivery channels** — Push (FCM/APNS) for critical/urgent, In-App (notification center + toast banner) for standard, Email for non-urgent digests. (3) **Priority-based routing** — CRITICAL (overheating, fire risk) → ALL channels + bypass silent mode. HIGH (maintenance due, low battery) → Push + In-App. INFO (weekly report ready) → In-App only. (4) **MAUI client** — maintains a notification center (list of all notifications), shows toasts for high priority, and updates badge count. (5) **Preferences** — users can opt out of channels per notification type (e.g., receive critical push but not promotional emails). The challenge is DEDUPLICATION: if the same alert is sent via push AND in-app, the in-app should suppress the toast if the user already swiped the push notification.

**Code Example:**
```csharp
public async Task SendNotificationAsync(Notification notif)
{
    switch (notif.Priority)
    {
        case AlertPriority.Critical:
            await SendPushAsync(notif);         // OS push
            await ShowInAppAlertAsync(notif);    // Banner overlay
            await SendEmailAsync(notif);         // Email
            break;
        case AlertPriority.High:
            await SendPushAsync(notif);
            await ShowInAppBadgeAsync(notif);    // Badge only
            break;
        case AlertPriority.Info:
            // In-app notification center only
            break;
    }
}
```

---

## 8. Behavioral & Cultural Fit

**Q57: Tell me about a time you had to debug a difficult production issue in a mobile app.**

**Answer:**

**Theory:** Behavioral questions use the **STAR** method (Situation, Task, Action, Result). The interviewer wants to see your PROBLEM-SOLVING PROCESS, not just the solution. Structure your answer: (1) **Situation** — set the context. "Users reported crashes on Android after the latest update." (2) **Task** — what needed to be done. "Identify the root cause and ship a fix within 24 hours." (3) **Action** — what YOU did. "Checked App Center → reproduced on Pixel 6 → identified null `opening_hours` → added null check → added unit test → shipped hotfix." (4) **Result** — measurable outcome. "Hotfix shipped in 6 hours, zero crashes after. Added nullable reference type analysis to prevent recurrence." The key: focus on YOUR specific actions (use "I" not "we"), show systematic thinking, and include what you LEARNED and how you PREVENTED it from recurring.

**Code Example:**
```
STAR Response Structure:
- Situation: Users crashing on Android 14 after update
- Task: Find root cause, ship fix in 24 hours
- Action: Checked App Center crash logs → found NullReferenceException
         Reproduced on Pixel 6 → confirmed null opening_hours field
         Fixed: added null check before .Split(',') call
         Prevention: added nullable reference type analysis to CI pipeline
- Result: Hotfix in 6 hours, zero recurrence, team adopted nullable analysis
```

---

**Q58: How do you handle disagreements with a backend developer about API contract design?**

**Answer:**

**Theory:** The interviewer wants to see CONFLICT RESOLUTION skills. The approach: (1) **Understand their perspective** — "You're optimizing for server performance; I'm optimizing for mobile UX. Let's find the middle ground." (2) **Use data** — "10 separate API calls take 8 seconds to load the dashboard. A single batched endpoint takes 1.5 seconds. Here are the profiler results." (3) **Propose options** — "Option A: single dashboard endpoint (1 call, 1.5s). Option B: three endpoint (3 calls, 3s). Option C: current (10 calls, 8s)." (4) **Find the compromise** — "Let's start with Option A. If server performance becomes an issue, we can split it." (5) **Escalate last** — if it's blocking the release, involve the tech lead. (6) **Document** — write down the decision and rationale for future reference. The key principles: focus on DATA, not opinions; offer TRADE-OFFS, not ultimatums; document DECISIONS for future reference.

**Code Example:**
```
Resolution approach:
1. "I understand your concern about server load."
2. "Here's the mobile impact: 10 calls = 8s load time."
3. "What if we batch into 3 calls instead of 10?"
4. "If we need to split later, we can — let's start batched."
5. Document: "Decision: /api/dashboard is a single batched endpoint."
```

---

**Q59: Describe a project where you had to learn a new technology quickly. How did you approach it?**

**Answer:**

**Theory:** This tests ADAPTABILITY and LEARNING STRATEGY. Interviewers want to see: (1) **Self-directed learning** — you didn't wait for a training course. (2) **Systematic approach** — you had a plan. (3) **Practical application** — you built something, not just read. Structure: (1) **Situation** — "Client needed a MAUI app but I had Xamarin experience." (2) **Action** — "Read Microsoft's migration guide (1 day) → built a prototype login→list→detail app (3 days) → joined MAUI Discord for community help → had a senior dev review my first PR." (3) **Result** — "Delivered on time. By sprint 2, velocity matched Xamarin." Show that you can ACQUIRE new skills independently and deliver value even while learning. Mention SPECIFIC resources you used (documentation, GitHub issues, community forums) — this shows you know HOW to learn.

**Code Example:**
```
Learning roadmap for MAUI:
Day 1: Microsoft MAUI migration guide + release notes
Day 2-4: Built prototype (login → list → detail)
Day 5: Community Discord for specific questions
Day 6: First PR with senior review
Week 2-3: Feature development at full speed
```

---

**Q60: Why do you want to work at Datakrew specifically?**

**Answer:**

**Theory:** This tests CULTURAL FIT and MOTIVATION. A good answer connects YOUR skills to Datakrew's MISSION and TECHNICAL CHALLENGES. Structure: (1) **Mission alignment** — "I'm passionate about using technology for sustainability. EV adoption is critical for the planet, and Datakrew enables EV fleets to operate efficiently." (2) **Technical challenge** — "Serving 1 million EVs in 5 years means solving hard problems: real-time telemetry at scale, offline-first architecture, IoT integration. These excite me." (3) **Your contribution** — "My MAUI experience (4-6 years) directly applies to building the OXRED mobile interface that fleet managers will use daily." (4) **Growth** — "I want to work on problems that matter, with a team that's pushing the boundaries of EV technology." Be SPECIFIC to Datakrew — generic answers ("I want to work at a great company") are weak. Research the company beforehand and reference their products and vision.

**Code Example:**
```
"I want to work at Datakrew because:
1. Your mission — 1M EVs in 5 years — is ambitious and impactful.
2. OXRED Platform Suite solves real problems for fleet operators.
3. The technical challenges (real-time telemetry, offline-first, scale) match my expertise.
4. My 4+ years of MAUI/Xamarin experience directly contributes to your product.
5. I want to build software that touches a billion lives through cleaner transportation."
```

---

> **Quick Tips for Datakrew Interview:**
> 
> 1. **Know the numbers** — 1 million EVs target, OXRED Platform Suite, IoT/AI focus.
> 2. **Emphasize scale** — Every answer should consider "how does this work for 10K+ vehicles / 1M+ users?"
> 3. **Show real-time awareness** — EV telemetry is all about real-time. Mention SignalR, WebSocket, MQTT.
> 4. **Offline-first mindset** — Fleet drivers may have poor connectivity. Offline support is critical.
> 5. **Security consciousness** — Vehicle control from a phone requires rigorous security. Mention biometrics, PIN, remote wipe.
> 6. **Performance obsession** — Battery efficiency, memory usage, startup time — EV app should be as efficient as the vehicles it monitors.
