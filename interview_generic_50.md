# 50+ Generic .NET MAUI Interview Questions — Crack Any Interview

> **Not project-specific.** These are standalone questions covering C#, .NET MAUI, MVVM, DI, async, REST, EF Core, design patterns, and more. Master these and you can handle any .NET MAUI interview.

---

## How to Use This Guide

Each question has two parts:
- **Theory** — The conceptual foundation. Understand the "why" behind the concept.
- **Code Example** — Practical implementation. See how the theory applies in real code.

Learn the theory first, then study the code. If you can explain both, you're interview-ready.

---

## C# Language & .NET Runtime

**Q1: Explain the difference between `value types` and `reference types` in C#.**

**Answer:**

**Theory:** In C#, types are divided into two categories based on how they store data in memory. **Value types** store the actual data directly on the stack. When you assign a value type to another variable, a COPY of the data is made — the two variables are completely independent. **Reference types** store a REFERENCE (memory address) to the data on the heap. When you assign a reference type, the REFERENCE is copied, not the data — both variables point to the SAME object in memory. This distinction affects performance (stack is faster than heap), memory management (heap needs garbage collection), and behavior (value types are safe from side effects, reference types are not). The `struct` keyword creates value types; `class` creates reference types. Choose `struct` for small, immutable data that represents a single value (like a `Point` or `Color`). Choose `class` for objects with identity, behavior, and mutable state.

**Code Example:**
```csharp
public struct Point { public int X; public int Y; }   // Value type
public class Person { public string Name = ""; }       // Reference type

void Demonstrate()
{
    // Value type: independent copies
    Point a = new Point { X = 1, Y = 2 };
    Point b = a;
    b.X = 99;
    Console.WriteLine(a.X); // 1 — a is unchanged, b is a copy

    // Reference type: shared reference
    Person p1 = new Person { Name = "Alice" };
    Person p2 = p1;
    p2.Name = "Bob";
    Console.WriteLine(p1.Name); // "Bob" — p1 and p2 point to same object
}
```

---

**Q2: What is boxing and unboxing? Why should you avoid it?**

**Answer:**

**Theory:** Boxing is the process of converting a VALUE type (like `int`, `bool`, `struct`) to a REFERENCE type (like `object` or an interface). The CLR creates a new object on the heap, copies the value into it, and returns a reference. Unboxing is the reverse — extracting the value from the heap object back to the stack. Both operations have performance costs: boxing requires heap allocation (triggers GC pressure), and unboxing requires a type check and copy. Hidden boxing is especially dangerous — it happens automatically when you pass value types to non-generic collections (`ArrayList`), use string concatenation with value types, or call `ToString()` on a nullable struct. Modern C# avoids boxing through generics (`List<T>` instead of `ArrayList`), which keep types as value types without conversion.

**Code Example:**
```csharp
int number = 42;
object boxed = number;        // Boxing: int (stack) → object (heap) — ALLOCATION!
int unboxed = (int)boxed;     // Unboxing: object (heap) → int (stack) — TYPE CHECK + COPY

// Hidden boxing — these look innocent but allocate!
ArrayList list = new ArrayList();
list.Add(42);                 // Boxing! ArrayList is non-generic
int sum = (int)list[0] + (int)list[1]; // Unboxing! Twice!

// Avoid with generics:
List<int> list2 = new List<int>();
list2.Add(42);                // No boxing — List<int> keeps int as value type
```

---

**Q3: What is the difference between `String` and `StringBuilder`?**

**Answer:**

**Theory:** Strings in C# are IMMUTABLE — once created, they cannot be changed. Every operation that appears to modify a string (like `+` concatenation, `Replace()`, `Substring()`) actually creates a NEW string object on the heap, leaving the original untouched. This is by design: immutability makes strings thread-safe, enables string interning (reusing identical string literals), and makes them safe to use as dictionary keys. However, immutability becomes a performance problem in loops — each concatenation allocates a new string, copies the old content, adds the new content, and discards the old string for garbage collection. `StringBuilder` solves this by maintaining a MUTABLE internal buffer (a `char[]` array) that grows as needed. Operations like `Append()`, `Insert()`, and `Replace()` modify the buffer in-place without creating intermediate strings. Use `String` for fixed text and occasional concatenation (< 5-10 operations). Use `StringBuilder` for loops, building large strings dynamically, or any situation with more than a few concatenations.

**Code Example:**
```csharp
// String concatenation — creates 10000 intermediate objects
string s = "";
for (int i = 0; i < 10000; i++)
    s += i;  // Each iteration: allocate new string, copy old data, append, discard old

// StringBuilder — single buffer
var sb = new StringBuilder();
for (int i = 0; i < 10000; i++)
    sb.Append(i);  // Mutates the same internal buffer
string result = sb.ToString();  // Single allocation at the end

// When to use each:
// Use String: "Hello " + name + "!", $"Welcome {name}", few small concatenations
// Use StringBuilder: loop building, CSV/XML generation, large string construction
```

---

**Q4: Explain `delegates`, `events`, and `lambda expressions`.**

**Answer:**

