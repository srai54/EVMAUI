# Interview Questions — .NET MAUI & EV Swap App (200+ Questions with Answers)

---

## 1. Application-Specific (EV Swap Project)

**Q1: Walk through the app architecture — how does a login request flow from UI to API?**

**Answer:**

**Theory:** The app follows a **layered architecture** that separates concerns into distinct tiers: Presentation (XAML), ViewModel (logic), Service (business orchestration), HTTP (communication), and API (backend). Each layer has a single responsibility and communicates only with the adjacent layer. This separation ensures that changes to one layer (e.g., swapping XAML for a different UI) don't affect the others. The login flow is the canonical example of how all five layers collaborate — it demonstrates the full round-trip from user input to secure credential verification and session establishment. Understanding this flow is essential because the same pattern repeats across every feature: dashboard data, swap requests, wallet operations — all follow the same ViewModel → Service → ApiService → HTTP → Controller pipeline.

**Code Example:**
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

**Answer:**

**Theory:** The bypass login is a **development/demo convenience** — it lets developers and stakeholders navigate the full app experience without a running backend. This is common in mobile development where the frontend and backend teams work in parallel. The pattern uses a **graceful degradation** strategy: rather than crashing or showing an endless spinner when the API is unavailable, the app creates a synthetic session and populates sample data on every screen. This approach has tradeoffs: it enables rapid UI development and demos, but it also means error paths are exercised differently than in production (real auth failures redirect to login; bypass mode just shows dummy data). From a security perspective, the bypass is only possible because the code explicitly allows it — it should never ship to production without being gated behind a compile-time flag or feature toggle.

**Code Example:**
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

**Theory:** `HandleResponse<T>` implements a **centralized error-handling strategy** that prevents duplication across every API call. Instead of each ViewModel writing its own response-parsing logic, a single generic method handles the four possible outcomes of any HTTP call: (1) non-success status code — throw with the API's error message, (2) empty body — return null, (3) successful deserialization — return the typed result, (4) null deserialization despite content — throw a parse error. This pattern follows the **DRY principle** and ensures consistent error behavior across the entire app. The method is `private static` because it's an internal implementation detail of `ApiService` — no external code should duplicate or bypass this logic. Making it `static` signals it has no side effects (pure function: given a response, return a typed result or throw).

**Code Example:**
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

**Answer:**

**Theory:** ViewModels in MVVM should never hold direct references to each other — doing so creates tight coupling and makes the app harder to test and refactor. Instead, communication happens through **indirection**: navigation routes + shared service state. After login, `LoginViewModel` tells the navigation system to switch to the dashboard route; it doesn't know or care which ViewModel the dashboard uses. The `DashboardViewModel` independently reads shared state (`AuthService.CurrentUser`) that was set by the login flow. This is the **mediator pattern**: ViewModels interact through a central coordinator (Shell navigation + DI container) rather than directly. This decoupling means you can change the dashboard implementation, add pre-loading logic, or swap ViewModels without touching the login code.

**Code Example:**
```csharp
await NavigationService.NavigateToAsync($"//{Constants.Routes.Dashboard}");
```
Shell routing resolves `DashboardPage` → DI creates `DashboardViewModel` → `DashboardViewModel` reads `AuthService.CurrentUser` to get the logged-in user's data. No direct reference between the two ViewModels exists.

---

**Q10: What is `Constants.ApiBaseUrl` and how is it configured?**

**Answer:**

**Theory:** Centralizing the API base URL in a single constant prevents the URL from being hard-coded across the app — if the API moves to a different port, domain, or protocol, only one file changes. This follows the **DRY principle** and is the simplest form of configuration management. In production apps, this would typically come from a configuration file (`appsettings.json`) or environment variable so different builds (dev, staging, production) can target different servers without recompiling. The current approach (hard-coded `const`) works for development but means every environment requires a separate build — a production improvement would be reading from `Preferences` or the `Options` pattern with `IOptions<T>`.

**Code Example:**
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

**Theory:** JWT tokens are **credentials** — if stolen, an attacker can impersonate the user. Storing them in plain text (`Preferences`, a local file, or `MemoryStream`) is a security vulnerability. `SecureStorage` provides **encrypted-at-rest storage** using each platform's native security infrastructure: Windows DPAPI (encrypts with the user's Windows login credentials), iOS KeyChain (hardware-backed encryption), and Android KeyStore (AES-encrypted with a per-app key). The token is decrypted only when `GetAsync` is called, minimizing exposure. The `DefaultRequestHeaders.Authorization` assignment attaches the token to every HTTP request automatically — but this means the token lives in memory in the `HttpClient` instance, so `HttpClient` should be a singleton to avoid redundant token attachments.

**Code Example:**
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

**Theory:** Auto-login improves user experience by eliminating the friction of re-entering credentials on every app launch. The check happens on startup because the token persists across sessions in `SecureStorage` — if the user logged in previously and the token hasn't expired, there's no need to show the login screen. This pattern assumes the token is still valid; the app will handle 401 responses from the API later if the token expired (by redirecting to login for a fresh token). The check is in `App.xaml.cs` (the application entry point) rather than a ViewModel because it's a **startup-routing decision** — which page to show first — not business logic. This is also where you'd add a splash screen or loading indicator while the token validation occurs.

**Code Example:**
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

**Theory:** Every ViewModel in the app needs three things: a busy-state indicator (to show/hide loading spinners), a title (for the page header), a refresh flag (for pull-to-refresh), navigation access, and connectivity awareness. Without a base class, each ViewModel would duplicate these properties and their `[ObservableProperty]` boilerplate. `BaseViewModel` applies the **Template Method pattern** — it defines common infrastructure that subclasses inherit while allowing each ViewModel to override or extend as needed. This is one of the few legitimate uses of inheritance in MAUI: the base encapsulates cross-cutting UI concerns, while the subclasses contain feature-specific logic. The base class also serves as a **convenience contract** — extending `BaseViewModel` signals "this class is a ViewModel" and ensures consistent behavior across all pages.

**Code Example:**
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

**Theory:** `SecureStorage.Default` is a **static API** — it's called directly as `SecureStorage.Default.SetAsync(...)`. Dependent code directly calling a static API creates two problems: (1) the code is coupled to a specific platform implementation, making it impossible to swap (e.g., for a test double or a cloud-backed storage), and (2) unit tests cannot mock a static method — you'd need real platform APIs, making tests slow, brittle, and environment-dependent. Wrapping it behind `ISecureStorageService` applies the **Dependency Inversion Principle** (the "D" in SOLID): high-level code (ViewModels) depends on an abstraction (the interface), not a concrete implementation. This is the standard adapter pattern for platform APIs — it decouples business logic from infrastructure, enabling both testability and future migration.

**Code Example:**
```csharp
// Interface (abstraction)
public interface ISecureStorageService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    void Remove(string key);
}

// Implementation (concrete)
public class SecureStorageService : ISecureStorageService
{
    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);
    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);
    public void Remove(string key) => SecureStorage.Default.Remove(key);
}
```

---

**Q15: How would you add offline support for swap requests when the API is unreachable?**

**Answer:**

**Theory:** Mobile apps must handle unreliable connectivity. The **offline-first** pattern treats the local device as the primary data store and the server as the sync target. This implementation uses a **write-ahead queue**: when an API call fails, instead of showing an error, the request is saved to a local SQLite database for later retry. When connectivity returns, the queue is drained. This is known as the **Outbox pattern** — requests are "sent" to an outbox (local DB) first, then asynchronously delivered to the actual server. The key design considerations are: (1) **idempotency** — the server must handle duplicate deliveries of the same request safely, (2) **ordering** — requests should be replayed in the order they were created, (3) **conflict resolution** — what happens if the local state diverges from the server state? A full offline-first implementation would use a local SQLite database with sync logic, conflict detection, and a background service for queue processing.

**Code Example:**
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

**Theory:** MAUI provides three navigation containers, each suited to different app architectures. `ContentPage` is the atomic unit — a single screen that fills the display area. **Every** page in a MAUI app IS a `ContentPage` (or inherits from it). `Shell` is the modern, recommended navigation container — it manages an entire app structure with URI-based routing, built-in flyout menus, tab bars, and search handlers. Think of Shell as the "app shell" that wraps all pages and provides navigation chrome. `NavigationPage` is the legacy stack-based navigation — it pushes/pops pages onto a LIFO stack with a back button. It's simpler than Shell but lacks URI routing, deep linking, and complex navigation structures. The choice is architectural: Shell for multi-page apps with tabs/flyouts, NavigationPage for simple back-button scenarios, and ContentPage for modal dialogs or embedded pages.

**Code Example:**

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

**Theory:** Shell uses **URI-based navigation** modeled after web URLs — each page is identified by a route string, and navigation is done by changing the current URI. Routes are hierarchical: `//dashboard` is an absolute route (regardless of current state, navigate to the dashboard), while `stations` is a relative route (push onto the current navigation stack). Shell maintains a **navigation stack** that maps URIs to page instances. When you navigate, Shell parses the URI, resolves the target page via DI, optionally passes query parameters, and animates the transition. This is fundamentally different from the push/pop model of `NavigationPage` — Shell routes can go to any page from anywhere without maintaining manual navigation state.

**Code Example:**
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

**Theory:** Service lifetime determines **when** the DI container creates an instance and **how long** it keeps it alive. `Singleton` creates one instance for the entire application lifetime — every consumer receives the same object. This is essential for shared state (auth tokens, HTTP client configuration, cached data) but dangerous if the service holds per-screen state (data from one ViewModel would leak to another). `Transient` creates a new instance every time it's requested — each ViewModel and Page gets its own fresh copy, preventing state leakage between navigations. `AddScoped` creates one instance per scope — in web apps this is per-request, but in MAUI there's no built-in per-request scope, so `AddScoped` behaves like `Singleton` in practice. Choose `Singleton` for stateless services and shared state; choose `Transient` for stateful objects like ViewModels.

**Code Example:**
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

**Theory:** Passing data between pages in MVVM is tricky because ViewModels should not hold direct references to each other (that would create tight coupling). MAUI provides three decoupled mechanisms. **QueryProperty** is the simplest — the source page passes data in a dictionary during navigation, and Shell sets the target ViewModel's property before it appears. This works for single objects but doesn't scale to many parameters. **Shared service state** is useful for data that naturally lives at the app level (current user, app configuration) — a singleton ViewModel can read from a shared service. **WeakReferenceMessenger** implements publish/subscribe messaging — any ViewModel can send a message, and any other ViewModel that has subscribed receives it. This is the most decoupled approach but requires discipline (subscriptions must be cleaned up to avoid memory leaks). Choose `QueryProperty` for navigation-specific data, shared services for cross-cutting state, and messaging for loosely-coupled notifications.

**Code Example:**
```csharp
// Method 1: QueryProperty
[QueryProperty(nameof(Station), "Station")]
public partial class SwapRequestViewModel : BaseViewModel
{
    public StationModel? Station { get; set; }
}
await Shell.Current.GoToAsync("swaprequest", new Dictionary<string, object>
{
    { "Station", selectedStation }
});

// Method 2: Static service / shared state
var user = _authService.CurrentUser;

// Method 3: MessagingCenter / WeakReferenceMessenger
WeakReferenceMessenger.Default.Send(new SwapCompletedMessage(swapId));
```

---

**Q20: What is `AppThemeBinding` and how is it used in `Styles.xaml`?**

**Answer:**

**Theory:** `AppThemeBinding` enables **adaptive theming** — UI properties automatically switch between Light and Dark values based on the OS theme setting without any code-behind. When the user changes their system theme (or the app overrides it via `UserAppTheme`), the binding engine re-evaluates all `AppThemeBinding` expressions and updates the UI. This is critical for user experience: dark mode reduces eye strain in low-light environments and saves battery on OLED screens. Without `AppThemeBinding`, you'd need manual theme detection and property-setting in every page. The binding is evaluated at the resource-dictionary level so it works in styles — define it once in `Styles.xaml` and every control in the app automatically adapts.

**Code Example:**
```xml
<Style TargetType="Entry">
    <Setter Property="TextColor" Value="{AppThemeBinding Light=Black, Dark=White}" />
</Style>
```

In light mode, Entry text is black. In dark mode, it's white. This is defined globally in `Resources/Styles/Styles.xaml` so every Entry in the app automatically adapts.

---

**Q21: How does MAUI handle platform-specific code?**

**Answer:**

**Theory:** MAUI targets Windows, Android, iOS, macOS, and Tizen with shared C# code, but each platform has unique APIs, behaviors, and UI conventions. MAUI provides three mechanisms for platform-specific code. The **platform folders** approach (`Platforms/Windows/`, `Platforms/Android/`) uses the `.csproj` build configuration to include platform-specific files only for their target — this is ideal for large blocks of platform code like push notification registration. **Conditional compilation** (`#if WINDOWS`) is best for small platform differences scattered across shared files. **Partial class files** (`.Windows.cs` suffix) split a class's platform-specific implementation across files — the shared part goes in `MyClass.cs` and platform parts go in `MyClass.Windows.cs`. The choice depends on volume: platform folders for entire files, conditional directives for a few lines, partial classes for mid-sized implementations.

**Code Example:**
```csharp
// Approach 1: Platform folders
// Files in Platforms/Windows/ compile only for Windows target

// Approach 2: Conditional compilation
#if WINDOWS
    _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5238") };
#endif

// Approach 3: Partial classes
// MyService.cs - shared code
// MyService.Windows.cs - Windows-specific code
```

---

**Q22: What layout panels are available in MAUI and when would you use each?**

**Answer:**

**Theory:** Layout panels are containers that arrange their child elements. Each layout has a different **measurement and arrangement** algorithm, which affects both visual outcome and performance. `VerticalStackLayout` and `HorizontalStackLayout` are simple one-dimensional stacks — fast for linear arrangements but don't wrap. `Grid` is the most powerful and most-used layout — it arranges children in rows and columns with proportional (`*`), absolute, or auto-sized dimensions. Grid is ideal for complex forms and dashboards. `FlexLayout` implements the CSS Flexbox model — children wrap to new lines when they exceed the container width, making it ideal for responsive UIs that adapt to different screen sizes. `AbsoluteLayout` positions children by exact coordinates — use sparingly for overlays and animations. `ScrollView` enables scrolling when content exceeds the available space but adds performance cost because all children are measured upfront.

**Code Example:**
```xml
<!-- Grid is the most versatile and most used in this app -->
<Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto">
    <Label Text="Balance" Grid.Row="0" Grid.Column="0" />
    <Label Text="$250" Grid.Row="0" Grid.Column="1" />
</Grid>
```

| Layout | Behavior | Use Case |
|--------|----------|----------|
| `VerticalStackLayout` | Stacks children vertically | Simple column layouts |
| `HorizontalStackLayout` | Stacks children horizontally | Toolbars, button rows |
| `Grid` | Row/column-based positioning | Complex forms, dashboards |
| `FlexLayout` | Flexbox-like wrapping | Responsive layouts |
| `AbsoluteLayout` | Exact x/y positioning | Overlays, absolute positioning |
| `ScrollView` | Scrollable content | Forms longer than screen height |

---

**Q23: Explain how `CollectionView` differs from `ListView`. Which is better for performance?**

**Answer:**

**Theory:** Both `CollectionView` and `ListView` display scrollable lists, but `CollectionView` is the **modern successor** with a fundamentally better virtualization engine. The key performance difference is **UI virtualization**: `CollectionView` creates and destroys item views on-demand as the user scrolls, using a view-recycling pool. `ListView` also virtualizes but its older architecture keeps more view objects alive, consuming more memory for large lists. `CollectionView` also supports horizontal layouts, grid layouts, multi-selection, and a built-in `EmptyView` — features that `ListView` lacks or implements poorly. For any new MAUI app, `CollectionView` should be the default choice. The only reason to use `ListView` is if you're maintaining a legacy Xamarin.Forms migration where swapping to `CollectionView` would introduce too many breaking changes.

**Code Example:**
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

**Theory:** Data binding is the **glue** that connects the View (XAML) to the ViewModel (logic) without either having a direct reference to the other. At its core, binding is an **observer pattern** implemented by the XAML binding engine. When you write `{Binding Username}`, the engine creates a `BindingExpression` that: (1) reads the `BindingContext` of the current element (which is typically set to the ViewModel), (2) subscribes to the source object's `PropertyChanged` event, (3) sets the target property (`Entry.Text`) to the current value, and (4) whenever `PropertyChanged` fires for `"Username"`, re-reads the property and updates the UI. This is what makes MVVM reactive — UI automatically reflects ViewModel state without manual synchronization. `[ObservableProperty]` auto-generates the property with `PropertyChanged` notification, eliminating boilerplate while keeping the binding mechanism intact.

**Code Example:**
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

**Theory:** By default, XAML bindings use **runtime reflection** — the binding engine checks property names only when the page loads. A typo like `{Binding Usernam}` compiles successfully but silently fails at runtime (the label just shows nothing). `x:DataType` enables **compiled bindings** — the XAML compiler knows the BindingContext's type at compile time, validates all binding paths, and generates strongly-typed code that accesses properties directly without reflection. This has two benefits: compile-time error detection (typos become build errors) and better performance (no runtime reflection overhead). Compiled bindings are especially important for `CollectionView` item templates where hundreds of bindings might be evaluated per second. Always set `x:DataType` at the page level and override it in `DataTemplate` scopes.

**Code Example:**
```xml
<ContentPage x:DataType="vm:LoginViewModel">
    <Entry Text="{Binding Username}" />  <!-- compiler checks Username exists on LoginViewModel -->
</ContentPage>
```

Without it, bindings are resolved at runtime. If you typo "Usernam", no compile error — just a silent runtime failure. With `x:DataType`, it becomes a compile error.

---

**Q26: How do you handle large lists in MAUI without freezing the UI?**

**Answer:**

**Theory:** UI freezes happen when the **main thread** is blocked by synchronous work — downloading, parsing, or constructing UI elements. Since MAUI's UI can only be updated from the main thread, any long-running operation on this thread prevents the UI from rendering, responding to touch, or animating. The solution combines four strategies: **virtualization** (`CollectionView` renders only visible items, recycling views as the user scrolls), **asynchronous loading** (data fetching runs on background threads via `async`/`await`), **pagination** (`RemainingItemsThreshold` triggers incremental loading before the user hits the end), and **incremental updates** (`ObservableCollection` adds items without replacing the entire collection, avoiding a full re-render). Without all four, even a modest list of 500 items can cause visible jank.

**Code Example:**
```xml
<CollectionView ItemsSource="{Binding Stations}"
                RemainingItemsThreshold="5"
                RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}">
```

ViewModel pagination:
```csharp
[ObservableProperty] private int _currentPage = 1;
[ObservableProperty] private bool _hasMorePages = true;

[RelayCommand]
async Task LoadMoreAsync()
{
    if (IsBusy || !HasMorePages) return;
    IsBusy = true;
    var page = await _api.GetAsync<List<StationModel>>($"/api/station?page={CurrentPage}&size=20");
    if (page?.Count > 0)
    {
        foreach (var s in page) Stations.Add(s);
        CurrentPage++;
    }
    else HasMorePages = false;
    IsBusy = false;
}
```

---

