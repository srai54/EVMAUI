# Interview Questions — .NET MAUI & EV Swap App (200+ Questions with Answers)

---

## 1. Application-Specific (EV Swap Project)

**Q1: Walk through the app architecture — how does a login request flow from UI to API?**

**Answer:**
```
LoginPage (XAML) → LoginViewModel.LoginAsync() → AuthService.LoginAsync()
  → ApiService.PostAsync<AuthResponse>(endpoint, data)
    → HttpClient.PostAsJsonAsync() → HTTP POST → API Controller
      → AuthService.LoginAsync() (API) → verify credentials → return JWT
    ← HTTP 200 with JSON body
  ← Deserialize JSON → AuthResponse object
← AuthService stores token in SecureStorage, sets CurrentUser
← LoginViewModel navigates to Dashboard
```

**Key files involved:**
- `Views/LoginPage.xaml` — two Entry fields bound to ViewModel, Button bound to `LoginCommand`
- `ViewModels/LoginViewModel.cs` — calls `_authService.LoginAsync(username, password)`
- `Services/AuthService.cs` — wraps API call + stores auth data
- `Services/ApiService.cs` — actual HTTP communication via `HttpClient`
- `Controllers/AuthController.cs` (API) — `[HttpPost("login")]` endpoint

---

**Q2: Why does the app use both `IApiService` and `IAuthService` instead of one service?**

**Answer:** Separation of concerns. `IApiService` handles raw HTTP communication (GET, POST, PUT, DELETE). `IAuthService` handles authentication-specific logic: storing tokens, managing current user state, bypass login, logout. If you merged them, every API call would be coupled to auth logic. With them separated, you can swap the HTTP implementation without touching auth, or vice versa.

```
IApiService:  GetAsync<T> / PostAsync<T> / PutAsync<T> / DeleteAsync
IAuthService: LoginAsync / RegisterAsync / LogoutAsync / BypassLogin / CurrentUser
```

---

**Q3: How does dependency injection work in this MAUI app? Where are services registered?**

**Answer:** All services are registered in `MauiProgram.cs` using `builder.Services`. The `MauiApp.CreateBuilder()` sets up the DI container. Services are registered as Singleton or Transient, then injected via constructor parameters.

```csharp
// MauiProgram.cs
builder.Services.AddSingleton<IApiService, ApiService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddTransient<LoginViewModel>();
builder.Services.AddTransient<LoginPage>();
```

When `LoginPage` is resolved, DI creates `LoginViewModel` first, which needs `IAuthService` and other services. DI walks the dependency graph and provides all required instances.

---

**Q4: What happens when the API is down and a user clicks "Bypass Login"?**

**Answer:** `AuthService.BypassLogin()` creates a hardcoded `UserModel` with admin role:

```csharp
public void BypassLogin()
{
    CurrentUser = new UserModel
    {
        Id = 1, Username = "admin", Email = "admin@evswap.com",
        Phone = "0000000000", IsActive = true,
        Roles = new List<string> { "Admin" }
    };
}
```

Then `LoginViewModel` navigates to Dashboard. All subsequent API calls (e.g., `GET /api/report/user-dashboard`) will fail because the API is down. Every ViewModel's `catch` block detects the failure and populates **dummy data** instead — battery stats, wallet balance, stations, trips, etc. So the app remains fully navigable with sample data.

---

**Q5: How does `ApiService.HandleResponse<T>` handle errors?**

**Answer:**

```csharp
private static async Task<T?> HandleResponse<T>(HttpResponseMessage response)
{
    var body = await response.Content.ReadAsStringAsync();

    // Non-success status codes → throw with API error message
    if (!response.IsSuccessStatusCode)
    {
        var msg = string.IsNullOrWhiteSpace(body)
            ? $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}"
            : body;
        throw new HttpRequestException(msg, null, response.StatusCode);
    }

    // Empty response → return default (null for reference types)
    if (string.IsNullOrWhiteSpace(body)) return default;

    // Deserialize with case-insensitive matching
    var result = JsonSerializer.Deserialize<T>(body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    // Deserialization returned null despite having content → throw
    if (result is null)
        throw new InvalidOperationException(
            $"Failed to parse response into {typeof(T).Name}. Body: {body[..Math.Min(body.Length, 300)]}");

    return result;
}
```

**Output example:** If API returns `401 {"message":"Invalid credentials"}`, the catch in `LoginViewModel` shows `"Login failed: Invalid credentials"`.

---

**Q6: Why was `CloneRequestAsync` removed from the original `ApiService`?**

**Answer:** It was over-engineering. The original code had a circular dependency: `ApiService` needed `AuthService` for token refresh, and `AuthService` needed `ApiService` for API calls. The `CloneRequestAsync` method existed to retry a failed request after refreshing the token. For a mobile app that hits a single API server, this complexity isn't justified. The simpler approach: let auth failures propagate and let the ViewModel/calling code handle re-authentication. Removed ~40 lines of complex request-cloning logic.

---

**Q7: How does `AuthService.BypassLogin()` work and why is it useful?**

**Answer:** It directly sets `CurrentUser` to a fake admin user without making any API call. Useful during development when the API isn't running, or for demo purposes. It bypasses the entire authentication flow.

---

**Q8: The app has `catch {}` blocks in ViewModels — is that good practice? How would you improve it?**

**Answer:** Empty `catch {}` is generally an anti-pattern — it swallows exceptions silently, making debugging difficult. However, in this app, the catch blocks are intentional: when the API is unavailable, they populate dummy data instead of crashing. 

**Improvement:** Log the exception to diagnose issues in production. Add a logging service:

```csharp
catch (Exception ex)
{
    Logger.LogError(ex, "Failed to load dashboard");
    // still populate dummy data
}
```

**Q9: How does `LoginViewModel` communicate with `DashboardViewModel` after login?**

**Answer:** They don't communicate directly. After successful login, `LoginViewModel` navigates to the Dashboard route:
```csharp
await NavigationService.NavigateToAsync($"//{Constants.Routes.Dashboard}");
```
Shell routing resolves `DashboardPage` → DI creates `DashboardViewModel` → `DashboardViewModel` reads `AuthService.CurrentUser` to get the logged-in user's data.

---

**Q10: What is `Constants.ApiBaseUrl` and how is it configured?**

**Answer:**
```csharp
public const string ApiBaseUrl = "http://localhost:5238";
```
Defined in `Helpers/Constants.cs`. Matches the API's `launchSettings.json`:
```json
"applicationUrl": "http://localhost:5238"
```

---

**Q11: How does the app handle JWT token storage and retrieval using `SecureStorage`?**

**Answer:**
```csharp
// Store
await SecureStorage.Default.SetAsync("auth_token", jwtToken);

// Retrieve
var token = await SecureStorage.Default.GetAsync("auth_token");

// ApiService attaches it to every request
_httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);
```

`SecureStorage` encrypts data at rest using platform-specific crypto (DPAPI on Windows, KeyChain on iOS, KeyStore on Android).

---

**Q12: Why does `App.xaml.cs` check for a stored token on startup?**

**Answer:**
To provide auto-login. If a valid token exists, the user goes straight to Dashboard instead of Login:

```csharp
shell.Loaded += async (s, e) =>
{
    var token = await _secureStorage.GetAsync("auth_token");
    if (!string.IsNullOrEmpty(token))
        await shell.GoToAsync("//dashboard");
    else
        await shell.GoToAsync("//login");
};
```

---

**Q13: How does `BaseViewModel` reduce code duplication?**

**Answer:**
It provides common properties and methods that every ViewModel needs:

```csharp
public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] bool _isBusy;
    [ObservableProperty] string _title = string.Empty;
    [ObservableProperty] bool _isRefreshing;

    protected INavigationService NavigationService { get; }
    protected IConnectivityService ConnectivityService { get; }

    protected async Task ShowAlertAsync(string title, string message, string cancel = "OK")
    {
        if (Shell.Current?.CurrentPage is not null)
            await Shell.Current.CurrentPage.DisplayAlert(title, message, cancel);
    }
}
```

Every ViewModel extends `BaseViewModel` and inherits `IsBusy`, `Title`, `IsRefreshing`, `NavigationService`, and `ShowAlertAsync`.

---

**Q14: What is the role of `ISecureStorageService`?**

**Answer:**
It wraps `SecureStorage.Default` into an injectable interface so ViewModels and services can store/retrieve sensitive data without depending on the platform API directly. This also makes the code testable — you can mock `ISecureStorageService` in unit tests.

---

**Q15: How would you add offline support for swap requests when the API is unreachable?**

**Answer:**
Use a local SQLite database to queue requests:

```csharp
// 1. Try API call
try
{
    await _api.PostAsync("/api/swap/request", request);
}
catch (HttpRequestException)
{
    // 2. Save to local queue
    await _localDb.SavePendingSyncItemAsync(new PendingSyncItem
    {
        Endpoint = "/api/swap/request",
        Payload = JsonSerializer.Serialize(request),
        CreatedAt = DateTime.UtcNow
    });
}

// 3. Background sync when connectivity restores
ConnectivityService.ConnectivityChanged += async (s, connected) =>
{
    if (connected) await SyncPendingRequestsAsync();
};
```

---

## 2. .NET MAUI Fundamentals

**Q16: What is the difference between `ContentPage`, `Shell`, and `NavigationPage` in MAUI?**

**Answer:**
| Type | Purpose |
|------|---------|
| `ContentPage` | A single screen. The base building block for all pages. |
| `Shell` | Provides URI-based navigation, flyout/tab bar, search. Used as the app's root container. |
| `NavigationPage` | Stack-based navigation with a navigation bar and back button. |

```csharp
// Shell - URI based
await Shell.Current.GoToAsync("//dashboard");

// NavigationPage - stack based
await Navigation.PushAsync(new DetailPage());
```

**Which to use?** Shell for most apps (built-in tabs, flyout, URI routing). NavigationPage for simple back-stack scenarios.

---

**Q17: How does Shell routing work? Give an example from the app.**

**Answer:**
Shell uses URI-based routing. Routes are defined in `AppShell.xaml`:

```xml
<Shell>
    <ShellContent Route="login" ContentTemplate="{DataTemplate views:LoginPage}" />
    <ShellContent Route="dashboard" ContentTemplate="{DataTemplate views:DashboardPage}" />
</Shell>
```

Navigation:
```csharp
// Absolute route (with //) - navigates to the route regardless of current state
await Shell.Current.GoToAsync("//dashboard");

// Relative route - pushes onto the current stack
await Shell.Current.GoToAsync("stations");
```

---

**Q18: What are the differences between `Transient` and `Singleton` lifetimes in MAUI DI?**

**Answer:**
| Lifetime | Created | Disposed | Use Case |
|----------|---------|---------|----------|
| `AddSingleton` | Once, on first resolution | When app exits | Shared state (AuthService, HttpClient) |
| `AddTransient` | Every time it's injected | When scope ends | ViewModels, Pages (new instance per navigation) |
| `AddScoped` | Once per scope | When scope ends | Rarely used in MAUI (no per-request scope like web apps) |

```csharp
builder.Services.AddSingleton<IAuthService, AuthService>();  // one instance
builder.Services.AddTransient<LoginViewModel>();             // new instance each time
```

---

**Q19: How would you pass complex data between pages in MAUI?**

**Answer:**
**Method 1: QueryProperty**
```csharp
[QueryProperty(nameof(Station), "Station")]
public partial class SwapRequestViewModel : BaseViewModel
{
    public StationModel? Station { get; set; }
}

// Navigation
await Shell.Current.GoToAsync("swaprequest", new Dictionary<string, object>
{
    { "Station", selectedStation }
});
```

**Method 2: Static service / shared state**
```csharp
// AuthService.CurrentUser is a singleton - accessible from any ViewModel
var user = _authService.CurrentUser;
```

**Method 3: MessagingCenter / WeakReferenceMessenger**
```csharp
WeakReferenceMessenger.Default.Send(new SwapCompletedMessage(swapId));
```

---

**Q20: What is `AppThemeBinding` and how is it used in `Styles.xaml`?**

**Answer:**
`AppThemeBinding` switches a property value based on the current OS theme (Light/Dark):

```xml
<Style TargetType="Entry">
    <Setter Property="TextColor" Value="{AppThemeBinding Light=Black, Dark=White}" />
</Style>
```

In light mode, Entry text is black. In dark mode, it's white. This is defined globally in `Resources/Styles/Styles.xaml` so every Entry in the app automatically adapts.

---

**Q21: How does MAUI handle platform-specific code?**

**Answer:**
Three approaches:

1. **Platform folders:** Files in `Platforms/Windows/`, `Platforms/Android/`, `Platforms/iOS/` compile only for that platform.

2. **Conditional compilation:** `#if WINDOWS`, `#if ANDROID`, `#if IOS`

3. **Partial classes:** Same file name with `.Windows.cs`, `.Android.cs`, `.iOS.cs` suffix.

```csharp
#if WINDOWS
    // Windows-specific code
    _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5238") };
#endif
```

---

**Q22: What layout panels are available in MAUI and when would you use each?**

**Answer:**
| Layout | Behavior | Use Case |
|--------|----------|----------|
| `VerticalStackLayout` | Stacks children vertically | Simple column layouts |
| `HorizontalStackLayout` | Stacks children horizontally | Toolbars, button rows |
| `Grid` | Row/column-based positioning | Complex forms, dashboards |
| `FlexLayout` | Flexbox-like wrapping | Responsive layouts |
| `AbsoluteLayout` | Exact x/y positioning | Overlays, absolute positioning |
| `ScrollView` | Scrollable content | Forms longer than screen height |

```xml
<!-- Grid is most used in this app -->
<Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto">
    <Label Text="Balance" Grid.Row="0" Grid.Column="0" />
    <Label Text="$250" Grid.Row="0" Grid.Column="1" />
</Grid>
```

---

**Q23: Explain how `CollectionView` differs from `ListView`. Which is better for performance?**

**Answer:**
| Feature | CollectionView | ListView |
|---------|---------------|----------|
| Performance | Better (uses less memory) | Good but older |
| Layout | Vertical, Horizontal, Grid | Vertical only |
| Selection | None, Single, Multiple | Single only |
| Empty view | Built-in `EmptyView` property | Manual implementation |
| Header/Footer | Built-in | Built-in |
| Grouping | Supported | Supported |

**Recommendation:** Always use `CollectionView` for new MAUI apps. It's the successor to `ListView` with better performance and more features.

---

**Q24: How does data binding work in MAUI XAML?**

**Answer:**
Data binding connects a UI property to a ViewModel property. When the ViewModel property changes, the UI updates automatically (if `INotifyPropertyChanged` is implemented).

