# 50+ Generic .NET MAUI Interview Questions — Crack Any Interview

> **Not project-specific.** These are standalone questions covering C#, .NET MAUI, MVVM, DI, async, REST, EF Core, design patterns, and more. Master these and you can handle any .NET MAUI interview.

---

## C# Language & .NET Runtime

**Q1: Explain the difference between `value types` and `reference types` in C#.**

**Answer:**

| Feature | Value Type | Reference Type |
|---------|-----------|---------------|
| Memory | Stack | Heap |
| Assignment | Copies the value | Copies the reference |
| Examples | `int`, `bool`, `struct`, `enum` | `class`, `string`, `array`, `delegate` |
| Nullable | Usually non-nullable (unless `int?`) | Always nullable |
| Performance | Fast for small data | Slightly slower (heap allocation + GC) |

```csharp
public struct Point { public int X; public int Y; }
public class Person { public string Name; }

Point a = new Point { X = 1, Y = 2 };
Point b = a; b.X = 99; // a.X is still 1 (copy)

Person p1 = new Person { Name = "Alice" };
Person p2 = p1; p2.Name = "Bob"; // p1.Name is now "Bob" (same object)
```

---

**Q2: What is boxing and unboxing? Why should you avoid it?**

**Answer:**

```csharp
int number = 42;
object boxed = number;       // Boxing: value type → reference type (allocates heap memory)
int unboxed = (int)boxed;    // Unboxing: reference type → value type

// Performance cost: heap allocation + CPU overhead
// Hidden boxing examples:
ArrayList list = new ArrayList();
list.Add(42);                          // Boxing!
int sum = (int)list[0] + (int)list[1]; // Unboxing!

// Avoid with generics:
List<int> list2 = new List<int>();
list2.Add(42);                         // No boxing
```

---

**Q3: What is the difference between `String` and `StringBuilder`?**

**Answer:**

```csharp
// String is IMMUTABLE - every concatenation creates a new object
string s = "";
for (int i = 0; i < 10000; i++)
    s += i;  // Creates 10000 intermediate strings! BAD

// StringBuilder uses a mutable internal buffer
var sb = new StringBuilder();
for (int i = 0; i < 10000; i++)
    sb.Append(i);  // Single buffer, grows as needed
string result = sb.ToString();  // GOOD

// When to use each:
// - String: Fixed text, small concatenations (< 5-10), string interpolation
// - StringBuilder: Loops, building large strings dynamically
```

---

**Q4: Explain `delegates`, `events`, and `lambda expressions`.**

**Answer:**

```csharp
// Delegate: type-safe function pointer
public delegate void ProgressHandler(int percent);

public class Downloader
{
    public ProgressHandler? OnProgress;  // Delegate field

    public void Download()
    {
        for (int i = 0; i <= 100; i += 10)
            OnProgress?.Invoke(i);  // Call all subscribers
    }
}

// Event: wrapped delegate (can only be invoked from declaring class)
public class Downloader
{
    public event ProgressHandler? ProgressChanged;  // Event
}

// Lambda: inline anonymous function
downloader.ProgressChanged += (percent) =>
    Console.WriteLine($"Download: {percent}%");
```

---

**Q5: What is the difference between `abstract class` and `interface`?**

**Answer:**