**Q27: What is `VisualStateManager` and how is it used in button styles?**

**Answer:**

**Theory:** `VisualStateManager` (VSM) lets you define **visual appearances** for different logical states of a control declaratively in XAML, without writing any code-behind. Each state (Normal, Disabled, PointerOver, Focused) specifies property values that apply when the control enters that state. The MAUI framework automatically transitions between states based on user interaction — for example, when a button's `IsEnabled` becomes `false`, the framework applies the `Disabled` visual state. VSM is superior to manually setting properties in code-behind event handlers because it keeps visual logic in XAML where it belongs, enables consistent styling across the app, and works with the Visual State Manager's transition animations. The most common use case is styling the `Disabled` state of buttons, but VSM can also handle validation states, selection states, and custom application states.

**Code Example:**
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

**Theory:** Pull-to-refresh is a standard mobile UX pattern where the user drags down on a scrollable list to trigger a data refresh. MAUI provides `RefreshView` as a **wrapper container** — it intercepts the pull gesture, shows a platform-specific refresh indicator (spinner), and fires a command when the pull threshold is reached. The key binding is `IsRefreshing` — a two-way binding to the ViewModel's property. The ViewModel sets `IsRefreshing = true` when the refresh starts, and the framework shows the spinner. When the refresh completes, the ViewModel sets `IsRefreshing = false`, and the framework hides the spinner and animates the content back. Without the two-way binding, the refresh indicator would never dismiss. The `Command` fires on the UI thread, so the refresh logic should be async and not block.

**Code Example:**
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

**Theory:** MAUI pages follow a predictable lifecycle that mirrors the mobile OS window lifecycle. `OnAppearing` fires every time the page becomes visible — including the first time it's created AND when the user navigates back to it. This makes it the correct place for **idempotent data loading** (loading that is safe to repeat). `OnDisappearing` fires when the page is no longer visible — either because the user navigated away or the app was backgrounded. This is where to **release resources**: stop timers, unsubscribe from events, dispose of streams. The constructor should only initialize services and set `BindingContext` — never load data there, because the page hasn't been measured yet and the user sees nothing. The flow is: `Constructor → OnAppearing (load data) → (user navigates away) → OnDisappearing (cleanup) → OnAppearing (if navigated back, reload)`.

**Code Example:**
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

**Theory:** `ResourceDictionary` is MAUI's centralized style repository — instead of setting `TextColor` or `FontSize` on every control individually, you define a `Style` once in a resource dictionary and apply it implicitly (by `TargetType`) or explicitly (by `Key`). The app's `Styles.xaml` merges into `App.xaml`, making all styles available app-wide. This follows the **DRY principle** for UI: change one value in `Styles.xaml` and every button, label, or entry in the app updates. Resource dictionaries support **inheritance** (a style can `BasedOn` another style), **theme-awareness** (via `AppThemeBinding`), and **merging** (multiple dictionaries combine into one scope). The merge order matters — later dictionaries override earlier ones for conflicting keys.

**Code Example:**
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
The `async` keyword does NOT run code on a background thread — it enables the compiler to transform your method into a **state machine** (a struct implementing `IAsyncStateMachine`). Each `await` becomes a state transition:

1. **Method starts** on the calling thread (UI thread in MAUI).
2. **At `await`**, the compiler checks if the awaited operation is already complete (`Task.IsCompleted`). If not, it suspends the method and returns an incomplete `Task` to the caller — the thread is freed.
3. **The awaiter** (`TaskAwaiter`) registers a continuation (the rest of the method) via `OnCompleted`. It captures the current `SynchronizationContext` — in MAUI, this is the **UI synchronization context** which schedules work back on the main thread.
4. When the async operation completes, the continuation is posted to the captured context and execution resumes after the `await` on the original thread.

**Why this matters for MAUI:** The captured `SynchronizationContext` ensures UI updates happen on the main thread automatically. You never need `Invoke` or `Dispatch` for code after an `await` — the runtime handles it.

```csharp
async Task LoadDataAsync()
{
    // Runs on UI thread
    IsBusy = true;

    // Await starts the HTTP call, yields UI thread
    var data = await _httpClient.GetStringAsync(url);

    // Continuation runs back on UI thread - safe to update UI
    IsBusy = false;
}
```

The compiler-generated state machine avoids blocking while keeping a linear code structure. Behind the scenes it becomes a `switch` statement over state values with `MoveNext()` calls — conceptually similar to `yield return` but for asynchronous continuations.

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
The classic async deadlock occurs when you **block on async code** from a UI (or ASP.NET) context:

```csharp
// DEADLOCK!
var data = _api.GetAsync<User>("/users/1").Result;
```

**Why it deadlocks (3 actors):**
1. You call `.Result` on the **UI thread** — the thread blocks waiting for the result.
2. The async method starts, hits its first `await`, and captures the **UI SynchronizationContext** to resume on the UI thread.
3. When the awaited operation completes, the continuation needs the UI thread to resume — but the UI thread is blocked by `.Result` in step 1.

This is a **context-capture deadlock**, not a thread-pool starvation issue. The async method can finish its I/O, but its continuation can't run because the context it needs is held by the blocked caller.

**Fix:** Never block on async code. Use `await` all the way up the call stack:

```csharp
public async Task InitializeAsync()
{
    var data = await _api.GetAsync<User>("/users/1");  // ✅ no deadlock
}
```

If you genuinely cannot make the caller async (legacy code), use `.ConfigureAwait(false)` in the async method to avoid capturing the UI context — though in MAUI ViewModels, avoid this for UI-touching code.

---

**Q34: Explain `ConfigureAwait(false)` — should you use it in MAUI apps?**

**Answer:**
`ConfigureAwait(false)` tells the runtime **not to capture** the current `SynchronizationContext` (or `TaskScheduler`). Without capture, the continuation after `await` can run on any available thread — typically a thread-pool thread.

**What is SynchronizationContext?** It's an abstraction that represents "where" work should run:
- **UI context** (`WindowsFormsSynchronizationContext`, `DispatcherSynchronizationContext`) — queues work onto the UI thread's message loop
- **ASP.NET context** (`AspNetSynchronizationContext`) — maintains request-scoped state
- **Default context** (`ThreadPoolSynchronizationContext`) — runs on thread-pool threads

In MAUI, the UI context posts continuations back to the main thread. This is desirable for ViewModels (you can update `IsBusy` after `await` without explicit dispatch) but unnecessary for library code:

```csharp
// Library code - no UI: ConfigureAwait(false) is safe and slightly faster
public async Task<User?> GetUserAsync(int id)
{
    var json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
    return JsonSerializer.Deserialize<User>(json);
}

// ViewModel code - touches UI: DO NOT use ConfigureAwait(false)
public async Task LoadAsync()
{
    var data = await _api.GetAsync(...);  // capture UI context
    IsBusy = false;  // still on UI thread - safe
}
```

**Performance benefit:** Skipping context capture avoids the overhead of posting to the message queue. In library code called thousands of times, this adds up. In MAUI ViewModels (a few calls per page), the difference is negligible.

---

**Q35: How does `JsonSerializer.Deserialize<T>` handle missing or extra JSON properties?**

**Answer:**

**Theory:** `System.Text.Json` uses a **lenient deserialization** model by default — it maps JSON properties to matching C# properties and silently ignores everything else. Missing JSON properties leave the C# property at its default value (null for reference types, 0 for numerics, false for bools). Extra JSON properties that don't exist on the target class are discarded. This leniency is intentional — it makes the client resilient to API changes (adding a field to the API response doesn't break the mobile app). The `PropertyNameCaseInsensitive` option enables case-insensitive matching because JSON is case-sensitive by default (`"name"` won't match `Name` without it). For stricter handling, use `JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow` to throw on extra properties, or `JsonRequiredAttribute` on required properties.

**Code Example:**
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

**Theory:** A `record` is a reference type (like `class`) but with **value semantics** — two records with the same property values are considered equal, whereas two classes with the same values are not (they compare by reference). This makes records ideal for DTOs, immutable data models, and message types where structural equality matters. Records also auto-generate `ToString()` (showing all properties), `GetHashCode()`, `Equals()`, `Deconstruct()`, and a clone method (`with` expression). The tradeoff is that records are designed for **immutability** — a positional `record Person(string Name)` creates read-only properties. If you need mutability, use a `class` or a `record struct`. In the EV Swap app, `AuthResponse`, `UserModel`, and DTOs would benefit from being records because they represent values that should be compared structurally.

**Code Example:**
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

**Theory:** Before C# 8, ALL reference types (`string`, `object`, `List<T>`) were implicitly nullable — you could assign null to any of them without the compiler complaining. This was the source of countless `NullReferenceException`s because there was no way to express "this value should never be null." C# 8 introduced **nullable reference types** with the `?` annotation: `string?` means "this can be null" and `string` means "this should never be null." The compiler performs **static flow analysis** to detect potential null violations — assigning a `string?` to a `string` without a null check produces a warning. The `#nullable enable` directive at the file or project level enables this analysis. This shifts null safety from runtime (exceptions) to compile-time (warnings), significantly reducing null-related bugs. In the EV Swap app, enabling nullable reference types would catch issues like `ApiService` methods that might return null.

**Code Example:**
```csharp
#nullable enable
public string Name { get; set; }         // non-nullable - compiler warns if might be null
public string? Description { get; set; } // nullable - OK to be null

UserModel? user = null;  // nullable reference type
if (user is not null)
    Console.WriteLine(user.Name);  // safe - compiler knows user is not null in this block
```

---

**Q38: What is pattern matching in C#?**

**Answer:**

**Theory:** Pattern matching is a **declarative alternative** to long `if-else` chains and type checks. Instead of writing `if (x is string s) { ... } else if (x is int i && i > 0) { ... }`, pattern matching lets you express the same logic as a single expression. C# has evolved pattern matching across versions: constant patterns (`is "Pending"`), type patterns (`is Person p`), property patterns (`is { IsActive: true }`), relational patterns (`is > 0`), list patterns (`is [1, 2, _]`), and discard patterns (`_`). Switch expressions (C# 8+) combine pattern matching with expression-bodied syntax for concise, exhaustive handling. The compiler checks that all cases are covered (exhaustiveness), preventing unhandled cases — a major improvement over traditional switch statements. In the EV Swap app, pattern matching is ideal for handling swap status transitions, role-based authorization, and API response types.

**Code Example:**
```csharp
// Switch expression with pattern matching - concise and exhaustive
var description = swap.Status switch
{
    "Pending" => "Awaiting approval",
    "InProgress" => "Swap in progress",
    "Completed" => "Swap done",
    _ => "Unknown status"  // discard pattern - default case
};

// Property pattern - clean null + property check
if (user is { IsActive: true, Roles: ["Admin"] })
    Console.WriteLine("Active admin user");
```

---

**Q39: How does `List<T>.ForEach` differ from a `foreach` loop?**

**Answer:**

**Theory:** `foreach` is a **language construct** that works with any `IEnumerable<T>` and compiles to a `GetEnumerator()`/`MoveNext()` pattern — it supports `await`, `break`, `continue`, and `return` inside the loop body. `List<T>.ForEach` is a **library method** that takes an `Action<T>` delegate — it's just a `foreach` loop internally but with a different calling convention. The key difference: `List.ForEach` cannot use `await` because `Action<T>` is synchronous (you'd need `Func<T, Task>` and `ForEachAsync`), and you can't `break` or `continue` from within a delegate. `List.ForEach` is slightly more allocation-heavy because each iteration calls a delegate. Use `foreach` by default for its flexibility; use `List.ForEach` only when you have a simple one-line action and readability benefits from the concise syntax.

**Code Example:**
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

**Theory:** Fire-and-forget is a pattern where you invoke an async method without `await`ing its completion — the method runs independently and the caller moves on immediately. It's useful when you need to trigger a background operation (saving a preference, logging, sending analytics) but don't need to wait for or handle its result. However, it's inherently dangerous because unhandled exceptions in the async method will crash the app unless explicitly caught — `FireAndForget` is an extension method that wraps the call in a `try-catch` to swallow exceptions. In the `SettingsViewModel`, the biometric preference is saved without blocking the UI thread, so the user can continue navigating while the save happens asynchronously in the background. A safer alternative is `async void` event handlers with explicit exception handling, or using `Task.Run` with a logging callback.

**Code Example:**
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

**Theory:** Unmanaged resources (file handles, network sockets, database connections, GDI objects, WinRT references) are not managed by .NET's garbage collector — they must be released deterministically or the system will leak them. `IDisposable` provides a standard contract: call `Dispose()` when you're done with the object. In C#, the `using` statement (or `using var` declaration) calls `Dispose()` automatically when the scope exits, even if an exception occurs. The full dispose pattern includes a `protected virtual void Dispose(bool disposing)` overload to handle both explicit disposal (release all resources) and finalizer-based disposal (release only unmanaged resources). In MAUI, the DI container calls `Dispose()` on Singleton services only when the app shuts down, and on Transient services when their scope ends (typically page navigation). `HttpClient` is `IDisposable`, but registering it as a Singleton means it lives for the app's lifetime, so explicit disposal is less critical.

**Code Example:**
```csharp
public class ApiService : IDisposable
{
    private readonly HttpClient _httpClient;

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Usage with 'using' ensures deterministic cleanup
using var stream = File.OpenRead("data.json");
// stream is disposed when this scope exits
```

In MAUI, Singleton services live as long as the app, so disposal happens at app shutdown. Transient services that hold resources should implement `IDisposable`.

---

**Q42: What is `Span<T>` and how does it differ from array slices?**

**Answer:**

**Theory:** `Span<T>` is a ref struct — allocated on the stack, never on the heap — that provides a safe, allocation-free view over contiguous memory. Unlike array slices (which create new arrays with copied data), `Span<T>` is a view into existing memory: changes through the span affect the original data. It supports multiple memory types: managed arrays, native memory, and stack-allocated memory (`stackalloc`). The tradeoff is that `Span<T>` is stack-only, so it cannot be a class field, used in async methods, or boxed. For heap-safe scenarios, use `Memory<T>` instead. `ReadOnlySpan<T>` provides a read-only view for scenarios like parsing strings without allocations — crucial for performance-sensitive code like JSON parsing or string manipulation in tight loops.

**Code Example:**
```csharp
int[] numbers = { 0, 1, 2, 3, 4, 5 };
Span<int> slice = numbers.AsSpan(1, 3);  // { 1, 2, 3 } - no allocation
slice[0] = 99;                           // modifies original array
Console.WriteLine(numbers[1]);           // 99

// String slicing without allocation - ReadOnlySpan<char>
ReadOnlySpan<char> email = "user@example.com".AsSpan();
var atIndex = email.IndexOf('@');
var domain = email[(atIndex + 1)..];     // "example.com" - no new string allocation
```

**vs array slice:** `Span<T>` is allocation-free and supports more memory types (stack, native, managed).

---

**Q43: How does `yield return` work internally?**

**Answer:**

**Theory:** When the compiler encounters `yield return`, it transforms the method into a state machine class that implements both `IEnumerable<T>` and `IEnumerator<T>`. This class has: (1) a `_state` field tracking the current position, (2) a `_current` field holding the most recently yielded value, (3) a `MoveNext()` method using `switch(state)` to resume at the correct yield point, and (4) a `Current` property returning `_current`. This is lazy evaluation — no values are produced until the caller iterates. When the caller calls `MoveNext()`, execution runs from the last yield point to the next `yield return` or the end of the method. This makes `yield` ideal for infinite sequences, deferred computations, and scenarios where the caller might short-circuit (`.Take(5)` stops iteration early — no more values are produced, avoiding wasted computation).

**Code Example:**
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

**Theory:** Strings in C# are immutable — every `+` or `+=` concatenation creates a NEW string on the heap, copying the entire old content plus the new content, then discarding the old string for garbage collection. In a loop with 10,000 iterations, this creates tens of thousands of intermediate allocations (megabytes of garbage) and O(n²) copy operations. `StringBuilder` maintains a mutable internal buffer (a `char[]` array that grows geometrically, typically doubling when full). `Append()` writes directly into the buffer, reallocating only occasionally. At the end, `ToString()` creates a single string copy of the final result — just one allocation. The performance difference grows linearly with the number of concatenations. For a few concatenations (2-5), `+` is fine. For loops, dynamic building, or any non-trivial string construction, `StringBuilder` is significantly faster and more memory-efficient.

**Code Example:**
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