```xml
<Entry Text="{Binding Username}" />
```

The ViewModel:
```csharp
[ObservableProperty]
private string _username = string.Empty;
```

The `[ObservableProperty]` source generator creates the property, raises `PropertyChanged`, and the UI re-reads the value.

---

**Q25: What is `x:DataType` and why is it important for compiled bindings?**

**Answer:**
`x:DataType` tells the XAML compiler the type of the binding context, enabling compile-time binding validation:

```xml
<ContentPage x:DataType="vm:LoginViewModel">
    <Entry Text="{Binding Username}" />  <!-- compiler checks Username exists on LoginViewModel -->
</ContentPage>
```

Without it, bindings are resolved at runtime. If you typo "Usernam", no compile error — just a silent runtime failure. With `x:DataType`, it becomes a compile error.

---

**Q26: How do you handle large lists in MAUI without freezing the UI?**

**Answer:**
1. **Use `CollectionView`** — it virtualizes items (only renders visible ones).
2. **Use `AsyncCommand` / background loading** — load data on a background thread.
3. **Paginate** — load 20 items at a time using `RemainingItemsThreshold`.
4. **Use `ObservableCollection`** for incremental adds (avoids replacing the entire list).

```xml
<CollectionView ItemsSource="{Binding Stations}"
                RemainingItemsThreshold="5"
                RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}">
```

---

**Q27: What is `VisualStateManager` and how is it used in button styles?**

**Answer:**
`VisualStateManager` lets you define different visual states for a control. MAUI buttons have states like Normal, Disabled, PointerOver, Focused:

```xml
<Style TargetType="Button">
    <Setter Property="VisualStateManager.VisualStateGroups">
        <VisualStateGroupList>
            <VisualState x:Name="Normal" />
            <VisualState x:Name="Disabled">
                <VisualState.Setters>
                    <Setter Property="TextColor" Value="Gray" />
                    <Setter Property="BackgroundColor" Value="LightGray" />
                </VisualState.Setters>
            </VisualState>
        </VisualStateGroupList>
    </Setter>
</Style>
```

When `IsEnabled=false`, the button automatically changes to the Disabled visual state.

---

**Q28: How would you implement pull-to-refresh in a MAUI app?**

**Answer:**
Use `RefreshView` wrapping your scrollable content:

```xml
<RefreshView IsRefreshing="{Binding IsRefreshing}"
             Command="{Binding RefreshStationsCommand}">
    <CollectionView ItemsSource="{Binding Stations}" />
</RefreshView>
```

ViewModel:
```csharp
[RelayCommand]
async Task RefreshStationsAsync()
{
    IsRefreshing = true;
    await LoadStationsAsync();  // API call or dummy data
    IsRefreshing = false;
}
```

---

**Q29: Explain the MAUI lifecycle: `OnAppearing`, `OnDisappearing`.**

**Answer:**
```csharp
protected override void OnAppearing()
{
    base.OnAppearing();
    // Called when page becomes visible
    // Best place to load data:
    if (ViewModel is not null)
        ViewModel.LoadCommand.Execute(null);
}

protected override void OnDisappearing()
{
    base.OnDisappearing();
    // Called when page is no longer visible
    // Cleanup timers, unsubscribes from events
}
```

Flow: `Constructor → OnAppearing → (user interacts) → OnDisappearing → OnAppearing (if navigated back)`

---

**Q30: How do you style an app globally using `ResourceDictionary`?**

**Answer:**
Define styles in `Resources/Styles/Styles.xaml` and merge it in `App.xaml`:

```xml
<!-- App.xaml -->
<Application.Resources>
    <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
</Application.Resources>
```

Then any control in the app automatically picks up the matching style:
```xml
<!-- This Entry automatically uses the style from Styles.xaml -->
<Entry Text="Hello" />
```

---

## 3. C# Language & .NET Runtime

**Q31: Explain `async` / `await` — what happens on the call stack when you await a task?**

**Answer:**
When you `await` a task:
1. The method returns control to its caller (not blocking the thread).
2. The runtime captures the current synchronization context (on UI thread, this is the main thread).
3. When the task completes, the remaining code executes on the captured context (back on the UI thread).

```csharp
async Task LoadDataAsync()
{
    // Runs on UI thread
    IsBusy = true;

    // Await starts the HTTP call on a background thread, yields UI thread
    var data = await _httpClient.GetStringAsync(url);

    // Continuation runs back on UI thread - safe to update UI
    IsBusy = false;
}
```

---

**Q32: What is `Task<T>` vs `ValueTask<T>`?**

**Answer:**
| Type | Heap allocation | Use Case |
|------|----------------|----------|
| `Task<T>` | Always allocates on heap | Async operations that usually run asynchronously |
| `ValueTask<T>` | No allocation if result is synchronous | Operations that often complete synchronously (e.g., cached results) |

```csharp
public async ValueTask<User?> GetUserAsync(int id)
{
    if (_cache.TryGetValue(id, out var user))
        return user;  // synchronous - no allocation
    return await _api.GetAsync<User>($"/users/{id}");  // async - may allocate
}
```

**Rule of thumb:** Use `Task<T>` by default. Use `ValueTask<T>` only when performance profiling shows it matters.

---

**Q33: What is a deadlock with `async` / `await` and how do you avoid it in MAUI?**

**Answer:**
Classic deadlock: blocking on async code with `.Result` or `.Wait()` on a UI thread:

```csharp
// DEADLOCK!
var data = _api.GetAsync<User>("/users/1").Result;
```

**Why:** `await` captures the UI context. `.Result` blocks the UI thread. The async method can't resume because the UI thread is blocked.

**Fix:** Always use `await` all the way down. Never use `.Result` or `.Wait()`.

---

**Q34: Explain `ConfigureAwait(false)` — should you use it in MAUI apps?**

**Answer:**
`ConfigureAwait(false)` tells the runtime NOT to capture the sync context. The continuation can run on any thread.

```csharp
await _httpClient.GetStringAsync(url).ConfigureAwait(false);
```

**In MAUI:** Do NOT use `ConfigureAwait(false)` in ViewModel code that updates UI — the continuation might run on a background thread, and UI updates must happen on the main thread. Use it only in library code (services, data access) that doesn't touch UI.

---

**Q35: How does `JsonSerializer.Deserialize<T>` handle missing or extra JSON properties?**

**Answer:**
```csharp
// JSON: {"name":"John","extra":"ignored"}
// Class: public class Person { public string Name { get; set; } }

var person = JsonSerializer.Deserialize<Person>(json,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
// Result: Person.Name = "John", extra field "extra" is ignored
```

- **Missing properties:** Set to default value (null for strings, 0 for ints).
- **Extra properties:** Ignored by default.
- **Case sensitivity:** `PropertyNameCaseInsensitive = true` matches "name" to "Name", "NAME", etc.

---

**Q36: What is the difference between `record` and `class` in C#?**

**Answer:**
| Feature | class | record |
|---------|-------|--------|
| Equality | Reference equality (`object.ReferenceEquals`) | Value equality (compares all properties) |
| Immutability | Mutable by default | `record` is immutable; `record class` is positional |
| `ToString()` | Returns type name | Returns all property values |
| `Deconstruct` | Manual | Auto-generated for positional records |

```csharp
public record Person(string Name, int Age);
// vs
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// Records are great for DTOs - two records with same values are equal
var a = new Person("Alice", 30);
var b = new Person("Alice", 30);
Console.WriteLine(a == b); // true (with record), false (with class)
```

---

**Q37: Explain nullable reference types.**

**Answer:**
```csharp
public string Name { get; set; }         // non-nullable - compiler warns if might be null
public string? Description { get; set; } // nullable - OK to be null

UserModel? user = null;  // nullable reference type
```

**Why `string` is already nullable but `UserModel?` uses `?`:** Before nullable reference types (C# 8), ALL reference types were nullable. Now, with `#nullable enable`, `string` means non-nullable, `string?` means nullable. The `?` suffix explicitly marks the reference type as nullable.

---

**Q38: What is pattern matching in C#?**

**Answer:**
Pattern matching lets you check the shape of data concisely:

```csharp
// Switch expression with pattern matching
var description = swap.Status switch
{
    "Pending" => "Awaiting approval",
    "InProgress" => "Swap in progress",
    "Completed" => "Swap done",
    _ => "Unknown status"  // default case
};

// Property pattern
if (user is { IsActive: true, Roles: ["Admin"] })
    Console.WriteLine("Active admin user");
```

---

**Q39: How does `List<T>.ForEach` differ from a `foreach` loop?**

**Answer:**
```csharp
// foreach - can use await, break, continue
foreach (var station in stations)
{
    await LoadDetailsAsync(station);  // async supported
    if (station.IsClosed) break;
}

// List.ForEach - no async, no break/continue
stations.ForEach(s => Console.WriteLine(s.Name));
```

`foreach` is more flexible (async, control flow). `List.ForEach` is more concise for simple side-effects.

---

**Q40: What is `FireAndForget` and why is it used in `SettingsViewModel`?**

**Answer:**
`FireAndForget` means running an async method without awaiting it. Used when you don't need to wait for the result:

```csharp
partial void OnIsBiometricEnabledChanged(bool value)
{
    // Fire and forget - don't block UI while saving
    _secureStorage.SaveAsync("biometric_enabled", value.ToString()).FireAndForget();
}
```

**Risk:** Unobserved exceptions crash the app. Always handle exceptions inside the fire-and-forget call.

---

**Q41: Explain the `IDisposable` pattern.**

**Answer:**
`IDisposable` releases unmanaged resources (file handles, network connections) deterministically:

```csharp
public class ApiService : IDisposable
{
    private readonly HttpClient _httpClient;

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
```

In MAUI, Singleton services live as long as the app, so disposal happens at app shutdown. Transient services that hold resources should implement `IDisposable`.

---

**Q42: What is `Span<T>` and how does it differ from array slices?**

**Answer:**
`Span<T>` is a stack-allocated, ref-safe view over contiguous memory. No heap allocation.

```csharp
int[] numbers = { 0, 1, 2, 3, 4, 5 };
Span<int> slice = numbers.AsSpan(1, 3);  // { 1, 2, 3 }
slice[0] = 99;                           // modifies original array
Console.WriteLine(numbers[1]);           // 99
```

**vs array slice:** `Span<T>` is allocation-free and supports more memory types (stack, native, managed).

---

**Q43: How does `yield return` work internally?**

**Answer:**
The compiler generates a state machine class that implements `IEnumerable<T>` and `IEnumerator<T>`:

```csharp
public IEnumerable<int> GetNumbers()
{
    yield return 1;
    yield return 2;  // method resumes here after MoveNext()
    yield return 3;
}
// Compiler generates a class with:
// - Current property
// - MoveNext() method with state machine
// - Dispose() for cleanup
```

---

**Q44: What is the difference between `StringBuilder` and string concatenation in a loop?**

**Answer:**
```csharp
// BAD - creates 10000 intermediate strings
string result = "";
for (int i = 0; i < 10000; i++)
    result += i.ToString();

// GOOD - single buffer, no intermediate allocations
var sb = new StringBuilder();
for (int i = 0; i < 10000; i++)
    sb.Append(i);
string result = sb.ToString();
```

Strings are immutable. Each `+=` creates a new string, copying the old content. `StringBuilder` maintains an internal buffer that grows as needed.

---

**Q45: Explain covariance and contravariance in C# generics.**

**Answer:**
```csharp
// Covariance (out) - you can use a more derived type
IEnumerable<object> objects = new List<string>();  // string → object

// Contravariance (in) - you can use a less derived type
Action<object> objAction = o => Console.WriteLine(o);
Action<string> strAction = objAction;  // works because Action<in T>

// Invariant - default - List<string> is NOT List<object>
List<object> objs = new List<string>();  // ❌ compile error
```

Covariance uses `out T` (output position only). Contravariance uses `in T` (input position only).

---

## 4. Entity Framework Core & Database

**Q46: How does `DbInitializer.SeedData()` work?**

**Answer:**
It runs at application startup (in `Program.cs`) to populate the database with demo data:

```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    DbInitializer.SeedData(db);
}
```

`SeedData` creates 6 users (admin + 5 riders), 30 batteries, 5 stations, 26 trips, 15 swaps, wallet transactions, fleet assignments, maintenance requests, support tickets, and notifications.

**Q47: What is Code-First migration in EF Core?**

**Answer:**
You write C# entity classes, and EF Core generates the database schema:

```bash
dotnet ef migrations add AddBatterySerialNumber
dotnet ef database update
```

The migration file contains `Up()` (apply) and `Down()` (revert) methods:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "SerialNumber",
        table: "Batteries",
        type: "nvarchar(100)",
        nullable: false,
        defaultValue: "");
}
```

---

**Q48: Explain `Include()` and `ThenInclude()` in EF Core.**

**Answer:**
Eager loading of related data:

```csharp
// Load station with its batteries
var station = context.Stations
    .Include(s => s.Batteries)          // load Batteries navigation property
    .ThenInclude(b => b.SwapHistories)  // then load each battery's swap history
    .FirstOrDefault(s => s.Id == 1);
```

Without `Include()`, navigation properties are null. EF Core lazy loading would make separate DB queries for each access.

---

**Q49: What is the difference between `FirstOrDefault()` and `SingleOrDefault()`?**

**Answer:**
| Method | Returns | Throws If |
|--------|---------|-----------|
| `FirstOrDefault()` | First matching element, or null | Never throws (for valid scenarios) |
| `SingleOrDefault()` | Single matching element, or null | More than one matches the predicate |

```csharp
var user = db.Users.FirstOrDefault(u => u.Id == 1);     // OK - one match
var user = db.Users.SingleOrDefault(u => u.Role == "Admin"); // ❌ might throw if multiple admins
```

Use `FirstOrDefault` when you expect 0 or 1+ results. Use `SingleOrDefault` when you expect exactly 0 or 1.

---

**Q50: How does EF Core change tracking work?**

**Answer:**
When you query entities, EF Core takes a snapshot of their values. When you call `SaveChangesAsync()`, it compares current values to the snapshot to determine what changed:

```csharp
var user = db.Users.Find(1);  // tracked - snapshot taken
user.Email = "new@email.com";  // EF detects modification
await db.SaveChangesAsync();   // generates UPDATE SQL
```

`AsNoTracking()` disables this for read-only queries, improving performance:
```csharp
var users = db.Users.AsNoTracking().ToList();  // no tracking overhead
```

---

**Q51: What is the difference between SQL Server and PostgreSQL with EF Core?**

**Answer:**
| Feature | SQL Server | PostgreSQL |
|---------|-----------|------------|
| NuGet package | `Microsoft.EntityFrameworkCore.SqlServer` | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Schema | `dbo` by default | `public` by default |
| JSON columns | Supported (SQL Server 2016+) | Excellent native JSON support |
| Array types | Not supported | Native array type |
| Case sensitivity | Case-insensitive by default | Case-sensitive |
| Connection string | `Server=.;Database=db;Trusted_Connection=True;` | `Host=localhost;Database=db;Username=postgres;` |

---

**Q52: How would you handle a million battery swap records efficiently?**

**Answer:**
1. **Indexing** — index on `CreatedAt`, `StationId`, `RiderId`
2. **Pagination** — never `ToList()` everything. Use `Skip()`/`Take()`.
3. **Projection** — select only needed columns with `.Select()`.
4. **Batch processing** — process in chunks of 1000.
5. **Read-only queries** — use `AsNoTracking()`.

```csharp
var page = await context.Swaps
    .AsNoTracking()
    .Where(s => s.StationId == stationId)
    .OrderByDescending(s => s.CreatedAt)
    .Select(s => new SwapSummary { Id = s.Id, CompletedAt = s.CreatedAt })
    .Skip(page * 20)
    .Take(20)
    .ToListAsync();