| Feature | Abstract Class | Interface |
|---------|---------------|-----------|
| Default implementation | Can have | Default methods (C# 8+) |
| Fields | Can have | Cannot |
| Constructor | Can have | Cannot |
| Multiple inheritance | Single | Multiple |
| Access modifiers | All | Public by default |
| When to use | Shared base logic ("is-a") | Contract/capability ("can-do") |

```csharp
public abstract class Vehicle
{
    public string VIN { get; set; }  // field
    public abstract void Start();    // must override
    public virtual void Stop() => Console.WriteLine("Stopped");  // optional override
}

public interface IChargeable
{
    void Charge(int minutes);  // contract
}

public class ElectricCar : Vehicle, IChargeable
{
    public override void Start() => Console.WriteLine("EV started silently");
    public void Charge(int minutes) => Console.WriteLine($"Charging for {minutes}min");
}
```

---

**Q6: Explain the `yield` keyword.**

**Answer:**

```csharp
public IEnumerable<int> GetEvenNumbers(int max)
{
    for (int i = 0; i <= max; i += 2)
    {
        yield return i;  // Returns one value, method pauses
    }
}

// The compiler generates a state machine class.
// No intermediate list is created - values are produced on-demand.
// Memory efficient for large/infinite sequences.

// Usage:
foreach (var even in GetEvenNumbers(1000000).Take(5))
    Console.WriteLine(even);  // Only first 5 are computed
```

---

**Q7: What is the difference between `Exception` and `System.Exception`? How do you create custom exceptions?**

**Answer:**

```csharp
// All exceptions inherit from System.Exception
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string? ResponseBody { get; }

    public ApiException(int statusCode, string? responseBody, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

// Usage:
throw new ApiException(401, "Invalid credentials", "Login failed");

// Catch specific exceptions first, generic last:
try { /* ... */ }
catch (ApiException ex) when (ex.StatusCode == 401) { /* re-auth */ }
catch (HttpRequestException ex) { /* network error */ }
catch (Exception ex) { /* unexpected */ throw; }
```

---

**Q8: What is `LINQ`? Explain deferred vs immediate execution.**

**Answer:**

```csharp
// LINQ = Language Integrated Query

var query = db.Users
    .Where(u => u.IsActive)      // DEFERRED: SQL not executed yet
    .OrderBy(u => u.Name)
    .Select(u => new { u.Id, u.Name });

// Deferred: query is an IQueryable - no DB call yet
// Can chain more operations without performance cost

// Immediate execution (triggers the query):
var list = query.ToList();        // SQL: SELECT Id, Name FROM Users WHERE IsActive = 1 ORDER BY Name
var count = query.Count();        // SQL: SELECT COUNT(*) ...
var first = query.FirstOrDefault(); // SQL: SELECT TOP 1 ...

// Deferred: .Where(), .Select(), .OrderBy(), .Skip(), .Take()
// Immediate: .ToList(), .ToArray(), .Count(), .First(), .Any(), .Sum()
```

---

**Q9: What are nullable reference types and how do they improve code safety?**

**Answer:**

```csharp
#nullable enable  // Enable in project or file

public class UserModel
{
    public string Name { get; set; }        // NON-nullable - compiler warns if not set
    public string? MiddleName { get; set; } // Nullable - OK to be null
    public string Email { get; set; } = ""; // Initialized to avoid warning
}

// Compiler warnings:
var user = new UserModel();
Console.WriteLine(user.Name.Length);  // Warning: possibly null

// Fix with null check:
if (user.Name is not null)
    Console.WriteLine(user.Name.Length);

// Or null-forgiving operator (use sparingly):
Console.WriteLine(user.Name!.Length);  // "I know it's not null"

// Null-conditional operator:
int? length = user.MiddleName?.Length;  // null if MiddleName is null
```

---

**Q10: Explain `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, and `IQueryable<T>`.**

**Answer:**

```csharp
// IEnumerable<T> - forward-only iteration (can't modify)
public void Process(IEnumerable<User> users)
{
    foreach (var user in users) { /* read only */ }
}

// ICollection<T> - add/remove/count
public void Manage(ICollection<User> users)
{
    users.Add(new User());
    users.Remove(existing);
    Console.WriteLine(users.Count);
}

// IList<T> - indexed access + all of the above
public void Sort(IList<User> users)
{
    users[0] = updatedUser;  // index access
    users.Insert(0, newUser);
}

// IQueryable<T> - LINQ-to-SQL (deferred, on the database)
public IQueryable<User> GetActiveQuery()
{
    return db.Users.Where(u => u.IsActive);  // builds SQL, no execution
}
```

---

## .NET MAUI Framework

**Q11: What is the difference between .NET MAUI and Xamarin.Forms?**

**Answer:**

| Feature | Xamarin.Forms | .NET MAUI |
|---------|---------------|-----------|
| Framework | .NET Framework / Mono | .NET 6+ (unified) |
| Desktop support | No | Windows, macOS (new!) |
| Renderers | ViewRenderer (heavy) | Handler (lightweight) |
| DI | DependencyService | Built-in DI container |
| Performance | Slower | Faster (50%+ improvement) |
| Tooling | Xamarin-specific | .NET CLI, VS, VS Code |
| Single project | Separate platform projects | Single project with Platforms folder |
| CSS styling | Limited | Built-in |
| Hot Reload | Limited | XAML Hot Reload + Live Visual Tree |

---

**Q12: Explain the MAUI lifecycle — how does a page get created and shown?**

**Answer:**

```
App.Startup → App constructor → CreateWindow() → Shell
  → Navigation → Page constructor →
    → OnNavigatedTo() → OnAppearing() →
      → (user interacts) →
    → OnDisappearing() → OnNavigatedFrom()
  → (app background) → OnSleep() → OnResume()
  → App closed → OnDestroy()
```

```csharp
public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;  // DI injects ViewModel
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Best place to load data
        (BindingContext as DashboardViewModel)?.LoadCommand.Execute(null);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Cleanup timers, event handlers, etc.
    }
}
```

---

**Q13: How do you pass data between pages in MAUI?**

**Answer:**

```csharp
// Method 1: Query Property (Shell navigation)
[QueryProperty(nameof(SelectedStation), "Station")]
public partial class StationDetailPage : ContentPage
{
    public StationModel? SelectedStation { get; set; }
}

// Navigation:
await Shell.Current.GoToAsync("stationdetail",
    new Dictionary<string, object>
    {
        ["Station"] = selectedStation
    });

// Method 2: Static singleton (shared state)
public class AuthService : IAuthService
{
    public UserModel? CurrentUser { get; set; }  // accessible from anywhere
}

// Method 3: WeakReferenceMessenger
WeakReferenceMessenger.Default.Send(new StationSelectedMessage(station));

// Method 4: Navigation parameters (MAUI 8+)
await Shell.Current.GoToAsync("stationdetail",
    new { id = station.Id, name = station.Name });
```

---

**Q14: What are `Handlers` in MAUI and how do they differ from `Renderers`?**

**Answer:**

```csharp
// Renderers (Xamarin.Forms): Heavy base class (ViewRenderer<TView, TNative>)
// - Full lifecycle management
// - Property changed handlers registered per instance
// - Memory overhead