**Theory:** Variance controls whether a generic type can be substituted with a more or less derived type argument while maintaining type safety. **Covariance** (`out T`) means "I only PRODUCE T" — `IEnumerable<string>` can be used as `IEnumerable<object>` because you only read strings (which are always objects). **Contravariance** (`in T`) means "I only CONSUME T" — `Action<object>` can be used as `Action<string>` because an action that handles any object can certainly handle a string. **Invariance** (no annotation) means "I both produce AND consume T" — `List<T>` is invariant because you can add AND read items, so substituting types would break type safety (you can't add `int` to a `List<object>` if it was really a `List<string>`). The `in` and `out` keywords let the compiler verify that variance is safe at compile time: `out` types must only appear in return positions, `in` types only in parameter positions.

**Code Example:**
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

**Theory:** Database seeding populates the database with initial or demo data so the app works immediately without manual setup. The `DbInitializer.SeedData()` pattern uses the DI container (`CreateScope()`) to get a fresh `AppDbContext` instance at startup because the DbContext is typically registered as scoped (one per request) — but at startup there's no request scope, so a manual scope is created. The seeding runs in `Program.cs` before the app starts accepting requests, ensuring the database is ready. This is the standard EF Core seeding pattern for development/demo environments. In production, you'd typically use EF Core migrations with seed data in the `HasData()` method of `OnModelCreating`, which is more reliable because it's transactional and versioned with the schema.

**Code Example:**
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

**Theory:** Code-First migrations let you define your database schema entirely in C# code. You write entity classes (`User`, `Battery`, `SwapRequest`) and configure relationships via Fluent API or data annotations, then EF Core generates the SQL to create or update the database schema. The `migrations add` command compares your current model (entity classes) with the previous migration snapshot and generates an `Up()` method (what to change) and a `Down()` method (how to revert). This makes schema changes **version-controlled**, **repeatable**, and **reviewable** — the migration file is C# code that goes through the same PR review as any other code change. The `database update` command applies pending migrations to the actual database. In CI/CD, `migrations bundle` creates a standalone executable to apply migrations during deployment without requiring the EF CLI.

**Code Example:**
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

**Theory:** By default, EF Core uses **lazy loading** — navigation properties are null until accessed. `Include()` performs **eager loading**: it generates SQL JOINs to load related data in a single round-trip. `ThenInclude()` chains onto an `Include()` to load nested relationships. Without eager loading, accessing `station.Batteries[0].SwapHistories` would make three separate database queries (station, then batteries, then swap histories) — the N+1 query problem. With `Include().ThenInclude()`, all three levels are loaded in a single SQL query with JOINs. However, overusing `Include()` can cause massive JOINs returning duplicate data — use it only for the relationships you actually need, and consider `AsSplitQuery()` for deep includes to split into multiple efficient queries.

**Code Example:**
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

**Theory:** Both return the first matching element or null, but they have different **expectations about cardinality**. `FirstOrDefault()` expects 0 or more results and returns the first one it finds — it's efficient because it adds `TOP 1` to the SQL query and stops after finding one match. `SingleOrDefault()` expects **exactly 0 or 1** result — it must read all matching rows to verify there's at most one. If more than one matches, it throws `InvalidOperationException`. Use `FirstOrDefault` when querying by non-unique criteria (e.g., "first active user"), and `SingleOrDefault` when querying by a unique key (e.g., `Where(u => u.Id == id)`). The wrong choice can either hide data integrity issues (`FirstOrDefault` on a non-unique field that should be unique) or throw an unnecessary exception (`SingleOrDefault` on a field that can legitimately have multiple matches).

**Code Example:**
```csharp
var user = db.Users.FirstOrDefault(u => u.Id == 1);     // OK - one match
var user = db.Users.SingleOrDefault(u => u.Role == "Admin"); // ❌ might throw if multiple admins
```

Use `FirstOrDefault` when you expect 0 or 1+ results. Use `SingleOrDefault` when you expect exactly 0 or 1.

---

**Q50: How does EF Core change tracking work?**

**Answer:**

**Theory:** Change tracking is EF Core's mechanism for detecting what changed between a query and `SaveChangesAsync()`. When you query entities with tracking enabled (default), EF Core creates a **snapshot** of each entity's property values. It also creates a **proxy** (if configured) or uses the original values stored internally. When `SaveChangesAsync()` is called, EF Core compares each tracked entity's current values against the snapshot to determine which entities were added, modified, or deleted. It then generates the appropriate INSERT, UPDATE, or DELETE SQL statements. The overhead of this comparison grows with the number of tracked entities — for read-only operations that don't need to be saved, `AsNoTracking()` **disables** change tracking entirely, eliminating the snapshot storage and comparison overhead. This can significantly improve performance for large read-only queries.

**Code Example:**
```csharp
var user = db.Users.Find(1);  // tracked - snapshot taken
user.Email = "new@email.com";  // EF detects modification
await db.SaveChangesAsync();   // generates UPDATE SQL

// Read-only query - no tracking needed
var users = db.Users.AsNoTracking().ToList();  // no tracking overhead
```

---

**Q51: What is the difference between SQL Server and PostgreSQL with EF Core?**

**Answer:**

**Theory:** EF Core uses a **provider model** — each database has a separate NuGet package that translates EF Core's LINQ expressions into the database's SQL dialect. SQL Server and PostgreSQL differ in features that affect schema design and query behavior: PostgreSQL has native support for arrays and superior JSON handling (you can query JSON fields directly in LINQ), while SQL Server requires more work for both. Case sensitivity matters at the query level — SQL Server is case-insensitive by default (`WHERE Name = 'john'` matches `'John'`), while PostgreSQL is case-sensitive (use `ILike()` for case-insensitive matching). Schema naming (`dbo` vs `public`) affects migration scripts. The provider abstraction means you can theoretically switch databases by changing the NuGet package and connection string, but in practice, database-specific features (like PostgreSQL's `jsonb` or SQL Server's `SCOPE_IDENTITY()`) require code changes.

**Code Example:**
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

**Theory:** Handling large datasets requires shifting from **loading everything in memory** to **processing in chunks on the database server**. The five strategies address different bottlenecks: **indexing** speeds up WHERE/ORDER BY by providing the database with a sorted lookup structure (without an index on `CreatedAt`, sorting 1M records requires a full table scan). **Pagination** prevents loading all 1M records at once — you load only the 20 records the user sees. **Projection** (`Select()`) reduces the data transferred from database to app — instead of loading entire entities with all columns, you retrieve only the columns needed. **Batch processing** avoids holding a large result set in memory — process 1000 records, then the next 1000. **AsNoTracking()** eliminates EF Core's change-tracking overhead for read-only queries (see Q50). Combined, these techniques enable sub-second queries on million-row tables.

**Code Example:**
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

**Theory:** A migration bundle is a self-contained executable that applies EF Core migrations to a database. It's the recommended approach for CI/CD deployments because it eliminates the need to install the EF Core CLI tool (`dotnet ef`) on the deployment server. The bundle contains all the migration logic compiled into a single executable — just copy it to the target machine and run it. This is particularly useful for Windows admin environments where developers might not have permission to install .NET SDK tools. The bundle also supports parameters like `--connection` to override the target database at runtime, making it safe to promote the same bundle through dev → staging → production environments with different connection strings.

**Code Example:**
```bash
dotnet ef migrations bundle --output deploy-migrations.exe
# Deploy and run (can specify connection string at runtime):
./deploy-migrations.exe --connection "Server=prod-db;Database=EVSwap;..."
```

---

**Q54: How does EF Core map C# `enum` types to the database?**

**Answer:**

**Theory:** Enums are natively supported by EF Core through **value conversions**. By default, EF Core stores enums as integers — `BatteryStatus.Available` (value 0) becomes `0` in the database, `InUse` (1) becomes `1`, etc. This is efficient but not human-readable. To store enum names as strings (e.g., `'Available'`, `'InUse'`), use `HasConversion<string>()` — the tradeoff is slightly more storage and slower queries (string comparison vs integer), but the data is self-documenting. A third option is to use a custom `ValueConverter<TModel, TProvider>` for scenarios like storing bit flags or mapping to legacy values. The conversion only affects how the data is stored — the C# code always works with the enum type, making the mapping transparent.

**Code Example:**
```csharp
public enum BatteryStatus { Available, InUse, Maintenance, Disposed }

public class Battery
{
    public BatteryStatus Status { get; set; }
}
// Database column: Status INT (0, 1, 2, 3)

// To store as human-readable strings:
builder.Property(b => b.Status).HasConversion<string>();
// Database column: Status NVARCHAR ("Available", "InUse", etc.)
```

---

**Q55: What is the `IQueryable<T>` vs `IEnumerable<T>` difference?**

**Answer:**

**Theory:** The fundamental difference is **where** the query executes. `IQueryable<T>` represents a query that will be translated by EF Core into SQL and executed on the database server. Operations like `.Where()`, `.Select()`, and `.OrderBy()` on `IQueryable` add clauses to the SQL query — the filtering happens in the database, so only matching rows are transferred to the client. `IEnumerable<T>` represents an in-memory collection — once you call `.ToList()` (or any method that materializes the query), all data is loaded into memory, and subsequent `.Where()` calls use LINQ-to-Objects (client-side filtering). The practical impact: `IQueryable` is efficient for large datasets because the database does the heavy lifting; `IEnumerable` loads everything and filters in memory, which can be dramatically slower and more memory-intensive. Always keep queries as `IQueryable` for as long as possible, and only materialize (`ToList`, `FirstOrDefault`, `Count`) when you actually need the data.

**Code Example:**
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
JWT (JSON Web Token) is a **stateless** authentication protocol — the server does NOT store session data. All identity information is encoded in the token itself.

**Structure:** `header.payload.signature` (three base64url-encoded segments separated by dots):
1. **Header** — algorithm & token type (`{"alg":"HS256","typ":"JWT"}`)
2. **Payload** — claims (user ID, role, expiration, etc.) — **not encrypted**, just base64-encoded
3. **Signature** — HMAC-hash of header + payload using a server secret — **this is what prevents tampering**

**Why stateless matters:** The API server can validate the token's signature using its private secret without hitting a database. Every request is self-contained — ideal for scaling horizontally across multiple server instances.

**Flow:**
1. Client sends credentials → server verifies → returns signed JWT.
2. Client sends JWT in `Authorization: Bearer <token>` header with every request.
3. Server middleware (`app.UseAuthentication()`) decodes and validates the signature on each request.
4. If valid, the identity is set on `HttpContext.User` — the `[Authorize]` attribute gates on this.

```csharp
// API validates the JWT signature on every request
[Authorize]  // ← this attribute checks if HttpContext.User.Identity.IsAuthenticated
public class SwapController : ControllerBase
```

**Why use both access + refresh tokens?** Access tokens (15-60 min) limit damage if leaked. Refresh tokens (days) sit in `SecureStorage` and are exchanged for new access tokens without re-login. The refresh token can be revoked server-side if compromised. The access token cannot be revoked (it's stateless) — hence the short expiry.

---

**Q59: How would you intercept all HTTP requests to add logging?**

**Answer:**

**Theory:** `DelegatingHandler` implements the **Chain of Responsibility** pattern — each handler in the pipeline can inspect/modify the request before passing it to the next handler, and inspect/modify the response on the way back. You stack handlers to compose cross-cutting concerns: logging, authentication, retry, compression. By overriding `SendAsync()`, you wrap every HTTP call that goes through that `HttpClient`. This is superior to adding logging in every ViewModel or service method because it's a single point of instrumentation — if you add a new API call, logging is automatic. The handler is registered as the `InnerHandler` chain: `LoggingHandler → AuthHandler → HttpClientHandler → Network`.

**Code Example:**
```csharp
public class LoggingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        Debug.WriteLine($"→ {request.Method} {request.RequestUri}");
        var stopwatch = Stopwatch.StartNew();
        var response = await base.SendAsync(request, ct);
        stopwatch.Stop();
        Debug.WriteLine($"← {(int)response.StatusCode} in {stopwatch.ElapsedMilliseconds}ms");
        return response;
    }
}

// Register
_httpClient = new HttpClient(new LoggingHandler { InnerHandler = new HttpClientHandler() });
```

---

**Q60: What is the `HttpClient` lifetime in a MAUI app?**

**Answer:**

**Theory:** `HttpClient` is designed as a **long-lived, reusable** object — each instance manages its own connection pool. Creating a new `HttpClient` per request exhausts TCP ports because each instance opens new connections that linger in `TIME_WAIT` state after disposal. This is called **socket exhaustion** — the OS runs out of ephemeral ports (typically 16,384 on Windows). In MAUI, the fix is simple: register `HttpClient` as a Singleton so one instance and one connection pool serve the entire app lifetime. `IHttpClientFactory` (standard in ASP.NET Core) solves this by pooling `HttpClient` instances and rotating `HttpMessageHandler` instances to handle DNS changes. In MAUI, it's generally unnecessary because mobile/desktop apps talk to a single API server and don't face the same scale of connection churn as a web server.

**Code Example:**
```csharp
builder.Services.AddSingleton<HttpClient>(_ =>
    new HttpClient { BaseAddress = new Uri("http://localhost:5238") });
```

`IHttpClientFactory` (common in ASP.NET Core) is not needed in MAUI since there's typically one API server.

---

**Q61: How does `PostAsJsonAsync<T>` serialize your request object?**

**Answer:**

**Theory:** `PostAsJsonAsync` is an extension method (in `System.Net.Http.Json`) that serializes the request object to JSON using `System.Text.Json` with `JsonSerializerDefaults.Web` — a predefined profile that uses **camelCase naming** (property `UserName` → JSON `"userName"`), **case-insensitive deserialization**, and **omits null values by default**. This matches the convention used by most REST APIs. The method sets `Content-Type: application/json` and writes the serialized JSON as `StringContent`. Behind the scenes, it calls `JsonSerializer.SerializeAsync()` with the Web defaults. If your API expects PascalCase or snake_case, you'd need to use `PostAsync()` with custom serialization and a custom `JsonSerializerOptions`.

**Code Example:**
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

**Theory:** `EnsureSuccessStatusCode()` is a convenience method on `HttpResponseMessage` that **throws `HttpRequestException`** if the status code is not in the 2xx range. It's a guard clause pattern — assert the response is successful, and if not, fail fast with a clear exception. The exception includes the numeric status code and reason phrase (e.g., "401 (Unauthorized)"). However, it's often better to use a custom handler like `HandleResponse<T>` (see Q5) because `EnsureSuccessStatusCode()` doesn't include the response body in the exception — the body often contains valuable error details from the API. Our `HandleResponse<T>` reads the body and includes it in the exception message, making debugging easier.

**Code Example:**
```csharp
response.EnsureSuccessStatusCode();
// Throws HttpRequestException if status code is NOT 2xx
// Exception includes status code: "Response status code does not indicate success: 401 (Unauthorized)."
```

---

**Q63: How would you implement request retry with exponential backoff?**

**Answer:**

**Theory:** **Exponential backoff** prevents retry storms — if a server is overloaded and all clients retry simultaneously, the problem compounds. By doubling the delay with each attempt (2s, 4s, 8s), clients naturally stagger their retries. The **Polly** library provides a fluent API for this: define the exception types to handle (`Handle<HttpRequestException>()`), the number of retries, and the delay formula. The `WaitAndRetryAsync` policy wraps the HTTP call and automatically retries on failure. Additional Polly features include circuit breaker (stop retrying when the server is clearly down), fallback (return a cached/default response), and timeout policies. For simpler scenarios without Polly, you can implement the same logic with a `for` loop and `Task.Delay()`.

**Code Example:**
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

**Theory:** `MultipartFormDataContent` implements the **multipart/form-data** MIME type — the standard format for sending files and structured data in a single HTTP request. The content is split into multiple parts, each with its own headers (Content-Disposition, Content-Type). This is the same format that HTML forms use when `<form enctype="multipart/form-data">` is specified. Each part can have a name (the form field name), a filename, and its own content type. This contrasts with `application/json` (single JSON body) or `application/x-www-form-urlencoded` (key=value pairs). Use multipart whenever you need to upload files along with metadata.

**Code Example:**
```csharp
var content = new MultipartFormDataContent();
var imageBytes = await File.ReadAllBytesAsync("photo.jpg");
content.Add(new ByteArrayContent(imageBytes), "file", "photo.jpg");
await _httpClient.PostAsync("/api/upload", content);
```

---

**Q65: How do you handle file uploads in MAUI?**

**Answer:**

**Theory:** File upload in MAUI combines two concerns: (1) **file picking** — letting the user select a file from the device via `FilePicker`, which requires platform-specific file type declarations (MIME types on Android, UTType identifiers on iOS, file extensions on Windows), and (2) **HTTP upload** — sending the selected file as `MultipartFormDataContent`. The `FilePicker.PickAsync()` returns a `FileResult` with the file name and a stream. You read the stream and include it in the multipart content. Platform permissions are required — `StorageRead` permission on Android and appropriate entitlements on iOS. The `PostMultipartAsync` method in `ApiService` wraps this pattern.

**Code Example:**
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
MVVM (Model-View-ViewModel) separates UI from business logic into three layers connected by **data binding**:

```
View (XAML) ←→ ViewModel (logic) ←→ Model (data)
   binds to        orchestrates          business objects
```

- **Model:** Data classes (UserModel, StationModel, AuthResponse) — pure data, no behavior
- **ViewModel:** Observable properties + commands (LoginViewModel, DashboardViewModel) — contains all presentation logic, holds no reference to the View
- **View:** XAML pages with data bindings (LoginPage.xaml) — purely declarative UI, no code-behind logic

**Why MVVM (the history):** Before MVVM, WinForms and WPF apps put logic in code-behind (`button_Click` methods), making UI automation and unit testing nearly impossible. MVVM inverts this — the ViewModel is a plain C# class with **zero dependency on the UI framework**, testable with a simple `new LoginViewModel(mockService)` call.

**How it differs from MVC (your web background):**
- **MVC:** Controller receives user action → updates Model → passes data to View (View renders from Model data). Controller is the entry point.
- **MVVM:** View binds to ViewModel properties. User interacts with View → binding updates ViewModel → ViewModel calls services → `INotifyPropertyChanged` pushes changes back to View. The ViewModel is the entry point, but the View declares the bindings declaratively.

**Flow:** View binds to ViewModel properties → user types in a text box → two-way binding updates ViewModel property → user clicks button → `[RelayCommand]` fires → ViewModel calls service → on completion, updates observable property → binding pushes new value to View.

---

**Q67: What does `[ObservableProperty]` generate behind the scenes?**

**Answer:**

**Theory:** C# **source generators** (introduced in .NET 5 / C# 9) run at compile time, analyzing attributes in your code and emitting additional source files that are compiled alongside yours. `[ObservableProperty]` is one such generator from the CommunityToolkit.Mvvm package. When you write `[ObservableProperty] private string _userName`, the generator: (1) identifies the field and its naming convention (`_userName` → `UserName`), (2) generates a public property with `INotifyPropertyChanged` notification, (3) creates a `partial void OnUserNameChanged(string value)` method that you can optionally implement for side effects. This eliminates ~30 lines of boilerplate per property while producing exactly the code you'd write manually. The generated code is viewable in the IDE (go to the `_userName` reference and navigate to the generated file under `Dependencies → Analyzers → CommunityToolkit.Mvvm.SourceGenerators`).

**Code Example:**
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
            OnUserNameChanged(value);  // partial method hook
        }
    }
}
// Plus the partial method signature:
partial void OnUserNameChanged(string value);
// You can implement this to react to changes:
partial void OnUserNameChanged(string value) => ValidateUserName(value);
```

---

**Q68: What is the difference between `[RelayCommand]` and `ICommand`?**

**Answer:**

**Theory:** `ICommand` is the interface that MAUI's binding engine uses to connect UI buttons to ViewModel methods. It has three members: `Execute(object?)` (runs the command), `CanExecute(object?)` (returns whether the command can run), and `CanExecuteChanged` (event the UI subscribes to for enabling/disabling). Manually implementing `ICommand` requires creating a class (or `RelayCommand` instance) per command — about 10+ lines each. `[RelayCommand]` is a source generator attribute that generates the `ICommand` property automatically from a method — `private async Task LoginAsync()` becomes `public ICommand LoginCommand => new AsyncRelayCommand(LoginAsync)`. It also supports `CanExecute` via `[RelayCommand(CanExecute = nameof(CanLogin))]` and async commands via `Task`-returning methods. This eliminates the repetitive `ICommand` boilerplate while keeping the binding contract intact.

**Code Example:**
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
`INotifyPropertyChanged` implements the **Observer pattern** — the ViewModel (subject) notifies the View (observer) when property values change. This is the core mechanism that makes data binding reactive.

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

**What happens in the binding engine:**
1. XAML binding `{Binding Name}` creates a `BindingExpression` that subscribes to the source's `PropertyChanged` event.
2. When the setter fires `PropertyChanged`, the binding receives the event with `"Name"` as the property name.
3. The binding re-reads `Name` from the source and updates the target UI element's property.
4. If `Mode=TwoWay`, changes from the UI (e.g., user typing) flow back to the source property.

**Why this matters:** Without `INotifyPropertyChanged`, the View would show stale data. The ViewModel is a plain C# object — there's no "push" mechanism besides this event. With `[ObservableProperty]` (CommunityToolkit.MVVM), the source generator produces the boilerplate above automatically.

---

**Q70: What is `ObservableCollection<T>` and when should you use it?**

**Answer:**

**Theory:** `ObservableCollection<T>` implements `INotifyCollectionChanged` — it fires a `CollectionChanged` event whenever items are added, removed, replaced, moved, or the entire list is cleared. MAUI's `CollectionView` and `ListView` bind to this event to automatically update the UI when the collection changes. This is essential for dynamic lists — think live search results, real-time swap updates, or chat messages. The alternative is replacing the entire `ItemsSource` with a new `List<T>` which forces the entire list to re-render. However, `ObservableCollection` has overhead: each change notification triggers a UI update, so bulk operations (adding 1000 items one by one) cause 1000 UI updates. For bulk adds, call `AddRange()` (from CommunityToolkit.Mvvm) or add items to a backing `List<T>` first, then clear and re-add to the `ObservableCollection` in one operation.

**Code Example:**
```csharp
// DO: use ObservableCollection for dynamic lists
[ObservableProperty]
ObservableCollection<StationModel> _stations = new();
stations.Add(newStation);  // UI updates automatically