**Theory:** A **delegate** is a type-safe function pointer — it defines a signature (return type + parameters) and can hold a reference to any matching method. Delegates enable callback mechanisms and the Observer pattern. An **event** is a wrapper around a delegate that restricts access — only the declaring class can INVOKE (fire) the event, but any class can SUBSCRIBE (add/remove handlers). This encapsulation prevents external code from accidentally clearing all subscribers or firing the event from outside. A **lambda expression** is an anonymous inline method — a concise way to create delegates without defining a separate method. Lambdas capture variables from their enclosing scope (closures), which means they can access local variables even after the method exits. The progression: named methods → anonymous methods (C# 2.0) → lambda expressions (C# 3.0) — each step adds more conciseness and flexibility.

**Code Example:**
```csharp
// Delegate: defines the "shape" of a callable method
public delegate void ProgressHandler(int percent);

// Event: encapsulated delegate — only Downloader can Invoke it
public class Downloader
{
    public event ProgressHandler? ProgressChanged;  // Event (not a field!)

    public void Download()
    {
        for (int i = 0; i <= 100; i += 10)
        {
            ProgressChanged?.Invoke(i);  // Only THIS class can invoke
        }
    }
}

// Lambda: inline method that subscribes to the event
var downloader = new Downloader();
downloader.ProgressChanged += (percent) =>      // Lambda expression
    Console.WriteLine($"Progress: {percent}%");

// Behind the scenes, the lambda captures the closure:
// It can access local variables even after they go out of scope
```

---

**Q5: What is the difference between `abstract class` and `interface`?**

**Answer:**

**Theory:** Both enable abstraction (separating "what" from "how"), but they serve different purposes. An **abstract class** represents an "is-a" relationship — derived classes SHARE a common base with some implementation. It can have fields, constructors, method implementations, and access modifiers. Use it when subclasses share state or behavior. An **interface** represents a "can-do" capability — a contract that any class can implement regardless of inheritance hierarchy. It historically had only method signatures (C# 8+ allows default implementations). A class can implement MULTIPLE interfaces (solving the diamond problem), but can only inherit ONE abstract class. The choice between them follows the **I** in SOLID (Interface Segregation): prefer small, focused interfaces for capabilities; use abstract classes for shared base logic. A practical rule: if the relationship is "a Dog IS AN Animal" → abstract class. If the relationship is "a Dog CAN Fly" → interface.

**Code Example:**
```csharp
// Abstract class: shared base with implementation ("is-a")
public abstract class Vehicle
{
    public string VIN { get; set; } = "";        // Shared state (field)
    public abstract void Start();                 // Must override
    public virtual void Stop()                    // Optional override
    {
        Console.WriteLine("Vehicle stopped");
    }
}

// Interface: capability contract ("can-do")
public interface IChargeable
{
    void Charge(int minutes);  // No implementation, just contract
}

public interface IConnectable
{
    void Connect();            // Another capability
}

// Class implements ONE abstract class + MULTIPLE interfaces
public class ElectricCar : Vehicle, IChargeable, IConnectable
{
    public override void Start() => Console.WriteLine("EV starts silently");
    public void Charge(int m) => Console.WriteLine($"Charging {m}min");
    public void Connect() => Console.WriteLine("Connected to charger");
}
```

---

**Q6: Explain the `yield` keyword.**

**Answer:**

**Theory:** `yield return` enables **deferred execution** — values are produced one at a time, on-demand, rather than computing the entire collection upfront. When the compiler sees `yield`, it generates a **state machine class** that implements `IEnumerable<T>` and `IEnumerator<T>`. This state machine tracks the current position in the method. Each call to `MoveNext()` resumes execution from where it left off, runs until the next `yield return`, and then pauses again. The key benefit is **lazy evaluation**: the caller controls how many items to consume, and items that are never requested are never produced. This is ideal for infinite sequences, large datasets where you only need the first N items, or expensive computations that should be deferred. The trade-off is that the state machine has some overhead (one object allocation per enumeration). Use `yield` when you don't know the full result set size, when the caller might short-circuit (`.Take(5)`), or when generating values is expensive.

**Code Example:**
```csharp
// Lazy sequence — no allocation, no computation until iterated
public IEnumerable<int> GetEvenNumbers(int max)
{
    for (int i = 0; i <= max; i += 2)
    {
        yield return i;  // Pauses here, returns one value
    }
}

// The caller controls consumption:
var evens = GetEvenNumbers(1000000);  // No computation yet!
var firstFive = evens.Take(5);        // Only computes 5 values
foreach (var n in firstFive)
    Console.WriteLine(n);             // Prints: 0, 2, 4, 6, 8

// Behind the scenes, the compiler generates:
// - A hidden class implementing IEnumerator<int>
// - MoveNext() method with a switch(state) that jumps to the right yield point
// - Current property that returns the last yielded value
```

---

**Q7: How do you create and use custom exceptions?**

**Answer:**

**Theory:** Custom exceptions allow you to create meaningful, domain-specific error types that convey more information than generic `Exception`. They should inherit from `Exception` (or a more specific base like `InvalidOperationException`). A well-designed custom exception follows these principles: (1) End the class name with "Exception", (2) Implement the three standard constructors (parameterless, message, message+innerException), (3) Add custom properties for additional context, (4) Mark the class as `[Serializable]` for cross-boundary scenarios (AppDomain, remoting). The throw vs. throw-ex distinction is crucial: `throw` preserves the original stack trace; `throw ex` resets the stack trace to the throw point, losing the original call site. Exception filters (`when` clause) allow catching exceptions conditionally without losing stack information. Use custom exceptions for domain-specific error conditions — don't use them for control flow.

**Code Example:**
```csharp
[Serializable]
public class ApiException : Exception
{
    public int StatusCode { get; }
    public string? ResponseBody { get; }

    // Three standard constructors
    public ApiException() { }
    public ApiException(string message) : base(message) { }
    public ApiException(string message, Exception inner) : base(message, inner) { }

    // Domain-specific constructor
    public ApiException(int statusCode, string? responseBody, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}

// Usage with exception filters:
try
{
    await _api.PostAsync(endpoint, data);
}
catch (ApiException ex) when (ex.StatusCode == 401)
{
    // Specific handling — re-authenticate
    await _auth.RefreshTokenAsync();
    await _api.PostAsync(endpoint, data);
}
catch (HttpRequestException ex)
{
    // Network error — retry
    _logger.LogWarning("Network error, will retry: {Error}", ex.Message);
}
catch (Exception ex)
{
    // Unexpected — log and rethrow preserving stack
    _logger.LogError(ex, "Unexpected error");
    throw;
}
```

---

**Q8: What is `LINQ`? Explain deferred vs immediate execution.**

**Answer:**

**Theory:** LINQ (Language Integrated Query) is a set of methods that provide query capabilities directly in C#. It has two syntaxes: query syntax (SQL-like: `from u in users where u.IsActive select u`) and method syntax (fluent: `users.Where(u => u.IsActive)`). The critical concept is **deferred execution vs immediate execution**. Deferred execution means the query is NOT executed when defined — it's executed when the result is iterated. This allows chaining multiple operations without performance penalty; each operation is composed into an expression tree that's evaluated only once. Immediate execution happens when you call methods like `.ToList()`, `.Count()`, `.First()`, `.Any()` — these force the query to execute and produce a result. Deferred is memory-efficient (no intermediate lists), but if you iterate the same query twice, it executes twice. Immediate execution caches the result. In EF Core, deferred execution means the SQL isn't sent to the database until the result is materialized, allowing the LINQ provider to build optimized SQL from the full expression tree.

**Code Example:**
```csharp
var query = db.Users
    .Where(u => u.IsActive)      // DEFERRED — just builds expression tree
    .OrderBy(u => u.Name)        // DEFERRED — still building query
    .Select(u => new { u.Id, u.Name }); // DEFERRED — no DB call yet

// At this point, NO SQL has been sent to the database.
// query is an IQueryable — it's an expression tree, not data.

// Immediate execution — triggers DB query:
var list = query.ToList();        // SQL: SELECT Id, Name FROM Users WHERE IsActive = 1 ORDER BY Name
var count = query.Count();        // SQL: SELECT COUNT(*) FROM Users WHERE IsActive = 1
var first = query.FirstOrDefault(); // SQL: SELECT TOP 1 Id, Name FROM Users WHERE IsActive = 1 ORDER BY Name

// Deferred methods: Where, Select, OrderBy, Skip, Take, GroupBy, Join
// Immediate methods: ToList, ToArray, ToDictionary, Count, Sum, First, FirstOrDefault, Any, All
```

---

**Q9: What are nullable reference types and how do they improve code safety?**

**Answer:**

**Theory:** Before C# 8, ALL reference types were implicitly nullable — a `string` could be `null`, and the compiler couldn't warn you. This was the source of the "billion-dollar mistake" — null reference exceptions. Nullable reference types (enabled with `#nullable enable`) allow you to explicitly mark which reference types CAN be null (`string?`) and which CANNOT (`string`). The compiler then performs static analysis to warn you when you might be dereferencing a null value. This shifts null checking from runtime (crashes) to compile-time (warnings). Key operators: `?.` (null-conditional — short-circuits if null), `??` (null-coalescing — provides default if null), `!` (null-forgiving — "I know this isn't null"). The feature doesn't affect runtime behavior — it's purely a compile-time analysis tool. It forces you to think about nullability in your API design and makes your contracts explicit.

**Code Example:**
```csharp
#nullable enable

public class User
{
    public string Name { get; set; } = "";    // NON-nullable — must be initialized
    public string? MiddleName { get; set; }    // Nullable — OK to be null
}

var user = new User { Name = "Alice" };         // MiddleName is null by default

// Compiler warning: user.Name COULD be null (but we initialized it, so it's fine)
// Compiler warning: user.MiddleName.Length — might be null!

// Safe access:
int? length = user.MiddleName?.Length;          // null-conditional: null if MiddleName is null
string displayName = user.MiddleName ?? "";     // null-coalescing: "" if MiddleName is null

// Null-forgiving operator (use sparingly):
int len = user.MiddleName!.Length;              // "I promise it's not null"

// Static analysis doesn't track across method calls:
if (user.MiddleName is not null)                // After this check, compiler knows it's safe
    Console.WriteLine(user.MiddleName.Length);  // No warning!
```

---

**Q10: Explain `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, and `IQueryable<T>`.**

**Answer:**

**Theory:** These four interfaces form a hierarchy of data access capabilities, each adding more functionality:

- **`IEnumerable<T>`** — The most basic. Represents a forward-only stream of elements. You can iterate it (with `foreach` or LINQ), but you CANNOT add, remove, or access by index. It uses deferred execution for LINQ methods. Best for: read-only iteration over any sequence.

- **`ICollection<T>`** — Extends `IEnumerable<T>` with mutation capabilities: `Add`, `Remove`, `Clear`, `Count`, `Contains`. Represents a general-purpose collection where you can both read and write. Best for: collections that need add/remove but not indexed access.

- **`IList<T>`** — Extends `ICollection<T>` with positional access: indexer (`[i]`), `Insert`, `RemoveAt`, `IndexOf`. Full random access and ordered operations. Best for: arrays, lists, and any collection needing indexed access.

- **`IQueryable<T>`** — Different from the above. Represents a query that can be translated to another language (like SQL). The LINQ provider (EF Core) builds an expression tree and translates it to SQL. Execution is deferred until materialized. Best for: database queries where you want the server to filter/sort before returning data.

The principle: **Program to the least power you need**. If you only need to read, accept `IEnumerable<T>`. If you need to add/remove, accept `ICollection<T>`. This gives callers maximum flexibility.

**Code Example:**
```csharp
// IEnumerable<T> — forward-only, read-only
public void PrintNames(IEnumerable<User> users)
{
    foreach (var user in users)      // Can only iterate
        Console.WriteLine(user.Name);
    // users[0] — ERROR: no indexer
    // users.Add(new User()) — ERROR: no Add method
}

// ICollection<T> — can modify
public void AddUsers(ICollection<User> users, User newUser)
{
    users.Add(newUser);              // Can add
    Console.WriteLine(users.Count);   // Can count
    users.Remove(existingUser);       // Can remove
    // users[0] — ERROR: no indexer (use IList if needed)
}

// IList<T> — indexed access
public void SortUsers(IList<User> users)
{
    users[0] = updatedUser;          // Index access
    users.Insert(0, newUser);         // Insert at position
    users.RemoveAt(users.Count - 1);  // Remove last
}

// IQueryable<T> — database query (not in-memory)
public IQueryable<User> GetActiveUsers(AppDbContext db)
{
    return db.Users
        .Where(u => u.IsActive)      // Builds SQL WHERE clause
        .OrderBy(u => u.Name);        // Builds SQL ORDER BY
    // NO SQL executed here — just building expression tree
}
```

---

## .NET MAUI Framework

**Q11: What is the difference between .NET MAUI and Xamarin.Forms?**

**Answer:**

**Theory:** Xamarin.Forms was Microsoft's first cross-platform UI framework (.NET Framework / Mono), while .NET MAUI is its successor (.NET 6+). The fundamental difference is that MAUI is built on the **unified .NET platform** — one BCL, one runtime, one SDK across all platforms. Xamarin had separate platform targets (MonoAndroid, Xamarin.iOS). MAUI introduces: (1) **Single project** — one `.csproj` targeting all platforms with a `Platforms/` folder for platform-specific code, instead of separate projects per platform. (2) **Handlers** (lightweight) instead of Renderers (heavy) — significantly better performance. (3) **Built-in DI** — no more `DependencyService`. (4) **Desktop support** — Windows and macOS, which Xamarin didn't support. (5) **Improved tooling** — Hot Reload, Live Visual Tree. The migration path from Xamarin to MAUI is well-documented, but requires updating namespaces, replacing renderers with handlers, and migrating DI.

**Code Example:**
```csharp
// Xamarin.Forms: Separate projects per platform
// XamarinApp.sln
// ├── XamarinApp (shared .NET Standard)
// ├── XamarinApp.Android (MonoAndroid)
// ├── XamarinApp.iOS (Xamarin.iOS)
// └── XamarinApp.UWP (UWP)

// .NET MAUI: Single project, single target framework
// MauiApp.sln
// └── MauiApp (single .csproj)
//     ├── Platforms/Android/
//     ├── Platforms/iOS/
//     ├── Platforms/Windows/
//     └── Platforms/MacCatalyst/

// Xamarin: DependencyService (service locator anti-pattern)
var service = DependencyService.Get<IAuthService>();

// MAUI: Built-in DI (proper constructor injection)
public class LoginViewModel
{
    private readonly IAuthService _auth;
    public LoginViewModel(IAuthService auth) => _auth = auth;
}
```

---

**Q12: Explain the MAUI page lifecycle.**

**Answer:**

**Theory:** The MAUI page lifecycle determines when your code runs during a page's existence. Understanding it is critical for: loading data at the right time, cleaning up resources to prevent memory leaks, and handling interruptions (phone calls, app switching). The sequence is: (1) **Constructor** — page object created, dependencies injected, `BindingContext` set. (2) **`OnNavigatedTo()`** — Shell navigation parameters received. (3) **`OnAppearing()`** — page becomes visible. This is the BEST place to load data because it fires every time the page appears (including when navigating back). (4) User interacts with the page. (5) **`OnDisappearing()`** — page is no longer visible. CLEANUP HERE: unsubscribe events, dispose timers, save draft data. (6) **`OnNavigatedFrom()`** — navigation away completed. (7) App background: **`OnSleep()`** triggered. (8) App resume: **`OnResume()`**. The distinction between `OnAppearing` and constructor is important: constructor fires ONCE per navigation, `OnAppearing` fires EVERY TIME the page becomes visible.

**Code Example:**
```csharp
public partial class DashboardPage : ContentPage
{
    private readonly IDispatcherTimer _refreshTimer;

    public DashboardPage(DashboardViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;             // Constructor: DI setup
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Called EVERY time page appears — load fresh data
        (BindingContext as DashboardViewModel)?.LoadCommand.Execute(null);

        // Start auto-refresh timer
        _refreshTimer = Application.Current!.Dispatcher.CreateTimer();
        _refreshTimer.Interval = TimeSpan.FromSeconds(30);
        _refreshTimer.Tick += OnTimerTick;
        _refreshTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // ALWAYS clean up — prevent memory leaks
        _refreshTimer?.Stop();
        _refreshTimer.Tick -= OnTimerTick;
    }
}
```

---

**Q13: How do you pass data between pages in MAUI?**

**Answer:**

**Theory:** Passing data between pages is one of the most common requirements. There are four main approaches, each suited to different scenarios:

1. **QueryProperty (Shell navigation)** — Best for simple parameters (IDs, strings). MAUI Shell sets the property automatically during navigation. Clean and decoupled, but limited to public settable properties.

2. **Static Singleton Service** — Best for globally shared state (current user, app settings). The service is registered as Singleton in DI, so all ViewModels get the same instance. No navigation coupling needed.

3. **WeakReferenceMessenger** — Best for loosely coupled communication between unrelated components. Uses the Publish/Subscribe pattern. The sender doesn't need to know about the receiver. The "Weak" part means subscribers can be garbage collected even if they don't unsubscribe (prevents memory leaks).

4. **Navigation Parameters (MAUI 8+)** — Clean dictionary-based approach using `IQueryAttributable`. More flexible than QueryProperty but requires implementing an interface.

**Code Example:**
```csharp
// Method 1: QueryProperty (attribute-based)
[QueryProperty(nameof(StationId), "id")]
public partial class StationDetailPage : ContentPage
{
    private int _stationId;
    public int StationId
    {
        get => _stationId;
        set
        {
            _stationId = value;
            (BindingContext as StationDetailViewModel)?.LoadStation(value);
        }
    }
}
// Navigation: await Shell.Current.GoToAsync($"stationdetail?id={station.Id}");

// Method 2: Shared Singleton (AuthService.CurrentUser)
public class AuthService : IAuthService
{
    public UserModel? CurrentUser { get; set; }  // Accessible from any ViewModel
}
// Usage: var user = _authService.CurrentUser;

// Method 3: WeakReferenceMessenger
public class StationSelectedMessage : ValueChangedMessage<StationModel>
{
    public StationSelectedMessage(StationModel value) : base(value) { }
}
// Send: WeakReferenceMessenger.Default.Send(new StationSelectedMessage(station));
// Receive: WeakReferenceMessenger.Default.Register<StationSelectedMessage>(this, (r, m) => { ... });
```

---

**Q14: What are `Handlers` in MAUI and how do they differ from `Renderers`?**

**Answer:**

**Theory:** Handlers and Renderers both map cross-platform MAUI controls (like `Entry`, `Button`) to their native platform equivalents (WinUI `TextBox`, Android `EditText`, iOS `UITextField`). **Renderers** (Xamarin.Forms) were full classes inheriting from `ViewRenderer<TView, TNative>` — they had their own lifecycle, property change management, and a heavy base class. Each renderer instance was dedicated to one control. **Handlers** (MAUI) use a **mapper-based architecture** — a lightweight dictionary maps property names to static methods that update the native control. Handlers are smaller, faster, and more composable. Multiple controls can share handler instances. The mapper pattern means adding a new property doesn't require creating a new renderer subclass — just add an entry to the mapper dictionary. For customization, you extend the mapper rather than subclass the handler. This is a significant performance improvement, especially for lists with many controls.

**Code Example:**
```csharp
// Xamarin Renderer (old way):
public class CustomEntryRenderer : ViewRenderer<Entry, MauiTextBox>
{
    protected override void OnElementChanged(ElementChangedEventArgs<Entry> e)
    {
        base.OnElementChanged(e);
        if (Control != null)
            Control.TextChanged += OnTextChanged;
    }
}

// MAUI Handler (new way):
public static class EntryCustomization
{
    public static void AddCustomMappings()
    {
        EntryHandler.Mapper.AppendToMapping("CustomEntry", (handler, entry) =>
        {
            if (entry is CustomEntry custom)
            {
#if WINDOWS
                handler.PlatformView.IsSpellCheckEnabled = false;
#elif ANDROID
                handler.PlatformView.InputType = Android.Text.InputTypes.TextFlagNoSuggestions;
#endif
            }
        });
    }
}
// Call in MauiProgram.cs: EntryCustomization.AddCustomMappings();
```

---

**Q15: How does `CollectionView` handle virtualization?**

**Answer:**

**Theory:** **UI virtualization** means only creating views for items that are VISIBLE on screen, not for all items in the data source. When you scroll, items that go off-screen are **recycled** — their views are repurposed for newly-visible items, updating the data but reusing the visual elements. This prevents creating thousands of views for a 10,000-item list. `CollectionView` does this automatically; `ListView` (legacy) also virtualizes but less efficiently. `VerticalStackLayout` with `BindableLayout.ItemsSource` does NOT virtualize — it creates ALL items upfront, which is fine for small lists (< 50 items) but disastrous for large ones. For optimal virtualization: (1) Keep the `ItemTemplate` visual tree shallow — deeply nested layouts hurt recycling. (2) Use compiled bindings (`x:DataType`) — reflection-based bindings are slower. (3) Use fixed item sizes when possible — variable sizes force the layout engine to measure every item. (4) Enable `RemainingItemsThreshold` for incremental loading (infinite scroll).

**Code Example:**
```xml
<!-- CollectionView with virtualization — good for 1000+ items -->
<CollectionView ItemsSource="{Binding Stations}"
                RemainingItemsThreshold="5"
                RemainingItemsThresholdReachedCommand="{Binding LoadMoreCommand}"
                x:DataType="vm:StationsViewModel">
    <CollectionView.ItemTemplate x:DataType="models:StationModel">
        <DataTemplate>
            <VerticalStackLayout Padding="10">
                <Label Text="{Binding Name}" FontSize="16" />
                <Label Text="{Binding Distance, StringFormat='{0:F1} km'}" />
            </VerticalStackLayout>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>

<!-- BindableLayout — NO virtualization, creates EVERY item upfront -->
<VerticalStackLayout BindableLayout.ItemsSource="{Binding SmallList}">
    <BindableLayout.ItemTemplate>
        <DataTemplate x:DataType="models:ItemModel">
            <Label Text="{Binding Name}" />
        </DataTemplate>
    </BindableLayout.ItemTemplate>
</VerticalStackLayout>
```

---

**Q16: What is `Shell` and why would you use it over `NavigationPage`?**

**Answer:**

**Theory:** `Shell` is MAUI's modern navigation framework. It provides **URI-based routing** (like ASP.NET routing for mobile), built-in flyout menus and tab bars, search integration, and visual consistency. `NavigationPage` is simpler — it's a **stack-based** navigation where you push and pop pages. The fundamental difference: Shell is **declarative** (define routes, Shell handles the rest), NavigationPage is **imperative** (you control the stack). Shell advantages: (1) URI navigation supports deep linking and back-navigation naturally. (2) Flyout and tab bar are built-in, no custom code. (3) Visual customization with templates. (4) Back button behavior is manageable. (5) Navigation parameters are strongly typed. Use Shell for any app with multiple pages, tabs, or flyout menus. Use NavigationPage for simple modal flows (wizards, sign-up sequences) where you need explicit stack control.

**Code Example:**
```csharp
// Shell: URI-based routing (like MVC routing)
// AppShell.xaml:
// <Shell>
//     <TabBar>
//         <ShellContent Route="dashboard" Title="Home" ContentTemplate="{DataTemplate views:DashboardPage}" />
//         <ShellContent Route="stations" Title="Stations" ContentTemplate="{DataTemplate views:StationsPage}" />
//     </TabBar>
// </Shell>

// Navigation with Shell:
await Shell.Current.GoToAsync("//dashboard");          // Absolute route
await Shell.Current.GoToAsync("stationdetail?id=123"); // Relative with params
await Shell.Current.GoToAsync("..");                   // Back

// NavigationPage: stack-based (simple modal)
await Navigation.PushAsync(new DetailPage(selectedItem));
await Navigation.PopAsync();

// When to use Shell:
// - App has 3+ tabs or a flyout menu
// - Need deep linking support
// - Consistent navigation UX across pages
// - Sharing the app (URI-based sharing)

// When to use NavigationPage:
// - Simple modal wizard (sign-up flow)
// - Full-screen detail without tab bar
// - Legacy Xamarin.Forms migration
```

---

**Q17: How do you implement custom fonts, images, and resources in MAUI?**

**Answer:**

**Theory:** MAUI has a simplified resource system compared to Xamarin. The key concept is **automatic resource processing** — you add files to specific folders in `Resources/`, and the build system automatically processes and bundles them. Fonts go in `Resources/Fonts/`, images in `Resources/Images/`, raw files in `Resources/Raw/`. Each resource type has a specific `Maui*` item group in the `.csproj` file. Fonts need to be registered in `MauiProgram.cs` with an alias (the name you use in XAML). Images are automatically converted to the platform's native format (PNG on Android, PDF on iOS) and resized for different screen densities. Raw resources are copied as-is (useful for JSON files, HTML templates). The `AppInfo.Current` class provides access to app metadata (version, name, package). This centralized system means you don't need platform-specific code for basic resources.

**Code Example:**
```csharp
// .csproj — auto-generated when you add files via Visual Studio
// <MauiFont Include="Resources\Fonts\Inter-Regular.ttf" />
// <MauiImage Include="Resources\Images\logo.svg" />
// <MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />

// MauiProgram.cs — register fonts with aliases
builder.ConfigureFonts(fonts =>
{
    fonts.AddFont("Inter-Regular.ttf", "InterRegular");
    fonts.AddFont("Inter-Bold.ttf", "InterBold");
});

// XAML usage:
<Label Text="Welcome" FontFamily="InterBold" FontSize="20" />
<Image Source="logo.png" />  <!-- No path needed — MAUI finds it in Resources/Images -->

// Raw resource access:
using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
using var reader = new StreamReader(stream);
var json = await reader.ReadToEndAsync();
```

---

**Q18: How do you handle connectivity changes in MAUI?**

**Answer:**

**Theory:** Mobile apps MUST handle connectivity changes gracefully — users move through tunnels, enter elevators, and lose signal constantly. The `Microsoft.Maui.Networking.Connectivity` API (from MAUI Essentials) provides real-time network state via events and a property check. The key patterns: (1) **Check before operation** — `Connectivity.Current.NetworkAccess == NetworkAccess.Internet` to fail fast before attempting an API call. (2) **React to changes** — subscribe to `ConnectivityChanged` to show/hide offline banners, queue data for sync, or retry failed operations. (3) **Graceful degradation** — show cached data when offline, disable write operations, display clear messaging. NEVER assume connectivity — always wrap API calls in try-catch and handle `HttpRequestException`. The `NetworkAccess` enum includes: `Internet`, `ConstrainedInternet` (captive portal — needs login), `Local` (local network only), `None`, and `Unknown`.

**Code Example:**
```csharp
public class ConnectivityAwareViewModel : BaseViewModel
{
    [ObservableProperty] private bool _isOnline = true;

    public ConnectivityAwareViewModel()
    {
        IsOnline = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
    }

    private async void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        IsOnline = e.NetworkAccess == NetworkAccess.Internet;

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (IsOnline)
            {
                // Back online — sync queued data
                SyncPendingChanges();
                ShowBanner("Back online", Colors.Green);
            }
            else
            {
                ShowBanner("You are offline. Showing cached data.", Colors.Orange);
            }
        });
    }

    public async Task<T?> LoadDataWithFallbackAsync<T>(Func<Task<T?>> apiCall, T? cachedData)
    {
        if (!IsOnline) return cachedData;  // Fail fast — return cache

        try
        {
            return await apiCall();
        }
        catch (HttpRequestException)
        {
            return cachedData;  // Graceful degradation
        }
    }
}
```

---

**Q19: How do you create and use custom `Behaviors` in MAUI?**

**Answer:**

**Theory:** Behaviors are reusable pieces of functionality that you can attach to any control WITHOUT subclassing it. They implement the **Decorator pattern** — adding behavior to an existing object dynamically. A behavior has two lifecycle hooks: `OnAttachedTo` (when added to a control) and `OnDetachingFrom` (when removed). Always unsubscribe from events in `OnDetachingFrom` to prevent memory leaks. Behaviors are ideal for: input validation, formatting, masking, visual effects, and any cross-cutting concern that multiple controls might need. The alternative to behaviors is creating a custom control (subclassing), but behaviors are more modular — you can mix and match multiple behaviors on the same control without creating a combinatorial explosion of subclasses. Bindable properties allow you to parameterize behaviors (like `MaxLength` in a text limiting behavior).

**Code Example:**
```csharp
public class NumericOnlyBehavior : Behavior<Entry>
{
    // Bindable property to configure the behavior
    public static readonly BindableProperty AllowDecimalProperty =
        BindableProperty.Create(nameof(AllowDecimal), typeof(bool), typeof(NumericOnlyBehavior), false);
    public bool AllowDecimal { get => (bool)GetValue(AllowDecimalProperty); set => SetValue(AllowDecimalProperty, value); }