// Handlers (MAUI): Lightweight mapper-based approach
public static class EntryHandler
{
    public static PropertyMapper<Entry, EntryHandler> Mapper = new()
    {
        [nameof(Entry.Text)] = (handler, entry) =>
            handler.PlatformView.Text = entry.Text,
        [nameof(Entry.TextColor)] = (handler, entry) =>
            handler.PlatformView.TextColor = entry.TextColor.ToPlatform(),
    };
}

// Custom handler:
public class CustomEntryHandler : EntryHandler
{
    protected override void ConnectHandler(MauiEntry platformView)
    {
        base.ConnectHandler(platformView);
        // Platform-specific setup
        platformView.TextChanged += OnCustomTextChanged;
    }
}
```

---

**Q15: How does `CollectionView` handle virtualization?**

**Answer:**

`CollectionView` uses UI virtualization — it only creates views for the items visible on screen. As you scroll, items that go off-screen have their views recycled and reused for newly visible items.

```csharp
// Without virtualization (bad):
<VerticalStackLayout BindableLayout.ItemsSource="{Binding Items}">
    <!-- Creates ALL items upfront, even invisible ones -->
</VerticalStackLayout>

// With virtualization (good):
<CollectionView ItemsSource="{Binding Items}">
    <!-- Only creates ~10-20 views regardless of list size -->
</CollectionView>

// Performance tips:
// - Keep ItemTemplate visual tree shallow
// - Use compiled bindings (x:DataType)
// - Don't nest CollectionViews
// - Set RemainingItemsThreshold for pagination
```

---

**Q16: What is `Shell` and why would you use it over `NavigationPage`?**

**Answer:**

```csharp
// Shell provides:
// 1. URI-based navigation (like web routing)
// 2. Built-in flyout menu or tab bar
// 3. Search handler integration
// 4. Visual consistency across pages
// 5. Back button behavior management

// NavigationPage is simpler — stack-based:
await Navigation.PushAsync(new DetailPage());
await Navigation.PopAsync();

// Shell is URI-based:
await Shell.Current.GoToAsync("//dashboard");
await Shell.Current.GoToAsync("../settings");
await Shell.Current.GoToAsync("stationdetail?id=123");

// Use Shell for: Multi-tab apps, flyout menus, complex navigation
// Use NavigationPage for: Simple wizard flows, modals
```

---

**Q17: How do you implement custom fonts, images, and resources in MAUI?**

**Answer:**

```csharp
// 1. Fonts in .csproj:
// <MauiFont Include="Resources\Fonts\Inter-Regular.ttf" />
// <MauiFont Include="Resources\Fonts\Inter-Bold.ttf" />

// 2. Register in MauiProgram.cs:
builder.ConfigureFonts(fonts =>
{
    fonts.AddFont("Inter-Regular.ttf", "InterRegular");
    fonts.AddFont("Inter-Bold.ttf", "InterBold");
});

// 3. Use in XAML:
<Label Text="Hello" FontFamily="InterRegular" FontSize="16" />

// 4. Images — auto-converted to PNG/WebP:
// <MauiImage Include="Resources\Images\logo.svg" />
<Image Source="logo.png" />  <!-- no path needed -->

// 5. Raw resources:
// <MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
var stream = await FileSystem.OpenAppPackageFileAsync("data.json");
```

---

**Q18: How do you handle connectivity changes in MAUI?**

**Answer:**

```csharp
public class ConnectivityService
{
    public ConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        var isConnected = e.NetworkAccess == NetworkAccess.Internet;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            // Update UI
            Shell.Current.DisplayAlert("Connection",
                isConnected ? "Back online" : "You are offline", "OK");
        });

        if (isConnected)
            await SyncPendingDataAsync();
    }

    public bool IsConnected =>
        Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}
```

---

**Q19: How do you create and use custom `Behaviors` in MAUI?**

**Answer:**

```csharp
public class MaxLengthBehavior : Behavior<Entry>
{
    public int MaxLength { get; set; } = 100;

    protected override void OnAttachedTo(Entry entry)
    {
        entry.TextChanged += OnTextChanged;
        base.OnAttachedTo(entry);
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && e.NewTextValue?.Length > MaxLength)
        {
            entry.Text = e.NewTextValue[..MaxLength];
        }
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnTextChanged;
        base.OnDetachingFrom(entry);
    }
}

// XAML:
// <Entry Placeholder="Name">
//     <Entry.Behaviors>
//         <local:MaxLengthBehavior MaxLength="50" />
//     </Entry.Behaviors>
// </Entry>
```

---

**Q20: What is `x:DataType` and why is it important?**

**Answer:**

```csharp
// x:DataType enables COMPILED BINDINGS
// Without it: bindings use reflection at runtime (slower, no type checking)

// With it:
<ContentPage x:DataType="vm:LoginViewModel">
    <Entry Text="{Binding Username}" />
    <!-- Compiler checks that Username exists on LoginViewModel -->
    <!-- Typo "Usernam" → COMPILE ERROR, not silent runtime failure -->
</ContentPage>

// Benefits:
// - 5-10x faster binding resolution
// - Compile-time error checking
// - Better IntelliSense in XAML
// - Reduced memory usage

// Always set x:DataType at Page and DataTemplate level:
<CollectionView x:DataType="vm:StationsViewModel">
    <CollectionView.ItemTemplate x:DataType="models:StationModel">
        <DataTemplate>
            <Label Text="{Binding Name}" />
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

---

## MVVM & Data Binding