```

---

**Q53: What is a migration bundle?**

**Answer:**
A self-contained executable that applies migrations to a database. Useful for CI/CD deployment where you don't want the EF Core CLI tool installed:

```bash
dotnet ef migrations bundle --output deploy-migrations.exe
# Deploy and run:
./deploy-migrations.exe
```

---

**Q54: How does EF Core map C# `enum` types to the database?**

**Answer:**
By default, enums are stored as integers:

```csharp
public enum BatteryStatus { Available, InUse, Maintenance, Disposed }

public class Battery
{
    public BatteryStatus Status { get; set; }
}
// Database column: Status INT (0, 1, 2, 3)
```

To store as strings:
```csharp
builder.Property(b => b.Status).HasConversion<string>();
// Database column: Status NVARCHAR ("Available", "InUse", etc.)
```

---

**Q55: What is the `IQueryable<T>` vs `IEnumerable<T>` difference?**

**Answer:**
| Feature | IQueryable | IEnumerable |
|---------|-----------|-------------|
| Execution | Deferred, on the database server | In-memory |
| Filtering | SQL WHERE clause | LINQ to Objects |
| Performance | Efficient for large datasets | Loads all data first |

```csharp
IQueryable<User> query = db.Users.Where(u => u.IsActive);  // SQL: WHERE IsActive = 1
IEnumerable<User> enumerable = db.Users.ToList().Where(u => u.IsActive);  // loads ALL users first
```

---

## 5. REST API & HTTP Communication

**Q56: Explain the full HTTP request/response lifecycle of a login call.**

**Answer:**
1. **Mobile:** `ApiService.PostAsync<AuthResponse>("/api/auth/login", loginRequest)`
2. **Serialization:** `PostAsJsonAsync` serializes `LoginRequest` to `{"username":"admin","password":"Admin@123"}`
3. **HTTP Request:** `POST http://localhost:5238/api/auth/login` with `Content-Type: application/json`
4. **API Routing:** ASP.NET Core maps to `AuthController.Login()`
5. **Model Binding:** Framework deserializes JSON to `LoginRequest` object
6. **Service:** `AuthService.LoginAsync()` verifies password hash, generates JWT
7. **Response:** Returns `200 OK` with JSON body: `{"token":"eyJ...","refreshToken":"...","user":{...}}`
8. **Deserialization:** Mobile receives response, `JsonSerializer.Deserialize<AuthResponse>(body)`
9. **Storage:** AuthService stores token in `SecureStorage`, sets `CurrentUser`
10. **Navigation:** LoginViewModel navigates to `//dashboard`

---

**Q57: What status codes does `ApiService.HandleResponse` consider successful?**

**Answer:**
Any 2xx status code. `HttpResponseMessage.IsSuccessStatusCode` returns `true` for 200-299. The method throws an `HttpRequestException` for all non-success codes (401, 403, 404, 500, etc.).

---

**Q58: How does JWT authentication work?**

**Answer:**
JWT (JSON Web Token) has three parts: `header.payload.signature`

```
eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIiwicm9sZSI6IkFkbWluIn0.abc123signature
```

- **Access token:** Short-lived (15-60 min). Sent with every API request in the `Authorization: Bearer` header.
- **Refresh token:** Long-lived (days). Used to get a new access token without re-entering credentials.

```csharp
// API validates token on every request
[Authorize]  // ← this attribute checks the JWT
public class SwapController : ControllerBase
```

---

**Q59: How would you intercept all HTTP requests to add logging?**

**Answer:**
Use `DelegatingHandler` — a middleware for `HttpClient`:

```csharp
public class LoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        Debug.WriteLine($"→ {request.Method} {request.RequestUri}");
        var response = await base.SendAsync(request, ct);
        Debug.WriteLine($"← {(int)response.StatusCode} {response.ReasonPhrase}");
        return response;
    }
}

// Register
_httpClient = new HttpClient(new LoggingHandler { InnerHandler = new HttpClientHandler() });
```

---

**Q60: What is the `HttpClient` lifetime in a MAUI app?**

**Answer:**
`HttpClient` is designed to be reused. Creating a new `HttpClient` for every request can exhaust TCP ports (socket exhaustion). In MAUI, register it as a Singleton:

```csharp
builder.Services.AddSingleton<HttpClient>(_ =>
    new HttpClient { BaseAddress = new Uri("http://localhost:5238") });
```

`IHttpClientFactory` (common in ASP.NET Core) is not needed in MAUI since there's typically one API server.

---

**Q61: How does `PostAsJsonAsync<T>` serialize your request object?**

**Answer:**
```csharp
await _httpClient.PostAsJsonAsync(endpoint, new { Username = "admin", Password = "Admin@123" });
// Sends: POST /api/auth/login
// Body: {"username":"admin","password":"Admin@123"}
// Content-Type: application/json
```

It uses `JsonSerializerDefaults.Web` (camelCase naming policy, case-insensitive deserialization).

---

**Q62: What is `EnsureSuccessStatusCode()` and what happens when it fails?**

**Answer:**
```csharp
response.EnsureSuccessStatusCode();
// Throws HttpRequestException if status code is NOT 2xx
// Exception includes status code: "Response status code does not indicate success: 401 (Unauthorized)."
```

---

**Q63: How would you implement request retry with exponential backoff?**

**Answer:**
Using Polly NuGet package:

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

await retryPolicy.ExecuteAsync(() =>
    _httpClient.PostAsync(endpoint, content));
```

Retry after 2s, then 4s, then 8s.

---

**Q64: What is `MultipartFormDataContent` used for?**

**Answer:**
Uploading files (images, documents) to the server:

```csharp
var content = new MultipartFormDataContent();
var imageBytes = await File.ReadAllBytesAsync("photo.jpg");
content.Add(new ByteArrayContent(imageBytes), "file", "photo.jpg");
await _httpClient.PostAsync("/api/upload", content);
```

---

**Q65: How do you handle file uploads in MAUI?**

**Answer:**
```csharp
var result = await FilePicker.PickAsync(new PickOptions
{
    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        { DevicePlatform.WinUI, new[] { ".jpg", ".png" } }
    })
});

if (result is not null)
{
    var stream = await result.OpenReadAsync();
    var content = new MultipartFormDataContent();
    content.Add(new StreamContent(stream), "file", result.FileName);
    await _apiService.PostMultipartAsync<object>("/api/user/avatar", content);
}
```

---

## 6. MVVM & Data Binding

**Q66: Explain the MVVM pattern.**

**Answer:**
MVVM separates code into three layers:

```
View (XAML) ←→ ViewModel (logic) ←→ Model (data)
   binds to        orchestrates          business objects
```

- **Model:** Data classes (UserModel, StationModel, AuthResponse)
- **ViewModel:** Observable properties + commands (LoginViewModel, DashboardViewModel)
- **View:** XAML pages with data bindings (LoginPage.xaml)

**Flow:** View binds to ViewModel properties. User interacts with View → ViewModel processes → updates Model → notifies View via `INotifyPropertyChanged`.

---

**Q67: What does `[ObservableProperty]` generate behind the scenes?**

**Answer:**
```csharp
// You write:
[ObservableProperty]
private string _userName = string.Empty;

// Source generator creates:
public string UserName
{
    get => _userName;
    set
    {
        if (_userName != value)
        {
            _userName = value;
            OnPropertyChanged(nameof(UserName));
            OnUserNameChanged(value);  // partial method
        }
    }
}
```

The source generator runs at compile time and produces the property, `INotifyPropertyChanged` implementation, and `partial void OnUserNameChanged()` method.

---

**Q68: What is the difference between `[RelayCommand]` and `ICommand`?**

**Answer:**
| Aspect | `[RelayCommand]` | Manual `ICommand` |
|--------|-----------------|-------------------|
| Code | One attribute | 10+ lines per command |
| CanExecute | `[RelayCommand(CanExecute = nameof(CanSave))]` | Must implement `CanExecute` |
| Async | `private async Task SaveAsync()` → generates `ICommand` | Must wrap manually |

```csharp
// With source generator:
[RelayCommand]
async Task LoginAsync() { ... }

// Generates:
public ICommand LoginCommand => _loginCommand ??= new AsyncRelayCommand(LoginAsync);
```

---

**Q69: How does `INotifyPropertyChanged` work?**

**Answer:**
```csharp
public class MyViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
        }
    }
}
```

When `Name` changes, it raises `PropertyChanged`. The UI binding listens for this event and re-reads the property value. With `[ObservableProperty]`, this is all auto-generated.

---

**Q70: What is `ObservableCollection<T>` and when should you use it?**

**Answer:**
`ObservableCollection<T>` raises `CollectionChanged` when items are added/removed/replaced. The UI automatically updates.

```csharp
// DO use ObservableCollection for lists that change after loading:
[ObservableProperty]
ObservableCollection<StationModel> _stations = new();

stations.Add(newStation);  // UI updates automatically

// DON'T use for read-only lists - List<T> is fine if you set it once
```

---

**Q71: How does two-way binding work for `Entry` fields?**

**Answer:**
```xml
<Entry Text="{Binding Username, Mode=TwoWay}" />
```

Two-way binding means:
- **ViewModel → View:** When `Username` changes, `Entry.Text` updates.
- **View → ViewModel:** When user types, `Username` property updates (on each keystroke by default).

The `UpdateSourceTrigger` can be changed:
```xml
<Entry Text="{Binding Username, Mode=TwoWay}" />
<!-- By default updates on each keystroke in MAUI -->
```

---

**Q72: What is `QueryProperty` and how is it used?**

**Answer:**
`QueryProperty` receives navigation parameters automatically:

```csharp
[QueryProperty(nameof(Station), "Station")]
public partial class SwapRequestViewModel : BaseViewModel
{
    public StationModel? Station { get; set; }
}

// Navigation:
await Shell.Current.GoToAsync("swaprequest", new Dictionary<string, object>
{
    { "Station", selectedStation }
});
```

When navigating, Shell sets the `Station` property on the ViewModel before `OnAppearing` fires.

---

**Q73: How does `BaseViewModel.ShowAlertAsync` work?**

**Answer:**
```csharp
protected async Task ShowAlertAsync(string title, string message, string cancel = "OK")
{
    if (Shell.Current?.CurrentPage is not null)
        await Shell.Current.CurrentPage.DisplayAlert(title, message, cancel);
}
```

It accesses `Shell.Current.CurrentPage` (the currently visible page) and calls `DisplayAlert` on it. This works because `Shell.Current` is a static property that always points to the app's Shell instance.

---

**Q74: What is the role of `IValueConverter`?**

**Answer:**
Converts binding values between the ViewModel and UI:

```csharp
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (bool)value ? Colors.Green : Colors.Red;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();  // one-way converter
    }
}
```

Usage: `TextColor="{Binding IsActive, Converter={StaticResource BoolToColorConverter}}"`

---

**Q75: How would you bind a button's `IsEnabled` to a ViewModel property?**

**Answer:**
```xml
<Button Text="Submit" Command="{Binding SubmitCommand}"
        IsEnabled="{Binding CanSubmit}" />
```

ViewModel:
```csharp
[ObservableProperty] bool _canSubmit;

// Or use CanExecute on the command:
[RelayCommand(CanExecute = nameof(CanSubmit))]
async Task SubmitAsync() { ... }
```

---

**Q76: Explain `x:Bind` vs `Binding` in MAUI.**

**Answer:**
| Feature | `x:Bind` (compiled) | `Binding` (reflection) |
|---------|--------------------|------------------------|
| Performance | Fast (compile-time) | Slower (runtime reflection) |
| Error detection | Compile-time | Runtime |
| DataType required | Yes (`x:DataType`) | No |
| Source | Must be known at compile time | Can be dynamic (e.g., code-behind) |

**Recommendation:** Use `x:Bind` with `x:DataType` for better performance and compile-time safety.

---

**Q77: What is `x:Load` and how does it improve performance?**

**Answer:**
`x:Load` defers loading of XAML elements until they're needed:

```xml
<AdminPanel x:Load="{Binding IsAdmin}" />  <!-- only loads if user is admin -->
```

Saves memory and startup time by not creating the control tree until the condition is met.

---

**Q78: How does the `CommunityToolkit.Mvvm` source generator work?**

**Answer:**
It uses C# Roslyn source generators. At compile time, it analyzes your code for attributes like `[ObservableProperty]` and `[RelayCommand]`, then generates the implementation code. The generated code is compiled into your assembly — no runtime reflection needed.

---

**Q79: What are `partial` methods and how does `OnIsBiometricEnabledChanged` get called?**

**Answer:**
The source generator creates a `partial` method signature:
```csharp
partial void OnIsBiometricEnabledChanged(bool value);
```

And calls it inside the property setter:
```csharp
set
{
    if (_isBiometricEnabled != value)
    {
        _isBiometricEnabled = value;
        OnPropertyChanged();
        OnIsBiometricEnabledChanged(value);  // ← called here
    }
}
```

You implement it to react to property changes:
```csharp
partial void OnIsBiometricEnabledChanged(bool value)
{
    _storage.SaveAsync("biometric_enabled", value.ToString()).FireAndForget();
}
```

---

**Q80: How would you implement master-detail navigation with Shell?**

**Answer:**
```xml
<Shell>
    <!-- Master/flyout -->
    <FlyoutItem Title="Menu">
        <ShellContent Route="stations" ContentTemplate="{DataTemplate views:StationsPage}" />
        <ShellContent Route="dashboard" ContentTemplate="{DataTemplate views:DashboardPage}" />
    </FlyoutItem>

    <!-- Detail pages (no flyout) -->
    <ShellContent Route="stationdetail" ContentTemplate="{DataTemplate views:StationDetailPage}"
                  FlyoutItemIsVisible="False" />