    protected override void OnAttachedTo(Entry entry)
    {
        entry.TextChanged += OnTextChanged;  // Subscribe
        base.OnAttachedTo(entry);
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry || string.IsNullOrEmpty(e.NewTextValue)) return;

        var allowed = AllowDecimal
            ? e.NewTextValue.Where(c => char.IsDigit(c) || c == '.')  // Allow decimal point
            : e.NewTextValue.Where(char.IsDigit);                     // Digits only

        var filtered = new string(allowed.ToArray());
        if (filtered != e.NewTextValue)
            entry.Text = filtered;  // Remove invalid characters
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnTextChanged;  // ALWAYS unsubscribe!
        base.OnDetachingFrom(entry);
    }
}

// XAML usage:
// <Entry Placeholder="Price">
//     <Entry.Behaviors>
//         <local:NumericOnlyBehavior AllowDecimal="True" />
//     </Entry.Behaviors>
// </Entry>
```

---

**Q20: What is `x:DataType` and why is it important?**

**Answer:**

**Theory:** `x:DataType` enables **compiled bindings** in MAUI XAML. Without it, all bindings (`{Binding Name}`) use **runtime reflection** to find and access properties — the MAUI framework uses `PropertyChanged` events and reflection at runtime, which is slower and error-prone (typos become silent runtime failures instead of compile errors). With `x:DataType`, the XAML compiler generates code at compile time that directly accesses the ViewModel's properties — no reflection needed. This brings: (1) 5-10x faster binding resolution, (2) compile-time type checking (typo "Usernam" = compile error), (3) IntelliSense in XAML editor, and (4) better memory usage. Set `x:DataType` at the page level for the main BindingContext and at each `DataTemplate` level for item types. The `x:DataType` is inherited by child elements but can be overridden. This is especially important in `CollectionView.ItemTemplate` where bindings are created and destroyed frequently.

**Code Example:**
```xml
<!-- WITHOUT x:DataType — runtime reflection, no error checking -->
<ContentPage>
    <Label Text="{Binding Usernam}" />  <!-- Typo! Compiles fine, shows nothing at runtime -->
</ContentPage>