**Q21: What is the MVVM pattern? Explain each layer with examples.**

**Answer:**

```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│    VIEW      │  binds  │  VIEWMODEL   │  uses   │    MODEL     │
│  (XAML)      │ ──────> │  (C# Code)   │ ──────> │  (Data)      │
│  LoginPage   │         │ LoginVM      │         │ UserModel    │
│  Button      │ <────── │ IsBusy       │         │ AuthResponse │
│  Entry       │  notif. │ LoginCommand │         │              │
└──────────────┘         └──────────────┘         └──────────────┘
```

- **View:** XAML pages. No code-behind logic (except UI-specific animation).
- **ViewModel:** Observable properties + commands. Orchestrates business logic.
- **Model:** Data classes (DTOs, entities). No behavior, just data.

**Flow:** User taps button → View executes bound command → ViewModel calls service → updates model → raises PropertyChanged → View re-reads property.

---

**Q22: What does `[ObservableProperty]` and `[RelayCommand]` generate?**

**Answer:**

```csharp
// [ObservableProperty] generates:
// - Public property with INotifyPropertyChanged
// - Partial method On<Name>Changed

[ObservableProperty]
private string _userName = string.Empty;

// GENERATES:
// public string UserName
// {
//     get => _userName;
//     set
//     {
//         if (!EqualityComparer<string>.Default.Equals(_userName, value))
//         {
//             _userName = value;
//             OnPropertyChanged();
//             OnUserNameChanged(value);
//         }
//     }
// }
// partial void OnUserNameChanged(string value); // hook for side effects

// [RelayCommand] generates:
// - ICommand property from async/sync method

[RelayCommand]
async Task LoginAsync() { /* ... */ }

// GENERATES:
// public ICommand LoginCommand => _loginCommand ??= new AsyncRelayCommand(LoginAsync);
```

---

**Q23: What is the difference between `OneWay`, `TwoWay`, and `OneTime` binding?**

**Answer:**

```csharp
// OneWay: ViewModel → View only (default for most bindings)
<Label Text="{Binding UserName}" />  <!-- VM→View only -->

// TwoWay: View ⇄ ViewModel (for input controls)
<Entry Text="{Binding UserName, Mode=TwoWay}" />  <!-- View→VM and VM→View -->

// OneTime: ViewModel → View only once (performance optimization)
<Label Text="{Binding StaticData, Mode=OneTime}" />
<!-- Reads the value once, doesn't listen for changes -->

// OneWayToSource: View → ViewModel only
<Entry Text="{Binding UserName, Mode=OneWayToSource}" />
<!-- User types → VM updates, but VM changes don't update UI -->

// Default modes:
// Label.Text: OneWay
// Entry.Text: TwoWay
// Image.Source: OneWay
```

---

**Q24: How does `INotifyPropertyChanged` work? Implement it manually.**

**Answer:**

```csharp
public class ManualViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// The UI framework (MAUI) listens to PropertyChanged.
// When it fires, the binding re-reads the property and updates the UI.
// With CommunityToolkit.Mvvm, [ObservableProperty] generates all this.
```

---

**Q25: What is the `IValueConverter` interface?**

**Answer:**

```csharp
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // ViewModel → View
        return (bool)value ? Colors.Green : Colors.Red;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // View → ViewModel (rarely needed for one-way converters)
        throw new NotSupportedException();
    }
}

// Register in XAML:
// <ContentPage.Resources>
//     <local:BoolToColorConverter x:Key="BoolToColor" />
// </ContentPage.Resources>

// Usage:
// <Label TextColor="{Binding IsActive, Converter={StaticResource BoolToColor}}" />
```

---

## Dependency Injection

**Q26: What is Dependency Injection and why is it important?**

**Answer:**

```csharp
// WITHOUT DI (tight coupling):
public class LoginViewModel
{
    private readonly AuthService _auth = new();  // Hard dependency!
    // Can't swap implementation, hard to test
}

// WITH DI (loose coupling):
public class LoginViewModel
{
    private readonly IAuthService _auth;

    public LoginViewModel(IAuthService auth)  // Dependency injected
    {
        _auth = auth;
    }
}

// Benefits:
// 1. TESTABILITY: Mock IAuthService in unit tests
// 2. FLEXIBILITY: Swap implementation without changing consumers
// 3. CENTRALIZED CONFIGURATION: All registrations in one place
// 4. LIFETIME MANAGEMENT: Container handles disposal
```

---

**Q27: Explain `AddSingleton`, `AddTransient`, and `AddScoped`.**

**Answer:**

```csharp
// Singleton: Created once, lives for app lifetime
builder.Services.AddSingleton<IAuthService, AuthService>();
// - Same instance everywhere
// - Good for: Shared state (auth token cache), HttpClient
// - Risk: Thread safety required

// Transient: Created every time injected
builder.Services.AddTransient<LoginViewModel>();
// - New instance per injection
// - Good for: ViewModels, Pages (each navigation gets fresh state)
// - Risk: Many short-lived objects → GC pressure

// Scoped: Created once per scope
builder.Services.AddScoped<IMyService, MyService>();
// - One instance per scope (rarely used in MAUI)
// - Good for: Unit of Work, DB transactions
```

---

**Q28: How do you register multiple implementations of the same interface?**

**Answer:**