</Shell>
```

---

## 7. Dependency Injection

**Q81: What is dependency injection and why is it used?**

**Answer:**
DI supplies an object's dependencies from the outside rather than the object creating them itself. Benefits: loose coupling, testability, centralized configuration.

```csharp
// Without DI - tight coupling
public class LoginViewModel
{
    private readonly AuthService _auth = new AuthService();  // hard dependency
}

// With DI - loose coupling
public class LoginViewModel
{
    private readonly IAuthService _auth;
    public LoginViewModel(IAuthService auth) => _auth = auth;
}
```

---

**Q82: What is the difference between `AddSingleton`, `AddTransient`, and `AddScoped`?**

**Answer:**
```csharp
builder.Services.AddSingleton<IAuthService, AuthService>();    // one instance for app lifetime
builder.Services.AddTransient<LoginViewModel>();                // new instance per injection
builder.Services.AddScoped<MyService>();                        // one per scope
```

- **Singleton:** Created once, shared everywhere. Good for: HttpClient, auth state, configuration.
- **Transient:** Created every time. Good for: ViewModels, pages.
- **Scoped:** One per scope. Rarely used in MAUI (no request scopes like web apps).

---

**Q83: How does the DI container resolve `LoginViewModel`?**

**Answer:**
1. DI container sees `LoginViewModel` needs `IAuthService`, `ISecureStorageService`, `INavigationService`, `IConnectivityService` in its constructor.
2. It looks up each interface in its registry.
3. `IAuthService` → registered as `AuthService` singleton.
4. `AuthService` constructor needs `IApiService` and `ISecureStorageService` → resolves them recursively.
5. Once all dependencies are built, creates `LoginViewModel` and returns it.

---

**Q84: What happens if you register the same interface twice?**

**Answer:**
The last registration wins:

```csharp
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IAuthService, FakeAuthService>();  // overrides above
```

For multiple implementations, register with keys or use `IEnumerable<T>`:
```csharp
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IAuthService, FakeAuthService>();
var services = serviceProvider.GetServices<IAuthService>();  // both
```

---

**Q85: How would you register a service differently for Debug vs Release?**

**Answer:**
```csharp
#if DEBUG
    builder.Services.AddSingleton<IApiService, MockApiService>();
#else
    builder.Services.AddSingleton<IApiService, ApiService>();
#endif
```

Or use the hosting environment:
```csharp
if (builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<IApiService, MockApiService>();
```

---

**Q86: What is the Service Locator anti-pattern?**

**Answer:**
Resolving services on demand from a static container:

```csharp
// Anti-pattern - hides dependencies
public void Login()
{
    var auth = App.ServiceLocator.GetService<IAuthService>();  // magic!
    auth.LoginAsync(...);
}
```

**Why it's bad:** Dependencies are implicit. Hard to know what a class needs without reading its entire code. Hard to test (can't easily mock). Proper DI makes dependencies explicit via the constructor.

---

**Q87: How does constructor injection work with MAUI Shell pages?**

**Answer:**
When Shell navigates to a page, MAUI's DI container resolves it:

```csharp
// Shell route resolves StationsPage
// DI sees StationsPage constructor needs StationViewModel
// DI creates StationViewModel (resolving its dependencies)
// DI creates StationsPage with the ViewModel
// Shell sets the page as BindingContext
```

---

**Q88: What is `IServiceProvider` and how was it used before refactoring?**

**Answer:**
`IServiceProvider` is the DI container itself. It was used in `ApiService` to lazily resolve `AuthService` to avoid a circular dependency:

```csharp
// Before refactoring
public class ApiService
{
    private readonly IServiceProvider _serviceProvider;
    private IAuthService? _authService;
    private IAuthService AuthService =>
        _authService ??= _serviceProvider.GetRequiredService<IAuthService>();
}
```

This was removed because the circular dependency was eliminated by simplifying the token refresh flow.

---

**Q89: How would you inject configuration like URLs into a service?**

**Answer:**
Options pattern:

```csharp
public class ApiOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5238";
}

// Registration
builder.Services.Configure<ApiOptions>(options =>
    options.BaseUrl = "http://localhost:5238");
builder.Services.AddSingleton<IApiService, ApiService>();

// Usage
public class ApiService
{
    public ApiService(IOptions<ApiOptions> options)
    {
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
    }
}
```

---

**Q90: What is the composition root in MAUI?**

**Answer:**
The composition root is where all dependencies are wired up — `MauiProgram.cs`:

```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    // All registrations happen here
    builder.Services.AddSingleton<IApiService, ApiService>();
    builder.Services.AddTransient<LoginViewModel>();
    builder.Services.AddTransient<LoginPage>();
    return builder.Build();  // Container is sealed after Build()
}
```

The composition root should be the only place where you configure DI. Avoid registering services in other parts of the code.

---

## 8. Testing & Debugging

**Q91: How would you unit test `LoginViewModel.LoginAsync()`?**

**Answer:**
Use a mocking framework (Moq, NSubstitute) to mock dependencies:

```csharp
[Test]
public async Task LoginAsync_ValidCredentials_NavigatesToDashboard()
{
    // Arrange
    var authMock = new Mock<IAuthService>();
    authMock.Setup(a => a.LoginAsync("admin", "pass"))
            .ReturnsAsync(new AuthResponse { Token = "jwt", User = new UserModel() });

    var navMock = new Mock<INavigationService>();
    var vm = new LoginViewModel(authMock.Object, Mock.Of<ISecureStorageService>(),
                                navMock.Object, Mock.Of<IConnectivityService>());

    // Act
    vm.Username = "admin";
    vm.Password = "pass";
    await vm.LoginCommand.ExecuteAsync(null);

    // Assert
    navMock.Verify(n => n.NavigateToAsync("//dashboard"), Times.Once);
}
```

---

**Q92: What is mocking?**

**Answer:**
Mocking creates fake objects that simulate real dependencies. You define what methods should return and verify they were called:

```csharp
var mockApi = new Mock<IApiService>();
mockApi.Setup(a => a.GetAsync<List<StationModel>>("/api/station/nearby"))
       .ReturnsAsync(new List<StationModel> { new StationModel { Name = "Test Station" } });
```

**Why mock?** Tests run fast, don't need a real API server, and can test error scenarios easily.

---

**Q93: How would you test `ApiService.HandleResponse<T>`?**

**Answer:**
```csharp
[Test]
public async Task HandleResponse_NonSuccessStatusCode_Throws()
{
    var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
    {
        Content = new StringContent("{\"message\":\"Invalid\"}")
    };

    var ex = Assert.ThrowsAsync<HttpRequestException>(() =>
        InvokeHandleResponse<UserModel>(response));

    Assert.That(ex.Message, Does.Contain("Invalid"));
}

// Use reflection to test private method, or make it internal
```

---

**Q94: What debugging tools are available for MAUI on Windows?**

**Answer:**
- **Visual Studio debugger** — breakpoints, watch, immediate window
- **XAML Hot Reload** — modify XAML while app is running
- **Live Visual Tree** — inspect the visual tree at runtime
- **Output window** — `Debug.WriteLine()` output
- **DevTools** — `dotnet trace`, `dotnet counters` for performance
- **Network tab** — Fiddler, Postman to inspect HTTP traffic

---

**Q95: How would you diagnose why a data-bound label isn't updating?**

**Answer:**
1. Check the `Output` window for binding errors (`"Binding: 'MyProperty' not found on 'MyViewModel'"`)
2. Verify the ViewModel property has `[ObservableProperty]` or raises `PropertyChanged`
3. Check `x:DataType` is correct and includes the property
4. Ensure the ViewModel is set as `BindingContext`
5. Try a simple test: `<Label Text="{Binding MyProperty}" />` with a known value

---

**Q96: What is UI testing in MAUI?**

**Answer:**
UI testing automates user interactions using `Microsoft.Maui.Testing`:

```csharp
[Test]
public async Task LoginPage_ValidLogin_NavigatesToDashboard()
{
    var app = AppFactory.StartApp();
    app.EnterText("UsernameEntry", "admin");
    app.EnterText("PasswordEntry", "Admin@123");
    app.Tap("LoginButton");
    await Task.Delay(2000);
    Assert.That(app.CurrentPage, Is.EqualTo("DashboardPage"));
}
```

---

**Q97: How would you test async methods that use `await`?**

**Answer:**
```csharp
[Test]
public async Task LoadDataAsync_WhenApiFails_SetsDummyData()
{
    // Arrange
    var apiMock = new Mock<IApiService>();
    apiMock.Setup(a => a.GetAsync<UserDashboardModel>("/api/report/user-dashboard"))
           .ThrowsAsync(new HttpRequestException());

    var vm = new DashboardViewModel(apiMock.Object, ...);

    // Act
    await vm.LoadDashboardCommand.ExecuteAsync(null);

    // Assert
    Assert.That(vm.BatteryPercent, Is.EqualTo(75));  // dummy data
    Assert.That(vm.WalletBalance, Is.EqualTo(250.00m));
}
```

Use `ReturnsAsync` for successful results and `ThrowsAsync` for exceptions.

---

**Q98: What is the Arrange-Act-Assert pattern?**

**Answer:**
```
// Arrange    - set up test data and mocks
// Act        - execute the method under test
// Assert     - verify the result
```

```csharp
[Test]
public void Add_TwoNumbers_ReturnsSum()
{
    // Arrange
    var calc = new Calculator();

    // Act
    var result = calc.Add(2, 3);

    // Assert
    Assert.That(result, Is.EqualTo(5));
}
```

---

**Q99: How would you verify a navigation call happened in a ViewModel test?**

**Answer:**
```csharp
var navMock = new Mock<INavigationService>();
var vm = new LoginViewModel(..., navMock.Object, ...);
await vm.LoginCommand.ExecuteAsync(null);
navMock.Verify(n => n.NavigateToAsync("//dashboard", It.IsAny<IDictionary<string, object>?>()),
               Times.Once);
```

`Times.Once` = exactly one call. `Times.Never` = verify it was NOT called.

---

**Q100: How do you test code that depends on `SecureStorage` or platform APIs?**

**Answer:**
Wrap platform APIs in injectable interfaces and mock them:

```csharp
public interface ISecureStorageService
{
    Task SaveAsync(string key, string value);
    Task<string?> GetAsync(string key);
}

// In tests:
var storageMock = new Mock<ISecureStorageService>();
storageMock.Setup(s => s.GetAsync("auth_token")).ReturnsAsync("test_jwt");
```

---

## 9. Performance & Security

**Q101: What MAUI-specific performance concerns are there?**

**Answer:**
- **Startup time** — minimize assembly loading, use compiled bindings
- **Memory** — `CollectionView` virtualizes; plain `StackLayout` with 1000 items does NOT
- **UI thread** — don't block it with sync operations
- **Images** — resize before displaying; don't load 4K photos into an avatar
- **XAML parsing** — use compiled bindings (`x:DataType`) to reduce runtime reflection

---

**Q102: How does `CollectionView` with `DataTemplate` get recycled?**

**Answer:**
`CollectionView` creates only as many items as fit on screen. When you scroll, items that go off-screen are recycled (their views are reused for the new items). This prevents creating thousands of views for a list of 10,000 items.

---

**Q103: What is AOT compilation in MAUI?**

**Answer:**
AOT (Ahead-Of-Time) compilation compiles C# to native code at build time rather than at runtime. On Windows, MAUI uses .NET Native AOT. Benefits: faster startup, less memory. The `MVVMTK0045` warnings in the app are about `[ObservableProperty]` fields not being compatible with WinRT AOT — they'd need to use `partial` properties instead.

---

**Q104: How would you reduce app startup time?**

**Answer:**
1. Use compiled bindings (`x:DataType`)
2. Defer XAML loading with `x:Load`
3. Lazy-initialize services
4. Use `.NET Native AOT` (Windows)
5. Reduce assembly sizes
6. Minimize fonts and resource dictionaries

---

**Q105: What is the risk of storing JWT tokens in `Preferences` vs `SecureStorage`?**

**Answer:**
| Storage | Encrypted? | Risk |
|---------|-----------|------|
| `Preferences` | No (plain text) | Other apps/malware can read the token |
| `SecureStorage` | Yes (DPAPI/KeyChain/KeyStore) | Token is encrypted at rest |

Always use `SecureStorage` for tokens, passwords, and any sensitive data. `Preferences` is fine for non-sensitive settings (e.g., theme preference).

---

**Q106: How does `HttpClient` timeout protect the app?**

**Answer:**
```csharp
_httpClient.Timeout = TimeSpan.FromSeconds(30);
```

If the server doesn't respond within 30 seconds, `HttpClient` throws `TaskCanceledException`. Without a timeout, the app could hang indefinitely waiting for a response.

---

**Q107: What is input validation and how is it done in `AddMoneyViewModel`?**

**Answer:**
```csharp
[RelayCommand]
async Task AddMoneyAsync()
{
    if (!decimal.TryParse(Amount, out var amount) || !Validators.IsValidAmount(amount))
    {
        await ShowAlertAsync("Validation", "Enter a valid amount greater than 0.");
        return;
    }
    // ...
}
```

Validation before API call prevents sending invalid data and gives instant user feedback.

---

**Q108: How would you prevent SQL injection in an API endpoint?**

**Answer:**
Always use parameterized queries with EF Core:

```csharp
// SAFE - EF Core parameterizes automatically
var user = db.Users.FirstOrDefault(u => u.Username == username);