// DON'T: use ObservableCollection for read-only lists
// List<T> is fine if you set the collection once and never modify it
```

---

**Q71: How does two-way binding work for `Entry` fields?**

**Answer:**

**Theory:** Two-way binding synchronizes data in **both directions** between the View and ViewModel. When the ViewModel property changes, `PropertyChanged` pushes the new value to the UI. When the user types in the `Entry`, the binding engine intercepts the platform's text-changed event and writes the new value back to the ViewModel property. By default in MAUI, the binding updates on **every keystroke** (equivalent to `UpdateSourceTrigger.PropertyChanged`). This enables immediate validation feedback but can be expensive for performance-critical forms. You can change this to focus-loss-only (`UpdateSourceTrigger.LostFocus`) for scenarios like auto-saving on field exit. The `Mode=TwoWay` is actually the default for `Entry.Text`, so you can omit it — but being explicit improves readability.

**Code Example:**
```xml
<Entry Text="{Binding Username, Mode=TwoWay}" />
```

Two-way binding means:
- **ViewModel → View:** When `Username` changes, `Entry.Text` updates.
- **View → ViewModel:** When user types, `Username` property updates (on each keystroke by default).

---

**Q72: What is `QueryProperty` and how is it used?**

**Answer:**

**Theory:** `QueryProperty` is Shell's mechanism for **passing navigation parameters** to a target ViewModel. When you navigate with a dictionary of parameters, Shell resolves the target page, sets its `BindingContext` (the ViewModel), and then sets any properties decorated with `[QueryProperty]`. The attribute takes two arguments: the property name and the dictionary key. This happens before `OnAppearing`, so the ViewModel can use the parameter immediately when loading data. The limitation is that `QueryProperty` only supports simple types and serializable objects — for complex objects, pass them directly in the dictionary (which Shell forwards to the target page by reference). Multiple parameters require multiple `[QueryProperty]` attributes.

**Code Example:**
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

**Theory:** `DisplayAlert()` is a method on `Page` that shows a native modal dialog. Since ViewModels should not have references to View objects (that would break MVVM), `ShowAlertAsync` uses `Shell.Current.CurrentPage` to access the currently visible page at runtime without needing a cached reference. `Shell.Current` is a static property that always returns the app's Shell instance. This is a pragmatic compromise — it's better than passing a `Page` reference through the constructor (which couples the ViewModel to a specific page), but it's not pure MVVM (it still accesses a UI element). In a stricter MVVM implementation, you'd inject an `IDialogService` interface that abstracts the alert, making it mockable in tests.

**Code Example:**
```csharp
protected async Task ShowAlertAsync(string title, string message, string cancel = "OK")
{
    if (Shell.Current?.CurrentPage is not null)
        await Shell.Current.CurrentPage.DisplayAlert(title, message, cancel);
}
```

It accesses `Shell.Current.CurrentPage` (the currently visible page) and calls `DisplayAlert` on it.

---

**Q74: What is the role of `IValueConverter`?**

**Answer:**

**Theory:** `IValueConverter` transforms binding values when the ViewModel type doesn't match the UI property type (e.g., `bool` → `Color`, `DateTime` → `string`, `enum` → `bool`). The `Convert` method runs when data flows from ViewModel to View; `ConvertBack` runs for two-way bindings when data flows from View to ViewModel. Converters are registered as static resources and referenced by `Converter={StaticResource Name}`. Common converters include: `BoolToVisibility`, `InverseBool`, `DateTimeFormat`, `EnumToString`, and `AmountToCurrency`. If your converter is used widely, register it in `Styles.xaml` or `App.xaml`. If used only in one page, define it in the page's resources. For simple conversions, consider using `x:DataType` + computed properties in the ViewModel instead.

**Code Example:**
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

**Theory:** A button's `IsEnabled` can be bound directly to a boolean ViewModel property, but the better approach uses the command's `CanExecute`. When you bind `Command` directly, the command's `CanExecute` method controls both the command execution and the button's visual state (disabled/grayed). The `[RelayCommand(CanExecute = nameof(CanSubmit))]` attribute tells the generated command to call `CanSubmit` before executing — if it returns false, the command won't run AND the button auto-disables. Binding `IsEnabled` separately works but duplicates the logic (you must keep `CanSubmit` and the command's `CanExecute` in sync). The `CanExecute` approach is cleaner because the command is the single source of truth for whether it can run.

**Code Example:**
```xml
<Button Text="Submit" Command="{Binding SubmitCommand}"
        IsEnabled="{Binding CanSubmit}" />
```

ViewModel:
```csharp
[ObservableProperty] bool _canSubmit;

// Better: use CanExecute on the command:
[RelayCommand(CanExecute = nameof(CanSubmit))]
async Task SubmitAsync() { ... }
// The button auto-disables when CanSubmit is false
```

---

**Q76: Explain `x:Bind` vs `Binding` in MAUI.**

**Answer:**

**Theory:** `Binding` (no prefix) uses **runtime reflection** — the binding expression `{Binding Name}` is parsed and resolved when the page loads. If `Name` doesn't exist on the ViewModel, the binding silently fails (the property just doesn't update). `x:Bind` uses **compile-time code generation** — the XAML compiler generates strongly-typed code that accesses ViewModel properties directly. The compiler validates that `Name` exists on the type specified by `x:DataType`, turning typos into build errors. Performance-wise, `x:Bind` is significantly faster because there's no reflection overhead at runtime — the generated code directly sets the target property. However, `x:Bind` requires the binding source type to be known at compile time (`x:DataType`), making it unsuitable for dynamic scenarios like `DataTemplate` items of unknown types. The recommendation: use `x:Bind` (compiled bindings) as default, fall back to `Binding` for dynamic scenarios.

**Code Example:**
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

**Theory:** `x:Load` enables **deferred XAML loading** — elements marked with `x:Load` are NOT created when the page initializes; they're created only when the bound condition becomes true. This reduces the initial visual tree size, which directly improves startup time and memory usage. Common use cases: conditionally loaded admin panels, advanced settings sections, or any UI that's hidden by default. Without `x:Load`, all elements are created even if hidden by `IsVisible` — they just don't render, but they still consume memory and CPU for layout. `x:Load` completely removes them from the visual tree until needed. The tradeoff is a slight latency when the element loads for the first time (the XAML must be parsed and the control tree created).

**Code Example:**
```xml
<AdminPanel x:Load="{Binding IsAdmin}" />  <!-- only loads if user is admin -->
```

Saves memory and startup time by not creating the control tree until the condition is met.

---

**Q78: How does the `CommunityToolkit.Mvvm` source generator work?**

**Answer:**

**Theory:** Roslyn source generators are C# components that hook into the compilation pipeline. At compile time, the generator receives a **compilation object** (containing all syntax trees and semantic models), analyzes your code for specific attributes (`[ObservableProperty]`, `[RelayCommand]`), and emits additional C# source files. These generated files are compiled into the same assembly as your code — no runtime reflection is needed because everything is determined at compile time. The generator creates `INotifyPropertyChanged` implementations for `[ObservableProperty]` fields, `ICommand` properties for `[RelayCommand]` methods, and `ObservableObject` base class implementations. This eliminates hundreds of lines of boilerplate while producing exactly the same IL as handwritten code. The key advantage over reflection-based approaches (like `PropertyChanged.Fody`) is AOT compatibility — no runtime weaving or IL manipulation.

**Code Example:**
```csharp
// The generator finds this at compile time:
[ObservableProperty]
private string _userName = string.Empty;
[RelayCommand]
async Task LoginAsync() { ... }

// And generates the INotifyPropertyChanged + ICommand boilerplate
// View the generated code: Dependencies → Analyzers → CommunityToolkit.Mvvm.SourceGenerators
```

---

**Q79: What are `partial` methods and how does `OnIsBiometricEnabledChanged` get called?**

**Answer:**

**Theory:** C# partial methods allow a method declaration in one part of a partial class to be optionally implemented in another part. If the implementation is not provided, the compiler removes all calls to the method — zero overhead. The CommunityToolkit source generator uses this pattern: it generates a `partial void OnPropertyNameChanged(Type value)` declaration and calls it in the property setter. You can optionally implement this method to react to property changes (persist to storage, validate, trigger other updates). If you don't implement it, the compiler removes the call entirely. This is more efficient than using events or virtual methods because there's no runtime dispatch when the method isn't implemented.

**Code Example:**
```csharp
// Source generator creates a partial method signature:
partial void OnIsBiometricEnabledChanged(bool value);

// And calls it inside the property setter:
set
{
    if (_isBiometricEnabled != value)
    {
        _isBiometricEnabled = value;
        OnPropertyChanged();
        OnIsBiometricEnabledChanged(value);  // ← called here
    }
}

// You implement it to react to property changes:
partial void OnIsBiometricEnabledChanged(bool value)
{
    _storage.SaveAsync("biometric_enabled", value.ToString()).FireAndForget();
}
// If you don't implement this, the compiler removes the call entirely
```

---

**Q80: How would you implement master-detail navigation with Shell?**

**Answer:**

**Theory:** Master-detail navigation shows a **master list** (stations, swaps) and a **detail view** for the selected item. Shell supports this through `FlyoutItem` (the master/menu shown as a side panel or hamburger menu) and nested `ShellContent` (detail pages shown in the main area). The `FlyoutItem` contains the master pages that appear in the navigation menu. Detail pages like `StationDetailPage` are registered as separate `ShellContent` with `FlyoutItemIsVisible="False"` so they don't appear in the menu — they're navigated to programmatically via `GoToAsync`. This separation keeps the navigation clear: the flyout shows top-level sections, and detail pages are transient navigation targets. Shell handles the platform-specific master presentation (Android drawer vs iOS tab bar vs Windows navigation panel) automatically.

**Code Example:**
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

Navigate from StationsPage to detail: `await Shell.Current.GoToAsync("stationdetail", params)`

---

## 7. Dependency Injection

**Q81: What is dependency injection and why is it used?**

**Answer:**
Dependency Injection (DI) is an application of the **Inversion of Control (IoC)** principle: instead of a class creating its own dependencies, dependencies are **provided from the outside** (typically via constructor parameters by a DI container).

```csharp
// Without DI - tight coupling
public class LoginViewModel
{
    private readonly AuthService _auth = new AuthService();  // hard-coded to AuthService
}

// With DI - loose coupling
public class LoginViewModel
{
    private readonly IAuthService _auth;
    public LoginViewModel(IAuthService auth) => _auth = auth;  // any implementation
}
```

**Why DI exists (the problem it solves):**
1. **Tight coupling:** The first example is bound to `AuthService` — you can't swap it for a mock in tests or a different implementation without modifying the class.
2. **Hard to test:** Unit-testing `LoginViewModel` requires a real `AuthService` with network calls — that's an integration test, not a unit test.
3. **Hidden dependencies:** The constructor signature `new LoginViewModel()` doesn't reveal its needs. With DI, all dependencies are explicit in the constructor.

**Three benefits of DI:**
- **Testability** — mock services can be injected in unit tests:
  `new LoginViewModel(Mock.Of<IAuthService>())`
- **Flexibility** — swap implementations without changing consumers (e.g., `FakeAuthService` for demos, `AuthService` for production)
- **Centralized lifetime management** — the DI container (`builder.Services` in `MauiProgram.cs`) controls whether a service is Singleton, Transient, or Scoped

---

**Q82: What is the difference between `AddSingleton`, `AddTransient`, and `AddScoped`?**

**Answer:**

**Theory:** Service lifetime controls **when** the container creates an instance and **how long** it survives. **Singleton** creates one instance for the entire application — every injection point receives the same object. This is ideal for stateless services (`HttpClient`, logging) and shared state (`AuthService.CurrentUser`), but dangerous if the service accidentally holds per-screen state that leaks across ViewModels. **Transient** creates a new instance for every injection — each ViewModel and page gets a fresh copy, preventing state leakage. However, creating many transient instances can increase GC pressure. **Scoped** creates one instance per DI scope — in ASP.NET Core this means one per HTTP request, but in MAUI there's no built-in per-request scope, so `AddScoped` behaves the same as `Singleton` unless you explicitly create scopes. Choose Singleton for stateless/shared services, Transient for stateful ViewModels, and avoid Scoped unless you have a specific scope pattern.

**Code Example:**
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

**Theory:** The DI container builds **dependency graphs** through constructor analysis. When asked for `LoginViewModel`, the container: (1) inspects its constructor to find all required parameters, (2) looks up each parameter's type in the registration registry, (3) recursively builds each dependency (if `AuthService` needs `IApiService`, it builds `ApiService` too), and (4) once the entire graph is resolved, creates `LoginViewModel` with all dependencies injected. If any dependency in the chain is missing (not registered), the container throws an `InvalidOperationException` at resolution time — not at registration time. This is one reason DI registration errors often surface at runtime when navigating to a page for the first time. Circular dependencies (A → B → A) will cause a `StackOverflowException` — avoid them by refactoring one of the services.

**Code Example:**
```csharp
// DI resolves LoginViewModel by:
// 1. Reading constructor: LoginViewModel(IAuthService, ISecureStorageService, INavigationService, IConnectivityService)
// 2. Looking up each interface:
//    - IAuthService → AuthService (singleton, already created or creates now)
//    - AuthService needs IApiService → ApiService (singleton)
//    - ApiService needs HttpClient → pre-configured singleton
// 3. Builds AuthService, then LoginViewModel with all 4 dependencies
```

---

**Q84: What happens if you register the same interface twice?**

**Answer:**

**Theory:** By default, the last registration wins — the container remembers only the most recent registration for each service type. This is by design: it lets you override default registrations (e.g., register `IApiService` as `ApiService` in production, then override with `MockApiService` for tests). If you need multiple implementations of the same interface (multiple logging providers, multiple configuration sources), inject `IEnumerable<T>` instead — the container automatically provides all registered implementations when the parameter type is `IEnumerable<IService>`. This works because the container tracks the full list even though it only returns the last one for single-resolution requests.

**Code Example:**
```csharp
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IAuthService, FakeAuthService>();  // overrides above

// For multiple implementations, inject IEnumerable<T>:
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IAuthService, FakeAuthService>();
var services = serviceProvider.GetServices<IAuthService>();  // returns both
```

---

**Q85: How would you register a service differently for Debug vs Release?**

**Answer:**

**Theory:** Conditional registration based on build configuration enables **testing without a real backend** during development. The `#if DEBUG` preprocessor directive is compile-time — `MockApiService` compiles only in Debug builds, and `ApiService` only in Release builds. This means the wrong implementation can never accidentally ship to production because the Release build literally doesn't contain the `MockApiService` code. The hosting environment approach (`IsDevelopment()`) is more flexible (controlled by runtime environment variable) but requires the environment to be correctly configured in production.

**Code Example:**
```csharp
#if DEBUG
    builder.Services.AddSingleton<IApiService, MockApiService>();
#else
    builder.Services.AddSingleton<IApiService, ApiService>();
#endif

// Or use hosting environment (runtime-checked, not compile-time):
if (builder.Environment.IsDevelopment())
    builder.Services.AddSingleton<IApiService, MockApiService>();
```

---

**Q86: What is the Service Locator anti-pattern?**

**Answer:**

**Theory:** Service Locator is an anti-pattern because it **hides dependencies**. A class using Service Locator (`App.ServiceLocator.Get<IAuthService>()`) can request any service at any point in its code — you can't know what it needs without reading every line. This breaks three DI benefits: (1) **constructor tells the story** — with proper DI, all dependencies are visible in the constructor signature; with Service Locator, they're scattered throughout methods. (2) **Testability** — you can't easily substitute services because they're resolved internally rather than injected. (3) **Container dependency** — the class is coupled to the service locator itself, making it hard to use outside the DI framework. The difference is subtle: calling `IServiceProvider.GetService()` in the composition root (factory) is fine; calling it in business code is the anti-pattern.

**Code Example:**
```csharp
// Anti-pattern - hides dependencies, hard to test
public void Login()
{
    var auth = App.ServiceLocator.GetService<IAuthService>();  // magic! What does this class need?
    auth.LoginAsync(...);
}

// Better - explicit constructor injection
public class LoginViewModel
{
    private readonly IAuthService _auth;
    public LoginViewModel(IAuthService auth) => _auth = auth;  // clear: needs IAuthService
}
```

---

**Q87: How does constructor injection work with MAUI Shell pages?**

**Answer:**

**Theory:** When Shell navigates to a route (e.g., `//stations`), it resolves the page through the DI container. Shell looks up the route registration (`ShellContent.ContentTemplate`), which specifies the page type. The DI container then resolves the page's constructor parameters — which typically include the ViewModel. The ViewModel in turn has its own constructor dependencies (services). The container resolves the entire graph and creates the page. Shell then sets the resolved `BindingContext` automatically. This means every page's ViewModel is **automatically provided by DI** — you never manually create a ViewModel or set `BindingContext` in code-behind. This is one of MAUI's biggest productivity gains over Xamarin.Forms.

**Code Example:**
```csharp
// Shell route resolves StationsPage
// DI sees StationsPage constructor needs StationViewModel
// DI creates StationViewModel (resolving IApiService, INavigationService, etc.)
// DI creates StationsPage with the ViewModel
// Shell automatically sets: stationsPage.BindingContext = stationViewModel
```

---

**Q88: What is `IServiceProvider` and how was it used before refactoring?**

**Answer:**

**Theory:** `IServiceProvider` is the **DI container interface** — it's the lowest-level way to resolve services (`GetRequiredService<T>()`). Using it directly in application code is essentially Service Locator (see Q86), but using it within the composition root or in infrastructure code is sometimes necessary. The old `ApiService` used it to lazily resolve `IAuthService` to break a **circular dependency**: `ApiService` needed `AuthService` for token refresh, and `AuthService` needed `ApiService` for the refresh token API call. The `IServiceProvider` pattern deferred the resolution so both could register without circular construction. The refactoring removed this by simplifying the token refresh flow — the circular dependency was eliminated rather than worked around.

**Code Example:**
```csharp
// Before refactoring - IServiceProvider to break circular dependency
public class ApiService
{
    private readonly IServiceProvider _serviceProvider;
    private IAuthService? _authService;
    private IAuthService AuthService =>
        _authService ??= _serviceProvider.GetRequiredService<IAuthService>();
}

// After refactoring - no circular dependency, no IServiceProvider needed
public class ApiService
{
    // AuthService no longer needs ApiService for refresh
}
```

---

**Q89: How would you inject configuration like URLs into a service?**

**Answer:**