<!-- WITH x:DataType — compiled bindings, compile-time checking -->
<ContentPage xmlns:vm="clr-namespace:MyApp.ViewModels"
             x:DataType="vm:LoginViewModel">
    <Label Text="{Binding Username}" />  <!-- Compiler checks Username exists on LoginViewModel -->
    <!-- <Label Text="{Binding Usernam}" /> -->  <!-- COMPILE ERROR! -->
</ContentPage>

<!-- In DataTemplates (most important for performance): -->
<CollectionView ItemsSource="{Binding Users}"
                x:DataType="vm:UsersViewModel">
    <CollectionView.ItemTemplate x:DataType="models:UserModel">
        <DataTemplate>
            <Label Text="{Binding Name}" />  <!-- Compiled: direct property access -->
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

---

## MVVM & Data Binding

**Q21: What is the MVVM pattern? Explain each layer with examples.**

**Answer:**

**Theory:** MVVM (Model-View-ViewModel) is a UI architectural pattern that separates concerns into three layers:

- **Model** — The data layer. Plain C# classes (DTOs, entities) that represent business data. They have NO behavior, NO UI logic, and NO dependencies on the View or ViewModel. Examples: `UserModel`, `StationModel`, `AuthResponse`.

- **View** — The UI layer. XAML files that define the visual layout. The View "knows about" the ViewModel (via `BindingContext`) but the ViewModel knows NOTHING about the View. Communication happens entirely through **data binding** — the View subscribes to property changes and updates automatically.

- **ViewModel** — The "bridge" layer. It exposes data as observable properties (`[ObservableProperty]`) and actions as commands (`[RelayCommand]`). It orchestrates business logic by calling services, but it doesn't know about buttons, labels, or any UI elements. It's the most tested layer.

**The flow:** User taps Button → View's binding executes ViewModel's Command → ViewModel calls Service → Service returns data → ViewModel updates ObservableProperty → View's binding detects PropertyChanged → View re-reads property → UI updates automatically.

**Key principle:** ViewModel is 100% unit-testable. No UI dependency, no platform dependency, just C# code that you can test with mocks.

**Code Example:**
```csharp
// Model — pure data
public class UserModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

// ViewModel — observable properties + commands
public partial class ProfileViewModel : BaseViewModel
{
    private readonly IUserService _userService;

    public ProfileViewModel(IUserService userService)
    {
        _userService = userService;
    }

    [ObservableProperty] private UserModel? _user;
    [ObservableProperty] private bool _isLoading;

    [RelayCommand]
    async Task LoadProfileAsync()
    {
        IsLoading = true;
        try
        {
            User = await _userService.GetCurrentUserAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }
}

<!-- View — UI bound to ViewModel -->
<!-- <Label Text="{Binding User.Name}" /> -->
<!-- <ActivityIndicator IsRunning="{Binding IsLoading}" IsVisible="{Binding IsLoading}" /> -->
```

---

**Q22: What does `[ObservableProperty]` and `[RelayCommand]` generate?**

**Answer:**

**Theory:** These are **source generators** from the `CommunityToolkit.Mvvm` package. At compile time, the C# compiler runs these generators, analyzes your code for these attributes, and generates additional C# code that gets compiled alongside yours. You never see the generated code in your project (it's emitted during compilation), but you can inspect it with decompilers.

`[ObservableProperty]` generates: (1) A public property from a private field. (2) `INotifyPropertyChanged` implementation in the setter. (3) A `partial void On<Name>Changing(value)` method (called BEFORE the value changes). (4) A `partial void On<Name>Changed(value)` method (called AFTER the value changes). You can implement these partial methods to react to changes.

`[RelayCommand]` generates: (1) An `ICommand` property from an async or sync method. (2) For async methods: creates `AsyncRelayCommand` which tracks execution and prevents re-entry. (3) For methods with `CanExecute` parameter: creates command with `CanExecute()` check. (4) A `NotifyCanExecuteChangedFor` attribute to trigger CanExecute reevaluation.

Without source generators, you'd write 10+ lines per property and 10+ per command. With generators, it's 1 line each.

**Code Example:**
```csharp
// What you write (2 lines):
[ObservableProperty] private string _userName = string.Empty;
[RelayCommand] async Task LoginAsync() { /* ... */ }

// What the source generator creates (~40 lines):
// For [ObservableProperty]:
public string UserName
{
    get => _userName;
    set
    {
        if (!EqualityComparer<string>.Default.Equals(_userName, value))
        {
            OnUserNameChanging(value);      // Your hook
            _userName = value;
            OnPropertyChanged(nameof(UserName));
            OnUserNameChanged(value);       // Your hook
        }
    }
}
partial void OnUserNameChanging(string value);  // Implement to validate
partial void OnUserNameChanged(string value);   // Implement to react

// For [RelayCommand]:
private AsyncRelayCommand? _loginCommand;
public ICommand LoginCommand =>
    _loginCommand ??= new AsyncRelayCommand(LoginAsync);
```

---

**Q23: What is the difference between `OneWay`, `TwoWay`, and `OneTime` binding?**

**Answer:**

**Theory:** Data binding modes control the **direction** and **frequency** of data flow between the View and ViewModel:

- **OneWay** — ViewModel → View only. The View reads the property once, then subscribes to `PropertyChanged` events. When the ViewModel changes the property, the View updates. The View can NEVER push changes back. Default for read-only controls like `Label.Text`, `Image.Source`. Most common mode.

- **TwoWay** — ViewModel ⇄ View. Changes in either direction propagate to the other. The View's `TextChanged` event updates the ViewModel property, and ViewModel's `PropertyChanged` updates the View. Default for input controls like `Entry.Text`, `Slider.Value`, `Switch.IsToggled`.