// DANGEROUS - SQL injection risk
db.Database.ExecuteSqlRaw($"SELECT * FROM Users WHERE Username = '{username}'");
```

EF Core's LINQ methods always generate parameterized SQL.

---

**Q109: What is XSS and how does MAUI protect against it?**

**Answer:**
XSS (Cross-Site Scripting) injects malicious scripts into web pages. MAUI is a native app, not a web app, so XSS is generally not a concern. However, if you load HTML in a `WebView`, sanitize the input:

```csharp
// Sanitize HTML before displaying in WebView
var sanitized = System.Web.HttpUtility.HtmlEncode(userInput);
```

---

**Q110: How would you secure the API over public internet?**

**Answer:**
1. **HTTPS** — always use TLS/SSL (not HTTP)
2. **JWT authentication** with short-lived tokens
3. **Rate limiting** — prevent brute force attacks
4. **Input validation** — validate all request data
5. **CORS** — restrict which origins can call the API
6. **API keys** for server-to-server communication
7. **Audit logging** — track all sensitive operations

---

## 10. General Software Engineering

**Q111: What is SOLID? Examples from this codebase.**

**Answer:**
| Principle | Meaning | Example |
|-----------|---------|---------|
| **S**ingle Responsibility | A class has one reason to change | `ApiService` handles HTTP; `AuthService` handles auth |
| **O**pen/Closed | Open for extension, closed for modification | `IApiService` can have new implementations without changing consumers |
| **L**iskov Substitution | Derived types work where base is expected | `ApiService : IApiService` passes anywhere `IApiService` is used |
| **I**nterface Segregation | Small, focused interfaces | Separate `IApiService`, `IAuthService`, `INavigationService` instead of a monolithic `IService` |
| **D**ependency Inversion | Depend on abstractions, not concretions | ViewModels depend on `IApiService` not `ApiService` |

---

**Q112: Composition vs inheritance? Which does MAUI prefer?**

**Answer:**
- **Inheritance:** "is-a" — `ContentPage`, `BaseViewModel`
- **Composition:** "has-a" — ViewModel `has-a` `IApiService`

MAUI prefers composition. ViewModels compose services via constructor injection rather than inheriting from a service class. `BaseViewModel` uses inheritance for shared UI concerns (IsBusy, Title) but composes services (IApiService, IAuthService).

---

**Q113: Explain the Repository pattern.**

**Answer:**
The Repository pattern abstracts data access behind an interface. In the API:

```csharp
public interface IStationRepository
{
    Task<List<Station>> GetNearbyAsync(double lat, double lng, double radius);
    Task<Station?> GetByIdAsync(int id);
}

public class StationRepository : IStationRepository
{
    private readonly AppDbContext _db;
    public async Task<List<Station>> GetNearbyAsync(double lat, double lng, double radius)
    {
        return await _db.Stations.FromSqlInterpolated(
            $"SELECT * FROM Stations WHERE ...").ToListAsync();
    }
}
```

The controller depends on `IStationRepository`, not `AppDbContext`. This makes data access testable and swappable.

---

**Q114: Map `EVSwap.API` layers to Clean Architecture.**

**Answer:**
| Layer | Clean Architecture | EVSwap.API |
|-------|-------------------|------------|
| 1 | Domain / Core | `Core/` — Entities, DTOs, Interfaces, Business logic |
| 2 | Infrastructure | `Infrastructure/` — DbContext, Repositories, External services |
| 3 | Presentation | `Controllers/` — API endpoints, `Program.cs` |

The domain layer (`Core/`) has no dependency on infrastructure — it defines interfaces, and infrastructure implements them.

---

**Q115: What is technical debt and how would you identify it?**

**Answer:**
Technical debt is the implied cost of rework caused by choosing an easy solution now instead of a better approach that would take longer. Signs:

- Duplicate code (e.g., similar ViewModel patterns copy-pasted)
- Dead code (SignalR, LocalDatabase services — deleted in this refactoring)
- Empty `catch {}` blocks
- Hardcoded URLs and credentials
- Missing error handling

---

**Q116: How would you implement logging across the app?**

**Answer:**
Use `Microsoft.Extensions.Logging`:

```csharp
// MauiProgram.cs
builder.Logging.AddDebug();
builder.Logging.AddConsole();
builder.Logging.AddFile("logs/app.log");  // third-party

// In ViewModel
public class LoginViewModel
{
    private readonly ILogger<LoginViewModel> _logger;
    public LoginViewModel(ILogger<LoginViewModel> logger) => _logger = logger;

    catch (Exception ex)
    {
        _logger.LogError(ex, "Login failed for user {Username}", Username);
    }
}
```

---

**Q117: Unit testing vs integration testing.**

**Answer:**
| Aspect | Unit Test | Integration Test |
|--------|-----------|-----------------|
| Scope | Single class/method | Multiple components together |
| Dependencies | Mocked | Real (database, API) |
| Speed | Milliseconds | Seconds to minutes |
| Reliability | Deterministic | Can be flaky (network, DB) |

**Both are needed.** Unit tests verify logic. Integration tests verify that components work together.

---

**Q118: How would you version a REST API?**

**Answer:**
```csharp
// URL-based versioning
[Route("api/v1/[controller]")]
public class SwapControllerV1 : ControllerBase { ... }

[Route("api/v2/[controller]")]
public class SwapControllerV2 : ControllerBase { ... }
```

Or header-based: `Accept: application/vnd.evswap.v2+json`

---

**Q119: What is CORS and when would you need it?**

**Answer:**
CORS (Cross-Origin Resource Sharing) controls which web domains can call your API. Needed when a web app on a different domain tries to make AJAX requests to your API. In MAUI (native app), CORS is NOT needed — native apps don't have origin restrictions.

For the API, CORS is configured:
```csharp
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
```

---

**Q120: How would you deploy a MAUI Windows app to end users?**

**Answer:**
1. **Build in Release mode:** `dotnet publish -f net10.0-windows10.0.19041.0 -c Release`
2. **Package as MSIX:** Create a Windows App Package project in Visual Studio
3. **Sign the package** with a code signing certificate
4. **Distribute via:**
   - Microsoft Store
   - Sideloading (install MSIX directly)
   - ClickOnce (for enterprise deployment)

---

## 11. Advanced MAUI & .NET Topics (Bonus 80 Questions)

**Q121: What is `Handler` vs `Renderer` in MAUI?**

**Answer:**
Handlers replaced Renderers in MAUI. Renderers (Xamarin.Forms) created a native view per platform. Handlers are more lightweight — they map cross-platform controls to native views via a mapping dictionary without the overhead of a full `ViewRenderer` base class.

---

**Q122: How does `IPlatformApplication` work in MAUI?**

**Answer:**
It allows platform-specific code to run at startup. On Windows, you can configure the window:

```csharp
// Platforms/Windows/App.xaml.cs
public class App : Microsoft.UI.Xaml.Application, IPlatformApplication
{
    // Configure WinUI-specific settings
}
```

---

**Q123: What is the difference between `OnPlatform` and `OnIdiom`?**

**Answer:**
```xml
<!-- Platform-specific -->
<Label Text="{OnPlatform Default='Running', Android='Droid', iOS='iPhone'}" />

<!-- Idiom-specific (phone vs tablet vs desktop) -->
<Label Text="{OnIdiom Phone='Phone', Desktop='Desktop', Tablet='Tablet'}" />
```

---

**Q124: How do you handle app themes (Light/Dark mode) in MAUI?**

**Answer:**
Use `AppThemeBinding` in styles (Q20) and detect the current theme:

```csharp
var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
Application.Current?.UserAppTheme = AppTheme.Dark;  // override
```

---

**Q125: What is `Shell.TabBar` and how does it differ from `FlyoutItem`?**

**Answer:**
`TabBar` shows tabs at the bottom. `FlyoutItem` shows items in a side menu (hamburger menu). You can mix both:

```xml
<TabBar>        <!-- bottom tabs -->
    <ShellContent Route="dashboard" Title="Home" />
    <ShellContent Route="wallet" Title="Wallet" />
</TabBar>
<FlyoutItem>    <!-- side menu -->
    <ShellContent Route="settings" Title="Settings" />
</FlyoutItem>
```

---

---

**Q126: What is `IDispatcher` and how is it used in MAUI?**

**Answer:**
`IDispatcher` schedules work on the UI thread. Use it when you need to update UI from a background thread:

```csharp
public class DashboardViewModel
{
    private readonly IDispatcher _dispatcher;

    public DashboardViewModel(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task LoadDataAsync()
    {
        await Task.Run(async () =>
        {
            var data = await FetchDataAsync();
            
            // Must update UI on main thread
            _dispatcher.Dispatch(() =>
            {
                BatteryPercent = data.BatteryLevel;
                WalletBalance = data.Balance;
            });
        });
    }
}
```

---

**Q127: What are `AppLinks` and `UriScheme` in MAUI?**

**Answer:**
AppLinks allow your app to handle custom URI schemes and deep links:

```csharp
// Register in MauiProgram.cs
builder.ConfigureEssentials(essentials =>
    essentials.AddAppLink("evswap", async uri =>
    {
        // Handle evswap://swap/123
        var segments = uri.Segments;
        if (segments.Length >= 2 && segments[0] == "swap")
            await Shell.Current.GoToAsync($"//swaprequest?id={segments[1]}");
    }));
```

Registered in `Platforms/Windows/Package.appxmanifest` and `AndroidManifest.xml`.

---

**Q128: How does `SecureStorage` work on each platform?**

**Answer:**
| Platform | Implementation |
|----------|---------------|
| **Windows** | Data Protection API (DPAPI) — encrypts with user's Windows credentials |
| **Android** | Android KeyStore — encrypted shared preferences with AES |
| **iOS** | KeyChain Services — hardware-backed encryption |
| **macOS** | KeyChain — same as iOS |

```csharp
// Store
await SecureStorage.Default.SetAsync("auth_token", jwtToken);
// Encryption happens automatically based on platform

// Retrieve
var token = await SecureStorage.Default.GetAsync("auth_token");
```

---

**Q129: What is the `Connectivity` API and how is it used?**

**Answer:**
Detects network state changes in real time:

```csharp
public class ConnectivityService : IConnectivityService
{
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    public ConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += (s, e) =>
        {
            var online = e.NetworkAccess == NetworkAccess.Internet;
            Debug.WriteLine($"Network changed: {(online ? "Online" : "Offline")}");
            
            // Could trigger offline sync
            if (online) SyncPendingRequestsAsync().FireAndForget();
        };
    }
}
```

---

**Q130: How do you implement `Geolocation` in MAUI?**

**Answer:**
```csharp
public async Task<Location?> GetCurrentLocationAsync()
{
    try
    {
        var request = new GeolocationRequest(GeolocationAccuracy.Medium,
            TimeSpan.FromSeconds(10));
        return await Geolocation.Default.GetLocationAsync(request);
    }
    catch (FeatureNotSupportedException)
    {
        await ShowAlertAsync("Error", "GPS not supported on this device");
    }
    catch (PermissionException)
    {
        await ShowAlertAsync("Error", "GPS permission not granted");
    }
    return null;
}
```

---

**Q131: How do you handle file picker interactions in MAUI?**

**Answer:**
```csharp
public async Task<string?> PickImageAsync()
{
    var result = await FilePicker.Default.PickAsync(new PickOptions
    {
        PickerTitle = "Select profile photo",
        FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png" } },
            { DevicePlatform.Android, new[] { "image/jpeg", "image/png" } },
            { DevicePlatform.iOS, new[] { "public.jpeg", "public.png" } },
        })
    });

    if (result is null) return null;

    using var stream = await result.OpenReadAsync();
    var ms = new MemoryStream();
    await stream.CopyToAsync(ms);
    return Convert.ToBase64String(ms.ToArray());
}
```

---

**Q132: What is `BackgroundService` in .NET MAUI?**

**Answer:**
`BackgroundService` runs long-lived background tasks. In a MAUI app, you can use it for periodic API polling or data sync:

```csharp
public class SyncBackgroundService : BackgroundService
{
    private readonly IApiService _api;
    private readonly ILogger<SyncBackgroundService> _logger;