**Theory:** The **Options pattern** (`IOptions<T>`) binds configuration values to strongly-typed classes, providing compile-time safety for configuration. Unlike a hard-coded constant (`Constants.ApiBaseUrl`), `IOptions<T>` can be reloaded at runtime (`IOptionsSnapshot<T>`), validated on startup, and configured from multiple sources (appsettings.json, environment variables, user secrets). The pattern has three parts: (1) a POCO options class, (2) registration with `Configure<T>()`, and (3) injection via `IOptions<T>`. The actual values come from configuration providers — in MAUI, typically `appsettings.json` — though you can also set them inline as shown below. `IOptions<T>` is a Singleton (snapshot of config at app start), `IOptionsSnapshot<T>` is Scoped (reloads per scope), and `IOptionsMonitor<T>` is Singleton with change notification.

**Code Example:**
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

**Theory:** The **composition root** is the single location in the application where the dependency graph is constructed — in MAUI, this is `MauiProgram.cs`. By the time `builder.Build()` is called, every service, ViewModel, and page must be registered. After `Build()`, the container is effectively **sealed** — you shouldn't add or modify registrations. This design follows the **Composition Root pattern**: the application's entry point is the only place that knows about ALL dependencies and their lifetimes. This prevents service registration from leaking into ViewModels, pages, or service constructors. A well-organized composition root groups registrations by category (services, ViewModels, pages, platform-specific) and uses extension methods (`builder.Services.AddAppServices()`) to keep `MauiProgram.cs` readable.

**Code Example:**
```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    // ALL registrations happen here - this is the composition root
    builder.Services.AddSingleton<IApiService, ApiService>();
    builder.Services.AddTransient<LoginViewModel>();
    builder.Services.AddTransient<LoginPage>();
    // ... all other registrations
    return builder.Build();  // Container is sealed after Build()
}
```

The composition root should be the only place where you configure DI. Avoid registering services in other parts of the code.

---

## 8. Testing & Debugging

**Q91: How would you unit test `LoginViewModel.LoginAsync()`?**

**Answer:**

**Theory:** DI enables unit testing by letting you replace real services with **mocks**. LoginViewModel receives `IAuthService` through its constructor — in production this is `AuthService` (makes real HTTP calls), but in a unit test you inject a mock that returns pre-configured results in milliseconds. This is the **testability** benefit of DI: you verify the ViewModel's logic (does it call the right method? does it navigate on success?) without needing a running API server. The test follows AAA: set up mocks, execute the command, verify interactions. Each test verifies one scenario (happy path, wrong password, network failure) by changing only what the mock returns.

**Code Example:**
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

**Theory:** Mocking creates **test doubles** — fake implementations of interfaces used in unit tests. A mock implements the same interface as the real service but returns pre-configured values (via `Setup`) and records which methods were called (verified via `Verify`). There are different test double types: **stubs** just return values, **fakes** have simplified implementations (like in-memory DB), and **mocks** track interaction expectations. The separation is important because a **stub** helps you test state (what value was returned), while a **mock** helps you test behavior (was the right method called with the right args?). Using mocks makes tests fast, isolated, and deterministic — no network, no filesystem, no database.

**Code Example:**
```csharp
var mockApi = new Mock<IApiService>();
mockApi.Setup(a => a.GetAsync<List<StationModel>>("/api/station/nearby"))
       .ReturnsAsync(new List<StationModel> { new StationModel { Name = "Test Station" } });
```

**Why mock?** Tests run fast, don't need a real API server, and can test error scenarios easily.

---

**Q93: How would you test `ApiService.HandleResponse<T>`?**

**Answer:**

**Theory:** `HandleResponse<T>` is typically an internal/private method — but it contains crucial parsing logic that deserves its own unit tests. You can test private methods either (1) by making them `internal` with `InternalsVisibleTo`, or (2) by using `PrivateObject`/reflection (brittle). The smarter approach is to refactor: extract the response-handling logic into a separate `IResponseHandler<T>` interface, inject it into `ApiService`, and test the handler independently. This follows the **Single Responsibility Principle** — `ApiService` orchestrates HTTP calls, `ResponseHandler` parses responses. Testing as-is: create `HttpResponseMessage` with various status codes/bodies, call the handler, assert correct exception or parsed output.

**Code Example:**
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

**Theory:** Debugging a MAUI app spans three layers: (1) **C# code** — breakpoints, watch window, immediate window for stepping through ViewModel logic; (2) **XAML data binding** — the Output window's binding error traces (`"Binding: 'X' not found on 'Y'"`) are often the fastest way to spot MVVM mistakes; (3) **HTTP/api** — tools like Fiddler/Postman intercept requests between app and server to inspect payloads, status codes, and timing. The **Live Visual Tree** helps diagnose layout issues (which element is where, what margins/paddings apply). `dotnet trace`/`dotnet counters` (.NET CLI tools) profile CPU, GC, and thread pool usage on-device without Visual Studio. Knowing which tool targets which layer is essential — using the debugger for a binding issue is slow; using the Output window's binding trace takes seconds.

**Code Example:**
- **Visual Studio debugger** — breakpoints, watch, immediate window
- **XAML Hot Reload** — modify XAML while app is running
- **Live Visual Tree** — inspect the visual tree at runtime
- **Output window** — `Debug.WriteLine()` output
- **DevTools** — `dotnet trace`, `dotnet counters` for performance
- **Network tab** — Fiddler, Postman to inspect HTTP traffic

---

**Q95: How would you diagnose why a data-bound label isn't updating?**

**Answer:**

**Theory:** A non-updating label means the binding pipeline is broken somewhere. The MAUI binding engine works in layers: (1) `BindingContext` must point to the ViewModel instance; (2) `x:DataType` must match the ViewModel type (for compiled bindings); (3) the property name in `{Binding MyProperty}` must exist on that type; (4) the property must raise `PropertyChanged` when its value changes. Diagnose by narrowing: first check the Output window (it logs binding errors verbosely), then verify `BindingContext` with a breakpoint, then test with a hardcoded value. A common gotcha: if the ViewModel is recreated after the binding is set (e.g., a new ViewModel assigned to `BindingContext` in code), the old binding still references the old instance. This is why setting `BindingContext` in XAML or constructor (once) is safer than reassigning.

**Code Example:**
1. Check the `Output` window for binding errors (`"Binding: 'MyProperty' not found on 'MyViewModel'"`)
2. Verify the ViewModel property has `[ObservableProperty]` or raises `PropertyChanged`
3. Check `x:DataType` is correct and includes the property
4. Ensure the ViewModel is set as `BindingContext`
5. Try a simple test: `<Label Text="{Binding MyProperty}" />` with a known value

---

**Q96: What is UI testing in MAUI?**

**Answer:**

**Theory:** UI testing (end-to-end testing) runs the **full application** — real pages, real ViewModels, real services (or a test server) — and automates user gestures like tapping, typing, and swiping. This contrasts with **unit testing**, which tests a single class in isolation with mocked dependencies. UI tests are slower (seconds vs milliseconds) and more fragile (they break if UI structure changes), but they catch integration bugs that unit tests miss — e.g., a button wired to the wrong command, a page navigation failing because the route isn't registered, or an alert not appearing because of a threading issue. `Microsoft.Maui.Testing` (or Appium, Xamarin.UITest) drives the app via accessibility IDs set with `AutomationId`. Use sparingly — cover the critical user journeys (login, main data load, logout), not every edge case.

**Code Example:**
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

**Theory:** Testing async methods requires your test framework to support async test methods (NUnit, xUnit, MSTest all do via `public async Task`). The test method must `await` the async operation so it completes before assertions run — forgetting `await` causes the test to pass prematurely (the assertion runs before the operation finishes, seeing null/0 instead of expected values). Mock libraries provide async-specific setup methods: `ReturnsAsync` for successful async results (the mock returns a completed `Task<T>`), `ThrowsAsync` for exceptions (the mock returns a faulted `Task<T>`). The key insight: the ViewModel's `IAsyncRelayCommand.ExecuteAsync(null)` returns a `Task` — await it inside the test to ensure the command completes synchronously from the test's perspective.

**Code Example:**
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

**Theory:** AAA (or Given-When-Then) is the standard structure for unit tests, separating each concern: **Arrange** creates the test fixture (objects, mocks, input data), **Act** performs the single action under test, **Assert** verifies the outcome. This structure forces clarity — if a test has multiple Act sections or no clear Assert, it's likely testing too many things. Each test should have exactly **one Act step** and assert **one logical outcome** (though you may check multiple properties of that outcome). The pattern also makes tests self-documenting: a new developer can read the three sections and immediately understand what scenario the test covers and what behavior it validates.

**Code Example:**
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

**Theory:** Navigation is a **side effect** — LoginViewModel calls `INavigationService.NavigateToAsync()` after login succeeds. You test this by injecting a mock `INavigationService` and using `Verify()` to assert the method was called with the expected route. `Times.Once` means the method must have been called exactly once — if it wasn't called, or was called twice, the test fails. `Times.Never` verifies a method was NOT called (e.g., login fails → navigation should NOT occur). `It.IsAny<T>()` is a flexible matcher that accepts any value of the given type — use it when you don't care about the exact parameter. The key principle: **verify interactions on mocks**, not state. You wouldn't check whether `vm.CurrentPage` changed (the ViewModel doesn't know the current page) — instead, you verify that navigation was requested with the correct route.

**Code Example:**
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

**Theory:** Platform APIs like `SecureStorage` (which calls DPAPI on Windows, KeyChain on iOS, KeyStore on Android) cannot run in a unit test — they require a device or emulator. The solution is the **Adapter pattern**: wrap the platform API in an interface (`ISecureStorageService`), inject it into your ViewModels, and in production use an implementation that delegates to `SecureStorage`. In tests, inject a mock. This is the same principle as abstracting `HttpClient` behind `IApiService` or the filesystem behind `IFileService`. The cost is one extra interface per platform dependency, but the benefit is complete testability — your ViewModel logic can be fully covered by fast unit tests without ever touching a real device.

**Code Example:**
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

**Theory:** MAUI apps face unique performance challenges because they run on resource-constrained mobile devices AND desktop. The five major areas: (1) **Startup time** — MAUI must load the runtime, all assemblies, and parse initial XAML. The more assemblies and XAML pages loaded at startup, the slower. (2) **UI thread** — MAUI is single-threaded for UI. Any synchronous operation on the UI thread (including sync HTTP calls) blocks the entire app. (3) **Memory** — MAUI native controls are heavyweight. A `StackLayout` with 1000 items creates 1000 platform views. `CollectionView` recycles views, keeping only ~10-15 platform views regardless of list size. (4) **Images** — loading a 4K photo into a 50x50 avatar means decoding and storing 22MB of pixels for 500 bytes of useful data. (5) **XAML parsing** — without compiled bindings (`x:DataType`), the binding engine uses runtime reflection for every property access, adding overhead per binding update. Compiled bindings generate IL code at build time, eliminating reflection entirely.

**Code Example:**
- **Startup time** — minimize assembly loading, use compiled bindings
- **Memory** — `CollectionView` virtualizes; plain `StackLayout` with 1000 items does NOT
- **UI thread** — don't block it with sync operations
- **Images** — resize before displaying; don't load 4K photos into an avatar
- **XAML parsing** — use compiled bindings (`x:DataType`) to reduce runtime reflection

---

**Q102: How does `CollectionView` with `DataTemplate` get recycled?**

**Answer:**

**Theory:** UI virtualization is essential for list performance. Without it, a list of 10,000 items creates 10,000 platform views, each with its own memory allocation, layout pass, and measurement cycle. `CollectionView` uses **view recycling**: it creates only enough item views to fill the visible area (roughly screen height / item height + a few extra). As the user scrolls, items scrolling off-screen have their views returned to a **reuse pool**. When a new item scrolls into view, `CollectionView` pulls a recycled view from the pool, updates its binding context to the new data item, and places it at the new position. This means the same ~15 platform views serve any number of data items. The critical factor: the `DataTemplate` must be the same for all items (homogeneous layout) — if each item needs a different template (heterogeneous), you lose full recycling and must use `DataTemplateSelector`.

**Code Example:**
`CollectionView` creates only as many items as fit on screen. When you scroll, items that go off-screen are recycled (their views are reused for the new items). This prevents creating thousands of views for a list of 10,000 items.

---

**Q103: What is AOT compilation in MAUI?**

**Answer:**

**Theory:** By default, .NET uses **JIT (Just-In-Time) compilation**: IL bytecode is compiled to native code at runtime, method by method, as each method is first called. This adds startup latency (each method compiled on first use) and keeps IL assemblies loaded in memory. **AOT (Ahead-Of-Time)** compiles ALL IL to native code at build time, producing a native executable with no JIT step at runtime. MAUI on Windows supports .NET Native AOT, which reduces startup time by 30-50% and working set by 20-40%. However, AOT requires all code to be "AOT-compatible" — reflection-heavy patterns (like expression trees or dynamic code generation) may not work. The `MVVMTK0045` warnings arise because the source generator's `[ObservableProperty]` approach (which generates property accessors) needs `partial` properties for AOT compatibility.

**Code Example:**
AOT (Ahead-Of-Time) compilation compiles C# to native code at build time rather than at runtime. On Windows, MAUI uses .NET Native AOT. Benefits: faster startup, less memory. The `MVVMTK0045` warnings in the app are about `[ObservableProperty]` fields not being compatible with WinRT AOT — they'd need to use `partial` properties instead.

---

**Q104: How would you reduce app startup time?**

**Answer:**

**Theory:** Startup performance breaks down into: (1) **Runtime initialization** — loading the CLR/runtime, JIT-compiling initial methods. (2) **Assembly loading** — each assembly must be loaded and verified. (3) **XAML parsing** — MAUI parses XAML at runtime, building the visual tree. (4) **Service initialization** — singleton service constructors run during DI container setup. Reducing each: use AOT to eliminate JIT, merge assemblies to reduce load count, use `x:Load="False"` on off-screen content (deferred XAML loading), and register services but construct them lazily (Lazy<T> or only on first use). Each millisecond at startup matters because users perceive launch latency — 1 second is acceptable, 3+ seconds feels "broken."

**Code Example:**
1. Use compiled bindings (`x:DataType`)
2. Defer XAML loading with `x:Load`
3. Lazy-initialize services
4. Use `.NET Native AOT` (Windows)
5. Reduce assembly sizes
6. Minimize fonts and resource dictionaries

---

**Q105: What is the risk of storing JWT tokens in `Preferences` vs `SecureStorage`?**

**Answer:**