- **OneTime** — ViewModel → View once. The View reads the property ONCE during initialization and does NOT subscribe to `PropertyChanged`. Best for static data that never changes (user's full name on a profile page, static labels). Performance optimization — no event subscription overhead.

- **OneWayToSource** — View → ViewModel only. The View can set the ViewModel property, but ViewModel changes don't update the View. Rarely used. Sometimes for password fields (VM reads it, never displays it back).

The mode is a trade-off between reactivity and performance. Use TwoWay only when needed (input controls). Use OneTime for static data. Default OneWay for everything else.

**Code Example:**
```xml
<!-- OneWay: ViewModel → View (Label shows current value) -->
<Label Text="{Binding UserName}" />  <!-- Default is OneWay -->

<!-- TwoWay: View ⇄ ViewModel (Entry syncs both ways) -->
<Entry Text="{Binding UserName, Mode=TwoWay}" />  <!-- Explicit TwoWay -->

<!-- OneTime: ViewModel → View once (static data, no event subscription) -->
<Label Text="{Binding FullName, Mode=OneTime}" />  <!-- Only reads once -->

<!-- OneWayToSource: View → ViewModel only -->
<Entry Text="{Binding Password, Mode=OneWayToSource}" IsPassword="True" />
```

---

**Q24: How does `INotifyPropertyChanged` work? Implement it manually.**

**Answer:**

**Theory:** `INotifyPropertyChanged` is the fundamental interface that enables data binding in .NET UI frameworks (WPF, MAUI, Xamarin, WinForms, UWP). It has ONE member: an event `PropertyChangedEventHandler? PropertyChanged`. When a ViewModel property changes, it raises this event with the property name. The View's binding system listens for this event; when it fires, the binding re-reads the property value and updates the UI. The pattern: (1) Check if the new value differs from the old (guard clause to prevent infinite loops). (2) Set the backing field. (3) Raise `PropertyChanged` with `nameof(propertyName)`. Without source generators, you'd write this boilerplate for every property. With `[ObservableProperty]`, it's generated automatically. The `[CallerMemberName]` attribute on the `OnPropertyChanged` method allows callers to omit the property name — the compiler fills it in automatically. This prevents typos and makes refactoring safe.

**Code Example:**
```csharp
// Manual implementation (what [ObservableProperty] generates):
public class ManualViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)  // Guard: prevent infinite loop
            {
                _name = value;
                OnPropertyChanged();  // [CallerMemberName] fills in "Name"
            }
        }
    }

    // [CallerMemberName] makes the propertyName parameter optional
    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        // Raise the event — MAUI binding system listens to this
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// The binding system in MAUI subscribes:
// When Name changes → event fires → binding re-reads Name → UI updates
// This is the magic that makes {Binding Name} work automatically
```

---

**Q25: What is the `IValueConverter` interface?**

**Answer:**

**Theory:** `IValueConverter` transforms data from one type to another for display. It bridges the gap between how data is stored (ViewModel) and how it's displayed (UI). The interface has two methods: `Convert` (ViewModel → View) and `ConvertBack` (View → ViewModel, needed for TwoWay bindings). Common conversions: bool to visibility, number to color, DateTime to formatted string, enum to display text, collection to count. A converter is stateless — same input always produces same output. They're registered as resources and referenced by key. For performance-critical scenarios, compiled converters (custom `IValueConverter` with `x:DataType`) avoid reflection. The `parameter` argument allows parameterizing converters (like passing a minimum value for a threshold converter). The `culture` argument handles locale-specific formatting.

**Code Example:**
```csharp
// Converter: bool → Color (Active = Green, Inactive = Red)
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ViewModel → View: bool becomes Color
        return value is true ? Colors.Green : Colors.Red;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // View → ViewModel: Color becomes bool (rarely needed)
        throw new NotSupportedException();
    }
}

// Converter with parameter: invert behavior
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool boolValue = value is true;
        bool invert = parameter?.ToString() == "Invert";
        return (boolValue ^ invert) ? true : false;  // XOR = invert if parameter is "Invert"
    }
}

// XAML:
// <ContentPage.Resources>
//     <local:BoolToColorConverter x:Key="BoolToColor" />
//     <local:BoolToVisibilityConverter x:Key="BoolToVis" />
// </ContentPage.Resources>

// <Label Text="Active" TextColor="{Binding IsActive, Converter={StaticResource BoolToColor}}" />
// <Frame IsVisible="{Binding IsHidden, Converter={StaticResource BoolToVis}, ConverterParameter=Invert}" />
```

---

## Dependency Injection

**Q26: What is Dependency Injection and why is it important?**

**Answer:**

**Theory:** Dependency Injection is a design pattern where an object's dependencies are PROVIDED from outside rather than CREATED by the object itself. It implements the **Dependency Inversion Principle** (the "D" in SOLID): "Depend on abstractions, not concretions." Instead of `var auth = new AuthService()` (tight coupling), you write `public LoginViewModel(IAuthService auth)` (loose coupling). The DI container (the framework) creates all dependencies and "injects" them through the constructor. Benefits: (1) **Testability** — you can mock `IAuthService` in unit tests. (2) **Flexibility** — swap `RealAuthService` for `MockAuthService` without changing ViewModel code. (3) **Centralized configuration** — all registration in one place (`MauiProgram.cs`). (4) **Lifecycle management** — container handles when objects are created and destroyed. Without DI, you get the **Service Locator anti-pattern** — classes reach out to get their dependencies from a global registry, making dependencies hidden and tests brittle.

**Code Example:**
```csharp
// WITHOUT DI (tight coupling — BAD):
public class LoginViewModel
{
    private readonly AuthService _auth = new();  // Hard-coded dependency

    public async Task LoginAsync(string u, string p)
    {
        await _auth.LoginAsync(u, p);  // Can't test without real AuthService
    }
}

// WITH DI (loose coupling — GOOD):
public class LoginViewModel
{
    private readonly IAuthService _auth;  // Depends on ABSTRACTION, not CONCRETION

    public LoginViewModel(IAuthService auth)  // Dependency injected from outside
    {
        _auth = auth;
    }

    public async Task LoginAsync(string u, string p)
    {
        await _auth.LoginAsync(u, p);  // Can mock IAuthService in tests
    }
}

// DI Container registers the mapping:
// builder.Services.AddSingleton<IAuthService, AuthService>();
// When LoginViewModel is requested, container:
// 1. Sees it needs IAuthService
// 2. Looks up IAuthService → AuthService
// 3. Creates AuthService (and ITS dependencies)
// 4. Passes it to LoginViewModel constructor
// 5. Returns fully constructed LoginViewModel
```

---

**Q27: Explain `AddSingleton`, `AddTransient`, and `AddScoped`.**

**Answer:**

**Theory:** These three methods control the **lifetime** of registered services — when they're created and when they're disposed. Choosing the wrong lifetime causes bugs (stale data if singleton when should be transient) or memory leaks (transient when should be singleton).

- **Singleton** — ONE instance for the entire application lifetime. Created on first request, destroyed when the app exits. Use for: stateless services (`HttpClient`, configuration), shared state (`AuthService.CurrentUser`), caches. **Thread safety required** — all callers share the same instance.

- **Transient** — NEW instance every time it's requested. Created on each injection, disposed when the scope ends (page navigation). Use for: ViewModels, Pages — each navigation gets fresh state. **Low allocation cost** — many short-lived objects create GC pressure.

- **Scoped** — ONE instance per scope. In web apps, one scope per HTTP request. In MAUI, scopes are less common — usually treated as singleton or transient. Use for: Unit of Work, database transactions (create once per operation, commit at end).

The rule of thumb: ViewModels → Transient (fresh state per navigation). Services → Singleton (single HttpClient, single auth state). If in doubt, start with Singleton and change if you see stale-state bugs.

**Code Example:**
```csharp
// Singleton — one for the app lifetime
builder.Services.AddSingleton<IAuthService, AuthService>();
// Only ONE AuthService ever created.
// All ViewModels share the same CurrentUser, same token cache.
// Thread-safe: AuthService must handle concurrent calls.

// Transient — new instance per injection
builder.Services.AddTransient<LoginViewModel>();
builder.Services.AddTransient<LoginPage>();
// NEW LoginViewModel created every time Shell navigates to LoginPage.
// Fresh state: Username and Password start empty each time.
// ViewModel.ctor() called each navigation.

// Scoped — one per scope (rare in MAUI)
builder.Services.AddScoped<ITransactionService, TransactionService>();
// Useful when you have a well-defined operation scope.
// Example: a wizard across 3 pages — one scope, same TransactionService.
```

---

**Q28: How do you register multiple implementations of the same interface?**

**Answer:**

**Theory:** Multiple implementations of the same interface arise when: (1) you have a mock/real variant (debug vs release), (2) you have multiple strategies (different retry strategies), (3) you need to decorate an implementation (logging wrapper around real service). The DI container handles this in different ways: (A) The LAST registration wins by default — only the last registered type is returned for `GetService<T>()`. (B) `IEnumerable<T>` returns ALL registered implementations. (C) Conditional registration (`#if DEBUG`) selects implementation at compile time. (D) Factory pattern allows runtime selection based on context. The Decorator pattern is especially powerful: register a "wrapper" that takes the real implementation and adds behavior (logging, caching, retry) without modifying the real implementation.

**Code Example:**
```csharp
// Option 1: Conditional compilation (most common for mock vs real)
#if DEBUG
    builder.Services.AddSingleton<IApiService, MockApiService>();
#else
    builder.Services.AddSingleton<IApiService, RealApiService>();
#endif

// Option 2: IEnumerable<T> to get all implementations
builder.Services.AddSingleton<INotificationService, EmailNotificationService>();
builder.Services.AddSingleton<INotificationService, SmsNotificationService>();
// Inject as IEnumerable<INotificationService> to notify via ALL channels

// Option 3: Decorator pattern (wrapper adds logging)
builder.Services.AddSingleton<IApiService, RealApiService>();
builder.Services.AddSingleton<IApiService>(sp =>
    new LoggingApiServiceDecorator(                        // Wrapper
        sp.GetRequiredService<RealApiService>()));         // Real implementation
// Calls to IApiService go through LoggingApiServiceDecorator → RealApiService
```

---

**Q29: What is the Service Locator anti-pattern?**

**Answer:**

**Theory:** Service Locator is an alternative to DI where classes ASK for their dependencies from a global registry: `var auth = App.ServiceLocator.GetService<IAuthService>()`. It's an ANTI-PATTERN because: (1) **Hidden dependencies** — you can't tell what a class needs by looking at its constructor. You must read the entire method body to find `GetService` calls. (2) **Testability** — mocking requires changing the global locator, which affects other tests (shared state). (3) **Runtime failures** — if a service isn't registered, `GetService` returns null at runtime — no compile-time error. (4) **Encourages god classes** — since it's easy to grab any service anywhere, classes tend to accumulate dependencies ad-hoc. Constructor injection fixes all of these: constructor parameters document exactly what's needed, test mocks are passed explicitly, and the container validates everything at resolution time. The composition root (`MauiProgram.cs`) is the ONLY place where the container is used — all other code receives pre-built dependencies.

**Code Example:**
```csharp
// SERVICE LOCATOR (ANTI-PATTERN):
public class LoginViewModel
{
    public async Task LoginAsync(string user, string pass)
    {
        // Hidden dependency! No way to know IAuthService is needed
        var auth = App.ServiceProvider.GetService<IAuthService>();
        if (auth is null) throw new Exception("Service not registered"); // Runtime failure!

        // Can't mock easily — static ServiceProvider is global
        await auth.LoginAsync(user, pass);
    }
}

// PROPER DI:
public class LoginViewModel
{
    private readonly IAuthService _auth;

    // Constructor tells you EXACTLY what this class needs
    public LoginViewModel(IAuthService auth)
    {
        _auth = auth;
    }

    public async Task LoginAsync(string user, string pass)
    {
        await _auth.LoginAsync(user, pass);
        // No coupling to container, no runtime failures
    }
}

// Test:
// var mock = new Mock<IAuthService>();
// var vm = new LoginViewModel(mock.Object);  // Clear, simple, isolated
```

---

**Q30: How does MAUI resolve dependencies for pages?**

**Answer:**

**Theory:** MAUI's Shell navigation system integrates with the DI container automatically. When you navigate to a page via `Shell.Current.GoToAsync()`, Shell asks the DI container to RESOLVE the page. The container sees the page's constructor parameters, recursively resolves their dependencies, and returns a fully constructed page. This is called **auto-wiring** or **auto-resolution**. The key insight: you don't need to manually set `BindingContext` in the page constructor if you construct the page through DI. Just declare the ViewModel as a constructor parameter. The container handles the rest. This same mechanism applies to: Pages, ViewModels, Services, and any type registered in the container. For pages that need runtime parameters (like an ID passed from navigation), use `QueryPropertyAttribute` or `IQueryAttributable` — these are set AFTER construction but BEFORE `OnAppearing`.

**Code Example:**
```csharp
// 1. Register everything:
builder.Services.AddTransient<LoginPage>();          // Page
builder.Services.AddTransient<LoginViewModel>();     // Its ViewModel
builder.Services.AddSingleton<IAuthService, AuthService>();  // Its service

// 2. Page constructor receives ViewModel via DI:
public class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel vm)  // DI provides this!
    {
        InitializeComponent();
        BindingContext = vm;  // Wire ViewModel to View
    }
}

// 3. When Shell navigates:
await Shell.Current.GoToAsync("//login");

// The resolution process:
// Shell requests: LoginPage
// Container sees: LoginPage needs LoginViewModel
// Container sees: LoginViewModel needs IAuthService
// Container sees: IAuthService → AuthService (singleton, already exists)
// Container: creates LoginViewModel(authService)
// Container: creates LoginPage(loginViewModel)
// Returns: fully constructed LoginPage with BindingContext set
```

---

## Async Programming

**Q31: Explain `async`/`await` with a real example.**

**Answer:**

**Theory:** `async`/`await` is C#'s approach to asynchronous programming — it allows a method to PAUSE without blocking the thread, then RESUME when the operation completes. When the `await` keyword is encountered: (1) The method returns control to its CALLER immediately (the thread is freed to do other work). (2) The runtime captures the current **SynchronizationContext** (on the UI thread, this is the main thread — ensures continuations run on UI thread). (3) When the awaited operation completes, the runtime executes the rest of the method on the captured context. This means: if you're on the UI thread and you await, the continuation runs on the UI thread — safe to update UI. The state machine pattern (generated by the compiler) handles all the complexity: saving state across awaits, managing the sync context, propagating exceptions. Without async/await, you'd need to manually manage callbacks (APM pattern) or use `ContinueWith` (TPL pattern) — both lead to nested callback hell.

**Code Example:**
```csharp
public async Task LoadDashboardAsync()
{
    // Runs on UI thread
    IsBusy = true;

    try
    {
        // AWAIT: method returns to caller (UI thread freed)
        // HttpClient.GetStringAsync runs on ThreadPool
        var json = await _httpClient.GetStringAsync("/api/dashboard");

        // CONTINUATION: runs back on UI thread (SyncContext captured)
        // Safe to update UI!
        var data = JsonSerializer.Deserialize<DashboardModel>(json);
        BatteryLevel = data.BatteryPercent;  // UI updates via binding
        IsBusy = false;
    }
    catch (HttpRequestException ex)
    {
        // Runs on UI thread
        await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
    }
}

// Behind the scenes, the compiler generates:
// - A struct implementing IAsyncStateMachine
// - MoveNext() method with switch(state) for resume points
// - AsyncTaskMethodBuilder for task management
// This is C# compiler magic — you write sequential-looking code
// that runs asynchronously
```

---

**Q32: What happens when you block on async code with `.Result` or `.Wait()`?**

**Answer:**

**Theory:** Blocking on async code (`.Result` or `.Wait()`) causes a **deadlock** in UI applications. Here's why: when you `await` on a UI thread, the runtime captures the **SynchronizationContext** (the UI context) so the continuation can run back on the UI thread. When you use `.Result` or `.Wait()`, you BLOCK the UI thread — it can't process messages or dispatch work. The async operation completes, tries to resume on the captured UI context, but the UI thread is blocked by `.Result`. The async method can't resume because the UI context is busy, and the UI thread can't become unblocked because the async method can't complete. **Circular wait = deadlock.** The fix is simple: use `await` all the way down. Never use `.Result` or `.Wait()` in UI code. If you're in a situation where you CAN'T await (e.g., a constructor), restructure the code — use a factory method, an async initialization pattern, or fire-and-forget with error handling. This is the most common async mistake in MAUI apps.

**Code Example:**
```csharp
// BAD: Deadlock on UI thread
public void ButtonClick(object? sender, EventArgs e)
{
    // BLOCKS UI thread: can't dispatch messages
    var data = _api.GetAsync<DashboardModel>("/api/dashboard").Result;
    // DEADLOCK! GetAsync completes, tries to resume on UI thread
    // but UI thread is blocked by .Result — never resumes
}

// GOOD: async all the way
public async void ButtonClick(object? sender, EventArgs e)
{
    var data = await _api.GetAsync<DashboardModel>("/api/dashboard");
    // UI thread is NOT blocked — await frees it
    // Continuation resumes on UI thread automatically
}

// If you really must call async from sync (rare):
// Use Task.Run to offload to ThreadPool (avoids UI context capture)
var data = Task.Run(() => _api.GetAsync<DashboardModel>("/api/dashboard")).Result;
// ...but this is still blocking a thread. Better to restructure.
```

---

**Q33: When would you use `Task.WhenAll` vs sequential awaits?**

**Answer:**

**Theory:** Sequential await and `Task.WhenAll` differ in **parallelism** — how many operations run simultaneously. Sequential: `await A; await B; await C` — each operation starts AFTER the previous completes. Total time = A + B + C. Parallel: `Task.WhenAll(A, B, C)` — all operations start IMMEDIATELY, run concurrently. Total time ≈ max(A, B, C). The trade-off: parallel is faster but uses more resources (thread pool threads, network connections, API server capacity). Use sequential when: (1) next operation depends on previous (`GetUserAsync()` → `GetOrdersAsync(userId)`). (2) Operations target the same resource (avoid overloading a single API). (3) You want to show progress step by step. Use parallel when: (1) Operations are INDEPENDENT (no data dependency). (2) You need maximum speed (dashboard loading multiple widgets). (3) Operations target different resources/users. The `WhenAll` overload accepts up to 4 tasks as parameters or an `IEnumerable<Task>` for dynamic numbers. It throws an `AggregateException` if any task fails — check each task's `Exception` property.

**Code Example:**
```csharp
// SEQUENTIAL (slow but safe): 3 seconds
var user = await _api.GetAsync<UserModel>("/api/user/profile");      // 1s
var dashboard = await _api.GetAsync<DashboardModel>("/api/dashboard"); // 1s
var stations = await _api.GetAsync<List<StationModel>>("/api/stations"); // 1s
// Total: 3 seconds

// PARALLEL (fast but resource-heavy): ~1 second
var userTask = _api.GetAsync<UserModel>("/api/user/profile");
var dashTask = _api.GetAsync<DashboardModel>("/api/dashboard");
var stationTask = _api.GetAsync<List<StationModel>>("/api/stations");

await Task.WhenAll(userTask, dashTask, stationTask);  // All 3 run concurrently
// Total: ~1 second (slowest of the three)

// Access results after all complete:
var user = userTask.Result;  // Already completed — safe to use .Result
var dashboard = dashTask.Result;
var stations = stationTask.Result;

// When one task depends on another — MUST be sequential:
var user = await _api.GetAsync<UserModel>("/api/user/profile");
var orders = await _api.GetAsync<List<OrderModel>>($"/api/orders?userId={user.Id}");
```

---

**Q34: What is `CancellationToken` and how do you use it?**

**Answer:**

**Theory:** `CancellationToken` is the standard .NET pattern for **cooperative cancellation** — you CAN'T force a thread to stop (that causes resource leaks), but you can send a cancellation request and the operation can CHOOSE to stop gracefully. The pattern: (1) Create a `CancellationTokenSource` (the "switch"). (2) Get the `CancellationToken` (the "signal"). (3) Pass it to async methods. (4) The async method periodically checks `token.IsCancellationRequested` or calls `token.ThrowIfCancellationRequested()`. Uses: debouncing search (cancel previous request), timeout operations (`CancelAfter(TimeSpan)`), user cancelling long upload, app shutting down. `HttpClient` accepts cancellation tokens natively. Without cancellation, a user's search query from 5 minutes ago might still be processing, wasting resources and showing stale results. Always create a new `CancellationTokenSource` per operation and `Dispose()` it to prevent memory leaks.

**Code Example:**
```csharp
public partial class SearchViewModel : BaseViewModel
{
    private CancellationTokenSource? _searchCts;

    // Called on each keystroke via OnSearchQueryChanged
    public async Task SearchAsync(string query)
    {
        // Cancel PREVIOUS search (user typed something new)
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        try
        {
            // Debounce: wait 300ms — if user types again before 300ms,
            // this cancellation runs and the new search starts
            await Task.Delay(300, token);

            // If we get here, user stopped typing for 300ms
            var results = await _api.GetAsync<List<StationModel>>(
                $"/api/stations?q={query}", token);

            token.ThrowIfCancellationRequested();  // Optional explicit check

            Stations = new ObservableCollection<StationModel>(results ?? new());
        }
        catch (OperationCanceledException)
        {
            // Expected — user typed something new, ignore this result
        }
    }

    // When page disappears, cancel all pending operations
    public void Cleanup()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }
}
```

---

**Q35: What is `ConfigureAwait(false)` and when should you use it?**

**Answer:**

**Theory:** `ConfigureAwait(false)` tells the runtime NOT to capture and return to the current `SynchronizationContext`. Without it, `await` captures the current context (UI thread in MAUI) and the continuation runs on that context. With `ConfigureAwait(false)`, the continuation runs on ANY available thread (usually the ThreadPool). This is a performance optimization for LIBRARY code — it avoids the overhead of context marshaling. The RULE: (1) In **library/service code** that doesn't touch UI — use `ConfigureAwait(false)` for faster execution. (2) In **ViewModel/UI code** — NEVER use it, because the continuation MUST run on the UI thread to update observable properties. If a ViewModel calls a library method that used `ConfigureAwait(false)`, the continuation after the library call is on the ThreadPool, NOT the UI thread. Any UI property updates after that point would cause cross-thread exceptions. Since MAUI 8, most built-in MAUI APIs internally use `ConfigureAwait(false)` where appropriate — you generally don't need to add it yourself unless writing library code.

**Code Example:**
```csharp
// LIBRARY CODE (service layer) — OK to use ConfigureAwait(false):
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var json = await _httpClient.GetStringAsync(endpoint)
            .ConfigureAwait(false);  // ← OK: no UI access in this method
        return JsonSerializer.Deserialize<T>(json);
    }
    // Continuation runs on ThreadPool — faster, no context switch
}

// VIEWMODEL CODE (UI layer) — NEVER use ConfigureAwait(false):
public class DashboardViewModel : BaseViewModel
{
    private readonly IApiService _api;

    public async Task LoadAsync()
    {
        var data = await _api.GetAsync<DashboardModel>("/api/dashboard");
        // ← NO ConfigureAwait — ensures continuation on UI thread
        IsBusy = false;  // MUST run on UI thread (property change notification)
    }
}

// RULE: ViewModel calls → NO ConfigureAwait(false)
//       Service calls → ConfigureAwait(false) optional optimization
```

---

## REST API & HTTP Communication

**Q36: Explain the full HTTP lifecycle of a POST request in MAUI.**

**Answer:**

**Theory:** Every HTTP request in MAUI goes through these stages: (1) **Serialization** — the C# request object is converted to JSON using `System.Text.Json`. (2) **Request construction** — `HttpClient` creates an `HttpRequestMessage` with method, URL, headers, and body. (3) **Handler pipeline** — the request passes through `DelegatingHandler`s (logging, auth, retry). (4) **Network** — DNS resolution, TCP connection (potentially TLS handshake for HTTPS). (5) **Server processing** — ASP.NET Core receives the request, routes to the controller, deserializes JSON, runs business logic, queries DB, constructs response. (6) **Response** — server sends back HTTP status code + JSON body. (7) **Deserialization** — MAUI's `HttpClient` reads the response stream, `System.Text.Json` deserializes it to the expected C# type. (8) **Error handling** — non-2xx responses throw `HttpRequestException` or are handled by the application. Each stage can fail: network timeout, DNS failure, TLS error, server error (500), or deserialization failure (JSON shape doesn't match). Robust apps handle each failure mode.

**Code Example:**
```csharp
public async Task<AuthResponse?> LoginAsync(string username, string password)
{
    // 1. Serialization: C# → JSON
    var loginData = new { Username = username, Password = password };
    // → {"username":"admin","password":"Admin@123"}

    // 2-5. HTTP Request
    var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginData);
    // POST http://localhost:5238/api/auth/login
    // Content-Type: application/json
    // Body: {"username":"admin","password":"Admin@123"}
    //
    // 3. Handler pipeline: LoggingHandler → AuthHandler → RetryHandler → ...
    // 4. DNS resolution: localhost → 127.0.0.1
    // 5. TCP connection: 127.0.0.1:5238
    //
    // 5-8. SERVER:
    // AuthController.Login() receives request
    // Verifies credentials (BL → DB)
    // Returns 200 OK + {"token":"eyJ...","user":{...}}

    // 6. Response:
    // ← 200 OK
    // ← Content-Type: application/json
    // ← {"token":"eyJ...","user":{"id":1,"name":"Admin"}}

    // 7. Deserialization: JSON → C#
    var authResponse = await response.Content
        .ReadFromJsonAsync<AuthResponse>();

    // 8. Error handling
    if (authResponse is null)
        throw new Exception("Login failed: empty response");

    return authResponse;
}
```

---

**Q37: How do you handle expired JWT tokens in MAUI?**

**Answer:**

**Theory:** JWT access tokens are short-lived (15-60 minutes) for security — if stolen, the damage is limited. When a token expires, the API returns **401 Unauthorized**. The MAUI app must detect this and REFRESH the token before retrying. The refresh flow: (1) Original API call fails with 401. (2) A `DelegatingHandler` intercepts the 401. (3) It calls the refresh endpoint (`POST /api/auth/refresh`) with the refresh token (stored in `SecureStorage`). (4) If refresh succeeds, it retries the original request with the new token. (5) If refresh fails (refresh token also expired), it redirects to the login page. This MUST happen transparently — the calling code (ViewModel) shouldn't need to handle token refresh. The `DelegatingHandler` approach (like ASP.NET Core middleware) is the cleanest — it's a pipeline interceptor that runs before/after every HTTP request. Without this, every ViewModel would need to catch 401 and call refresh — lots of duplication.

**Code Example:**
```csharp
public class TokenRefreshHandler : DelegatingHandler
{
    private readonly IAuthService _auth;

    public TokenRefreshHandler(IAuthService auth)
    {
        _auth = auth;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        // Step 1: Execute the original request
        var response = await base.SendAsync(request, ct);

        // Step 2: Check for 401 (expired token)
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Step 3: Try to refresh the token
            var refreshed = await _auth.RefreshTokenAsync();

            if (refreshed)
            {
                // Step 4: Retry with new token
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
                response = await base.SendAsync(request, ct);
            }
            else
            {
                // Step 5: Refresh failed — force re-login
                await Shell.Current.GoToAsync("//login");
            }
        }

        return response;
    }
}

// Register in DI:
// builder.Services.AddTransient<TokenRefreshHandler>();
// builder.Services.AddHttpClient<IApiService, ApiService>()
//     .AddHttpMessageHandler<TokenRefreshHandler>();
```

---

**Q38: What HTTP status codes should your app handle?**

**Answer:**

**Theory:** HTTP status codes tell you what happened with the server's processing. Your app must handle each category differently. **2xx Success** codes mean everything worked — your app should process the response body normally. **3xx Redirection** means the resource moved — `HttpClient` follows redirects by default (configurable). **4xx Client Error** means the request was bad — your app sent something wrong. **5xx Server Error** means the server failed — your app should retry after a delay (exponential backoff). The critical distinction: 4xx errors are YOUR fault (don't retry — they'll keep failing), 5xx errors are THE SERVER's fault (retry with backoff — they might recover). A well-designed app categorizes errors and responds appropriately: 401 → re-authenticate, 403 → show "access denied" message, 404 → show "not found", 429 → wait and retry (rate limited), 500 → log and show "try again later", 503 → retry with backoff (service temporarily unavailable).

**Code Example:**
```csharp
public async Task<T?> HandleApiResponseAsync<T>(HttpResponseMessage response)
{
    switch (response.StatusCode)
    {
        // 2xx — Success
        case HttpStatusCode.OK:
        case HttpStatusCode.Created:
            return await response.Content.ReadFromJsonAsync<T>();

        case HttpStatusCode.NoContent:
            return default; // 204 = success, no body

        // 3xx — Redirection (let HttpClient handle it)
        case HttpStatusCode.NotModified:
            return default; // 304 = use cached version

        // 4xx — Client errors (our fault, don't retry)
        case HttpStatusCode.Unauthorized:       // 401
            throw new UnauthorizedAccessException("Session expired — login again");
        case HttpStatusCode.Forbidden:           // 403
            throw new UnauthorizedAccessException("You don't have permission");
        case HttpStatusCode.NotFound:            // 404
            throw new KeyNotFoundException("Resource not found");
        case HttpStatusCode.Conflict:            // 409
            throw new InvalidOperationException("Resource already exists");
        case HttpStatusCode.TooManyRequests:     // 429
            throw new RateLimitExceededException("Too many requests — slow down");

        // 5xx — Server errors (their fault, retry with backoff)
        case HttpStatusCode.InternalServerError: // 500
        case HttpStatusCode.BadGateway:          // 502
        case HttpStatusCode.ServiceUnavailable:  // 503
            throw new HttpRequestException($"Server error: {(int)response.StatusCode}");
    }

    throw new HttpRequestException($"Unexpected status: {(int)response.StatusCode}");
}
```

---

**Q39: How do you upload a file to a REST API from MAUI?**

**Answer:**

**Theory:** File uploads use `MultipartFormDataContent` — a special HTTP content type that can mix binary data (the file) with text fields (metadata) in a single request. The browser/MAUI sends `Content-Type: multipart/form-data; boundary=---boundary123` and the body contains parts separated by the boundary string. On the server side, ASP.NET Core's model binder automatically reads `MultipartFormDataContent` and binds to `IFormFile` parameters. The MAUI flow: (1) User picks a file with `FilePicker`. (2) Open a `Stream` from the file. (3) Wrap the stream in `StreamContent`. (4) Add it to `MultipartFormDataContent` with a field name and filename. (5) POST to the server. For large files (videos, firmware updates), consider: (a) Chunked upload — split into 1MB parts with a progress callback. (b) Resumable upload — save progress and resume from failure. (c) Compression — ZIP the file before uploading. The `HttpClient.Timeout` must be long enough for large uploads.

**Code Example:**
```csharp
public async Task<UploadResult?> UploadFileAsync(FileResult file, IProgress<double>? progress = null)
{
    // 1. Pick file using MAUI FilePicker
    // var file = await FilePicker.Default.PickAsync();

    // 2. Open stream
    using var stream = await file.OpenReadAsync();
    var totalBytes = stream.Length;

    // 3. Wrap in progress-tracking stream (optional)
    using var progressStream = new ProgressStream(stream, totalBytes, progress);
    using var content = new MultipartFormDataContent();

    // 4. Add file as multipart content
    content.Add(new StreamContent(progressStream), "file", file.FileName);

    // 5. Add metadata fields
    content.Add(new StringContent(DateTime.UtcNow.ToString("O")), "uploadedAt");
    content.Add(new StringContent("document"), "category");

    // 6. Upload
    var response = await _httpClient.PostAsync("/api/upload", content);
    response.EnsureSuccessStatusCode();

    return await response.Content.ReadFromJsonAsync<UploadResult>();
}

// Progress-tracking stream wrapper
public class ProgressStream : DelegatingStream
{
    private readonly long _totalBytes;
    private readonly IProgress<double>? _progress;
    private long _bytesWritten;

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        await base.WriteAsync(buffer, ct);
        _bytesWritten += buffer.Length;
        _progress?.Report((double)_bytesWritten / _totalBytes * 100);
    }
}
```

---

**Q40: How do you implement request retry with Polly?**

**Answer:**

**Theory:** Network failures and server errors are inevitable — a retry policy is essential for a robust app. **Polly** is the .NET library for handling transient faults. The core concepts: (1) **Retry policy** — retry N times with delay between attempts. Use exponential backoff (2s, 4s, 8s) to avoid hammering a recovering server. (2) **Circuit breaker** — if N consecutive calls fail, STOP trying for a duration (let the server recover). (3) **Timeout** — fail fast if the server doesn't respond within a limit. (4) **Bulkhead isolation** — limit concurrent calls to prevent thread pool starvation. The policies can be COMBINED: retry 3 times, but if 5 consecutive retries fail, open the circuit for 30 seconds. Polly integrates with `HttpClientFactory` via the `Microsoft.Extensions.Http.Polly` package. The policy handles what to retry (transient failures: 5xx, 408 Timeout, 429 Rate Limit, `HttpRequestException`) and what NOT to retry (4xx client errors, which will keep failing).

**Code Example:**
```csharp
using Polly;
using Polly.Extensions.Http;

// Define retry policy: 3 attempts with exponential backoff
var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()        // 5xx or 408
    .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests) // 429
    .WaitAndRetryAsync(3, attempt =>
        TimeSpan.FromSeconds(Math.Pow(2, attempt)));  // 2s, 4s, 8s

// Circuit breaker: after 5 failures, stop for 30 seconds
var circuitBreaker = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// Timeout: 10 seconds per request
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(10);

// Combine: retry → circuit breaker → timeout
var combinedPolicy = Policy.WrapAsync(retryPolicy, circuitBreaker, timeoutPolicy);

// Register with HttpClientFactory
builder.Services.AddHttpClient<IApiService, ApiService>()
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreaker)
    .AddPolicyHandler(timeoutPolicy);
```

---

## Database & Entity Framework Core

**Q41: Explain Code-First vs Database-First in EF Core.**

**Answer:**

**Theory:** Entity Framework Core supports two development approaches. **Code-First** is the modern approach: you write C# entity classes, and EF Core GENERATES the database schema from them. This gives you full control over the code (inheritance, validation, encapsulation) and treats the database as a storage detail, not the source of truth. **Database-First** is the legacy approach: you have an existing database, and EF Core GENERATES entity classes from it. This is useful when working with a legacy database, a DBA-managed schema, or a third-party database you can't modify. The choice depends on WHO owns the schema. If the development team owns the database → Code-First (schema follows code). If a DBA team or third party owns the database → Database-First (code follows schema). In Code-First, migrations track schema changes over time. In Database-First, you regenerate entity files when the schema changes. Both produce the same runtime behavior — the difference is where the schema definition lives.

**Code Example:**
```csharp
// CODE-FIRST: You write C#, EF generates SQL
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
}
// > dotnet ef migrations add InitialCreate
// > dotnet ef database update
// SQL: CREATE TABLE Users (Id int IDENTITY(1,1), Name nvarchar(max), Email nvarchar(max))

// DATABASE-FIRST: Existing DB, EF generates C#
// > dotnet ef dbcontext scaffold "Server=.;Database=MyDb;Trusted_Connection=True;"
//       Microsoft.EntityFrameworkCore.SqlServer
// Generates: User.cs, AppDbContext.cs matching the database schema
```

---

**Q42: What is the difference between `Include()` and `ThenInclude()`?**

**Answer:**

**Theory:** By default, EF Core uses **lazy loading** — navigation properties are null unless you explicitly load them. `Include()` and `ThenInclude()` enable **eager loading** — loading related data in a SINGLE database query using SQL JOINs. `Include()` loads an immediate navigation property (e.g., `Station.Batteries`). `ThenInclude()` loads a NESTED navigation property (e.g., `Station.Batteries.SwapHistories`). The key performance insight: without `Include()`, accessing `station.Batteries` would either be null (if no lazy loading) or trigger N+1 queries (if using lazy loading proxies) — one query for the station, then N queries for each station's batteries. With `Include()`, it's ONE query with LEFT JOINs. However, too many `Include()` calls can cause huge JOIN explosions — if Station has 10 Batteries and each Battery has 100 SwapHistories, a single query returns 10 × 100 = 1000 rows. Use `Include()` for "Include one or two related collections" and consider **explicit loading** or **projection** (`.Select()`) for deeper graphs.

**Code Example:**
```csharp
// Without Include(): Batteries is null
var station = db.Stations.FirstOrDefault(s => s.Id == 1);
var batteries = station.Batteries;  // NullReferenceException!

// With Include(): loads Batteries in same query
var station = db.Stations
    .Include(s => s.Batteries)       // LEFT JOIN Batteries ON StationId
    .FirstOrDefault(s => s.Id == 1);
// SQL: SELECT * FROM Stations LEFT JOIN Batteries ON Stations.Id = Batteries.StationId

// With ThenInclude(): loads nested navigation
var station = db.Stations
    .Include(s => s.Batteries)                // Load Batteries
        .ThenInclude(b => b.SwapHistories)    // Load each battery's SwapHistories
    .FirstOrDefault(s => s.Id == 1);
// SQL: SELECT * FROM Stations
//      LEFT JOIN Batteries ON Stations.Id = Batteries.StationId
//      LEFT JOIN SwapHistories ON Batteries.Id = SwapHistories.BatteryId

// Multiple includes:
var station = db.Stations
    .Include(s => s.Batteries)
        .ThenInclude(b => b.SwapHistories)
    .Include(s => s.Address)                   // Another navigation property
    .FirstOrDefault(s => s.Id == 1);
```

---

**Q43: What is `AsNoTracking()` and when should you use it?**

**Answer:**

**Theory:** EF Core's **change tracking** keeps a SNAPSHOT of every entity it loads. When you call `SaveChangesAsync()`, it compares each entity's current values to the snapshot to detect what changed. This is essential for updates, but it has overhead: (1) Memory — every loaded entity has a snapshot. (2) CPU — snapshot comparison on `SaveChanges`. (3) Performance — tracking state machine per entity. `AsNoTracking()` DISABLES this overhead — EF Core still loads the data, but doesn't track it. Use it for READ-ONLY queries where you never update the returned entities. The performance gain is 2-5x for large result sets. NEVER use `AsNoTracking()` if you plan to update and save the entities later — EF Core won't know they changed. For mixed scenarios (read some data, update some), use tracking by default and consider `AsNoTrackingWithIdentityResolution()` (which caches identity but doesn't track changes).

**Code Example:**
```csharp
// QUERY (read-only) — use AsNoTracking() for performance
var users = await db.Users
    .AsNoTracking()                    // No tracking overhead!
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .ToListAsync();
// Memory: only the user data (no snapshot, no state)
// SQL: SELECT * FROM Users WHERE IsActive = 1 ORDER BY Name

// UPDATE (read + write) — DON'T use AsNoTracking()
var user = await db.Users
    .FirstOrDefaultAsync(u => u.Id == 1);  // Tracked by default
user.Email = "new@email.com";
await db.SaveChangesAsync();              // EF detects change, generates UPDATE SQL

// AsNoTrackingWithIdentityResolution() — caches identity but no change tracking
var users = await db.Users
    .AsNoTrackingWithIdentityResolution()  // Prevents duplicate instances
    .ToListAsync();
```

---

**Q44: What is the difference between `FirstOrDefault()` and `SingleOrDefault()`?**

**Answer:**

**Theory:** Both retrieve one entity, but they differ in HOW MANY results they expect and how they handle MULTIPLE results. `FirstOrDefault()` returns the FIRST element regardless of how many match. It stops querying as soon as it finds one match (SQL: `SELECT TOP 1`). Use it when you expect ZERO OR MORE results and only need the first one. `SingleOrDefault()` expects EXACTLY ONE match — if multiple rows match, it THROWS `InvalidOperationException`. It must scan all results to verify uniqueness. Use it when the predicate should uniquely identify ONE row (by primary key, by unique constraint). The performance difference: `FirstOrDefault` stops at the first match; `SingleOrDefault` must scan the entire result set. For primary key lookups, both are equivalent (PK guarantees uniqueness). For non-unique filters, `FirstOrDefault` is faster and safer. When in doubt about uniqueness, use `FirstOrDefault` — it never throws for multiple matches.

**Code Example:**
```csharp
// FirstOrDefault — safe, fast, expects 0 or more results
var activeUser = db.Users.FirstOrDefault(u => u.IsActive);
// Returns the FIRST active user (or null if none)
// SQL: SELECT TOP 1 * FROM Users WHERE IsActive = 1
// Safe if 5 active users exist — returns first one

// SingleOrDefault — strict, expects exactly 0 or 1
var userById = db.Users.SingleOrDefault(u => u.Id == 5);
// Returns user with Id=5 (or null if doesn't exist)
// SQL: SELECT * FROM Users WHERE Id = 5
// Throws if somehow 2 users have Id=5 (shouldn't happen with PK)

// DANGEROUS — SingleOrDefault with non-unique field:
var admin = db.Users.SingleOrDefault(u => u.Role == "Admin");
// Throws InvalidOperationException if 2+ admins exist!
// Use FirstOrDefault instead:
var anyAdmin = db.Users.FirstOrDefault(u => u.Role == "Admin");  // Safe
```

---

**Q45: How do you handle concurrent updates in EF Core?**

**Answer:**

**Theory:** When two users read the same data, then both try to update it, the second update OVERWRITES the first — this is a **lost update** (last-in-wins). EF Core handles this with **optimistic concurrency** — instead of locking rows (pessimistic), it checks that the data hasn't changed since you read it. A **concurrency token** (usually a `byte[] RowVersion` column, also called `Timestamp` in SQL Server) tracks the version. When you read the entity, you get the row version. When you save, EF Core includes the original row version in the UPDATE's WHERE clause: `UPDATE ... WHERE Id = @id AND RowVersion = @originalVersion`. If someone else updated the row first, the row version doesn't match, EF Core throws `DbUpdateConcurrencyException`, and ZERO rows are affected. You then DETECT this, reload the current data, and let the user RESOLVE the conflict (overwrite, merge, or cancel). This is the standard pattern for web/mobile apps where locking rows for long periods is impractical.

**Code Example:**
```csharp
public class Battery
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public int AssignedToUserId { get; set; }

    [Timestamp]  // ← This is the concurrency token
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public async Task<bool> AssignBatteryAsync(int batteryId, int userId)
{
    var battery = await db.Batteries.FindAsync(batteryId);
    var originalVersion = battery.RowVersion;  // Capture original version

    battery.AssignedToUserId = userId;
    battery.Status = "Assigned";

    try
    {
        await db.SaveChangesAsync();
        return true;
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // Someone else modified this battery between our read and write
        var entry = ex.Entries.Single();
        var databaseValues = await entry.GetDatabaseValuesAsync();

        // databaseValues.RowVersion != originalVersion
        // Reload and let user decide:
        await entry.ReloadAsync();
        // Now battery has latest data
        return false;  // Let caller decide what to do
    }
}
```

---

## Design Patterns & SOLID Principles

**Q46: Explain the SOLID principles with examples.**

**Answer:**

**Theory:** SOLID is five design principles that make code maintainable, testable, and extensible. They're the foundation of clean OOP architecture.

- **S — Single Responsibility Principle**: A class should have ONE reason to change. If a class handles authentication AND logging AND email, it changes for three reasons. Split it into `AuthService`, `LoggingService`, `EmailService`. Each has one job, one reason to change.

- **O — Open/Closed Principle**: Classes should be OPEN for extension but CLOSED for modification. Add new behavior by creating NEW classes (extending), not modifying existing ones. Implement with interfaces and inheritance — add new implementations without touching the originals.

- **L — Liskov Substitution Principle**: Subtypes must be SUBSTITUTABLE for their base types. If code uses a `Rectangle`, it should work with a `Square` that inherits from `Rectangle`. The classic violation: `Square` setting Height also changes Width — breaks the expectation.

- **I — Interface Segregation Principle**: Many SMALL, FOCUSED interfaces are better than one LARGE interface. `IAuthService` (login/logout) and `IApiService` (HTTP calls) are separate. A class that only needs HTTP shouldn't depend on auth methods it never uses.

- **D — Dependency Inversion Principle**: Depend on ABSTRACTIONS, not CONCRETIONS. `LoginViewModel` depends on `IAuthService` (interface), NOT on `AuthService` (concrete class). This allows swapping implementations and mocking for tests.

**Code Example:**
```csharp
// S — Single Responsibility: One class, one job
public class AuthService { /* handles authentication only */ }
public class EmailService { /* sends emails only */ }
// NOT: public class AuthService { void Login(); void SendEmail(); void LogError(); }

// O — Open/Closed: Extend via new classes, don't modify existing
public interface INotificationService { void Send(string message); }
public class EmailNotification : INotificationService { public void Send(string m) { /* email */ } }
public class SmsNotification : INotificationService { public void Send(string m) { /* SMS */ } }
// Add PushNotification without touching EmailNotification or SmsNotification

// L — Liskov Substitution: Subtypes work where base is expected
// BAD: Square modifies Rectangle behavior
// GOOD: Both inherit from IShape, not from each other

// I — Interface Segregation: Small, focused interfaces
public interface IAuthService { Task LoginAsync(string u, string p); }
public interface IApiService { Task<T> GetAsync<T>(string ep); }
// A class that only needs HTTP doesn't depend on auth methods

// D — Dependency Inversion: Depend on abstractions
public class LoginViewModel {
    private readonly IAuthService _auth;  // Interface, not concrete class
    public LoginViewModel(IAuthService auth) => _auth = auth;
}
```

---

**Q47: Explain the Repository Pattern.**

**Answer:**

**Theory:** The Repository pattern is a **mediator** between the data access layer and the business logic layer. Instead of writing EF Core queries everywhere in your business code, you encapsulate all queries in a Repository class that exposes CLEAN methods like `GetByIdAsync()`, `GetNearbyAsync()`, `SearchAsync()`. Benefits: (1) **Centralization** — all Station queries in one place. (2) **Testability** — mock `IStationRepository` in unit tests. (3) **Abstraction** — business code doesn't know about EF Core, SQL, or the database provider. (4) **Flexibility** — switch from SQL Server to PostgreSQL by changing ONE repository implementation. The Repository interface is defined in the DOMAIN layer (your business code), and the implementation is in the INFRASTRUCTURE layer (EF Core project). This follows the Dependency Inversion Principle — domain defines the interface, infrastructure implements it. The pattern shines with complex queries (spatial queries, full-text search, aggregation) that would clutter business code.

**Code Example:**
```csharp
// Domain layer — defines the interface (what queries are possible)
public interface IStationRepository
{
    Task<Station?> GetByIdAsync(int id);
    Task<List<Station>> GetNearbyAsync(double lat, double lng, double radiusKm);
}

// Infrastructure layer — implements with EF Core
public class StationRepository : IStationRepository
{
    private readonly AppDbContext _db;

    public StationRepository(AppDbContext db) => _db = db;

    public async Task<Station?> GetByIdAsync(int id) =>
        await _db.Stations.FindAsync(id);

    public async Task<List<Station>> GetNearbyAsync(double lat, double lng, double radiusKm)
    {
        return await _db.Stations
            .FromSqlInterpolated($@"
                SELECT * FROM Stations
                WHERE dbo.Distance(Latitude, Longitude, {lat}, {lng}) <= {radiusKm}")
            .ToListAsync();
    }
}

// Business layer — depends on abstraction, not implementation
public class StationService
{
    private readonly IStationRepository _repo;
    public StationService(IStationRepository repo) => _repo = repo;

    public async Task<List<Station>> FindNearbyStationsAsync(double lat, double lng)
    {
        return await _repo.GetNearbyAsync(lat, lng, 10);  // 10km radius
        // StationService has NO idea this queries SQL Server
    }
}
```

---

**Q48: What is the Factory Pattern?**

**Answer:**

**Theory:** The Factory pattern **encapsulates object creation** — instead of calling `new` directly, you ask a Factory to create the object for you. This is useful when: (1) The creation logic is complex (needs configuration, DI resolution, or conditional logic). (2) The exact type isn't known until runtime (decided by configuration, feature flags, or context). (3) You want to centralize creation (all API service creation in one place). In MAUI with DI, `IServiceProvider` IS a factory — you call `_serviceProvider.GetRequiredService<T>()`. But sometimes you need runtime parameters that DI can't provide (an ID from user input, a configuration flag). A factory method takes these parameters and passes them to the constructor. The Factory pattern is closely related to DI — they're both about INVERTING control of object creation. DI handles most cases; Factory handles cases where runtime parameters determine the object.

**Code Example:**
```csharp
// Factory: decides at runtime which service to create
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
        // Runtime decision based on environment or feature flag
        return useMock
            ? _provider.GetRequiredService<MockApiService>()    // Mock for dev
            : _provider.GetRequiredService<RealApiService>();   // Real for prod
    }
}

// Usage: decide at runtime based on connectivity
public async Task LoadDataAsync()
{
    // If offline, use mock factory (returns cached/stub data)
    var api = _factory.Create(!_connectivity.IsConnected);
    var data = await api.GetAsync<DashboardModel>("/api/dashboard");
}
```

---

**Q49: What is the Strategy Pattern?**

**Answer:**

**Theory:** The Strategy pattern lets you SWAP algorithms at runtime without changing the code that uses them. You define a family of algorithms (retry strategies, authentication methods, notification channels), encapsulate each in its own class, and make them INTERCHANGEABLE through a common interface. The calling code depends on the interface, NOT on a specific strategy. This is the **O** in SOLID (Open/Closed) — you add new strategies without modifying existing ones. Common uses: (1) Retry strategies — exponential backoff vs no retry vs immediate retry. (2) Authentication — JWT vs OAuth vs API Key. (3) Notification — Email vs SMS vs Push. (4) Pricing — normal price vs holiday discount vs bulk discount. In MAUI, strategies are often injected via DI — the consumer doesn't know which strategy it received. A configuration setting or feature flag determines which strategy to register.

**Code Example:**
```csharp
// Strategy interface: defines the contract
public interface IRetryStrategy
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
}

// Concrete strategy 1: Exponential backoff
public class ExponentialBackoffStrategy : IRetryStrategy
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        for (int i = 0; i < 3; i++)
        {
            try { return await operation(); }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));  // 2s, 4s
            }
        }
        return await operation();  // Last attempt (no delay)
    }
}

// Concrete strategy 2: No retry
public class NoRetryStrategy : IRetryStrategy
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        => await operation();  // Single attempt, no retry
}

// Consumer — doesn't know which strategy it uses:
public class ApiService
{
    private readonly IRetryStrategy _retryStrategy;

    public ApiService(IRetryStrategy retryStrategy)  // Strategy injected via DI
    {
        _retryStrategy = retryStrategy;
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        return await _retryStrategy.ExecuteAsync(() => /* actual HTTP call */);
        // Behavior changes based on which IRetryStrategy is registered
    }
}
```

---

**Q50: What is the difference between `Aggregate Root`, `Entity`, and `Value Object` in DDD?**

**Answer:**

**Theory:** These are Domain-Driven Design concepts that model business domains more accurately than simple "classes."

- **Entity** — Has a UNIQUE IDENTITY that persists across state changes. Two entities with the same field values are DIFFERENT if they have different IDs. Examples: `User` (userId=1 vs userId=2), `Order`, `Station`. Entities are mutable — their properties change over time.

- **Value Object** — Has NO identity. Two value objects with the same property values are EQUAL. They're IMMUTABLE — once created, they never change. Examples: `Address`, `Money(amount, currency)`, `Color`, `DateRange`. If you need a "different" value, you create a new one, you don't modify the existing one.

- **Aggregate Root** — An Entity that OWNS other entities/value objects and enforces business INVARIANTS (rules that must always be true). External code can ONLY reference the aggregate root, never its internal children. Example: `Order` (aggregate root) owns `OrderLineItem`s (entities). You add items THROUGH the `Order.AddItem()` method, which validates business rules (item not already added, quantity > 0, total doesn't exceed credit limit). You never manipulate `OrderLineItem` directly.

The purpose: **encapsulation and consistency**. Aggregates ensure that related objects change together, and business rules are enforced in one place.

**Code Example:**
```csharp
// Entity: has identity
public class User
{
    public int Id { get; set; }       // Identity! Two users with same name are different
    public string Name { get; set; }  // Mutable — can change over time
}

// Value Object: no identity, immutable
public class Address
{
    public string Street { get; }      // Immutable — set in constructor only
    public string City { get; }
    public string ZipCode { get; }

    public Address(string street, string city, string zip)
    {
        Street = street;
        City = city;
        ZipCode = zip;
    }

    public override bool Equals(object? obj) =>
        obj is Address a && Street == a.Street && City == a.City;
    // Two Addresses with same values ARE equal
}

// Aggregate Root: owns entities, enforces invariants
public class SwapRequest  // Aggregate Root
{
    public int Id { get; private set; }
    private List<Battery> _batteries = new();
    public IReadOnlyList<Battery> Batteries => _batteries.AsReadOnly();

    // Business rule: max 5 batteries per swap request
    public void AddBattery(Battery battery)
    {
        if (_batteries.Count >= 5)
            throw new InvalidOperationException("Max 5 batteries per swap");

        if (_batteries.Any(b => b.Id == battery.Id))
            throw new InvalidOperationException("Battery already added");

        _batteries.Add(battery);
        // Invariant enforced here — external code can't break it
    }
}
```

---

## 50+ Done — You Can Crack Any Interview

**You've mastered the core concepts.** Here's the truth: 90% of interview questions are variations of the 50 above. If you can explain each concept's THEORY (what, why, when) and show the CODE, you can handle any .NET MAUI interview.

### How to prepare (48-hour plan):

| Day | Focus | Activities |
|-----|-------|-----------|
| **Day 1** | **C# & .NET Core** (Q1-Q10, Q31-Q35) | Read theory, code each concept. Write async/await by hand. Explain SOLID with examples. |
| **Day 1** | **MAUI Framework** (Q11-Q20, Q21-Q25) | Understand Shell, CollectionView, compiled bindings. Explain MVVM without code. |
| **Day 2** | **DI, REST, EF Core** (Q26-Q30, Q36-Q45) | Explain DI lifetimes. Write an HTTP client wrapper. Practice EF Core queries. |
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
