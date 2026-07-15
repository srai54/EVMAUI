# MAUI Data Flow Explained — For Web Developers (MVC Background)

> You know ASP.NET MVC: Controller → Business Layer → Repository → Database.
> Now learn MAUI MVVM: View → ViewModel → Service → API → API Controller → BL → Repository → DB.

---

## 1. The Big Picture: MVC vs MAUI (MVVM)

### MVC (Web — What You Know)

```
Browser                    Server
┌──────┐   HTTP Request    ┌──────────────────────────────────────┐
│ View │ ←──────────────── │ Controller ← Business Layer ← Repo ← DB │
│(Razor)│ ────────────────→│ (C#)         (Services)    (EF Core)  │
└──────┘   HTML Response   └──────────────────────────────────────┘
```

**Flow:** User clicks button → Browser sends HTTP → Controller receives → calls BL → BL calls Repo → Repo queries DB → returns data → BL processes → Controller picks View → renders HTML → sends to browser.

### MAUI (Native App — What We Have)

```
Mobile Device                          Server (ASP.NET API)
┌────────────────────────┐   HTTP     ┌──────────────────────────────┐
│ ┌──────┐   binds to   │  Request   │                              │
│ │ View │ ←─────────── │ ──────────→│ API Controller → BL → Repo → DB │
│ │(XAML)│ ────────────→│ ←──────────│   (C#)        (Srvc) (EF Core)│
│ └──────┘   properties │  JSON Resp └──────────────────────────────┘
│    ↑ ↓                │
│ ┌──────────┐          │
│ │ViewModel │ ←→ Services (IAuthService, IApiService, etc.)
│ └──────────┘          │
│    ↑ ↓                │
│ ┌──────────┐          │
│ │  Models  │          │
│ └──────────┘          │
└────────────────────────┘
```

**Key Insight:** The MAUI app is BOTH the Browser AND the Controller from MVC. It renders UI (like browser) AND handles user actions (like Controller). But the actual business logic and database live on a separate API server.

---

## 2. Layer-by-Layer Comparison

| MVC (Web) | MAUI (Mobile) | What It Does |
|-----------|---------------|--------------|
| **View** (Razor `.cshtml`) | **View** (XAML `.xaml`) | Renders UI to user |
| **Controller** (action methods) | **ViewModel** (commands + properties) | Handles user actions, manages state |
| **Model** (ViewModels, DTOs) | **Model** (same concept) | Data containers shared between layers |
| **Business Layer** (Services) | **Services** (AuthService, etc.) | Business logic, orchestration |
| **Repository** (EF Core) | **ApiService** (HttpClient) | Data access — talks to DB/API |
| **Database** (SQL Server) | **API Server** (ASP.NET Core) + **SQLite** (local cache) | Data persistence |

---

## 3. Data Flow: Login Example (End to End)

Let's trace ONE user action — clicking "Login" — from both perspectives:

### MVC Flow (Web)

```
1. Browser renders Login.cshtml (HTML form)
2. User types username/password, clicks Submit
3. Browser sends POST /Account/Login HTTP request
4. AccountController.Login() receives request
5. Controller calls _authService.LoginAsync(username, password)
6. AuthService calls _userRepo.FindByUsername(username)
7. UserRepo queries DB: SELECT * FROM Users WHERE Username = @u
8. DB returns user row
9. AuthService verifies password hash
10. Controller sets auth cookie
11. Controller returns RedirectToAction("Index", "Dashboard")
12. Browser receives 302 redirect, follows to /Dashboard
13. DashboardController.Index() renders Dashboard.cshtml
```

### MAUI Flow (Mobile)