```csharp
// Register both:
builder.Services.AddSingleton<IApiService, LiveApiService>();
builder.Services.AddSingleton<IApiService, MockApiService>();

// Option 1: IEnumerable<T>
public class CompositeService
{
    public CompositeService(IEnumerable<IApiService> services)
    {
        var allApis = services.ToList();  // [LiveApiService, MockApiService]
    }
}

// Option 2: Named registration (manual factory)
public class ApiServiceFactory
{
    private readonly IServiceProvider _provider;
    public ApiServiceFactory(IServiceProvider provider) => _provider = provider;

    public IApiService GetLive() => _provider.GetRequiredService<LiveApiService>();
    public IApiService GetMock() => _provider.GetRequiredService<MockApiService>();
}

// Option 3: Conditional (most common)
#if DEBUG
    builder.Services.AddSingleton<IApiService, MockApiService>();
#else
    builder.Services.AddSingleton<IApiService, LiveApiService>();
#endif
```

---

**Q29: What is the Service Locator anti-pattern?**

**Answer:**

```csharp
// SERVICE LOCATOR (anti-pattern):
public class LoginViewModel
{
    public async Task LoginAsync()
    {
        // Where did this come from? Magic! Hard to know dependencies.
        var auth = (IAuthService)App.ServiceProvider.GetService(typeof(IAuthService));
        await auth.LoginAsync("user", "pass");
    }
}

// Why it's bad:
// 1. HIDDEN DEPENDENCIES: Constructor doesn't reveal what's needed
// 2. HARD TO TEST: Can't easily mock without modifying static App
// 3. RUNTIME FAILURES: GetService returns null if not registered
// 4. ENCOURAGES GOD CLASSES: Just grab whatever you need

// CORRECT approach: Constructor injection
public class LoginViewModel
{
    private readonly IAuthService _auth;
    public LoginViewModel(IAuthService auth) => _auth = auth;
    // Clear, testable, explicit
}
```

---

**Q30: How does MAUI resolve dependencies for pages?**

**Answer:**

```csharp
// 1. Register page and ViewModel:
builder.Services.AddTransient<LoginPage>();
builder.Services.AddTransient<LoginViewModel>();

// 2. Page constructor receives ViewModel via DI:
public class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}

// 3. When Shell navigates to "login":
await Shell.Current.GoToAsync("//login");
// - Shell asks DI container for LoginPage
// - DI sees LoginPage needs LoginViewModel
// - DI creates LoginViewModel (needs IAuthService, etc.)
// - Recursively resolves all dependencies
// - Returns LoginPage with fully constructed BindingContext
```

---

## Async Programming

**Q31: Explain `async`/`await` with a real example.**

**Answer:**

```csharp
public async Task LoadDashboardAsync()
{
    IsBusy = true;  // UI thread

    try
    {
        // await: yields UI thread, starts API call on background
        var data = await _api.GetAsync<DashboardModel>("/api/dashboard");

        // Continuation: back on UI thread (SyncContext captured)
        BatteryLevel = data.BatteryPercent;
        IsBusy = false;
    }
    catch (HttpRequestException ex)
    {
        await ShowAlertAsync("Error", ex.Message);
    }
    finally
    {
        IsBusy = false;
    }
}
```

---

**Q32: What happens when you block on async code with `.Result` or `.Wait()`?**

**Answer:**

```csharp
// DANGEROUS: Deadlock risk!
public void ButtonClick()
{
    // This blocks the UI thread waiting for the async method
    var data = _api.GetAsync<DashboardModel>("/api/dashboard").Result;
    // DEADLOCK! UI thread is blocked. Async method can't resume.
}

// Why deadlock happens:
// 1. GetAsync starts, captures UI SyncContext
// 2. .Result blocks UI thread
// 3. GetAsync completes, tries to resume on UI thread
// 4. UI thread is blocked → DEADLOCK

// SAFE:
public async Task ButtonClickAsync()
{
    var data = await _api.GetAsync<DashboardModel>("/api/dashboard");
    // await yields the thread, no blocking
}
```

---

**Q33: When would you use `Task.WhenAll` vs sequential awaits?**

**Answer:**

```csharp
// Sequential (slow): 3 seconds total
var user = await _api.GetAsync<User>("/api/user");
var dashboard = await _api.GetAsync<Dashboard>("/api/dashboard");
var stations = await _api.GetAsync<List<Station>>("/api/stations");

// Parallel (fast): ~1 second total (independent calls)
var userTask = _api.GetAsync<User>("/api/user");
var dashboardTask = _api.GetAsync<Dashboard>("/api/dashboard");
var stationsTask = _api.GetAsync<List<Station>>("/api/stations");

await Task.WhenAll(userTask, dashboardTask, stationsTask);
// All 3 execute concurrently

var user = userTask.Result;         // Already completed
var dashboard = dashboardTask.Result;
var stations = stationsTask.Result;
```

---

**Q34: What is `CancellationToken` and how do you use it?**

**Answer:**