**Theory:** `Preferences` stores data as **plain text** in the app's local storage (on Windows: registry or local settings file, Android: SharedPreferences XML, iOS: NSUserDefaults plist). Any code running on the device — including malware, sideloaded apps, or a debugger — can read this data. `SecureStorage` uses each platform's hardware-backed encryption: Windows uses **DPAPI** (Data Protection API, encrypts with the user's Windows credentials), iOS uses **KeyChain** (hardware-enforced encryption on devices with Secure Enclave), Android uses **EncryptedSharedPreferences** backed by **Android KeyStore**. A JWT token in `Preferences` can be exfiltrated by any app with read access to storage. The rule: anything that could cause harm if leaked (tokens, passwords, PII) goes in `SecureStorage`; UI preferences (theme, font size) go in `Preferences`.

**Code Example:**
| Storage | Encrypted? | Risk |
|---------|-----------|------|
| `Preferences` | No (plain text) | Other apps/malware can read the token |
| `SecureStorage` | Yes (DPAPI/KeyChain/KeyStore) | Token is encrypted at rest |

Always use `SecureStorage` for tokens, passwords, and any sensitive data. `Preferences` is fine for non-sensitive settings (e.g., theme preference).

---

**Q106: How does `HttpClient` timeout protect the app?**

**Answer:**

**Theory:** Network requests are inherently unreliable — the server might be overloaded, the network could drop packets, or an intermediate proxy could hang. Without a timeout, `HttpClient` would wait indefinitely for a response, blocking the calling thread (or coroutine). In a MAUI app, this means the UI thread is blocked (if called synchronously) or the app appears frozen waiting for data. `HttpClient.Timeout` sets a maximum wall-clock time from request start to response completion. If the timeout expires, a `TaskCanceledException` is thrown (wrapping an inner `TimeoutException`). The app should catch this and show a user-friendly message ("Network timeout, please try again") rather than hanging. Choose a timeout that balances user patience vs. legitimate slow operations: 10-30 seconds for typical API calls, longer (60-120s) for file uploads.

**Code Example:**
```csharp
_httpClient.Timeout = TimeSpan.FromSeconds(30);
```

If the server doesn't respond within 30 seconds, `HttpClient` throws `TaskCanceledException`. Without a timeout, the app could hang indefinitely waiting for a response.

---

**Q107: What is input validation and how is it done in `AddMoneyViewModel`?**

**Answer:**

**Theory:** Input validation enforces that data conforms to expected formats and ranges before processing. There are two layers: (1) **Client-side** (in the ViewModel) — provides instant feedback without a network round trip. If the user enters "abc" as an amount, they should know immediately, not after the API call fails. (2) **Server-side** (in the API controller) — the last line of defense; never trust client input. In `AddMoneyViewModel`, validation happens before the API call: parse the string input, check it's a valid positive decimal, show an alert if invalid, and return early without making the API call. This prevents wasted API calls and gives the user sub-second feedback. The `Validators.IsValidAmount()` encapsulates reusable validation logic (amount > 0, max limit, decimal places).

**Code Example:**
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

**Theory:** SQL injection attacks exploit string concatenation in SQL queries. If `username` is `"'; DROP TABLE Users; --"`, the concatenated query becomes `SELECT * FROM Users WHERE Username = ''; DROP TABLE Users; --'` which deletes the entire Users table. **Parameterized queries** separate SQL code from data: the database treats the query structure as fixed and parameters as data values only. EF Core's LINQ methods (`FirstOrDefault`, `Where`, `Select`) always generate parameterized SQL automatically. Even `ExecuteSqlInterpolated` (the safe version of `ExecuteSqlRaw`) generates parameters. The dangerous case is `ExecuteSqlRaw` with string interpolation — this bypasses parameterization entirely. The rule: never use string concatenation or interpolation in SQL queries, even if you think the input is "safe."

**Code Example:**
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

**Theory:** XSS (Cross-Site Scripting) occurs when an attacker injects `<script>` tags or event handlers into a web page, which then execute in other users' browsers. This is the #1 web vulnerability. MAUI is a native desktop/mobile application, not a web browser — it doesn't execute HTML or JavaScript from data-bound strings. A `<Label Text="{Binding UserInput}" />` will display `<script>...</script>` as literal text, not execute it. The XSS concern only arises if you're using a `WebView` to render user-supplied HTML (e.g., in-app browser, HTML emails). In that case, `HtmlEncode` escapes `<`, `>`, `&`, `"` characters into their HTML entity equivalents (`&lt;`, `&gt;`, etc.), preventing script execution. Always sanitize BEFORE the data enters the WebView, not in the MAUI layer.

**Code Example:**
```csharp
// Sanitize HTML before displaying in WebView
var sanitized = System.Web.HttpUtility.HtmlEncode(userInput);
```

---

**Q110: How would you secure the API over public internet?**

**Answer:**

**Theory:** Securing a public API follows **defense in depth** — multiple layers of security so that if one fails, others still protect. (1) **HTTPS** encrypts all data in transit, preventing MITM (Man-in-the-Middle) attacks that could steal tokens or modify payloads. (2) **JWT with short expiry** (15-30 min) limits the damage if a token is stolen; combine with refresh tokens for UX. (3) **Rate limiting** (e.g., 100 req/min per IP) prevents brute-force password guessing and DDoS. (4) **Input validation** rejects malformed data at the controller level before it reaches business logic. (5) **CORS** restricts browser-based access (relevant if a web frontend also uses the API). (6) **API keys** authenticate server-to-server calls (e.g., from your mobile app's backend service). (7) **Audit logging** records who accessed what and when — essential for incident response and compliance. These layers protect against different attack vectors; together they provide comprehensive defense.

**Code Example:**
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

**Theory:** SOLID is five principles of **object-oriented design** that make code maintainable, testable, and adaptable. They address the most common causes of software rot: (1) classes that do too much (S), (2) modifying working code to add features (O), (3) inheritance hierarchies that break when substituted (L), (4) interfaces that force consumers to depend on methods they don't use (I), (5) high-level policy depending on low-level implementation details (D). In a MAUI/MVVM app, SOLID maps naturally: ViewModels are the **high-level policy** (they orchestrate UI logic), services are **low-level details** (they implement HTTP calls, platform APIs), and interfaces are the **abstractions** that decouple them. Violating SOLID leads to ViewModels that directly create `HttpClient` instances (hard to test, hard to change API providers), interfaces with 20 methods (every ViewModel depends on all 20 even if it uses 1), or classes with mixed UI/data access logic.

**Code Example:**
| Principle | What it means | Codebase Example | Why it matters |
|-----------|--------------|------------------|----------------|
| **S**ingle Responsibility | A class should have exactly one reason to change | `ApiService` handles HTTP only; `AuthService` handles login/logout/tokens only. If you change auth logic, you change `AuthService` only — `ApiService` is untouched. | Prevents cascading changes. Each class is focused, readable, and independently testable. |
| **O**pen/Closed | Open for extension, closed for modification | `IApiService` can get a new implementation (e.g., `CachedApiService`, `MockApiService`) without modifying any ViewModel that consumes it. | Adding features doesn't break existing, tested code. |
| **L**iskov Substitution | A derived class must be substitutable for its base | Any `IApiService` implementation (`ApiService`, `MockApiService`) can be injected wherever `IApiService` is expected — the caller should not need to know the concrete type. | Enables polymorphism and mocking. Violated when a derived class throws exceptions the base doesn't or changes expected behavior. |
| **I**nterface Segregation | Many small, focused interfaces are better than one large one | `IApiService`, `IAuthService`, `INavigationService`, `IConnectivityService` are separate instead of a monolithic `IService` with all methods. | Consumers only depend on what they use. `LoginViewModel` gets `IAuthService` — it doesn't need to know about navigation or connectivity. |
| **D**ependency Inversion | Depend on abstractions, not concretions | ViewModels declare `IAuthService _auth` in constructors, not `AuthService _auth`. | Decouples high-level policy (ViewModel) from low-level details (HTTP calls). Enables swapping implementations for tests or changes. |

---

**Q112: Composition vs inheritance? Which does MAUI prefer?**

**Answer:**

**Theory:** **Inheritance** creates an "is-a" relationship: `LoginViewModel` IS a `BaseViewModel`. It's good for sharing behavior (e.g., `IsBusy`, `Title`, `ShowAlertAsync`) across related classes. But inheritance creates a tight coupling — a change to the base class affects ALL subclasses (the "fragile base class" problem). Deep inheritance hierarchies (A → B → C → D) are hard to reason about and test. **Composition** creates a "has-a" relationship: `LoginViewModel` HAS an `IAuthService`. The ViewModel doesn't care about the concrete type — it only depends on the interface. Composition is easier to test (you mock the composed service) and more flexible (you swap implementations without changing the composed class). MAUI prefers composition via DI. Use inheritance sparingly for shared UI patterns (like `BaseViewModel` for `IsBusy`/`Title`), but compose all business logic services.

**Code Example:**
- **Inheritance:** "is-a" — `ContentPage`, `BaseViewModel`
- **Composition:** "has-a" — ViewModel `has-a` `IApiService`

MAUI prefers composition. ViewModels compose services via constructor injection rather than inheriting from a service class. `BaseViewModel` uses inheritance for shared UI concerns (IsBusy, Title) but composes services (IApiService, IAuthService).

---

**Q113: Explain the Repository pattern.**

**Answer:**

**Theory:** The Repository pattern mediates between domain/business logic and the data mapping layer (EF Core, Dapper, etc. in the server side; `IApiService` in the MAUI client). It acts as an **in-memory collection** abstraction — you call `stationRepo.GetNearbyAsync(lat, lng, radius)` and get back `List<Station>` without knowing whether the data came from SQL Server, a text file, or an API. This has three benefits: (1) **Testability** — the controller can be unit-tested by mocking `IStationRepository` instead of needing a real database. (2) **Swappability** — switching from SQL Server to PostgreSQL means writing a new `StationRepository` implementation; the controller doesn't change. (3) **Encapsulation** — complex query logic (spatial queries, LINQ with multiple joins) lives in the repository, not scattered across controllers. In the MAUI client, `IApiService` serves the same role — the ViewModel doesn't know whether data comes from HTTP, SignalR, or a local cache.

**Code Example:**
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

**Theory:** Clean Architecture organizes code into concentric layers: the **innermost** layer (Domain/Core) contains enterprise business rules with zero external dependencies; the **outer** layers contain infrastructure (databases, HTTP clients, file systems) and presentation (controllers, pages). The **Dependency Rule** states: dependencies can only point INWARD — outer layers depend on inner layers, never the reverse. This means `Core/` defines interfaces (e.g., `IStationRepository`), `Infrastructure/` implements those interfaces (e.g., `StationRepository` with EF Core), and `Controllers/` depend on the interfaces from `Core/`. If you swap EF Core for Dapper, you rewrite only `Infrastructure/` — `Core/` and `Controllers/` are untouched. This isolation is critical for long-lived applications where technology choices evolve over time.

**Code Example:**
| Layer | Clean Architecture | EVSwap.API |
|-------|-------------------|------------|
| 1 | Domain / Core | `Core/` — Entities, DTOs, Interfaces, Business logic |
| 2 | Infrastructure | `Infrastructure/` — DbContext, Repositories, External services |
| 3 | Presentation | `Controllers/` — API endpoints, `Program.cs` |

The domain layer (`Core/`) has no dependency on infrastructure — it defines interfaces, and infrastructure implements them.

---

**Q115: What is technical debt and how would you identify it?**

**Answer:**

**Theory:** Technical debt is a metaphor: taking a "loan" by writing code quickly (cutting corners) that must be "repaid" later with interest through bugs, slow development, and fragile code. Like financial debt, some technical debt is strategic (ship now, refactor later for a critical deadline), but uncontrolled debt leads to the **broken windows** effect — once the codebase is messy, developers stop caring and quality declines. Identify debt via code smells: **duplication** (copy-pasted ViewModel patterns — a bug fix in one must be repeated in 10 places), **dead code** (SignalR, LocalDatabase — unused but compiled, adding cognitive load), **empty catch blocks** (swallowed errors make debugging impossible), **hardcoded values** (URLs, credentials — prevent environment switching), and **missing error handling** (unhandled exceptions crash the app). Each of these indicates work deferred that will cost more later.

**Code Example:**
Signs:
- Duplicate code (e.g., similar ViewModel patterns copy-pasted)
- Dead code (SignalR, LocalDatabase services — deleted in this refactoring)
- Empty `catch {}` blocks
- Hardcoded URLs and credentials
- Missing error handling

---

**Q116: How would you implement logging across the app?**

**Answer:**

**Theory:** Logging follows a **producer-consumer** pattern: ViewModels and services produce log messages with severity levels (Trace, Debug, Information, Warning, Error, Critical), and logging providers consume those messages (write to console, file, cloud). `Microsoft.Extensions.Logging` (MEL) provides the abstraction: inject `ILogger<T>` where T is the class producing the log. MEL supports **structured logging** — `_logger.LogError(ex, "Login failed for user {Username}", Username)` captures the message AND the structured property `Username="admin"`. This enables powerful log analysis: "show all logins that failed in the last hour grouped by username." Add log sinks based on environment: Debug/Console for development, file or Application Insights/Sentry for production. Avoid logging sensitive data (passwords, tokens) even at Debug level.

**Code Example:**
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

**Theory:** Unit tests and integration tests serve different, complementary purposes. **Unit tests** verify a single class in isolation — all dependencies are mocked. They're fast (1000 tests in < 1s), deterministic (same result every run), and pinpoint failures to the exact class. **Integration tests** verify that multiple real components work together — database, API, filesystem. They're slower and flakier (network hiccups cause false failures) but catch contract mismatches (e.g., the ViewModel sends a property the API serializes differently). The **test pyramid** recommends 70% unit tests (fast feedback), 20% integration tests (component boundaries), 10% end-to-end UI tests (critical user journeys). Over-investing in UI tests is a common mistake — they're slow and brittle. Under-investing in unit tests leads to long debugging cycles where the failure point is far from where the bug manifests.

**Code Example:**
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

**Theory:** API versioning lets you evolve your API without breaking existing clients. When you add, change, or remove fields in response payloads, old clients (which may not update immediately, especially mobile apps) still work. Two main approaches: (1) **URL-based** (`/api/v1/swap`, `/api/v2/swap`) — simplest, most explicit; clients clearly know which version they're calling. But it clutters the URL space and requires maintaining duplicate controllers. (2) **Header-based** (`Accept: application/vnd.evswap.v2+json`) — cleaner URLs but harder to discover/debug. The choice depends on your client upgrade strategy: for mobile apps that can't force-update all users simultaneously, URL versioning is safer because the version is explicit in every request. For web SPAs that update atomically, header-based works well. Keep the number of supported versions small (2-3 max) — each version is a maintenance burden.

**Code Example:**
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

**Theory:** CORS (Cross-Origin Resource Sharing) is a **browser security mechanism** that prevents a web page from making AJAX requests to a different domain than the one that served the page. The browser sends a preflight `OPTIONS` request and checks the server's `Access-Control-Allow-Origin` header before allowing the actual request. CORS is a browser-enforced policy — it does NOT apply to native mobile apps, desktop apps, or server-to-server calls. So in a MAUI app, CORS is irrelevant: `HttpClient` makes requests directly without browser-enforced origin restrictions. However, the API might need CORS if it also serves a web frontend (e.g., an admin portal on `app.evswap.com`). In that case, configure CORS with specific allowed origins rather than `AllowAnyOrigin()` for production.

**Code Example:**
```csharp
builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
```

---

**Q120: How would you deploy a MAUI Windows app to end users?**

**Answer:**

**Theory:** Deploying a MAUI Windows app involves **packaging** (bundling the executable, DLLs, and resources into a single distributable package) and **distribution** (getting that package to users). Windows uses **MSIX** as the modern packaging format — it provides a clean install/uninstall experience, automatic updates, and identity for the app (tied to the publisher certificate). The deployment pipeline: `dotnet publish` compiles the app and its dependencies; Visual Studio's Windows App Packaging project wraps the output into an MSIX package; the package must be **code-signed** with a trusted certificate (otherwise Windows SmartScreen warns users); distribute through the **Microsoft Store** (largest reach, automatic updates, but requires Store certification) or **sideloading** (install MSIX directly via double-click — simpler but requires manual updates). For enterprise, **ClickOnce** provides web-based deployment with automatic updates without Store certification.

**Code Example:**
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
Handlers replaced Renderers in MAUI as part of a fundamental architectural change.

**Renderers (Xamarin.Forms):** Each cross-platform control (e.g., `Entry`) had a corresponding `Renderer` class per platform (`EntryRenderer` iOS, `EntryRenderer` Android). The renderer was a full `FrameworkElement`/`View` subclass created via inheritance. To customize, you subclassed the entire renderer. This created a **heavy object graph** — each control had a multi-level inheritance chain, impacting startup time and memory.

**Handlers (MAUI):** A Handler is a lightweight **mapper** that maps cross-platform properties → native platform properties as a simple dictionary, not an inheritance chain:

```
Cross-platform Property (e.g., Entry.Text)
    → Handler.Mapper["Text"] → native control.Text = value
```

**Key differences:**
| Aspect | Renderer | Handler |
|--------|----------|---------|
| Architecture | Inheritance (subclass ViewRenderer) | Composition (property mapping dictionary) |
| Memory | Full platform view subclass | Thin mapper object |
| Startup | Must load renderer assemblies | Lazy, on-demand mapping |
| Customization | Subclass the renderer | Append/override mapping via `Mapper.AppendToMapping` |
| Service location | Manual | DI-integrated via `IMauiHandlersCollection` |

**Why this matters:** The Handler architecture reduced MAUI's startup memory and made custom control creation simpler — instead of subclassing a platform renderer, you register a property mapping override in `MauiProgram.cs`:

---

**Q122: How does `IPlatformApplication` work in MAUI?**

**Answer:**

**Theory:** MAUI apps start with a shared `MauiProgram.cs`, but each platform has its own startup sequence. `IPlatformApplication` provides a hook for platform-specific initialization before the MAUI framework fully boots. On Windows, the MAUI `App` class must implement `IPlatformApplication` (or use the default MAUI implementation) to integrate with WinUI's `Application` lifecycle — this is where you configure the window size, title, icon, and handle Windows-specific lifecycle events (activated, suspended, resumed). You typically don't need to customize this unless you need platform-specific behavior that MAUI's cross-platform abstractions don't expose. The pattern: MAUI provides cross-platform APIs for 90% of scenarios; `IPlatformApplication` is the escape hatch for the remaining 10%.

**Code Example:**
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

**Theory:** `OnPlatform` adapts UI per **operating system** — what displays on Android vs iOS vs Windows. This is needed because each platform has different design conventions: iOS uses "Cancel" and "Done" in navigation bars, Android uses a back arrow, Windows uses an X button. `OnIdiom` adapts per **device form factor** — phone vs tablet vs desktop. A phone may show a single-pane layout, a tablet shows a split-pane master-detail, and a desktop shows a full multi-column layout. Both use XAML markup extensions that evaluate at runtime based on the current platform/device. Use them sparingly — heavy use indicates you should probably extract platform-specific views rather than cluttering shared XAML with conditional logic. Prefer `OnPlatform` for trivial differences (font size, padding) and `OnIdiom` for structural layout changes.

**Code Example:**
```xml
<!-- Platform-specific -->
<Label Text="{OnPlatform Default='Running', Android='Droid', iOS='iPhone'}" />

<!-- Idiom-specific (phone vs tablet vs desktop) -->
<Label Text="{OnIdiom Phone='Phone', Desktop='Desktop', Tablet='Tablet'}" />
```

---

**Q124: How do you handle app themes (Light/Dark mode) in MAUI?**

**Answer:**

**Theory:** Theme support in MAUI uses `AppThemeBinding` markup extension — it lets you specify different values for Light and Dark mode in a single XAML property, and MAUI automatically switches when the OS theme changes. This works because the device emits a theme-change event, MAUI's binding engine updates all `AppThemeBinding` evaluations, and the UI re-renders automatically. `Application.Current?.UserAppTheme` lets you override (force light mode even when the device is in dark mode). Important: themes affect only colors and images that use `AppThemeBinding` — you need to audit every color and image in your app. A common approach: define theme-aware colors in `ResourceDictionary` styles at the app level, so all controls inherit the correct colors without per-control `AppThemeBinding`.

**Code Example:**
```csharp
var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
Application.Current?.UserAppTheme = AppTheme.Dark;  // override
```

---

**Q125: What is `Shell.TabBar` and how does it differ from `FlyoutItem`?**

**Answer:**

**Theory:** Shell provides two navigation structures: `TabBar` for **bottom tabs** (most common in mobile apps — the primary screens you switch between frequently) and `FlyoutItem` for a **side menu** (less common in mobile, more in desktop — for secondary screens like Settings, Help, About). You can use both in the same app: `TabBar` for the main user journey (Dashboard, Wallet, Swap) and `FlyoutItem` for configuration screens (Settings, Profile). Shell handles the navigation chrome automatically — when you define a `TabBar`, Shell renders the tab strip; when you define a `FlyoutItem`, Shell renders the hamburger menu. The choice depends on UI hierarchy: `TabBar` is for horizontal navigation (peer sections), `FlyoutItem` is for vertical categorization (groups of sections with flyout sub-items).

**Code Example:**
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

**Theory:** MAUI UI properties can only be modified from the **main/UI thread**. Any background thread (Task.Run, async continuation on a ThreadPool thread) that tries to set `BindingContext`, `Text`, or any bindable property will throw an `InvalidOperationException`. `IDispatcher` is the MAUI abstraction for marshaling work to the UI thread — it wraps `Dispatcher.RunAsync` (WinUI), `Handler.post` (Android), and `DispatchQueue.Main.InvokeAsync` (iOS). In MVVM, you usually don't need it because `IAsyncRelayCommand` automatically marshals property changes to the UI thread. You need `IDispatcher` explicitly when you manually execute `Task.Run` and update bindable properties or interact with platform views from inside the background task. Prefer letting the command infrastructure handle threading; use `IDispatcher` only for low-level scenarios.

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

**Theory:** Deep linking lets external sources (another app, a push notification, an email link) navigate directly to a specific page in your app. `AppLinks` register a custom URI scheme (like `evswap://swap/123`) that the OS routes to your app. When the app opens via the deep link, `AddAppLink` callback receives the URI, parses it, and navigates to the appropriate Shell route. This is essential for: (1) push notifications that open a specific swap request, (2) password reset emails that deep-link to the reset page, (3) QR code scanners that open station details. Implementation requires two parts: (a) register the handler in `MauiProgram.cs`, (b) declare the URI scheme in each platform manifest (`Package.appxmanifest` for Windows, `AndroidManifest.xml` for Android, `Info.plist` for iOS).

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

**Theory:** MAUI's `SecureStorage` provides a cross-platform API that delegates to each OS's hardware-backed encryption. The encryption **occurs at the OS level**, not in MAUI — `SetAsync` passes the data to the platform's security service, which encrypts it with a device-derived key before writing to storage. On **Windows**, DPAPI uses the user's Windows login credentials as the encryption key — if another user logs into the same machine, they cannot decrypt the data. On **Android**, the Android KeyStore generates an AES key that's stored in a hardware-backed Trusted Execution Environment (TEE) on supported devices. On **iOS/macOS**, the KeyChain stores data in a secure enclave. Because encryption is OS-managed, there's no key management burden on the developer — just call `SetAsync`/`GetAsync`. The tradeoff: on app uninstall/reinstall, the keys are lost, so previously stored data becomes permanently inaccessible.
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

**Theory:** The `Connectivity` API monitors the device's network state and raises events when connectivity changes. This is critical for an EV app that depends on real-time API data — if the user enters an underground parking garage with no signal, the app should show cached data (if available) or a "No internet" message rather than a generic error. The `IsConnected` property provides a synchronous check; the `ConnectivityChanged` event fires when the network status changes. Best practice: inject `IConnectivityService` (wrapping `Connectivity.Current`) so you can mock it in tests. When connectivity drops, pause background sync; when it returns, trigger a sync for pending operations. On Windows, connectivity is almost always available (Ethernet/WiFi), but on mobile it changes constantly — this makes the Connectivity API especially important for the mobile MAUI app.

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

**Theory:** The `Geolocation` API abstracts platform GPS hardware. Getting a location requires: (1) declaring the right permissions in each platform manifest (AccessFineLocation on Android, NSLocationWhenInUseUsageDescription on iOS), (2) handling runtime permission requests (first request returns a `PermissionException` if the user hasn't granted access), (3) specifying accuracy (Highest uses GPS for precise lat/lng but drains battery; Medium/Low uses WiFi/cell triangulation for faster, less accurate results), (4) handling exceptions for GPS-disabled devices (`FeatureNotSupportedException`). For an EV app, geolocation drives core features: finding nearby stations, geofencing for swap reminders, and tracking fleet vehicle locations. The returned `Location` object provides `Latitude`, `Longitude`, `Altitude`, `Course` (heading), `Speed`, and `Accuracy`.

**Q131: How do you handle file picker interactions in MAUI?**

**Answer:**

**Theory:** `FilePicker` opens the OS-native file selection dialog, allowing the user to pick files from any location (local storage, cloud drives, photos). The API requires: (1) specifying allowed file types per platform (extensions on Windows, MIME types on Android, UTType identifiers on iOS), (2) handling the null result (user presses Cancel), (3) reading the file stream (don't assume the stream supports seeking or length — copy to a `MemoryStream` first). After picking, you typically upload the file to the API as a base64 string or multipart form data (Q64 pattern). For an EV app, file picker is used for: uploading profile photos, attaching documents to swap requests (invoice photos, inspection reports), and importing CSV data for fleet management.

---

**Q132: What is `BackgroundService` in .NET MAUI?**

**Answer:**

**Theory:** `BackgroundService` (from `Microsoft.Extensions.Hosting`) provides a structured way to run a recurring background loop. The `ExecuteAsync` method runs on a background thread; inside you have a `while(!stoppingToken.IsCancellationRequested)` loop that performs work and then `Task.Delay`s before the next iteration. The key benefits over a raw `Task.Run`: (1) graceful shutdown — `stoppingToken` is signaled when the app is closing, letting you finish the current iteration cleanly, (2) DI integration — `BackgroundService` is registered as a singleton, and its constructor receives fully resolved dependencies, (3) lifecycle management — MAUI's host starts the service automatically and stops it on shutdown. For an EV app, this enables: periodic polling for swap status updates, background data sync when connectivity returns, and sending pending cached requests to the API.

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

**Theory:** Custom fonts give the app a branded, professional look beyond the system fonts (Segoe UI on Windows, Roboto on Android, San Francisco on iOS). Implementation: (1) place `.ttf` or `.otf` files in `Resources/Fonts/`, (2) register each font in `MauiProgram.cs` with an alias (the alias is used in XAML as `FontFamily`), (3) the font files are automatically bundled as `MauiAsset` in the `.csproj`. The alias must not include the file extension. Bundling fonts adds to app size (typically 50-200KB per font file) — only include the weights you need (Regular, Medium, Bold) not the entire family. For accessibility, ensure fonts are legible at small sizes and support the character sets your users need (Latin, Devanagari, CJK).

**Code Example:**
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

**Theory:** MAUI's handler architecture uses a **mapper dictionary** to translate cross-platform properties to native platform properties. `Mapper.AppendToMapping` lets you add or override property mappings for a specific `ClassId` or condition without subclassing. This is the **Open/Closed principle** in action: the handler is open for extension (append mappings) but closed for modification (don't change existing mappings). The `#if WINDOWS` conditional compilation ensures the platform-specific code only compiles on Windows. The `handler.PlatformView` property gives you access to the native WinUI/Android/iOS control. Use this pattern sparingly — for most customization, MAUI's cross-platform APIs suffice. Custom handlers are for edge cases like: disabling spell check on a specific Entry, setting native keyboard types, or custom touch feedback.

**Code Example:**
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

**Theory:** Behaviors encapsulate reusable control logic that would otherwise require subclassing or code-behind. The pattern: attach to a control (`OnAttachedTo`), subscribe to events, modify the control's behavior, and clean up on detach (`OnDetachingFrom`). The critical requirement: **always unsubscribe** from events in `OnDetachingFrom` — failure to unsubscribe creates a strong reference from the control to the behavior, preventing GC and causing memory leaks. Behaviors are the MAUI equivalent of attached behaviors in WPF or directives in Angular. Common use cases: numeric-only input, auto-capitalization, input validation (highlight red border on invalid), and auto-complete dropdowns. Prefer behaviors over code-behind for logic that's reusable across multiple pages.

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

**Theory:** `DataTemplateSelector` allows a `CollectionView` or `ListView` to use different item templates depending on the data item's properties. This is essential for heterogeneous lists — e.g., pending swaps show one layout (with "Cancel" button), completed swaps show another (with "Rate" button). The selector receives each data item and returns the appropriate `DataTemplate`. Important performance consideration: `DataTemplateSelector` disables **full view recycling** because items with different templates can't be reused across template boundaries. MAUI still recycles within the same template group, but total platform views = sum of visible items across all template types. For best performance, minimize distinct templates (2-3 max) and keep them structurally similar.

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

**Theory:** `SwipeView` wraps any content and reveals contextual action buttons when the user swipes left or right. This is the MAUI equivalent of iOS's `UITableViewRowAction` or Android's `SwipeToDismiss`. The actions (Delete, Edit, Share) appear as buttons when swiped, and tapping an action executes its bound command. Key considerations: (1) `SwipeView` only works when the content is scrollable — it conflicts with scroll gestures if nested inside a `ScrollView` or `CollectionView` that scrolls in the same direction; (2) use `CommandParameter="{Binding .}"` to pass the current data item to the action command; (3) swipe actions should be **destructive or quick actions** (Delete, Mark as Read) — don't put primary navigation in swipe menus (it's not discoverable). For an EV app, SwipeView is ideal for: deleting a saved station, canceling a pending swap, or dismissing a notification.

**Code Example:**
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

**Theory:** MAUI provides built-in animation extension methods on `VisualElement` (all views inherit from this). Each method returns a `Task` that completes when the animation finishes, allowing composition via `await` or `Task.WhenAll`. Animations run on the **UI thread** and modify layout properties (TranslationX/Y, Scale, Rotation, Opacity). They are **not** hardware-accelerated (they run on the UI thread), so complex or long-running animations can cause jank. For smooth animations: keep duration under 500ms, avoid animating layout-affecting properties (Width/Height/Margin — these trigger re-layout), and use `Easing` functions (CubicInOut, SpringIn, etc.) for natural motion. MAUI does NOT have a built-in animation composition system like Flutter's or Android's — for complex animations, consider Community Toolkit's `Animations` or Lottie for JSON-based animations.

**Code Example:**
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

**Theory:** Custom page transitions let you override the default slide-in/slide-out animation when navigating between pages. Shell uses `INavigationTransition` interface for this: the `Apply` method receives the target page and a `isForward` flag, and you call MAUI's animation methods (TranslateTo, FadeTo, ScaleTo). The transition runs **after** the new page is loaded but **before** it's displayed to the user. For smooth transitions: use `Easing` functions (CubicInOut is the most natural), keep duration at 300-500ms, and animate only opacity and translation (not layout properties). Apply transitions at the Shell or NavigationPage level — don't mix multiple transition types. For an EV app, consider: slide-up for swap request details (like iOS modal), fade for login/logout transitions, and no animation for tab switches (feels sluggish).

**Code Example:**
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

**Theory:** GestureRecognizers add touch interaction to any `View` (Frame, Label, Image, etc.) without subclassing. Tap, swipe, pinch, pan, drag, and drop gestures are all available. The key pattern: attach gesture recognizers to a container (Frame, Border, ContentView) rather than directly to Label/Image for better hit-test area (minimum 44x44px touch target). `Command` binding connects gestures to ViewModel commands (MVVM-friendly), while event handlers (`Swiped`, `PinchUpdated`) require code-behind. Drag-and-drop (`DragGestureRecognizer` + `DropGestureRecognizer`) is especially useful for EV fleet management — reordering stations in a saved list, dragging a swap item to a timeline.

**Code Example:**
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

**Theory:** .NET historically used `Newtonsoft.Json` (Json.NET) as the de facto JSON library — it was fast, flexible, and feature-rich. In .NET Core 3.0+, Microsoft built `System.Text.Json` as a first-party replacement focused on **performance and AOT compatibility**. The key architectural difference: `System.Text.Json` uses `Utf8JsonReader`/`Utf8JsonWriter` that process UTF-8 directly (no string encoding/decoding step), while Newtonsoft operates on decoded strings. `System.Text.Json` defaults to **case-sensitive** property matching and camelCase naming, reflecting modern API conventions (REST APIs use camelCase). For AOT scenarios (like MAUI Windows AOT compilation), `System.Text.Json` with source generators eliminates runtime reflection entirely — the JSON serialization code is generated at compile time. Use `System.Text.Json` for all new projects; keep Newtonsoft only for legacy code that depends on its specific features (e.g., `ReferenceLoopHandling`, `TypeNameHandling`, `JsonPath`).

**Code Example:**
| Feature | System.Text.Json | Newtonsoft.Json |
|---------|-----------------|-----------------|
| Performance | Faster (no reflection-heavy fallback) | Slower |
| AOT compatible | Yes (source generators) | No |
| Default naming | CamelCase | PascalCase |
| Case insensitive | Set `PropertyNameCaseInsensitive = true` | Default |
| Custom converters | `JsonConverter<T>` | `JsonConverter` |
| `[JsonProperty]` | `[JsonPropertyName]` | `[JsonProperty]` |

---

**Q142: What are JSON source generators?**

**Answer:**

**Theory:** JSON source generators solve the AOT/reflection problem. Normally `System.Text.Json` uses **runtime reflection** to discover the properties of your types — it inspects `typeof(AuthResponse).GetProperties()` at runtime. This fails on AOT-compiled platforms (iOS, Windows AOT) where the reflection metadata is trimmed. Source generators analyze your code at **compile time**, generate a `JsonTypeInfo<T>` for each annotated type, and wire it to a `JsonSerializerContext`. The generated code contains explicit property-read/write logic for each type — no reflection needed. The result: faster serialization (generated code is specialized), smaller app size (reflection metadata for those types is trimmed), and AOT compatibility. Register generated types via `TypeInfoResolver = AppJsonContext.Default`.

**Code Example:**
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

**Theory:** The `dotnet` CLI is the primary tool for .NET development — project creation, building, testing, and deployment all go through it. The key commands form a development pipeline: `restore` resolves NuGet dependencies, `build` compiles the code, `test` runs unit tests, `run` launches the application, `publish` produces a deployment package. For EF Core, the `dotnet ef` commands manage migrations — `migrations add` creates a migration file based on model changes, `database update` applies pending migrations to the database. Understanding this pipeline is essential because CI/CD (GitHub Actions, Azure DevOps) uses these same commands; any local build issue will manifest in CI.

**Code Example:**
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

**Theory:** MAUI uses multi-targeting — the same C#/XAML code compiles to different platforms based on `TargetFrameworks`. Each target framework identifier (TFM) specifies a .NET version and platform: `net10.0-android` targets Android, `net10.0-ios` targets iOS, etc. The MAUI SDK handles platform-specific compilation: `#if ANDROID`, `#if IOS`, `#if WINDOWS` conditional compilation directives let you write platform-specific code in shared files, and `Platforms/` folders contain platform-specific entry points. The Windows TFM includes a minimum Windows version (`10.0.19041.0` = Windows 10 2004) — versions below this can't run the app. Multi-targeting means a single codebase produces app packages for all supported platforms.

**Code Example:**
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

**Theory:** .NET's GC is a **generational, non-deterministic** memory manager. Objects start in Gen 0 (short-lived — created and freed within a method). Most Gen 0 collections happen quickly (< 10ms). Surviving objects move to Gen 1, then Gen 2 (long-lived). The Large Object Heap (>85KB objects like byte arrays, strings, images) is collected with Gen 2 and causes the longest pauses. In MAUI, GC-related issues often stem from **reference leaks** — an object the developer thinks is unreachable still has a strong reference (e.g., an event handler not unsubscribed, a ViewModel stored in a static list, an image bitmap held by a cached page). These leaks prevent GC from collecting ViewModels, pages, and their associated native resources. The GC on mobile platforms (Android/iOS) is more aggressive (runs on low memory notifications), while desktop (Windows) GC runs less frequently. Use `dotnet counters` or Visual Studio's memory profiler to track allocations and detect leaks.

**Code Example:**
```csharp
// Common MAUI memory issues:
// 1. Event handler leaks - subscribing without unsubscribing
// 2. Static references - holding ViewModel in static field
// 3. Large collections - not clearing ObservableCollection
// 4. Image caching - loading full resolution photos
```

---

**Q146: What are `WeakReference` and `WeakReferenceMessenger`?**

**Answer:**

**Theory:** A normal (strong) reference prevents the GC from collecting the referenced object. This creates a problem for **caches** and **event buses** — a cache that stores ViewModel references indefinitely prevents those ViewModels from being collected, even if the user has navigated away. `WeakReference<T>` holds a reference that doesn't prevent GC — the object can be collected, and `TryGetTarget` returns false afterward. `WeakReferenceMessenger` (from MVVM Toolkit) applies this to messaging: when a ViewModel registers for a message via `WeakReferenceMessenger`, the messenger stores a weak reference to the recipient. When the ViewModel is no longer referenced anywhere else, it can be GC'd without explicitly unregistering. This eliminates a whole class of memory leak bugs where developers forget to call `Unregister`. Use `WeakReferenceMessenger` for cross-ViewModel communication (e.g., "user logged out, navigate to login"). Use `StrongReferenceMessenger` (the default) only in rare cases where you need guaranteed delivery to long-lived recipients.

**Code Example:**
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

**Theory:** `SemaphoreSlim` is a lightweight synchronization primitive that limits concurrent access to a resource. Think of it as a bouncer at a club: `WaitAsync` checks if there's room (initial count), enters if yes, and `Release` signals departure so the next caller can enter. With `initialCount=1, maxCount=1`, it behaves like an **async lock** — only one caller at a time. This prevents: (1) duplicate API calls — if two ViewModel commands call `GetStationsAsync` simultaneously before either completes, the semaphore queues the second until the first finishes; (2) rate limiting — limit concurrent calls to an external API that throttles connections; (3) resource contention — prevent multiple threads from writing to the same file or database record. Unlike `lock{}` (which blocks a thread), `SemaphoreSlim` uses `await WaitAsync` which is non-blocking — the thread is freed to do other work while waiting.

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

**Theory:** `Channel<T>` is a high-performance, thread-safe producer/consumer queue. It replaces `BlockingCollection<T>` with an async-first API. **Producer** (the writer) adds items via `channel.Writer.WriteAsync()` — this is the service that receives new data (e.g., WebSocket messages, API polling results). **Consumer** (the reader) reads items via `await foreach (var item in channel.Reader.ReadAllAsync())` — this is typically a background service or UI-bound loop that processes items. Channels can be **bounded** (fixed capacity — writer blocks when full, providing backpressure) or **unbounded** (infinite capacity — writer never blocks, but memory grows unboundedly if consumer can't keep up). For EV fleet data streaming (real-time battery telemetry, live swap status), `Channel<T>` provides the async pipeline: producer receives raw data from IoT devices → consumer transforms and dispatches to UI. The key advantage: no manual locking, no `Monitor`/`lock` needed — the channel handles all synchronization internally.

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

**Theory:** Source generators (introduced in .NET 5 / Roslyn) run as part of the compilation pipeline. They analyze your code's syntax tree (AST) and emit additional C# code that's compiled into the same assembly — all at compile time, before any IL is generated. The MVVM Toolkit's `[ObservableProperty]` generator detects fields annotated with the attribute, strips the underscore prefix, and generates the corresponding public property with full `INotifyPropertyChanged` implementation. The generated code is visible in your IDE (expand Dependencies → Analyzers → MVVM Toolkit). Benefits: no runtime reflection, no base class requirement (works with any `[ObservableObject]`), and AOT safe — generated code is regular IL, not reflection calls.

**Code Example:**
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

**Theory:** The C# compiler doesn't execute `async` methods directly — it transforms them into a state machine struct implementing `IAsyncStateMachine`. Each `await` becomes a state in a `switch` statement inside `MoveNext()`. The struct tracks: (1) `_state` — which await point we're at, (2) `_builder` — the `AsyncTaskMethodBuilder` that creates the `Task` and manages completion, (3) captured locals as fields. When `await` sees the operation hasn't completed, `MoveNext` returns (yielding the Task to the caller). When the operation completes, the awaiter calls back into `MoveNext` to continue from the saved state. This is why `async` has zero thread overhead during waits — no thread is blocked; the state machine resumes on completion.

**Code Example:**
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

**Theory:** `Span<T>` is a **ref struct** — a stack-only type that provides a type-safe view over contiguous memory (arrays, unmanaged memory, stackalloc). It has zero allocation overhead because it doesn't box or allocate on the heap. The limitation: ref structs can't be used in `async` methods (they might be captured across await points, which requires heap allocation). `Memory<T>` solves this — it's a regular struct that wraps a `Span<T>`-like view but can live on the heap, making it usable with `async` and lambdas. The key use case: slicing strings/arrays without allocation. `"hello world".AsSpan().Slice(0, 5)` gives `"hello"` without allocating a new string. For JSON parsing, base64 encoding, and string manipulation in hot paths, Span<T> eliminates memory allocations entirely.

**Code Example:**
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

**Theory:** When you `await` a Task, the compiler captures the current `SynchronizationContext` (UI context in MAUI) and posts the continuation back to it. This is essential in ViewModels — after `await _api.GetAsync()`, execution returns to the UI thread so you can set bindable properties. But in library code (services, repositories) that never touches UI, this capture is wasteful: the continuation thread must be marshaled back through the UI message pump, adding latency. `ConfigureAwait(false)` skips the capture — the continuation runs on any available thread-pool thread. Use it ONLY in non-UI code. The rule of thumb: every `await` in a service/repository class should have `ConfigureAwait(false)`; every `await` in a ViewModel should NOT.

**Code Example:**
`ConfigureAwait(false)` tells the awaiter to skip capturing the current `SynchronizationContext`. The continuation after `await` runs on an arbitrary thread-pool thread instead of returning to the captured context.

**The two scenarios:**

```csharp
// Scenario 1: Service/library code (no UI) - use ConfigureAwait(false)
public async Task<UserModel?> GetUserAsync(int id)
{
    var json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
    return JsonSerializer.Deserialize<UserModel>(json);
    // Continuation runs on thread pool - fine, no UI access
}

// Scenario 2: ViewModel (touches UI) - do NOT use
public async Task LoadDataAsync()
{
    var data = await _api.GetAsync(...);  // UI context captured automatically
    IsBusy = false;  // still on UI thread - safe without dispatch
}
```

**Context switching cost:** Without `ConfigureAwait(false)`, the continuation must be marshaled back to the UI thread via the message pump (`Dispatcher.BeginInvoke`). This has overhead:
- Posting a message to the UI message queue
- The UI thread must process its queue before the continuation runs
- Creates pressure on the UI thread's message loop, especially in tight loops

**The rule:** Use `ConfigureAwait(false)` in **all library code that doesn't touch UI** (services, repositories, data access layers). Do NOT use it in ViewModels, code-behind, or any code that reads/writes UI-bound properties. This is also covered in Q34 with more detail on SynchronizationContext.

---

**Q153: How does `IHttpClientFactory` work and is it needed in MAUI?**

**Answer:**

**Theory:** `IHttpClientFactory` addresses a specific server-side problem: each `new HttpClient()` creates a new `HttpClientHandler` which opens new TCP connections. In high-throughput ASP.NET Core apps, creating/disposing HttpClients rapidly leads to **socket exhaustion** (all ephemeral ports consumed, TIME_WAIT connections pile up). The factory pools and reuses handlers. In MAUI, this problem rarely occurs: (1) a mobile/desktop app typically makes 1-10 concurrent requests, not thousands, (2) a single `HttpClient` singleton is sufficient for one API server, (3) socket exhaustion takes thousands of connections per second. The simple MAUI pattern — one `HttpClient` registered as Singleton — is correct and sufficient. Only use `IHttpClientFactory` if your MAUI app makes many concurrent requests to multiple services and you see `HttpRequestException: Only one usage of each socket address is permitted`.

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

**Theory:** The Options pattern provides strongly-typed, validated access to configuration settings. Define a POCO class with properties matching your config section, register it with `builder.Services.Configure<T>(configSection)`, and inject `IOptions<T>` where needed. The framework binds the config section to the class at app startup. Benefits: (1) **no magic strings** — strongly typed access via `options.Value.BaseUrl`, (2) **reload on change** — `IOptionsSnapshot<T>` reloads when config file changes (ASP.NET Core), (3) **validation** — use `[Validate]` attributes or `IValidateOptions<T>` to catch misconfiguration at startup. In MAUI, appsettings.json is less common, but the Options pattern still applies when you want type-safe configuration for API URLs, timeouts, and feature flags without hardcoding.

**Code Example:**
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

**Theory:** `ILogger<T>` is part of `Microsoft.Extensions.Logging` (MEL), which MAUI's host builder includes by default. The `T` is the class that produces the log — the framework uses it to create a **named logger** and include the class name in log output. MEL supports **structured logging** via message templates with `{Placeholder}` syntax — `_logger.LogInformation("User {User} logging in", username)` captures the placeholder value as a structured property (key=User, value="admin"), enabling querying: "show all logins by user." Add providers in `MauiProgram.cs`: `AddDebug()` writes to VS Output window, `AddConsole()` to terminal, third-party providers like `Application Insights` or `Sentry` for production. Log levels (Trace → Critical) control verbosity; use `Information` for normal operations, `Warning` for recoverable issues, `Error` for failures, `Critical` for app-crashing errors.

**Code Example:**
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

**Theory:** `DelegatingHandler` implements the **Chain of Responsibility** pattern for HTTP requests. Each handler in the chain can inspect/modify the request before passing it to the next handler (via `base.SendAsync`), and inspect/modify the response on the way back. Multiple handlers can be chained: logging → retry → auth token injection → actual HTTP call. This separation is cleaner than putting logging, retries, and auth in `ApiService` itself — each handler has one responsibility. The handler receives the request as `HttpRequestMessage` and returns `HttpResponseMessage`, working at the HTTP abstraction level. Register via `builder.Services.AddTransient<LoggingDelegatingHandler>()` and attach to `HttpClient` via `AddHttpMessageHandler`. Key: handlers are instantiated per request (Transient), so they can be stateful per-call (like tracking a Stopwatch).

**Code Example:**
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

**Theory:** Auto-refresh polls the API at regular intervals to keep the dashboard up to date. MAUI provides `IDispatcherTimer` — a timer that fires on the UI thread, so the Tick handler can safely update bindable properties without an explicit `Dispatcher.Dispatch()`. The ViewModel implements `IDisposable` and unsubscribes/stop the timer in `Dispose()` to prevent memory leaks when the page is navigated away from. The alternative: use `BackgroundService` with periodic polling and send results via messaging; `IDispatcherTimer` is simpler when the timer directly updates a single ViewModel. For dashboard auto-refresh: start the timer after initial data load, stop on page disappear, restart on page appear. Avoid very short intervals (<10s) on mobile — constant polling drains battery.

**Code Example:**
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

**Theory:** `INotifyPropertyChanging` fires **before** a property changes, `INotifyPropertyChanged` fires **after**. The "Changing" event lets you: (1) **validate** — throw an exception in `OnChanging` to prevent the property from being set (useful for rejecting invalid values); (2) **save previous value** — the UI or a dependent service can record the old value before it's replaced; (3) **cancel an edit** — in an editable grid, the "Changing" event can trigger a confirmation dialog ("Discard changes?"). The MVVM Toolkit's `[ObservableProperty]` generates both `partial void OnUserNameChanging(string value)` and `partial void OnUserNameChanged(string value)` — you only implement the ones you need, leaving the other as an empty partial method that the compiler optimizes away.

**Code Example:**
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

**Theory:** `IComparable<T>` defines a **natural ordering** for a type — `CompareTo` returns a negative number (this < other), zero (equal), or positive (this > other). When you call `OrderBy(s => s)` on a collection of `StationModel`, LINQ uses `CompareTo` to sort. This is simpler than providing a lambda every time you sort. Use `IComparable<T>` when a type has a single, obvious default sort order (e.g., stations sorted by distance). For multiple sort criteria (sort by name OR by distance OR by rating), use `IComparer<T>` implementations (`DistanceComparer`, `NameComparer`) passed to `OrderBy`.

**Code Example:**
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

**Theory:** `INotifyDataErrorInfo` provides **per-property validation** that the MAUI binding engine understands natively. When you implement this interface, MAUI controls (Entry, Editor, etc.) can display validation errors automatically (red border, error message) by binding to the validation state. The flow: (1) property changes → `OnAmountChanged` runs validation → adds/clears errors → fires `ErrorsChanged` → MAUI re-evaluates `HasErrors` and `GetErrors()` → UI updates error visuals. This is the MVVM-clean way to do validation — the ViewModel owns validation logic, the UI just reflects it. The `[ObservableProperty]` `partial void OnChanged` hook is the perfect place to run validation because it fires on every property change, giving instant feedback.

**Code Example:**
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

**Theory:** `MethodImplAttribute` tells the JIT compiler how to handle a method's compilation. **AggressiveInlining** tells the JIT to inline the method's IL into the caller, eliminating the call overhead (push args, jump, pop, return). Use for tiny, frequently-called methods (property getters, helper math). **NoInlining** prevents inlining — useful when you want clear stack traces for exception logging or when inlining would increase code cache pressure. **Synchronized** is a dangerous legacy option — it's equivalent to `lock(this)` (locks on the instance), which can cause deadlocks. Modern code should use `lock` explicitly with a dedicated sync object. MAUI's AOT compilation makes `AggressiveInlining` less relevant (AOT code is already optimized), but in JIT scenarios it can improve hot path performance by 5-15%.

**Code Example:**
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

**Theory:** `DllImport` (Platform Invoke / P/Invoke) lets managed C# call unmanaged C/C++ functions in native DLLs. The runtime loads the specified DLL, marshals the arguments (converting .NET strings to C char*, structs to C structs), calls the native function, and marshals the return value back. This is how MAUI itself works internally — every cross-platform API call eventually P/Invokes into native platform code (Win32 API on Windows, JNI on Android, Objective-C runtime on iOS). You almost never need `DllImport` in MAUI apps because MAUI's cross-platform APIs cover most scenarios. Use it only for: (1) calling a Windows-specific API not exposed by MAUI (e.g., `GetSystemMetrics`), (2) interacting with a custom native C/C++ library (e.g., serial port communication, hardware drivers). Each platform requires its own `DllImport` for its native libraries — use `#if WINDOWS`, `#if ANDROID` to conditional compile.

**Code Example:**
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

**Theory:** `Interlocked` provides **atomic** operations on primitive types — they complete in a single CPU instruction, making them thread-safe without requiring `lock`. `Interlocked.Increment` reads, adds 1, and writes the value as an indivisible operation — two threads calling it simultaneously never see stale values. `CompareExchange` is the foundation of **lock-free** programming: it checks if a value equals an expected value and, if so, replaces it — all atomically. This enables thread-safe counters, flags, and simple state machines without the overhead of `lock` (which involves Monitor.Enter/Exit, kernel transitions). Use `Interlocked` for simple numeric operations; use `lock` or `SemaphoreSlim` for complex operations involving multiple fields or async.

**Code Example:**
```csharp
public class CounterService
{
    private int _activeRequests;

    public int Increment()
    {
        return Interlocked.Increment(ref _activeRequests);
    }

    public int Decrement()
    {
        return Interlocked.Decrement(ref _activeRequests);
    }

    public bool TrySet(ref int target, int value, int expected)
    {
        return Interlocked.CompareExchange(ref target, value, expected) == expected;
    }
}
```

---

**Q164: What is `readonly` vs `const` vs `static readonly`?**

**Answer:**

**Theory:** These three keywords control **when and how** a value is fixed. `const` is a compile-time constant — its value is baked into the IL at compile time. If you change `DefaultPageSize` from 20 to 50, all assemblies referencing the const must be recompiled (they have the old value embedded). `readonly` is a runtime constant — the value is set in the constructor and cannot change afterward. It can be calculated at runtime (e.g., `ApiUrl = configuration["ApiUrl"]`). `static readonly` is a single value shared across all instances, initialized when the type is first accessed (or eagerly via static initializer). It's the most flexible for configuration values that are fixed per application run. Rule: prefer `static readonly` over `const` for any value that might change in the future; use `const` only for values that are truly universal and immutable (math constants, enum values).

**Code Example:**
```csharp
public class Constants
{
    public const int DefaultPageSize = 20;
    public readonly string ApiUrl;
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
}
```

---

**Q165: How does `async` work in `foreach` with `IAsyncEnumerable<T>`?**

**Answer:**

**Theory:** `IAsyncEnumerable<T>` is the async version of `IEnumerable<T>` — it produces items asynchronously, fetching each batch or item on demand. The `await foreach` statement calls `GetAsyncEnumerator`, then loops: `MoveNextAsync()` (awaits the next item's availability) → `Current` (reads the item). This is streaming — items are processed one at a time without loading the entire collection into memory. The producer uses `yield return` inside an `async` method returning `IAsyncEnumerable<T>` — the compiler generates an async state machine that suspends after each yield. Use this for: paging through API results (fetch page 1 → yield items → fetch page 2 → yield items), processing large files line by line, or streaming real-time data. Cancellation is supported via `WithCancellation(CancellationToken)`.

**Code Example:**
```csharp
public async Task ProcessSwapsAsync(IAsyncEnumerable<SwapModel> swaps)
{
    await foreach (var swap in swaps)
    {
        await ProcessSwapAsync(swap);
    }
}

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

**Theory:** The stack trace is a **living record** of the call path that led to the exception. `throw` (bare, no variable) rethrows the current exception preserving the original stack trace — it's like saying "I don't know how to handle this, pass it up the chain exactly as it happened." `throw ex` creates a new exception object and sets its stack trace to the current line, overwriting the original. This is **destructive** — the original line number, method, and call chain are lost. Debugging becomes guessing. The correct patterns: (a) `throw` — rethrow with full context, (b) `throw new Exception("context", ex)` — wrap with additional context, preserving the original as `InnerException`. Never use `throw ex` in any code.

**Code Example:**
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

---

**Q167: How does `ExceptionDispatchInfo` preserve exception context across threads?**

**Answer:**

**Theory:** When you catch an exception and rethrow it on a different thread (e.g., in a Task continuation, a callback, or a background service), the original stack trace is lost because the call stack is thread-local. `ExceptionDispatchInfo.Capture(ex)` captures the exception's state (stack trace, data, HResult) as an opaque snapshot. Later, `capturedEx.Throw()` rethrows that snapshot on the current thread, preserving the original stack trace as if the exception originated from the original call site. This is essential in async coordination patterns where exceptions cross thread boundaries — e.g., a background sync task catches an HTTP exception, you capture it, and rethrow on the UI thread when the user checks sync status.

**Code Example:**
```csharp
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

**Theory:** The `using` statement guarantees `Dispose()` is called on an `IDisposable` resource, even if an exception occurs. C# 8 introduced **using declarations** — `using var x = new Resource()` — which disposes at the end of the enclosing scope, avoiding the extra indentation of a `using` block. Both compile to the same `try/finally` IL: the resource is disposed in the `finally` block, ensuring cleanup regardless of exceptions. Key: `using` only works with types implementing `IDisposable` (synchronous) or `IAsyncDisposable` (async `await using`). For async operations (file streams, HTTP responses), use `await using` which calls `DisposeAsync`. For HttpClient, wrapping in `using` is actually WRONG — HttpClient is meant to be reused (singleton pattern), not disposed after each call.

**Code Example:**
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

**Theory:** `IProgress<T>` decouples progress reporting from progress handling. The **producer** (e.g., an upload method) calls `progress.Report(value)` periodically without knowing where or how the progress is displayed. The **consumer** (the caller) creates a `Progress<T>` instance and passes its handler — the handler runs on the **UI thread** (via `SynchronizationContext` capture), so it can safely update bindable properties. This is the MVVM-clean way to report progress: the ViewModel's upload method receives `IProgress<double>` as a parameter, calls `Report` in a loop, and the caller's lambda updates `UploadProgress` property which the UI binds to a ProgressBar. `Progress<T>` uses the captured `SynchronizationContext` to marshal the handler to the UI thread, eliminating the need for `Dispatcher.Dispatch()`.

**Code Example:**
```csharp
public async Task UploadImageAsync(Stream image, IProgress<double> progress)
{
    var totalBytes = image.Length;
    var buffer = new byte[81920];
    int bytesRead;
    long totalRead = 0;

    while ((bytesRead = await image.ReadAsync(buffer)) > 0)
    {
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

**Theory:** The **Memento pattern** implements undo/redo by capturing an object's state (a "memento") before each change and storing it in a stack. Two stacks: `_undo` records past states (pop to go back), `_redo` records undone states (pop to go forward). When a new change is made, `_redo` is cleared because the undone history is invalidated by a new action. Each memento implements `IMemento.Restore()` which reverts the object to the captured state. For ViewModels, the memento could capture the entire ViewModel state (serialize to JSON) or only the changed properties. Use this for: editing swap request details, adjusting station filters, or any multi-step form where users expect Undo (Ctrl+Z) support.

**Code Example:**
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

**Theory:** Unhandled exceptions are bugs that escaped all `try/catch` blocks. In MAUI, they can crash the app with no recovery. Three global handlers catch different categories: (1) `AppDomain.CurrentDomain.UnhandledException` catches exceptions from **non-UI threads** (background services, Task.Run). (2) `TaskScheduler.UnobservedTaskException` catches exceptions from **fire-and-forget tasks** (tasks that faulted but were never awaited). (3) WinUI's `Application.UnhandledException` catches **UI thread exceptions** (XAML binding errors, dispatcher errors). The strategy: (a) log the exception with full details (stack trace, device info, timestamp), (b) optionally display a user-friendly "Something went wrong" message, (c) on Windows, set `e.Handled = true` to prevent the app from crashing. On mobile, the OS typically terminates the app anyway on unhandled exceptions — the handler's main job is logging the crash for debugging.

**Code Example:**
```csharp
// MauiProgram.cs
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    
    AppDomain.CurrentDomain.UnhandledException += (s, e) =>
    {
        var ex = e.ExceptionObject as Exception;
        File.WriteAllText("crash.log", $"{DateTime.UtcNow}: {ex}");
    };

    TaskScheduler.UnobservedTaskException += (s, e) =>
    {
        File.WriteAllText("task_crash.log", $"{DateTime.UtcNow}: {e.Exception}");
        e.SetObserved();
    };

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

**Theory:** Biometric authentication (fingerprint, Face ID, Windows Hello) provides a **convenient, secure** second-factor authentication. The flow: (1) after initial password login, store the JWT token in `SecureStorage`. (2) On subsequent launches, check if biometrics are available via `Biometrics.Default.CanAuthenticateAsync()` (device must have enrolled fingerprint/Face ID/Windows Hello). (3) Show the OS-native biometric prompt — the user authenticates with their fingerprint/face. (4) On success, read the stored token from `SecureStorage` and proceed; on failure, fall back to password login. The security model: the biometric prompt is OS-controlled, so the app never sees the fingerprint data — it only gets a success/failure result. The token is stored in `SecureStorage` separately (encrypted at rest), decoupled from biometrics. This is a **second factor for convenience** — the biometric prompt proves the user is physically present and authorizes token access.

**Code Example:**
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

**Theory:** Infinite scroll loads data in pages as the user scrolls down, rather than loading all items at once. `RemainingItemsThreshold` tells `CollectionView` to fire `RemainingItemsThresholdReachedCommand` when the remaining unloaded items count reaches the threshold (e.g., 5 items from the end). This triggers `LoadMoreCommand`, which fetches the next page and appends it to the `ObservableCollection`. Key design: (1) use `IsBusy` guard to prevent duplicate loads — if the first page hasn't finished, don't fire a second load. (2) `HasMorePages` stops loading when the API returns an empty page. (3) The API must support pagination (`?page=N&size=20`). For an EV app, infinite scroll works well for: nearby stations list, swap history, notification history.

**Code Example:**
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

**Theory:** Debounce delays the execution of an action until after a specified quiet period — if the action is triggered again during the quiet period, the timer resets. This prevents firing an API call on every keystroke (which would overwhelm the server and cause a poor UX with flickering results). Implementation: each keystroke in the search field triggers `OnSearchQueryChanged` (via `[ObservableProperty]`). The method cancels any pending search (via `CancellationTokenSource.Cancel()`), creates a new token, and starts a 300ms delay. If 300ms pass without another keystroke, the search fires. If a new keystroke arrives before 300ms, the previous `CancellationToken` cancels the pending search, and a new delay starts. The 300ms delay is the standard for "fast but not too fast" — it feels responsive while preventing excessive calls.

**Code Example:**
```csharp
private CancellationTokenSource? _searchCts;

partial void OnSearchQueryChanged(string value)
{
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();
    var token = _searchCts.Token;

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

**Theory:** Caching prevents redundant network requests for data that doesn't change frequently. The **Decorator pattern** wraps the real `IApiService` — `CachedApiService` has the same interface but checks an in-memory cache before calling the real `IApiService`. On cache hit, return cached data instantly (no network). On cache miss, call the real service, store the result with a TTL, and return it. `MemoryCache` (from `Microsoft.Extensions.Caching.Memory`) provides expiration, size limits, and thread safety. For an EV app, cache: nearby stations list (changes infrequently, TTL 5 min), station details (TTL 10 min), user profile (TTL 30 min). Do NOT cache: swap request status (must be real-time), wallet balance (must be accurate). Cache invalidation is the hardest problem — use short TTLs and manual refresh (pull-to-refresh clears cache for the refreshed endpoint).

**Code Example:**
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