```
┌─── MAUI APP ───────────────────────────────────────────────────────┐
│                                                                    │
│ 1. LoginPage.xaml renders <Entry> and <Button>                    │
│                                                                    │
│ 2. User types in Entry fields (bound to ViewModel properties)     │
│    Text="{Binding Username}" ← two-way binding                    │
│                                                                    │
│ 3. User taps "Login" Button (bound to Command)                    │
│    Command="{Binding LoginCommand}"                               │
│                                                                    │
│ 4. LoginViewModel.LoginAsync() executes                           │
│    [RelayCommand] async Task LoginAsync()                         │
│    {                                                               │
│        IsBusy = true;  ← UI shows spinner (binding updates)       │
│                                                                    │
│ 5. ViewModel calls _authService.LoginAsync(Username, Password)    │
│                                                                    │
│ 6. AuthService calls _apiService.PostAsync<AuthResponse>(         │
│        "/api/auth/login", loginRequest)                           │
│                                                                    │
│ 7. ApiService serializes to JSON, sends HTTP POST                 │
│    HttpClient.PostAsJsonAsync("http://localhost:5238/...", data)  │
│                                                                    │
├── NETWORK ────────────────────────────────────────────────────────┤
│                                                                    │
│ 8. API AuthController.Login() receives request                    │
│    [HttpPost("login")] public async Task<IActionResult> Login(...) │
│                                                                    │
│ 9. API AuthService verifies credentials (same as MVC step 6-9)   │
│                                                                    │
│ 10. API returns JSON response: { "token": "eyJ...", "user": {...}}│
│                                                                    │
├── BACK TO MAUI APP ───────────────────────────────────────────────┤
│                                                                    │
│ 11. ApiService deserializes JSON → AuthResponse object            │
│     JsonSerializer.Deserialize<AuthResponse>(body)                 │
│                                                                    │
│ 12. AuthService stores token in SecureStorage                     │
│     SecureStorage.SetAsync("auth_token", token)                   │
│                                                                    │
│ 13. AuthService sets CurrentUser                                   │
│     this.CurrentUser = authResponse.User                           │
│                                                                    │
│ 14. LoginViewModel receives control back                           │
│     IsBusy = false;  ← UI hides spinner (binding updates)         │
│                                                                    │
│ 15. ViewModel navigates to Dashboard page                         │
│     Shell.Current.GoToAsync("//dashboard")                        │
│                                                                    │
│ 16. DashboardPage.xaml appears — DashboardViewModel loads data     │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

---

## 4. MVVM Explained (with MVC Comparison)

### MVC: Model-View-Controller

```
┌──────────┐     ┌──────────┐     ┌──────────┐
│  Model   │     │   View   │     │Controller│
│ (Data)   │ ←── │ (Razor)  │ ←── │ (C#)     │
│ DB data  │     │ HTML     │     │ Handles  │
│ Entities │     │ Template │     │ Requests │
└──────────┘     └──────────┘     └──────────┘
```

- **Model:** Data from DB (User, Product, Order)
- **View:** Razor template that renders HTML (`.cshtml`)
- **Controller:** Receives HTTP request, calls services, picks View

### MVVM: Model-View-ViewModel

```
┌──────────┐     ┌──────────┐     ┌──────────────┐
│  Model   │     │   View   │     │  ViewModel   │
│ (Data)   │ ←── │ (XAML)   │ ←── │ (C# Class)   │
│ DTOs     │     │ UI       │     │ Properties + │
│ Entities │     │ Controls │     │ Commands     │
└──────────┘     └──────────┘     └──────────────┘
                      ↑
              Data Binding (automatic!)
```

### Direct Comparison

| MVC Concept | MVVM Equivalent | Why Different |
|-------------|----------------|---------------|
| **Controller** action method | **ViewModel** `[RelayCommand]` method | Controller receives HTTP request. ViewModel is bound to Button click. Both handle "user wants to do something." |
| **Controller** sets `ViewBag.Message` | **ViewModel** sets `[ObservableProperty] string _message` | Both pass data to the View. But MVVM is strongly-typed and automatic (no magic strings). |
| **Controller** returns `View(model)` | **ViewModel** sets properties, View auto-updates | In MVC, you explicitly pass a model to the View. In MVVM, the View "watches" the ViewModel and updates automatically via data binding. |
| **Controller** calls `BL.LoginAsync()` | **ViewModel** calls `_authService.LoginAsync()` | Same pattern — both orchestrate business logic. But ViewModel does it from the client side. |
| **Razor View** `@Model.Name` | **XAML View** `{Binding Name}` | Same purpose — display data. XAML binding is two-way (changes flow both directions). |
| **Model** (`User.cs`) | **Model** (`UserModel.cs`) | Same concept — data containers. |
| **`RedirectToAction`** | **`Shell.Current.GoToAsync("//dashboard")`** | Both navigate to another page. |

---

## 5. Project Structure: MVC vs MAUI

### MVC Web App Structure
```
/Controllers/
    AccountController.cs
    DashboardController.cs
/Services/
    AuthService.cs
    UserService.cs
/Repositories/
    UserRepository.cs
/Models/
    User.cs
    LoginViewModel.cs
/Views/
    Account/
        Login.cshtml
    Dashboard/
        Index.cshtml
/Data/
    AppDbContext.cs
```

### MAUI App Structure (This Project)
```
/Views/                  ← Like /Views/ in MVC
    LoginPage.xaml       ← Like Login.cshtml
    DashboardPage.xaml   ← Like Dashboard/Index.cshtml

/ViewModels/             ← Like /Controllers/ + some of /Services/
    LoginViewModel.cs    ← Like AccountController + Login logic
    DashboardViewModel.cs

/Services/               ← Like /Services/ in MVC
    AuthService.cs       ← Same concept
    ApiService.cs        ← Like Repository (talks to external data source)

/Models/                 ← Like /Models/
    UserModel.cs         ← Same concept
    AuthResponse.cs
    StationModel.cs

/Helpers/
    Constants.cs         ← Like Web.config/appsettings.json
```

**Key difference:** In MVC, Controller, BL, and Repository ALL run on the server. In MAUI, View+ViewModel+Services run on the phone, and the API (Controller+BL+Repo) runs on a separate server.

---

## 6. The Mindset Shift

### What stays the same:

| Concept | MVC | MAUI |
|---------|-----|------|
| Separation of concerns | Controller/Services/Repo | ViewModel/Services/ApiService |
| Dependency Injection | `Startup.cs` → `AddScoped<IService, Service>()` | `MauiProgram.cs` → `AddSingleton<IService, Service>()` |
| Async/await | `await _repo.FindAsync(id)` | `await _api.GetAsync<T>("/endpoint")` |
| Models/DTOs | `public class User { get; set; }` | Same |
| EF Core | On the server side | Only on API side (same as what you know) |
| JSON serialization | `JsonResult` / `System.Text.Json` | Same |

### What changes:

| Concept | MVC | MAUI |
|---------|-----|------|
| **Where logic runs** | Server (your code is safe) | **Phone** (user's device — security matters!) |
| **Data access** | `_repo.FindAsync()` → direct DB query | `_api.GetAsync()` → HTTP call to API (no direct DB) |
| **UI updates** | Controller sets `ViewBag`, Razor renders | ViewModel sets property → **automatic** UI update via binding |
| **State management** | Session, cookies, TempData | ViewModel properties (in-memory), SecureStorage (persistent) |
| **Navigation** | URL routing (`/Account/Login`) | Shell routing (`//login`) |
| **Page lifetime** | Request → Response (milliseconds) | Page stays open for minutes/hours (lifecycle matters!) |

---

## 7. Real Code Comparison: Login Action

### MVC Controller (Server-Side)
```csharp
public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.LoginAsync(model.Username, model.Password);

        if (result.Success)
        {
            // Set auth cookie
            await SignInAsync(result.User);
            return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError("", "Invalid credentials");
        return View(model);
    }
}
```

### MAUI ViewModel (Client-Side)
```csharp
public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    // Constructor injection — same as MVC controller!
    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [ObservableProperty]  // ← New: auto-generates property + event
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [RelayCommand]  // ← New: turns method into ICommand for Button binding
    async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            await ShowAlertAsync("Validation", "Enter credentials");
            return;  // ← Like ModelState.IsValid check
        }

        IsBusy = true;  // ← Shows loading spinner via binding

        try
        {
            await _authService.LoginAsync(Username, Password);
            // ← On success: navigate (like RedirectToAction)
            await Shell.Current.GoToAsync("//dashboard");
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Error", ex.Message);
            // ← Like ModelState.AddModelError
        }
        finally
        {
            IsBusy = false;
        }
    }
}
```

### MAUI View (XAML) — The "Razor Template"
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:vm="clr-namespace:EVSwap.Mobile.ViewModels"
             x:Class="EVSwap.Mobile.Views.LoginPage"
             x:DataType="vm:LoginViewModel">  <!-- ← Like @model in Razor -->

    <VerticalStackLayout Padding="20">
        <!-- Like @Html.TextBoxFor(m => m.Username) -->
        <Entry Placeholder="Username"
               Text="{Binding Username}" />     <!-- Two-way binding -->

        <Entry Placeholder="Password"
               Text="{Binding Password}"
               IsPassword="True" />

        <!-- Like <input type="submit"> bound to action -->
        <Button Text="Login"
                Command="{Binding LoginCommand}" />  <!-- ← Calls LoginAsync -->

        <!-- Shows when IsBusy = true (like a loading spinner in MVC) -->
        <ActivityIndicator IsRunning="{Binding IsBusy}"
                           IsVisible="{Binding IsBusy}" />
    </VerticalStackLayout>
</ContentPage>
```

---

## 8. Service Layer Comparison

### MVC: Service calls Repository
```csharp
public class AuthService : IAuthService
{
    private readonly IUserRepository _repo;  // Injected

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var user = await _repo.FindByUsernameAsync(username);  // Direct DB query
        if (user is null) return AuthResult.Failure("User not found");

        var valid = BCrypt.Verify(password, user.PasswordHash);
        if (!valid) return AuthResult.Failure("Invalid password");

        var token = GenerateJwtToken(user);
        return AuthResult.Success(token, user);
    }
}
```

### MAUI: Service calls ApiService (which calls HTTP → API)
```csharp
public class AuthService : IAuthService
{
    private readonly IApiService _api;  // ← Instead of IUserRepository

    public async Task<AuthResponse> LoginAsync(string username, string password)
    {
        // Instead of _repo.FindByUsername(), we make an HTTP call
        var response = await _api.PostAsync<AuthResponse>("/api/auth/login", new
        {
            Username = username,
            Password = password
        });

        if (response is null)
            throw new Exception("Login failed");

        // Store token (like setting auth cookie)
        await SecureStorage.Default.SetAsync("auth_token", response.Token);
        CurrentUser = response.User;

        return response;
    }
}
```

### ApiService = MAUI's "Repository"
```csharp
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        // This is like _repo.Query() but over HTTP
        var response = await _httpClient.PostAsJsonAsync(endpoint, data);
        var body = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
```

---

## 9. Complete Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          MAUI APP (Mobile)                              │
│                                                                          │
│  ┌──────────────┐      ┌──────────────────┐      ┌─────────────────┐   │
│  │   XAML View  │      │    ViewModel     │      │    Services     │   │
│  │              │      │                  │      │                 │   │
│  │ Button taps  │─ ─ → │ Command executes │─ ─ → │ AuthService     │   │
│  │ Entry input  │← ─ ─ │ Properties update│      │   .LoginAsync() │   │
│  │ List scrolls │      │ IsBusy changes   │      │        ↓        │   │
│  └──────────────┘      └──────────────────┘      │ ApiService      │   │
│        ↑ ↓ (binding)                             │  .PostAsync()   │   │
│  ┌──────────────────────────────────────────┐    │        ↓        │   │
│  │         ObservableObject                 │    │ HttpClient      │   │
│  │  [ObservableProperty] + [RelayCommand]   │    │  .PostAsJson()  │   │
│  └──────────────────────────────────────────┘    └────────┬────────┘   │
│                                                            │           │
└────────────────────────────────────────────────────────────┼───────────┘
                                                             │ HTTP POST
                                                             │ JSON body
                                                             ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                        ASP.NET API (Server)  ← THIS IS YOUR MVC WORLD    │
│                                                                           │
│  ┌──────────────────┐      ┌──────────────────┐      ┌──────────────┐   │
│  │ API Controller   │      │  Business Layer  │      │  Repository  │   │
│  │                  │      │                  │      │              │   │
│  │ AuthController   │─ ─ → │ AuthService      │─ ─ → │ UserRepo     │   │
│  │  .Login()        │      │  .LoginAsync()   │      │  .FindBy()   │   │
│  │                  │      │                  │      │       ↓      │   │
│  └──────────────────┘      └──────────────────┘      │  EF Core     │   │
│        ↑ ↓ (JSON)                                     │  DbContext   │   │
│  ┌──────────────────────────────────────────────┐    │       ↓      │   │
│  │    Program.cs (MVC Middleware Pipeline)       │    │  SQL Server  │   │
│  │  app.MapControllers(); app.UseAuthentication()│    └──────────────┘   │
│  └──────────────────────────────────────────────┘                       │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## 10. Key Takeaways for a Web Developer

### 1. "Where is my Controller?"
In MAUI, the **ViewModel** is your Controller. Instead of receiving HTTP requests, it receives user actions through **Commands** (Button taps, list selections).

### 2. "Where is my Repository?"
The **ApiService** is your Repository. Instead of `_repo.FindAsync(id)`, you call `_api.GetAsync<T>("/endpoint")`. Instead of direct SQL, you make HTTP requests to your existing API.

### 3. "Where is my Business Layer?"
Your **Services** (AuthService, StationService, etc.) are your Business Layer — same concept, same design patterns (DI, interfaces, separation of concerns).

### 4. "Where is my Razor View?"
XAML is your Razor View. Instead of `@Model.Name`, you write `{Binding Name}`. Both render data from a model.

### 5. "Where is my `ModelState.IsValid`?"
You still validate — but manually in the ViewModel: `if (string.IsNullOrEmpty(Username)) return;` instead of data annotations + `ModelState.IsValid`.

### 6. "Where is my `RedirectToAction`?"
`Shell.Current.GoToAsync("//dashboard")` is your redirect. Instead of URL-based routing, MAUI uses named routes.

### 7. "The API is the same!"
The ASP.NET API side (Controllers, Services, Repositories, EF Core, DbContext, Migrations) is **exactly** what you already know. The API in this project uses the same patterns as any MVC web app.

### 8. Quick Reference Card

| You Want To... | In MVC Web App | In MAUI Mobile App |
|----------------|---------------|-------------------|
| Show data on page | `ViewBag.Data = data;` → `@ViewBag.Data` | `Data = value;` → `{Binding Data}` (auto!) |
| Handle button click | `[HttpPost] Action()` → form submit | `[RelayCommand] Method()` → `Command="{Binding MethodCommand}"` |
| Call DB | `_repo.FindAsync(id)` | `_api.GetAsync<T>("/api/endpoint")` |
| Navigate to page | `return RedirectToAction("Index")` | `await Shell.GoToAsync("//route")` |
| Pass data to page | `return View(model)` | `[QueryProperty]` or navigation params |
| Show validation error | `ModelState.AddModelError("", "msg")` | `await DisplayAlert("Error", "msg")` |
| Start app | `Program.cs` → `app.Run()` | `MauiProgram.cs` → `builder.Build()` |
| Register services | `builder.Services.AddScoped<IService, Service>()` | `builder.Services.AddSingleton<IService, Service>()` |
| Configuration | `appsettings.json` | `appsettings.json` + `Constants.cs` |
| API server | Built into same project | Separate project (`EVSwap.API`) — the part you already know! |

---

> **Bottom line:** The MAUI app is a "smart browser" that renders UI and handles user interactions locally, then calls your familiar ASP.NET API for data. The API side (Controllers → Services → Repositories → EF Core → DB) is **identical** to what you've been doing in MVC for 3 years. You already know 50% of this stack.