    public SyncBackgroundService(IApiService api, ILogger<SyncBackgroundService> logger)
    {
        _api = api;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _api.PostAsync("/api/swap/sync-pending", new { });
                _logger.LogInformation("Sync completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Sync failed, will retry");
            }
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

---

**Q133: How do you implement custom fonts in MAUI?**

**Answer:**
```csharp
// MauiProgram.cs
builder.ConfigureFonts(fonts =>
{
    fonts.AddFont("Inter-Regular.ttf", "InterRegular");
    fonts.AddFont("Inter-Bold.ttf", "InterBold");
});
```

XAML usage:
```xml
<Label Text="Hello" FontFamily="InterRegular" FontSize="16" />
```

Font files go in `Resources/Fonts/` folder. The filename extension must be `.ttf` or `.otf`.

---

**Q134: How do you create custom handlers for platform-specific behavior?**

**Answer:**
```csharp
// Custom handler to disable Entry spell check on specific platforms
public static class EntrySpellCheckHandler
{
    public static void DisableSpellCheck()
    {
#if WINDOWS
        EntryHandler.Mapper.AppendToMapping("NoSpellCheck", (handler, entry) =>
        {
            if (entry is Entry e && e.ClassId == "NoSpellCheck")
            {
                // WinUI specific
                handler.PlatformView.IsSpellCheckEnabled = false;
            }
        });
#endif
    }
}

// Call in MauiProgram.cs
EntrySpellCheckHandler.DisableSpellCheck();
```

---

**Q135: What are `Behaviors` in MAUI?**

**Answer:**
Behaviors attach reusable functionality to controls without subclassing:

```csharp
public class NumericEntryBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry entry)
    {
        entry.TextChanged += OnTextChanged;
        base.OnAttachedTo(entry);
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrEmpty(e.NewTextValue))
        {
            entry.Text = new string(e.NewTextValue.Where(char.IsDigit).ToArray());
        }
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnTextChanged;
        base.OnDetachingFrom(entry);
    }
}

// XAML: <Entry><Entry.Behaviors><local:NumericEntryBehavior /></Entry.Behaviors></Entry>
```

---

**Q136: How does `DataTemplateSelector` work?**

**Answer:**
Creates different templates based on data type or condition:

```csharp
public class SwapTemplateSelector : DataTemplateSelector
{
    public DataTemplate PendingTemplate { get; set; } = null!;
    public DataTemplate CompletedTemplate { get; set; } = null!;

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        return item is SwapModel { Status: "Completed" }
            ? CompletedTemplate
            : PendingTemplate;
    }
}

// XAML:
// <CollectionView ItemTemplate="{StaticResource SwapSelector}">
// <ContentPage.Resources>
//   <DataTemplate x:Key="PendingTemplate">...</DataTemplate>
//   <DataTemplate x:Key="CompletedTemplate">...</DataTemplate>
//   <local:SwapTemplateSelector PendingTemplate="{StaticResource PendingTemplate}"
//                                CompletedTemplate="{StaticResource CompletedTemplate}" />
// </ContentPage.Resources>
```

---

**Q137: What is `SwipeView` and how does it work?**

**Answer:**
`SwipeView` reveals action buttons on swipe:

```xml
<SwipeView>
    <SwipeView.RightItems>
        <SwipeItems>
            <SwipeItem Text="Delete" BackgroundColor="Red"
                       Command="{Binding DeleteStationCommand}"
                       CommandParameter="{Binding .}" />
            <SwipeItem Text="Edit" BackgroundColor="Blue"
                       Command="{Binding EditStationCommand}" />
        </SwipeItems>
    </SwipeView.RightItems>
    <Frame Padding="10">
        <Label Text="{Binding Name}" />
    </Frame>
</SwipeView>
```

---

**Q138: How do you implement animations in MAUI?**

**Answer:**
```csharp
// In code-behind or ViewModel
await myLabel.FadeTo(0, 300);       // fade out in 300ms
await myLabel.FadeTo(1, 300);       // fade in
await myBox.TranslateTo(100, 0, 500); // slide right
await myBox.ScaleTo(1.5, 200);       // scale up
await myBox.RotateTo(360, 1000);    // full rotation

// Chaining
await Task.WhenAll(
    myLabel.FadeTo(0, 300),
    myBox.TranslateTo(100, 0, 500),
    myButton.ScaleTo(0.8, 200)
);
```

---

**Q139: How do you create custom page transitions?**

**Answer:**
Register custom animations in Shell navigation:

```csharp
// Custom transition
public class SlideUpTransition : INavigationTransition
{
    public void Apply(Page page, bool isForward)
    {
        page.TranslationY = isForward ? 500 : 0;
        page.TranslateTo(0, 0, 400, Easing.CubicInOut);
    }
}

// Use on navigation page
NavigationPage.SetTransitionType(page, typeof(SlideUpTransition));
```

---

**Q140: What are `GestureRecognizers` in MAUI?**

**Answer:**
```xml
<!-- Tap -->
<Frame>
    <Frame.GestureRecognizers>
        <TapGestureRecognizer Command="{Binding StationTappedCommand}"
                              CommandParameter="{Binding .}" />
        <SwipeGestureRecognizer Direction="Right"
                                Swiped="OnSwiped" />
        <PinchGestureRecognizer PinchUpdated="OnPinchUpdated" />
        <PanGestureRecognizer PanUpdated="OnPanUpdated" />
        <DragGestureRecognizer CanDrag="True" />
        <DropGestureRecognizer AllowDrop="True" />
    </Frame.GestureRecognizers>
</Frame>
```

---

**Q141: What is `System.Text.Json` vs `Newtonsoft.Json`?**

**Answer:**
| Feature | System.Text.Json | Newtonsoft.Json |
|---------|-----------------|-----------------|
| Performance | Faster (no reflection-heavy fallback) | Slower |
| AOT compatible | Yes (source generators) | No |
| Default naming | CamelCase | PascalCase |
| Case insensitive | Set `PropertyNameCaseInsensitive = true` | Default |
| Custom converters | `JsonConverter<T>` | `JsonConverter` |
| `[JsonProperty]` | `[JsonPropertyName]` | `[JsonProperty]` |

**Recommendation:** Use `System.Text.Json` for new .NET projects. It's built-in, AOT-friendly, and faster. Newtonsoft is only needed for legacy code.

---

**Q142: What are JSON source generators?**

**Answer:**
```csharp
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(UserDashboardModel))]
public partial class AppJsonContext : JsonSerializerContext { }

// Usage - no reflection, AOT safe
var options = new JsonSerializerOptions
{
    TypeInfoResolver = AppJsonContext.Default
};
var user = JsonSerializer.Deserialize(json, typeof(UserModel), options);
```

---

**Q143: What is the `dotnet` CLI and what commands are used in this project?**

**Answer:**
```bash
dotnet restore          # Restore NuGet packages
dotnet build            # Build the solution
dotnet run --project src/EVSwap.API  # Run the API
dotnet publish -f net10.0-windows10.0.19041.0 -c Release  # Publish MAUI app
dotnet ef migrations add AddSerialNumber  # Add EF migration
dotnet ef database update  # Apply migrations
dotnet test             # Run unit tests
```

---

**Q144: What are `TargetFrameworks` in MAUI .csproj?**

**Answer:**
```xml
<!-- EVSwap.Mobile.csproj -->
<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0-windows10.0.19041.0</TargetFrameworks>
```

Each target framework compiles the same code for a different platform:
- `net10.0-android` → Android APK/AAB
- `net10.0-ios` → iOS IPA
- `net10.0-maccatalyst` → macOS via Mac Catalyst
- `net10.0-windows10.0.19041.0` → Windows MSIX

---

**Q145: How does `GC` (Garbage Collection) work in .NET MAUI apps?**

**Answer:**
The GC automatically reclaims memory of unreachable objects. In MAUI:

```csharp
// Objects on the managed heap are collected in generations:
// Gen 0: short-lived (local variables) - collected frequently
// Gen 1: medium-lived
// Gen 2: long-lived (singletons) - collected rarely

// Large Object Heap (>85KB) - strings, byte arrays - collected with Gen 2

// Common MAUI memory issues:
// 1. Event handler leaks - subscribing without unsubscribing
// 2. Static references - holding ViewModel in static field
// 3. Large collections - not clearing ObservableCollection
// 4. Image caching - loading full resolution photos
```

---

**Q146: What are `WeakReference` and `WeakReferenceMessenger`?**

**Answer:**
`WeakReference` allows the GC to collect an object while you still hold a reference:

```csharp
// Without WeakReference - prevents GC
var vm = new DashboardViewModel();
_strongCache.Add(vm);  // keeps alive forever

// With WeakReference - allows GC
_weakCache.Add(new WeakReference<DashboardViewModel>(vm));
if (_weakCache[0].TryGetTarget(out var cached)) { /* use it */ }
```

`WeakReferenceMessenger` (from MVVM Toolkit) uses weak references for subscribers, so ViewModels can be GC'd without explicitly unsubscribing:

```csharp
WeakReferenceMessenger.Default.Register<SwapCompletedMessage>(this, (r, m) =>
{
    // Handles message - recipient can be GC'd if no other references
});
```

---

**Q147: What is `SemaphoreSlim` and when would you use it?**

**Answer:**
Controls access to a limited resource. Used to prevent concurrent API calls:

```csharp
public class StationService
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<List<StationModel>> GetStationsAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            // Only one caller at a time
            return await _api.GetAsync<List<StationModel>>("/api/station/nearby");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

---

**Q148: What is `Channel<T>` in `System.Threading.Channels`?**

**Answer:**
A thread-safe producer/consumer queue for high-throughput scenarios:

```csharp
// Producer
var channel = Channel.CreateUnbounded<SwapRequest>();

// Producer (background API polling)
await channel.Writer.WriteAsync(newSwapRequest);

// Consumer (UI thread processing)
await foreach (var request in channel.Reader.ReadAllAsync())
{
    // Process on UI thread
    SwapRequests.Add(request);
}
```

---

**Q149: How do C# Source Generators work in MVVM Toolkit?**

**Answer:**
Source generators run at compile time, analyzing attributes and generating code:

```csharp
// Your code:
[ObservableProperty]
private string _userName = string.Empty;

// Generator produces:
public string UserName
{
    get => _userName;
    set
    {
        if (!EqualityComparer<string>.Default.Equals(_userName, value))
        {
            OnPropertyChanging(nameof(UserName));
            _userName = value;
            OnPropertyChanged(nameof(UserName));
            OnUserNameChanged(value);
        }
    }
}
partial void OnUserNameChanged(string value);
```

The generator is an `ISourceGenerator` interface implementation that uses Roslyn syntax trees to analyze code and emit new compilation units.

---

**Q150: What is `IAsyncStateMachine` and how does `async`/`await` compile?**

**Answer:**
The compiler transforms `async` methods into a state machine struct implementing `IAsyncStateMachine`:

```csharp
// This:
async Task LoginAsync()
{
    var result = await _auth.LoginAsync(user, pass);
    await NavigateToDashboard();
}

// Compiles to something like:
[AsyncStateMachine(typeof(LoginAsyncStateMachine))]
public Task LoginAsync()
{
    var machine = new LoginAsyncStateMachine { _this = this, _builder = AsyncTaskMethodBuilder.Create() };
    machine._builder.Start(ref machine);
    return machine._builder.Task;
}

private struct LoginAsyncStateMachine : IAsyncStateMachine
{
    public int _state;
    public LoginViewModel _this;
    public AuthResult _result;
    public AsyncTaskMethodBuilder _builder;

    public void MoveNext()
    {
        switch (_state)
        {
            case 0:
                _state = -1;
                _this._auth.LoginAsync(_this.Username, _this.Password)...
                // awaiter pattern
            case 1:
                // continuation
        }
    }
}
```

---

**Q151: What is `Span<T>` and `Memory<T>` for performance?**

**Answer:**
Stack-allocated views over contiguous memory with zero allocations:

```csharp
// Span<T> - stack-only, synchronous
Span<byte> buffer = stackalloc byte[256];
int written = Encoding.UTF8.GetBytes("hello", buffer);

// Memory<T> - heap-safe, supports async
Memory<byte> memory = new byte[1024];
await stream.ReadAsync(memory);

// String slice without allocation
ReadOnlySpan<char> email = "user@example.com".AsSpan();
var atIndex = email.IndexOf('@');
var domain = email[(atIndex + 1)..]; // "example.com" - no new string
```

---

**Q152: What is `ConfigureAwait(false)` and when should you use it in MAUI?**

**Answer:**
```csharp
// In service layer (no UI touch) - OK to use
public async Task<UserModel?> GetUserAsync(int id)
{
    var json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
    return JsonSerializer.Deserialize<UserModel>(json);
}

// In ViewModel (touches UI) - DO NOT USE
public async Task LoadDataAsync()
{
    var data = await _api.GetAsync(...);
    // Without ConfigureAwait, continuation returns to UI thread
    IsBusy = false;  // must be on UI thread
}
```

**Rule:** Use `ConfigureAwait(false)` in library code only. Never in ViewModels or any code that updates UI properties.

---

**Q153: How does `IHttpClientFactory` work and is it needed in MAUI?**

**Answer:**
`IHttpClientFactory` manages `HttpClient` lifetime to prevent socket exhaustion in ASP.NET Core apps. In MAUI, it's generally unnecessary because:

1. MAUI apps typically call one API server
2. A single `HttpClient` singleton is sufficient
3. Socket exhaustion is rare on desktop/mobile

```csharp
// MAUI approach - simple singleton
builder.Services.AddSingleton<HttpClient>(_ =>
    new HttpClient { BaseAddress = new Uri(Constants.ApiBaseUrl) });

// ASP.NET Core approach - IHttpClientFactory
builder.Services.AddHttpClient<IApiService, ApiService>(client =>
    client.BaseAddress = new Uri("http://localhost:5238"));
```

---

**Q154: What is the `Options` pattern in .NET?**

**Answer:**
```csharp
// Define options class
public class ApiOptions
{
    public const string Section = "Api";
    public string BaseUrl { get; set; } = "http://localhost:5238";
    public int TimeoutSeconds { get; set; } = 30;
}

// Register
builder.Services.Configure<ApiOptions>(
    builder.Configuration.GetSection(ApiOptions.Section));

// Use with IOptions<T>
public class ApiService
{
    private readonly HttpClient _httpClient;
    public ApiService(IOptions<ApiOptions> options)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(options.Value.BaseUrl),
            Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds)
        };
    }
}
```

---

**Q155: How does `ILogger<T>` integrate with MAUI?**

**Answer:**
```csharp
// MauiProgram.cs
builder.Logging.AddDebug();
builder.Logging.AddConsole();

// ViewModel
public class LoginViewModel
{
    private readonly ILogger<LoginViewModel> _logger;

    public LoginViewModel(ILogger<LoginViewModel> logger)
    {
        _logger = logger;
    }

    [RelayCommand]
    async Task LoginAsync()
    {
        try
        {
            _logger.LogInformation("User {User} attempting login", Username);
            await _authService.LoginAsync(Username, Password);
            _logger.LogInformation("Login successful for {User}", Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {User}", Username);
            await ShowAlertAsync("Error", "Login failed");
        }
    }
}
```

---

**Q156: How do you implement a custom proxy/wrapper around HttpClient for logging?**

**Answer:**
```csharp
public class LoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingDelegatingHandler> _logger;