```csharp
public async Task SearchStationsAsync(string query, CancellationToken ct)
{
    // Pass token to HttpClient
    var response = await _httpClient.GetAsync(
        $"/api/stations?q={query}", ct);

    // Check cancellation manually
    ct.ThrowIfCancellationRequested();

    var result = await response.Content.ReadFromJsonAsync<List<Station>>(ct);
    return result;
}

// Caller with cancellation:
private CancellationTokenSource? _searchCts;

public async void OnSearchTextChanged(string text)
{
    // Cancel previous search
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();

    try
    {
        await Task.Delay(300, _searchCts.Token); // debounce 300ms
        var results = await SearchStationsAsync(text, _searchCts.Token);
        Stations = new ObservableCollection<Station>(results);
    }
    catch (OperationCanceledException)
    {
        // Expected when cancelled - do nothing
    }
}
```

---

**Q35: What is `ConfigureAwait(false)` and when should you use it?**

**Answer:**

```csharp
// ConfigureAwait(false): Don't return to original SyncContext
// Continuation runs on any thread (ThreadPool)

public async Task<string> LoadDataAsync()
{
    // LIBRARY CODE (no UI touch) - OK to use ConfigureAwait(false)
    var data = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
    return data;
    // Continuation may run on ThreadPool, not UI thread
}

public async Task UpdateUIAsync()
{
    // UI CODE (ViewModel) - DO NOT use ConfigureAwait(false)
    var data = await LoadDataAsync();  // No ConfigureAwait
    IsBusy = false;  // MUST be on UI thread
    // If LoadDataAsync used ConfigureAwait(false), continuation
    // might not be on UI thread → cross-thread exception
}

// RULE: In MAUI apps, never use ConfigureAwait(false)
// in ViewModel or page code. Only use it in library/services
// that don't touch UI.
```

---

## REST API & HTTP Communication

**Q36: Explain the full HTTP lifecycle of a POST request in MAUI.**

**Answer:**

```csharp
// 1. Create request object
var request = new LoginRequest { Username = "admin", Password = "Admin@123" };

// 2. MAUI calls HttpClient.PostAsJsonAsync
var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
// → Serializes to JSON: {"username":"admin","password":"Admin@123"}
// → Sets Content-Type: application/json
// → Sends HTTP POST to http://localhost:5238/api/auth/login

// 3. Network layer
// → DNS resolution → TCP connection → TLS handshake → HTTP request

// 4. Server receives, processes, responds:
// ← HTTP 200 OK
// ← Content-Type: application/json
// ← Body: {"token":"eyJ...","user":{"id":1,"username":"admin"}}

// 5. MAUI deserializes response
var authResponse = await response.Content
    .ReadFromJsonAsync<AuthResponse>();

// 6. Handle response
if (response.IsSuccessStatusCode) { /* success */ }
else { /* error */ }
```

---

**Q37: How do you handle expired JWT tokens in MAUI?**

**Answer:**

```csharp
public class TokenRefreshHandler : DelegatingHandler
{
    private readonly IAuthService _auth;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token expired — try to refresh
            var refreshed = await _auth.RefreshTokenAsync();
            if (refreshed)
            {
                // Retry with new token
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
                response = await base.SendAsync(request, ct);
            }
            else
            {
                // Refresh failed — force re-login
                await Shell.Current.GoToAsync("//login");
            }
        }

        return response;
    }
}
```

---

**Q38: What HTTP status codes should your app handle?**

**Answer:**

```csharp
// 2xx Success
//   200 OK: Request succeeded
//   201 Created: Resource created (POST)
//   204 No Content: Success, no body (DELETE)

// 3xx Redirection
//   304 Not Modified: Use cached version (ETag)

// 4xx Client Errors
//   400 Bad Request: Invalid input → show validation error
//   401 Unauthorized: No/expired token → redirect to login
//   403 Forbidden: No permission → show "access denied"
//   404 Not Found: Resource doesn't exist → show "not found"
//   409 Conflict: Duplicate → show "already exists"
//   429 Too Many Requests: Rate limited → retry after delay

// 5xx Server Errors
//   500 Internal Server Error: Server bug → show "try again later"
//   502 Bad Gateway: Upstream failure → retry
//   503 Service Unavailable: Overloaded → retry with backoff
```

---

**Q39: How do you upload a file to a REST API from MAUI?**

**Answer:**

```csharp
public async Task<bool> UploadAvatarAsync(FileResult file)
{
    using var stream = await file.OpenReadAsync();
    using var content = new MultipartFormDataContent();
    using var streamContent = new StreamContent(stream);

    content.Add(streamContent, "file", file.FileName);

    var response = await _httpClient.PostAsync("/api/user/avatar", content);
    return response.IsSuccessStatusCode;
}

// Or use HttpClient directly:
public async Task<string> UploadAndGetUrlAsync(byte[] imageBytes, string fileName)
{
    using var formData = new MultipartFormDataContent();
    formData.Add(new ByteArrayContent(imageBytes), "file", fileName);

    var response = await _httpClient.PostAsync("/api/upload", formData);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<UploadResult>();
    return result?.Url ?? "";
}
```

---

**Q40: How do you implement request retry with Polly?**

**Answer:**

```csharp
using Polly;
using Polly.Extensions.Http;

// Define retry policy
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()  // 5xx or 408
    .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests) // 429
    .WaitAndRetryAsync(3, attempt =>
        TimeSpan.FromSeconds(Math.Pow(2, attempt))); // 2s, 4s, 8s

// Circuit breaker (stop trying if failing)
var circuitBreaker = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// Apply to HttpClient
builder.Services.AddHttpClient<IApiService, ApiService>()
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreaker);
```

---

## Database & Entity Framework Core