    public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        _logger.LogInformation("→ {Method} {Uri}", request.Method, request.RequestUri);

        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, ct);
        stopwatch.Stop();

        _logger.LogInformation("← {StatusCode} {Uri} in {Elapsed}ms",
            (int)response.StatusCode, request.RequestUri, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

---

**Q157: How do you implement a `Timer`-based auto-refresh in a ViewModel?**

**Answer:**
```csharp
public partial class DashboardViewModel : BaseViewModel, IDisposable
{
    private readonly IDispatcherTimer _timer;

    public DashboardViewModel()
    {
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(30);
        _timer.Tick += async (s, e) => await RefreshDashboardAsync();
    }

    [RelayCommand]
    async Task StartAutoRefreshAsync()
    {
        await LoadDashboardAsync();
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= async (s, e) => await RefreshDashboardAsync();
    }
}
```

---

**Q158: What is `INotifyPropertyChanging` vs `INotifyPropertyChanged`?**

**Answer:**
```csharp
// INotifyPropertyChanging - fires BEFORE the property changes
// INotifyPropertyChanged - fires AFTER the property changes

// MVVM Toolkit supports both with [ObservableProperty]
// Use case: cancel an edit if validation fails
partial void OnUserNameChanging(string value)
{
    if (string.IsNullOrWhiteSpace(value))
        throw new ArgumentException("Username cannot be empty");
}
// Throwing in OnChanging prevents the property from being set
```

---

**Q159: How do you implement `IComparable` for sorting in CollectionView?**

**Answer:**
```csharp
public class StationModel : IComparable<StationModel>
{
    public string Name { get; set; } = "";
    public double Distance { get; set; }

    public int CompareTo(StationModel? other)
    {
        if (other is null) return 1;
        return Distance.CompareTo(other.Distance);  // sort by nearest
    }
}

// ViewModel
Stations = new ObservableCollection<StationModel>(stations.OrderBy(s => s));
```

---

**Q160: How do you use `INotifyDataErrorInfo` for validation?**

**Answer:**
```csharp
public partial class AddMoneyViewModel : BaseViewModel, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Count > 0;
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        return propertyName is not null && _errors.TryGetValue(propertyName, out var errors)
            ? errors : Enumerable.Empty<string>();
    }

    [ObservableProperty]
    private string _amount = string.Empty;

    partial void OnAmountChanged(string value)
    {
        ClearErrors(nameof(Amount));
        if (!decimal.TryParse(value, out var amount) || amount <= 0)
            AddError(nameof(Amount), "Enter a valid positive amount");
    }

    private void AddError(string property, string error)
    {
        if (!_errors.ContainsKey(property))
            _errors[property] = new List<string>();
        _errors[property].Add(error);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
    }

    private void ClearErrors(string property)
    {
        _errors.Remove(property);
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
    }
}
```

---

**Q161: How does `MethodImplAttribute` affect performance?**

**Answer:**
```csharp
// Inline method aggressively
[MethodImpl(MethodImplOptions.AggressiveInlining)]
private int Square(int x) => x * x;

// No inlining (useful for hot path methods that shouldn't bloat code)
[MethodImpl(MethodImplOptions.NoInlining)]
public void LogError(string msg) { /* logging */ }

// Synchronize (forces method to run on single thread - use lock instead)
[MethodImpl(MethodImplOptions.Synchronized)]
public void CriticalSection() { }
```

---

**Q162: How does `DllImport` work in .NET MAUI for P/Invoke?**

**Answer:**
```csharp
// Windows native API call
[DllImport("user32.dll", CharSet = CharSet.Auto)]
private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

// Android JNI
[DllImport("libc")]
private static extern int getpid();

// Use only when MAUI's built-in APIs don't cover what you need
```

---

**Q163: What is `Interlocked` and how does it prevent race conditions?**

**Answer:**
```csharp
public class CounterService
{
    private int _activeRequests;

    public int Increment()
    {
        // Thread-safe atomic increment - no lock needed
        return Interlocked.Increment(ref _activeRequests);
    }

    public int Decrement()
    {
        return Interlocked.Decrement(ref _activeRequests);
    }

    // Atomic compare-and-swap
    public bool TrySet(ref int target, int value, int expected)
    {
        return Interlocked.CompareExchange(ref target, value, expected) == expected;
    }
}
```

---

**Q164: What is `readonly` vs `const` vs `static readonly`?**

**Answer:**
```csharp
public class Constants
{
    // const - compile-time constant, embedded in IL
    public const int DefaultPageSize = 20;
    
    // readonly - runtime constant, set in constructor
    public readonly string ApiUrl;
    
    // static readonly - single value shared across all instances, initialized once
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
}

// const must be known at compile time (numbers, strings, enums)
// readonly can be calculated at runtime
// static readonly is lazy-initialized (if needed) or eagerly on type load
```

---

**Q165: How does `async` work in `foreach` with `IAsyncEnumerable<T>`?**

**Answer:**
```csharp
public async Task ProcessSwapsAsync(IAsyncEnumerable<SwapModel> swaps)
{
    // Process items as they arrive without blocking
    await foreach (var swap in swaps)
    {
        await ProcessSwapAsync(swap);
    }
}

// Producer
public async IAsyncEnumerable<SwapModel> GetSwapsAsync()
{
    var page = 0;
    List<SwapModel>? batch;
    do
    {
        batch = await _api.GetAsync<List<SwapModel>>($"/api/swap?page={page}");
        if (batch is not null)
        {
            foreach (var swap in batch)
                yield return swap;
            page++;
        }
    } while (batch?.Count > 0);
}
```

---

**Q166: What is the difference between `throw` and `throw ex`?**

**Answer:**
```csharp
try
{
    await _api.PostAsync(endpoint, data);
}
catch (HttpRequestException ex)
{
    // throw - preserves the original stack trace
    throw;

    // throw ex - resets stack trace to this line (loses original caller)
    throw ex;

    // throw with new exception - wraps original
    throw new ApiException("API call failed", ex);
}
```

**Never use `throw ex`** — it destroys the original stack trace, making debugging much harder.

---

**Q167: How does `ExceptionDispatchInfo` preserve exception context across threads?**

**Answer:**
```csharp
// Captures exception with its original stack trace
ExceptionDispatchInfo? capturedEx = null;

try
{
    await _api.PostAsync(endpoint, data);
}
catch (Exception ex)
{
    capturedEx = ExceptionDispatchInfo.Capture(ex);
}

// Later, potentially on another thread:
capturedEx?.Throw();  // rethrows with original stack trace preserved
```

---

**Q168: What is the `using` statement and how does it compile?**

**Answer:**
```csharp
// C# 8+ using declaration (disposed at end of scope)
using var httpClient = new HttpClient();
var response = await httpClient.GetAsync(url);

// Compiles to:
HttpClient httpClient = new HttpClient();
try
{
    var response = await httpClient.GetAsync(url);
}
finally
{
    if (httpClient != null)
        ((IDisposable)httpClient).Dispose();
}
```

---

**Q169: How does `IProgress<T>` work for reporting progress?**

**Answer:**
```csharp
public async Task UploadImageAsync(Stream image, IProgress<double> progress)
{
    var totalBytes = image.Length;
    var buffer = new byte[81920];
    int bytesRead;
    long totalRead = 0;

    while ((bytesRead = await image.ReadAsync(buffer)) > 0)
    {
        // Simulate upload chunk
        totalRead += bytesRead;
        progress.Report((double)totalRead / totalBytes * 100);
    }
}

// Caller
var progress = new Progress<double>(p =>
{
    UploadProgress = p;  // runs on UI thread
});
await UploadImageAsync(image, progress);
```

---

**Q170: What is `ConcurrentDictionary<TKey, TValue>` and when to use it?**

**Answer:**
Thread-safe dictionary that works without explicit locks:

```csharp
public class CacheService
{
    private readonly ConcurrentDictionary<string, object> _cache = new();

    public T GetOrAdd<T>(string key, Func<string, T> factory)
    {
        return (T)_cache.GetOrAdd(key, factory);
    }

    public void Evict(string key)
    {
        _cache.TryRemove(key, out _);
    }
}

// Atomic operations: GetOrAdd, TryAdd, TryUpdate, AddOrUpdate
// Don't need lock - internal uses fine-grained locking
```

---

**Q171: How does `System.Threading.Tasks.Dataflow` work?**

**Answer:**
Actor-based parallelism for pipeline processing:

```csharp
// Pipeline: Download → Process → Save
var downloadBlock = new TransformBlock<string, byte[]>(async url =>
{
    return await _httpClient.GetByteArrayAsync(url);
});

var processBlock = new TransformBlock<byte[], ReportData>(bytes =>
{
    return ParseReport(bytes);
});

var saveBlock = new ActionBlock<ReportData>(async data =>
{
    await _db.Reports.AddAsync(data);
    await _db.SaveChangesAsync();
});

// Link pipeline
downloadBlock.LinkTo(processBlock);
processBlock.LinkTo(saveBlock);

// Feed data
downloadBlock.Post("http://api/report/1");
downloadBlock.Complete();
await downloadBlock.Completion;
```

---

**Q172: How does the `Factory` pattern apply in MAUI DI?**

**Answer:**
```csharp
// Factory pattern - create instances with runtime parameters
public interface IViewModelFactory
{
    T Create<T>() where T : BaseViewModel;
    SwapRequestViewModel CreateSwapRequest(StationModel station);
}

public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _provider;

    public ViewModelFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public T Create<T>() where T : BaseViewModel
    {
        return _provider.GetRequiredService<T>();
    }

    public SwapRequestViewModel CreateSwapRequest(StationModel station)
    {
        var vm = _provider.GetRequiredService<SwapRequestViewModel>();
        vm.Station = station;
        return vm;
    }
}
```

---

**Q173: How does the `Strategy` pattern apply to API error handling?**

**Answer:**
```csharp
public interface IRetryStrategy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
}

public class ExponentialBackoffStrategy : IRetryStrategy
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        var delay = 1;
        for (int i = 0; i < 3; i++)
        {
            try { return await operation(); }
            catch (HttpRequestException) when (i < 2)
            {
                await Task.Delay(TimeSpan.FromSeconds(delay));
                delay *= 2;
            }
        }
        return await operation(); // last attempt
    }
}

public class NoRetryStrategy : IRetryStrategy
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        return await operation();
    }
}
```

---

**Q174: How does the `Observer` pattern work with `INotifyPropertyChanged`?**

**Answer:**
```csharp
// Subject (observable)
public partial class UserModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
}

// Observer (subscriber)
public class ProfileViewModel : BaseViewModel
{
    public ProfileViewModel(UserModel user)
    {
        user.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(UserModel.Name))
                OnUserNameChanged();
        };
    }
}
```

The `ObservableObject` base class (from MVVM Toolkit) implements `INotifyPropertyChanged`, making any property change observable by multiple subscribers.

---

**Q175: How does the `Chain of Responsibility` pattern apply to `DelegatingHandler`?**

**Answer:**
```csharp
// Each handler in the chain does its job then passes to the next
// HttpClient pipeline:
// LoggingHandler → AuthHandler → RetryHandler → HttpClientHandler → Network

public class AuthDelegatingHandler : DelegatingHandler
{
    private readonly IAuthService _auth;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        // Add auth header (handler 1's responsibility)
        var token = await _auth.GetTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Pass to next handler in chain
        return await base.SendAsync(request, ct);
    }
}
```

---

**Q176: How does `Builder` pattern relate to `MauiAppBuilder`?**

**Answer:**
```csharp
// The Builder pattern separates construction from representation.
// MauiAppBuilder is the classic Builder pattern:

var builder = MauiApp.CreateBuilder();  // Builder
builder.UseMauiApp<App>();              // Configure step 1
builder.Services.AddSingleton<...>();    // Configure step 2
builder.Logging.AddDebug();              // Configure step 3
var app = builder.Build();              // Build final object

// You can create your own builder extensions:
public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder AddAppServices(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<IAuthService, AuthService>();
        return builder;
    }
}
```

---

**Q177: How does the `Facade` pattern simplify API integration?**

**Answer:**
```csharp
// Facade - simplified interface to a complex subsystem
public class SwapFacade
{
    private readonly IApiService _api;
    private readonly IAuthService _auth;
    private readonly INavigationService _nav;

    public SwapFacade(IApiService api, IAuthService auth, INavigationService nav)
    {
        _api = api;
        _auth = auth;
        _nav = nav;
    }

    public async Task<bool> RequestSwapAsync(StationModel station, BatteryModel battery)
    {
        if (_auth.CurrentUser is null) return false;
        
        var request = new SwapRequest
        {
            UserId = _auth.CurrentUser.Id,
            StationId = station.Id,
            BatteryId = battery.Id
        };

        var response = await _api.PostAsync<SwapResponse>("/api/swap/request", request);
        await _nav.NavigateToAsync("//swaps");
        return response?.Success == true;
    }
}
```

---

**Q178: How do you implement undo/redo in a MAUI app?**

**Answer:**
```csharp
public class UndoRedoService
{
    private readonly Stack<IMemento> _undo = new();
    private readonly Stack<IMemento> _redo = new();

    public void Record(IMemento memento)
    {
        _undo.Push(memento);
        _redo.Clear();
    }

    public IMemento? Undo()
    {
        if (_undo.Count == 0) return null;
        var current = _undo.Pop();
        _redo.Push(current);
        current.Restore();
        return current;
    }

    public IMemento? Redo()
    {
        if (_redo.Count == 0) return null;
        var current = _redo.Pop();
        _undo.Push(current);
        current.Restore();
        return current;
    }
}
```

---

**Q179: How do you handle unhandled exceptions globally in MAUI?**

**Answer:**
```csharp
// MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    
    // .NET exception handler
    AppDomain.CurrentDomain.UnhandledException += (s, e) =>
    {
        var ex = e.ExceptionObject as Exception;
        File.WriteAllText("crash.log", $"{DateTime.UtcNow}: {ex}");
    };

    // Task exception handler
    TaskScheduler.UnobservedTaskException += (s, e) =>
    {
        File.WriteAllText("task_crash.log", $"{DateTime.UtcNow}: {e.Exception}");
        e.SetObserved();
    };

    // MAUI thread exception handler
#if WINDOWS
    Microsoft.UI.Xaml.Application.Current.UnhandledException += (s, e) =>
    {
        File.WriteAllText("ui_crash.log", $"{DateTime.UtcNow}: {e.Exception}");
        e.Handled = true;
    };
#endif

    return builder.Build();
}
```

---

**Q180: How do you implement biometric authentication in MAUI?**

**Answer:**
```csharp
public async Task<bool> AuthenticateWithBiometricsAsync()
{
    try
    {
        var result = await SecureStorage.Default.GetAsync("auth_token");
        if (string.IsNullOrEmpty(result)) return false;

        var isAvailable = await Biometrics.Default.CanAuthenticateAsync();
        if (!isAvailable)
        {
            await ShowAlertAsync("Biometrics not available");
            return false;
        }

        var authResult = await Biometrics.Default.AuthenticateAsync(
            new AuthenticationRequest
            {
                Title = "EV Swap Login",
                Reason = "Authenticate to access your account"
            });

        return authResult.Status == BiometricAuthenticationStatus.Success;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Biometric auth failed");
        return false;
    }
}
```

---

**Q181: How do you implement infinite scroll with `RemainingItemsThreshold`?**

**Answer:**
```xml
<CollectionView ItemsSource="{Binding Stations}"
                RemainingItemsThreshold="5"
                RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}">
```

```csharp
[ObservableProperty] private int _currentPage = 1;
[ObservableProperty] private bool _hasMorePages = true;

[RelayCommand]
async Task LoadMoreAsync()
{
    if (IsBusy || !HasMorePages) return;
    IsBusy = true;
    try
    {
        var page = await _api.GetAsync<List<StationModel>>(
            $"/api/station?page={CurrentPage}&size=20");
        if (page?.Count > 0)
        {
            foreach (var station in page)
                Stations.Add(station);
            CurrentPage++;
        }
        else
        {
            HasMorePages = false;
        }
    }
    finally { IsBusy = false; }
}
```

---

**Q182: How do you implement search with debounce in MAUI?**

**Answer:**
```csharp
private CancellationTokenSource? _searchCts;

partial void OnSearchQueryChanged(string value)
{
    // Cancel previous search
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();
    var token = _searchCts.Token;

    // Debounce 300ms
    _ = Task.Run(async () =>
    {
        await Task.Delay(300, token);
        if (!token.IsCancellationRequested)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await SearchStationsAsync(value);
            });
        }
    });
}
```

---

**Q183: How do you cache API responses in MAUI?**

**Answer:**
```csharp
public class CachedApiService : IApiService
{
    private readonly IApiService _inner;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 100,
        ExpirationScanFrequency = TimeSpan.FromMinutes(5)
    });

    public CachedApiService(IApiService inner)
    {
        _inner = inner;
    }

    public async Task<T?> GetAsync<T>(string endpoint) where T : class
    {
        var cacheKey = $"GET:{endpoint}";
        if (_cache.TryGetValue(cacheKey, out T? cached))
            return cached;

        var result = await _inner.GetAsync<T>(endpoint);
        if (result is not null)
        {
            _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                Size = 1
            });
        }
        return result;
    }
}
```

---

**Q184: How do you handle simultaneous API calls efficiently?**

**Answer:**
```csharp
// Parallel independent calls
public async Task LoadDashboardDataAsync()
{
    // Run all API calls concurrently
    var userTask = _api.GetAsync<UserModel>("/api/user/profile");
    var dashboardTask = _api.GetAsync<DashboardModel>("/api/report/user-dashboard");
    var stationsTask = _api.GetAsync<List<StationModel>>("/api/station/nearby");

    // Wait for all to complete
    await Task.WhenAll(userTask, dashboardTask, stationsTask);

    // Assign results
    User = userTask.Result;
    Dashboard = dashboardTask.Result;
    Stations = new ObservableCollection<StationModel>(stationsTask.Result ?? new());
}

// Limit concurrency with SemaphoreSlim
private static readonly SemaphoreSlim _gate = new(3, 3);

public async Task<T> ThrottledCallAsync<T>(Func<Task<T>> call)
{
    await _gate.WaitAsync();
    try { return await call(); }
    finally { _gate.Release(); }
}
```

---

**Q185: How do you structure multi-module MAUI apps?**

**Answer:**
```
EVSwap.slnx
├── src/
│   ├── EVSwap.API/             # Backend (ASP.NET Core)
│   ├── EVSwap.Shared/          # Shared models & interfaces
│   │   ├── Models/
│   │   ├── Interfaces/
│   │   └── Constants.cs
│   ├── EVSwap.Mobile/          # MAUI app
│   ├── EVSwap.Admin/           # Admin MAUI app (references EVSwap.Shared)
│   └── EVSwap.Tests/           # Unit tests
│       ├── Services/
│       └── ViewModels/
```

Modules communicate through shared interfaces in `EVSwap.Shared`, avoiding direct project references between mobile and admin apps.

---

**Q186: How do you implement app-wide theming with dynamic switching?**

**Answer:**
```csharp
public partial class ThemeService
{
    public void ApplyTheme(string themeName)
    {
        var mergedDicts = Application.Current!.Resources.MergedDictionaries;
        mergedDicts.Clear();

        mergedDicts.Add(new ResourceDictionary
        {
            Source = new Uri($"Resources/Styles/{themeName}.xaml", UriKind.Relative)
        });
    }
}

// ViewModel
[RelayCommand]
void ToggleTheme()
{
    var current = Application.Current!.RequestedTheme;
    Application.Current.UserAppTheme = current == AppTheme.Dark
        ? AppTheme.Light
        : AppTheme.Dark;
}
```

---

**Q187: How do you handle app state persistence between sessions?**

**Answer:**
```csharp
// Save state on OnSleep
public partial class App : Application
{
    protected override void OnSleep()
    {
        base.OnSleep();
        // Save navigation state
        Preferences.Default.Set("last_page", Shell.Current.CurrentState.Location.ToString());
        Preferences.Default.Set("last_activity", DateTime.UtcNow.ToString("O"));
    }

    protected override void OnResume()
    {
        base.OnResume();
        // Restore state
        var lastPage = Preferences.Default.Get("last_page", "//login");
        Shell.Current.GoToAsync(lastPage).FireAndForget();
    }
}
```

---

**Q188: How do you handle app version checking and forced update?**

**Answer:**
```csharp
public async Task CheckVersionAsync()
{
    try
    {
        var versionInfo = await _api.GetAsync<VersionInfo>("/api/app/version");
        if (versionInfo is null) return;

        var currentVersion = Version.Parse(AppInfo.Current.VersionString);
        var minimumVersion = Version.Parse(versionInfo.MinimumVersion);

        if (currentVersion < minimumVersion)
        {
            var update = await Shell.Current.CurrentPage.DisplayAlert(
                "Update Required",
                $"Version {versionInfo.MinimumVersion} is required. You have {currentVersion}.",
                "Update Now", "Exit");

            if (update)
                await Launcher.Default.OpenAsync(versionInfo.StoreUrl);
            else
                Application.Current?.Quit();
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Version check failed");
    }
}
```

---

**Q189: What is `IMauiHandlersCollection` and how do you register custom handlers?**

**Answer:**
```csharp
// MauiProgram.cs
builder.ConfigureMauiHandlers(handlers =>
{
    handlers.AddHandler<CustomEntry, CustomEntryHandler>();
});

// Custom entry with platform-specific behavior
public class CustomEntry : Entry { }

public class CustomEntryHandler : EntryHandler
{
    protected override void ConnectHandler(MauiEntry platformView)
    {
        base.ConnectHandler(platformView);
        // Platform-specific setup
        platformView.TextChanged += OnTextChanged;
    }

    private void OnTextChanged(object? sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
    {
        // Handle text changed on WinUI
    }
}
```

---

**Q190: How does `IApplication` work in MAUI?**

**Answer:**
```csharp
public partial class App : Application, IApplication
{
    // IApplication provides access to:
    // - Application.Current (static)
    // - MainPage (the root page)
    // - RequestedTheme (light/dark)
    // - Resources (merged dictionaries)
    // - Windows (open windows collection)

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = new AppShell();
        return new Window(shell);
    }
}
```

On Windows, each `Window` can be resized, positioned, and titled independently.

---

**Q191: How does MAUI handle 60fps animations with `GraphicsView`?**

**Answer:**
```csharp
public class BatteryGaugeDrawable : IDrawable
{
    public float BatteryLevel { get; set; } = 0.75f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Background circle
        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = 10;
        canvas.DrawCircle(dirtyRect.Center, 80);

        // Battery level arc
        canvas.StrokeColor = BatteryLevel > 0.2f ? Colors.Green : Colors.Red;
        canvas.DrawArc(40, 40, 160, 160, 90, 90 + (360 * BatteryLevel), false, false);

        // Center text
        canvas.FontColor = Colors.White;
        canvas.FontSize = 24;
        canvas.DrawString($"{BatteryLevel:P0}", dirtyRect.Center.X, dirtyRect.Center.Y,
            HorizontalAlignment.Center);
    }
}

// XAML:
// <GraphicsView x:Name="GaugeView" HeightRequest="200" />
// GaugeView.Drawable = new BatteryGaugeDrawable();
// GaugeView.Invalidate();  // trigger redraw
```

---

**Q192: How do you implement a `Map` control in MAUI?**

**Answer:**
```csharp
// NuGet: Microsoft.Maui.Controls.Maps

// XAML:
// <maps:Map x:Name="StationsMap" IsShowingUser="True" />

public async Task LoadStationsOnMapAsync(List<StationModel> stations)
{
    StationsMap.Pins.Clear();

    foreach (var station in stations)
    {
        var pin = new Pin
        {
            Label = station.Name,
            Address = station.Address,
            Location = new Location(station.Latitude, station.Longitude),
            Type = PinType.Place
        };
        pin.MarkerClicked += (s, e) =>
        {
            Shell.Current.GoToAsync("//stationdetail", new Dictionary<string, object>
            {
                { "Station", station }
            });
        };
        StationsMap.Pins.Add(pin);
    }

    // Center on first station
    if (stations.Any())
    {
        var center = new Location(
            stations.Average(s => s.Latitude),
            stations.Average(s => s.Longitude));
        StationsMap.MoveToRegion(MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(5)));
    }
}
```

---

**Q193: How do you implement `LocalNotification` in MAUI?**

**Answer:**
```csharp
public class NotificationService
{
    public async Task ShowSwapNotificationAsync(string title, string body)
    {
        var notification = new NotificationRequest
        {
            Title = title,
            Description = body,
            ScheduleType = NotificationScheduleType.Time,
            DeliveryTime = DateTime.Now.AddSeconds(1),
            CategoryType = NotificationCategoryType.Status,
            Android = new AndroidOptions
            {
                ChannelId = "swap_updates",
                Priority = AndroidPriority.High
            }
        };

        await LocalNotificationCenter.Current.Show(notification);
    }
}
// Requires Plugin.LocalNotification NuGet package
```

---

**Q194: How does `HttpCompletionOption` affect HTTP performance?**

**Answer:**
```csharp
// ResponseContentRead (default) - waits for entire response body
await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);

// ResponseHeadersRead - returns as soon as headers arrive, body streams
using var response = await _httpClient.GetAsync(url,
    HttpCompletionOption.ResponseHeadersRead);
using var stream = await response.Content.ReadAsStreamAsync();
// Process stream incrementally - useful for large responses
```

---

**Q195: How do you implement `GlobalUsings` in .NET MAUI?**

**Answer:**
```csharp
// GlobalUsings.cs (at project root)
global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Linq;
global using System.Net.Http;
global using System.Net.Http.Json;
global using System.Threading.Tasks;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using EVSwap.Mobile.Models;
global using EVSwap.Mobile.Services;
global using EVSwap.Mobile.ViewModels;
global using EVSwap.Mobile.Views;

// These are available in all .cs files without explicit using statements
```

---

**Q196: What are `Conditional` compilation symbols in MAUI?**

**Answer:**
```csharp
// Built-in conditional symbols:
// DEBUG, RELEASE (all platforms)
// WINDOWS, ANDROID, IOS, MACCATALYST (platform-specific)
// NET10_0 (framework version)

public class PlatformService
{
    public static string GetPlatformName()
    {
#if WINDOWS
        return "Windows";
#elif ANDROID
        return "Android";
#elif IOS
        return "iOS";
#elif MACCATALYST
        return "macOS";
#else
        return "Unknown";
#endif
    }
}

// Custom symbols in .csproj:
// <DefineConstants>$(DefineConstants);USE_MOCK_API</DefineConstants>
```

---

**Q197: How does `Assembly` loading work in MAUI for reflection?**

**Answer:**
```csharp
public class ViewModelLocator
{
    public static IEnumerable<Type> GetAllViewModels()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseViewModel))
                     && !t.IsAbstract);
    }

    public static BaseViewModel? ResolveViewModel(string name)
    {
        var type = Assembly.GetExecutingAssembly()
            .GetType($"EVSwap.Mobile.ViewModels.{name}ViewModel");
        return type is not null
            ? Activator.CreateInstance(type) as BaseViewModel
            : null;
    }
}
```

---

**Q198: How do you implement `RateThisApp` functionality?**

**Answer:**
```csharp
public async Task PromptForReviewAsync()
{
    var launchCount = Preferences.Default.Get("launch_count", 0);
    Preferences.Default.Set("launch_count", launchCount + 1);

    if (launchCount >= 5)  // Prompt after 5 launches
    {
        var lastPrompt = Preferences.Default.Get("last_review_prompt", DateTime.MinValue);
        if ((DateTime.UtcNow - lastPrompt).TotalDays > 90)  // Max every 90 days
        {
            var result = await Shell.Current.CurrentPage.DisplayAlert(
                "Enjoying EV Swap?",
                "Rate us on the store to support development!",
                "Rate Now", "Later");

            if (result)
            {
                await Launcher.Default.OpenAsync(
                    "ms-windows-store://pdp/?productid=9WZDNCRXXXXX");
                Preferences.Default.Set("last_review_prompt", DateTime.UtcNow);
            }
        }
    }
}
```

---

**Q199: What is the `InterProcessCommunication` strategy in .NET MAUI?**

**Answer:**
MAUI itself doesn't provide IPC, but on Windows you can use:

```csharp
// Named pipes for IPC between two .NET apps
public class NamedPipeService
{
    public async Task SendMessageAsync(string message)
    {
        using var pipeClient = new NamedPipeClientStream(".", "EVSwapPipe", PipeDirection.Out);
        await pipeClient.ConnectAsync(1000);
        using var writer = new StreamWriter(pipeClient);
        await writer.WriteLineAsync(message);
    }

    public async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var pipeServer = new NamedPipeServerStream("EVSwapPipe", PipeDirection.In);
            await pipeServer.WaitForConnectionAsync(ct);
            using var reader = new StreamReader(pipeServer);
            var message = await reader.ReadLineAsync(ct);
            // Process message
        }
    }
}
```

---

**Q200: How do you prepare for a .NET MAUI interview using this project?**

**Answer:**
1. **Understand the architecture** — practice explaining the full flow: LoginPage → ViewModel → AuthService → ApiService → HttpClient → API Controller → EF Core → Database.

2. **Know the MVVM pattern inside out** — be ready to explain `[ObservableProperty]`, `[RelayCommand]`, `INotifyPropertyChanged`, compiled bindings.

3. **Study the refactoring decisions** — why `CloneRequestAsync` was removed, why `catch {}` with dummy data, why services were split. Interviewers love hearing about design decisions.

4. **Practice the async flow** — be comfortable explaining async/await, ConfigureAwait(false), thread marshaling in MAUI.

5. **Know the API side** — JWT authentication, EF Core migrations, repository pattern, Controller structure.

6. **Have opinions** — "I prefer `CollectionView` over `ListView` because...", "I'd add logging here...", "The auth flow could be improved by..."

7. **Code walkthrough** — be ready to screenshare and explain any file in the project. Know where things live: `MauiProgram.cs` for DI, `AppShell.xaml` for routing, `Styles.xaml` for theming.

8. **Mock an improvement** — "If I had another sprint, I'd add offline queueing with SQLite, logging with ILogger, and unit tests for all ViewModels."

---

## Quick Reference: Key Interview Topics Summary

| Topic | Key Points |
|-------|-----------|
| **App Architecture** | MVVM, Shell routing, DI, services layer |
| **MAUI** | ContentPage, Shell, CollectionView, bindings, styles |
| **C#** | async/await, LINQ, records, pattern matching, nullable refs |
| **EF Core** | Migrations, Include/ThenInclude, AsNoTracking, IQueryable |
| **REST** | JWT, HttpClient, status codes, serialization |
| **DI** | Singleton vs Transient, composition root, mocking |
| **Testing** | Moq/NSubstitute, Arrange-Act-Assert, async testing |
| **Security** | SecureStorage, HTTPS, input validation, parameterized queries |
| **Performance** | CollectionView virtualization, AOT, compiled bindings |

---

> **Tip:** Connect every answer back to this specific app. When asked "How does DI work?", say "In our app, MauiProgram.cs registers services, and every ViewModel receives them via constructor injection — for example, LoginViewModel receives IAuthService, which is backed by AuthService registered as a singleton."

> **Tip:** If you don't know an answer, say "I haven't encountered that in this project, but here's how I'd approach it..." — honesty + problem-solving beats bluffing.