**Q41: Explain Code-First vs Database-First in EF Core.**

**Answer:**

```csharp
// CODE-FIRST: Write C# classes → EF generates DB schema
public class User { public int Id { get; set; } public string Name { get; set; } = ""; }
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
}
// dotnet ef migrations add InitialCreate
// dotnet ef database update
// → Creates Users table with Id (int PK) and Name (nvarchar)

// DATABASE-FIRST: Existing DB → EF generates C# classes
// dotnet ef dbcontext scaffold "ConnectionString" Microsoft.EntityFrameworkCore.SqlServer
// → Generates User.cs, AppDbContext.cs from existing tables
```

---

**Q42: What is the difference between `Include()` and `ThenInclude()`?**

**Answer:**

```csharp
// Include: Eager load a related entity
var station = db.Stations
    .Include(s => s.Batteries)  // Load all batteries for this station
    .FirstOrDefault(s => s.Id == 1);
// SQL: SELECT * FROM Stations LEFT JOIN Batteries ON ...

// ThenInclude: Chain to load nested relations
var station = db.Stations
    .Include(s => s.Batteries)          // Load batteries
        .ThenInclude(b => b.SwapHistories)  // Load each battery's swap history
    .Include(s => s.Address)            // Load address (separate navigation)
    .FirstOrDefault(s => s.Id == 1);
```

---

**Q43: What is `AsNoTracking()` and when should you use it?**

**Answer:**

```csharp
// AsNoTracking: Disables change tracking
// EF doesn't store snapshots or track changes

// WITH tracking (default):
var user = db.Users.Find(1);
user.Name = "New Name"; // EF detects this
db.SaveChanges();       // Generates UPDATE SQL

// WITHOUT tracking (read-only):
var users = db.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .ToList();
// Faster - no snapshot, no tracking overhead
// Use for: GET endpoints, reports, read-only data

// Performance: AsNoTracking can be 2-5x faster
```

---

**Q44: What is the difference between `FirstOrDefault()` and `SingleOrDefault()`?**

**Answer:**

```csharp
// FirstOrDefault(): Returns FIRST match or null
var user = db.Users.FirstOrDefault(u => u.Role == "Admin");
// If 5 admins exist, returns the first one. Safe.

// SingleOrDefault(): Returns SINGLE match or null
var user = db.Users.SingleOrDefault(u => u.Email == "admin@test.com");
// If 2 users have same email → throws InvalidOperationException!
// Use when you expect AT MOST ONE result (e.g., unique key)

// Performance:
// - FirstOrDefault stops after finding the first match
// - SingleOrDefault must scan all results to verify uniqueness
```

---

**Q45: How do you handle concurrent updates in EF Core?**

**Answer:**

```csharp
public class Battery
{
    public int Id { get; set; }
    public string Status { get; set; } = "";

    [Timestamp]  // ← Concurrency token
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

// When User A and User B try to update the same battery:
// User A reads Battery (RowVersion = [0x01, 0x02])
// User B reads Battery (RowVersion = [0x01, 0x02])
// User A saves: updates RowVersion to [0x01, 0x03] ← success
// User B saves: RowVersion [0x01, 0x02] ≠ DB [0x01, 0x03]
//   → DbUpdateConcurrencyException thrown

// Handle:
try
{
    await db.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    var entry = ex.Entries.Single();
    var databaseValues = await entry.GetDatabaseValuesAsync();
    var currentValues = entry.CurrentValues;
    // Let user resolve conflict
}
```

---

## Design Patterns & SOLID Principles

**Q46: Explain the SOLID principles with examples.**

**Answer:**

```csharp
// S - Single Responsibility: A class has one reason to change
public class AuthService { /* handles authentication only */ }
public class ApiService { /* handles HTTP only */ }
// NOT: public class AuthService { void Login(); void SendEmail(); void GenerateReport(); }

// O - Open/Closed: Open for extension, closed for modification
public interface INotificationService { void Send(string message); }
public class EmailNotification : INotificationService { /* ... */ }
public class SmsNotification : INotificationService { /* ... */ }
// Add new types without modifying existing code

// L - Liskov Substitution: Subtypes must be substitutable for base types
public class Rectangle { public virtual int Width { get; set; } public virtual int Height { get; set; } }
public class Square : Rectangle { /* Violates LSP - changing Width changes Height */ }

// I - Interface Segregation: Small, focused interfaces
public interface IAuthService { Task LoginAsync(string u, string p); }
public interface IApiService { Task<T> GetAsync<T>(string ep); Task PostAsync(string ep, object d); }
// NOT: public interface IService { void Login(); void GetData(); void SendEmail(); void LogError(); }

// D - Dependency Inversion: Depend on abstractions, not concretions
public class LoginViewModel { private readonly IAuthService _auth; } // depends on interface
// NOT: public class LoginViewModel { private readonly AuthService _auth; } // depends on concrete
```

---

**Q47: Explain the Repository Pattern.**

**Answer:**

```csharp
// Repository: Abstraction over data access
public interface IStationRepository
{
    Task<Station?> GetByIdAsync(int id);
    Task<List<Station>> GetNearbyAsync(double lat, double lng, double radiusKm);
}

public class StationRepository : IStationRepository
{
    private readonly AppDbContext _db;

    public async Task<List<Station>> GetNearbyAsync(double lat, double lng, double radiusKm)
    {
        return await _db.Stations
            .FromSqlInterpolated($@"
                SELECT * FROM Stations
                WHERE dbo.Distance(Latitude, Longitude, {lat}, {lng}) <= {radiusKm}")
            .ToListAsync();
    }
}

// Benefits:
// 1. Centralized data access logic
// 2. Easy to unit test (mock IStationRepository)
// 3. Switch DB provider without changing business logic
```

---

**Q48: What is the Factory Pattern?**

**Answer:**

```csharp
// Factory: Creates objects without specifying exact class
public interface IApiServiceFactory
{
    IApiService Create(bool useMock);
}

public class ApiServiceFactory : IApiServiceFactory
{
    private readonly IServiceProvider _provider;

    public ApiServiceFactory(IServiceProvider provider) => _provider = provider;

    public IApiService Create(bool useMock)
    {
        return useMock
            ? _provider.GetRequiredService<MockApiService>()
            : _provider.GetRequiredService<RealApiService>();
    }
}

// Usage:
var api = _factory.Create(isDevelopment);
var data = await api.GetAsync<DashboardModel>("/api/dashboard");
```

---

**Q49: What is the Strategy Pattern?**

**Answer:**

```csharp
// Strategy: Select algorithm at runtime
public interface IRetryStrategy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
}

public class ExponentialBackoffStrategy : IRetryStrategy
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        for (int i = 0; i < 3; i++)
        {
            try { return await operation(); }
            catch { await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); }
        }
        return await operation(); // last attempt
    }
}

public class NoRetryStrategy : IRetryStrategy
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        => await operation();
}

// Usage (DI injects the strategy):
public class ApiService
{
    private readonly IRetryStrategy _retry;
    public ApiService(IRetryStrategy retry) => _retry = retry;

    public async Task<T?> GetAsync<T>(string endpoint) =>
        await _retry.ExecuteAsync(() => /* actual HTTP call */);
}
```

---

**Q50: What is the difference between `Aggregate Root`, `Entity`, and `Value Object` in DDD?**

**Answer:**

```csharp
// Entity: Has identity (ID), mutable, tracked
public class User
{
    public int Id { get; set; }      // identity
    public string Name { get; set; } // can change
}
// Two users with same name are different (different IDs)

// Value Object: No identity, immutable, compared by value
public class Address
{
    public string Street { get; }
    public string City { get; }
    public string ZipCode { get; }

    public override bool Equals(object? obj) =>
        obj is Address a && Street == a.Street && City == a.City && ZipCode == a.ZipCode;
}
// Two addresses with same values are equal

// Aggregate Root: Entity that owns other entities
public class SwapRequest  // Aggregate Root
{
    public int Id { get; set; }
    public User Renter { get; set; }      // Entity (part of aggregate)
    public Address PickupLocation { get; set; }  // Value Object
    public List<Battery> Batteries { get; set; } = new(); // Entities

    // Operations go through the aggregate root
    public void AddBattery(Battery battery) { /* validate business rules */ }
}
```

---

## 50+ Done — You Can Crack Any Interview

**You've mastered the core concepts.** Here's the truth: 90% of interview questions are variations of the 50 above. If you can explain each of these clearly with code examples, you can handle any .NET MAUI interview.

### How to prepare (48-hour plan):

| Day | Focus | Activities |
|-----|-------|-----------|
| **Day 1** | **C# & .NET Core** (Q1-Q10, Q31-Q35) | Code each concept. Write async/await by hand. Explain SOLID with examples. |
| **Day 1** | **MAUI Framework** (Q11-Q20, Q21-Q25) | Understand Shell, CollectionView, compiled bindings. Build a mini app with MVVM. |
| **Day 2** | **DI, REST, EF Core** (Q26-Q30, Q36-Q45) | Set up DI manually. Write an HTTP client wrapper. Practice EF Core queries. |
| **Day 2** | **Design Patterns & Mock Interview** (Q46-Q50) | Explain patterns out loud. Do a mock interview with a friend. |

### Interview mindset:

1. **Be honest** — "I haven't used that in production, but here's how I'd approach it based on my understanding of X, Y, and Z."
2. **Connect to experience** — "That's similar to a problem I solved in my app where..."
3. **Think out loud** — "First I'd check X, then Y, and if that doesn't work, Z." Shows problem-solving process.
4. **Ask clarifying questions** — "By scaling, do you mean 1,000 or 1,000,000 users? The approach is very different."
5. **Code quality matters** — Write clean, readable code even on a whiteboard. Use meaningful variable names.

### Red flag phrases to avoid:

- ❌ "I've never done that" (without offering an approach)
- ❌ "It works on my machine" (blaming the environment)
- ❌ "That's not my job" (rigid role definition)
- ❌ "We should rewrite everything in [new tech]" (NIH syndrome)

### Green flag phrases:

- ✅ "Here's how I handled a similar situation..."
- ✅ "I'd start by understanding the requirements, then..."
- ✅ "The trade-off here is between X and Y, so I'd choose..."
- ✅ "I'd add logging and monitoring to validate this approach."
- ✅ "Let me think about edge cases..."

---

> **Final tip:** Companies hire for attitude and train for skills. If you've built and shipped even one MAUI app, you have enough technical foundation. The rest is communication, problem-solving, and cultural fit.
>
> **You've got this. Go crack that interview.**
