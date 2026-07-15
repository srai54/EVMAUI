# .NET Deep Dive — 100+ Interview Questions

> **Pure .NET questions** covering CLR internals, loading strategies, memory management, threading, reflection, performance, and advanced runtime concepts. No MAUI/EF Core/REST — just the .NET runtime and framework.

---

## How to Use This Guide

Each question has two parts:
- **Theory** — The conceptual foundation. Understand the "why" behind the concept.
- **Code Example** — Practical implementation. See how the theory applies in real code.

---

## 1. Loading Strategies (Lazy, Eager, Explicit)

**Q1: What is the difference between Lazy Loading and Eager Loading in .NET/EF Core?**

**Answer:**

**Theory:** These are two strategies for loading related data, primarily in the context of EF Core and ORMs. **Eager Loading** loads related entities as part of the initial query using `.Include()` / `.ThenInclude()`. It issues a single SQL query with JOINs — all data arrives in one round trip. **Lazy Loading** defers loading related entities until they're accessed. When you access `order.Customer`, EF Core issues a separate SQL query at that moment. Lazy Loading requires either (a) installing `Microsoft.EntityFrameworkCore.Proxies` and calling `UseLazyLoadingProxies()`, or (b) making navigation properties `virtual`. The trade-off: eager loading = fewer queries but potentially over-fetches data; lazy loading = fetches only what's accessed but causes N+1 query problems. Think of eager loading as "buying everything you might need upfront" and lazy loading as "buying each item when you pick it up."

**Code Example:**
```csharp
// Eager Loading — single SQL with JOIN
var orders = await db.Orders
    .Include(o => o.Customer)
    .ThenInclude(c => c.Address)
    .ToListAsync();
// SQL: SELECT * FROM Orders JOIN Customers ON ... JOIN Addresses ON ...

// Lazy Loading — separate query on access
var orders = await db.Orders.ToListAsync();
foreach (var order in orders)
{
    // Each access to order.Customer triggers a NEW SQL query
    Console.WriteLine(order.Customer.Name);
}
// SQL: SELECT * FROM Orders
//      SELECT * FROM Customers WHERE Id = 1
//      SELECT * FROM Customers WHERE Id = 2  (N+1 problem)
```

---

**Q2: What is Explicit Loading and how does it differ from Lazy Loading?**

**Answer:**

**Theory:** Explicit Loading is a middle ground: you manually trigger the loading of related data with an explicit `.Load()` call, but you control when it happens. Unlike Lazy Loading (fires automatically on property access), Explicit Loading requires your code to say `await entry.Collection(o => o.Items).LoadAsync()`. Unlike Eager Loading (loaded upfront in the query), Explicit Loading happens after the initial query completes. Use Explicit Loading when: you need the initial data fast (no JOIN overhead), but you KNOW you'll need some related data later based on a condition. It gives you the performance of a lean initial query with the control of manual loading — no surprise N+1 queries.

**Code Example:**
```csharp
// Initial query — no related data
var order = await db.Orders.FirstAsync(o => o.Id == 1);

// Later, explicitly load related items only if needed
if (order.Status == "Pending")
{
    // Explicit Load — triggers one additional query
    await db.Entry(order)
        .Collection(o => o.OrderItems)
        .LoadAsync();
}

// Also works for references (single related entity)
await db.Entry(order)
    .Reference(o => o.Customer)
    .LoadAsync();
```

---

**Q3: What is Lazy Initialization (`Lazy<T>`) in general .NET?**

**Answer:**

**Theory:** `Lazy<T>` is a thread-safe wrapper that defers object creation until the first time `.Value` is accessed. It's useful for expensive objects that might not be needed — database connections, large configuration objects, service clients. The factory function passed to the constructor runs once and its result is cached for all subsequent accesses. `Lazy<T>` provides three thread-safety modes: `IsThreadSafe = true` (default, uses lock-free or locking), `LazyThreadSafetyMode.PublicationOnly` (multiple threads can race, winner's value is used), `LazyThreadSafetyMode.None` (not thread-safe, fastest). Think of it as a "create on first use" contract that the runtime guarantees is executed at most once. The alternative is eager initialization (`new ExpensiveObject()` at construction time) which guarantees the object is ready but may waste resources if never used.

**Code Example:**
```csharp
public class DashboardViewModel
{
    private readonly Lazy<ExpensiveService> _service;

    public DashboardViewModel()
    {
        // Service is NOT created yet — just the factory is stored
        _service = new Lazy<ExpensiveService>(() =>
        {
            // This factory runs ONCE, on first .Value access
            var s = new ExpensiveService();
            s.Initialize("db-connection-string");
            return s;
        });
    }

    public async Task LoadDataAsync()
    {
        // First access triggers creation; subsequent accesses use cached instance
        var data = await _service.Value.FetchDataAsync();
    }
}

// Eager equivalent (creates immediately even if never used):
private readonly ExpensiveService _service = new ExpensiveService();
```

---

**Q4: How does `LazyInitializer` differ from `Lazy<T>`?**

**Answer:**

**Theory:** `LazyInitializer` is a static utility class that provides lightweight lazy initialization without allocating a `Lazy<T>` wrapper object. It uses `ref` parameters and `Volatile` / `Interlocked` internally. `EnsureInitialized` is the key method — it checks if the target is null, and if so, calls the factory to create it. This is more memory-efficient than `Lazy<T>` because no wrapper object exists, but it's also more limited — no `IsValueCreated`, no `Mode` options, and the target field must be accessible (not readonly). Use `LazyInitializer` in hot paths / low-allocation scenarios where every byte of heap allocation matters.

**Code Example:**
```csharp
private static ExpensiveService? _service;

public static ExpensiveService Service
{
    get
    {
        // No Lazy<T> allocation — uses Interlocked.CompareExchange internally
        LazyInitializer.EnsureInitialized(ref _service, () => new ExpensiveService());
        return _service;
    }
}

// Equivalent Lazy<T> — allocates ~32 bytes for the Lazy wrapper
private static readonly Lazy<ExpensiveService> _lazy = new(() => new ExpensiveService());
public static ExpensiveService Service => _lazy.Value;
```

---

**Q5: What are the memory implications of `Lazy<T>` vs eager initialization?**

**Answer:**

**Theory:** A `Lazy<T>` instance itself occupies ~32 bytes on the heap (object header + vtable + fields). The factory delegate it captures also allocates memory. On first access, the created value is stored. The overhead of `Lazy<T>` is: (1) the wrapper object, (2) the delegate closure (if capturing state), (3) synchronization primitives for thread safety. Eager initialization has ZERO overhead if the object is always used (no wrapper, no delegate, no locking) but wastes memory and CPU if the object is never used. The decision rule: if the object is used > 80% of the time, use eager (simpler, faster). If < 20%, use `Lazy<T>` (save memory). Between 20-80%, profile first. For singleton services in DI, eager is fine — the container creates them once and they live for the app's lifetime.

**Code Example:**
```csharp
// Eager — object created immediately, always occupies memory
private static readonly ExpensiveService _service = new();

// Lazy — wrapper + delegate overhead (~80 bytes), object created on first use
private static readonly Lazy<ExpensiveService> _service = new(() => new ExpensiveService());

// Ultra-light — no wrapper, no delegate (if static factory)
private static ExpensiveService? _service;
public static ExpensiveService Service =>
    _service ??= CreateService();
```

---

**Q6: What is the difference between `FirstOrDefault()` and `SingleOrDefault()` in LINQ — is one "lazier"?**

**Answer:**

**Theory:** Both use deferred execution (lazy evaluation), but they differ in iteration behavior. `FirstOrDefault()` stops iterating as soon as it finds the first match — it's O(1) best case, O(n) worst case. `SingleOrDefault()` must iterate the ENTIRE sequence to verify there's exactly one match — it's always O(n) because it needs to check for duplicates. Neither is "lazier" — both are lazy (deferred). The difference is the **contract**: `FirstOrDefault` says "give me the first one or null", `SingleOrDefault` says "there MUST be exactly one or null". Use `FirstOrDefault` for paginated/top-N queries; use `SingleOrDefault` when you expect uniqueness (e.g., fetching by primary key where duplicate rows are an error condition).

**Code Example:**
```csharp
// FirstOrDefault — stops at first match
var user = await db.Users
    .Where(u => u.Email == "test@test.com")
    .FirstOrDefaultAsync();
// SQL: SELECT TOP 1 * FROM Users WHERE Email = @p0

// SingleOrDefault — requires full scan to verify uniqueness
var user = await db.Users
    .Where(u => u.Email == "test@test.com")
    .SingleOrDefaultAsync();
// SQL: SELECT * FROM Users WHERE Email = @p0  (then checks count client-side)

// For primary key lookups, SingleOrDefault is safe (DB enforces uniqueness)
var user = await db.Users.FindAsync(42);
// Already unique — no need for SingleOrDefault
```

---

**Q7: What is the difference between `IEnumerable<T>`, `IQueryable<T>`, and `ICollection<T>` in terms of loading?**

**Answer:**

**Theory:** `IEnumerable<T>` is an **in-memory iterator** — when you call `.Where()`, it iterates the in-memory collection and filters client-side. ALL data is loaded into memory first. `IQueryable<T>` is a **query builder** — it builds an expression tree that gets translated to SQL (or other query language) and executed on the server. Only the result set is loaded into memory. `ICollection<T>` guarantees in-memory collection features (Add, Remove, Count) — it's always fully loaded. The key: `IQueryable` = lazy composition (build query, execute once), `IEnumerable` = lazy iteration (pull items one-by-one but all data is in memory), `ICollection` = fully loaded. In EF Core, `db.Users.Where(u => u.Age > 18)` returns `IQueryable` — the SQL filter is generated. If you call `.ToList()` first, then `.Where()`, it's `IEnumerable` — filtering happens in memory.

**Code Example:**
```csharp
// IQueryable — SQL filter, only matching rows loaded
IQueryable<User> query = db.Users.Where(u => u.Age > 18);
// SQL: SELECT * FROM Users WHERE Age > 18

// IEnumerable — ALL users loaded, then filtered in memory
IEnumerable<User> enumerable = db.Users.ToList();
var adults = enumerable.Where(u => u.Age > 18);
// SQL: SELECT * FROM Users  (no WHERE!)
// Then C# loops through every row in memory

// ICollection — fully loaded, supports Add/Remove/Clear
ICollection<User> collection = db.Users.ToList();
collection.Add(new User());  // works, but doesn't persist to DB
```

---

**Q8: What is deferred execution in LINQ and how does it affect loading?**

**Answer:**

**Theory:** Deferred execution (lazy evaluation) means a LINQ query is NOT executed when defined — it's executed when enumerated. `.Where()`, `.Select()`, `.OrderBy()` all return `IEnumerable<T>` / `IQueryable<T>` without touching the data source. Only terminal operations (`.ToList()`, `.First()`, `.Count()`, `.Any()`, `foreach`) trigger execution. This lets you compose complex queries: you can chain 10 LINQ methods, all building expression trees / iterator state machines, and nothing happens until the final `ToList()`. The SQL query is sent to the DB only once, at enumeration. Benefits: (1) compose queries conditionally, (2) avoid redundant DB round trips, (3) the DB evaluates filter/sort/aggregate, not your app.

**Code Example:**
```csharp
// Deferred — nothing executes yet
var query = db.Users.Where(u => u.IsActive);

// Add conditions conditionally
if (filterByRole)
    query = query.Where(u => u.Role == "Admin");

if (sortByName)
    query = query.OrderBy(u => u.Name);

// EXECUTION happens here — single SQL query
var results = await query.ToListAsync();
// SQL: SELECT * FROM Users WHERE IsActive = 1 AND Role = 'Admin' ORDER BY Name
```

---

## 2. Garbage Collection & Memory Management

**Q9: How does the .NET GC work?**

**Answer:**

**Theory:** The .NET Garbage Collector is a **generational, compacting, mark-and-sweep** collector. It divides the managed heap into three generations: **Gen 0** (short-lived objects like local variables — collected most frequently, ~10ms), **Gen 1** (objects that survived Gen 0 — buffer zone), **Gen 2** (long-lived objects like singletons, static references — collected rarely, can cause pauses of 100ms+). The **Large Object Heap (LOH)** holds objects > 85KB (arrays, strings, images) and is collected with Gen 2. GC modes: **Workstation GC** (default for client apps — one heap per process, low latency) vs **Server GC** (default for ASP.NET Core — one heap per logical CPU, higher throughput, more memory). On each collection, the GC: (1) marks reachable objects by tracing from roots (static fields, stack locals, CPU registers), (2) compacts survivors by sliding them together (eliminates fragmentation), (3) promotes survivors to the next generation. A Gen 0 collection is fast (~1-10ms) because Gen 0 is small. A Gen 2 collection is slow (~50-500ms) because it scans the entire heap.

**Code Example:**
```csharp
// Avoiding allocations on hot paths
public class StringBuilderCache
{
    [ThreadStatic]
    private static StringBuilder? _cached;

    public static StringBuilder Acquire(int capacity = 256)
    {
        if (_cached is not null)
        {
            var sb = _cached;
            _cached = null;
            sb.Clear();
            return sb;
        }
        return new StringBuilder(capacity);
    }

    public static void Release(StringBuilder sb)
    {
        if (_cached is null && sb.Length < 4096)
            _cached = sb;  // Reuse — avoids Gen 0 allocation next time
    }
}
```

---

**Q10: What causes Gen 2 / LOH collections and how do you minimize them?**

**Answer:**

**Theory:** Gen 2 collections are triggered when: (1) Gen 1 fills up after a Gen 1 collection, (2) LOH fragmentation reaches a threshold, (3) `GC.Collect(2)` is called explicitly (DON'T do this). Gen 2 collections pause all threads (for workstation GC) and can cause noticeable UI stutter in MAUI apps or request latency in ASP.NET Core. Minimize by: (1) keeping the LOH defragmented — avoid pinning large objects, (2) pooling large arrays (`ArrayPool<T>`), (3) reducing allocations in hot paths (use `StringBuilder`, cached delegates, structs), (4) using `GC.TryStartNoGCRegion()` for latency-sensitive operations, (5) preferring `Server GC` in ASP.NET Core (each heap is smaller, collections are more frequent but faster). The worst offender: repeatedly allocating and discarding large byte arrays (e.g., loading full images into memory).

**Code Example:**
```csharp
// BAD — allocates LOH repeatedly
byte[] buffer = new byte[100_000];
// ... use buffer ...
// buffer goes out of scope → Gen 2 collects it later

// GOOD — pool large arrays to reduce LOH pressure
var pool = ArrayPool<byte>.Shared;
byte[] buffer = pool.Rent(100_000);
try
{
    // ... use buffer ...
}
finally
{
    pool.Return(buffer);  // Returns to pool, NOT garbage
}
```

---

**Q11: What is a `WeakReference` and when would you use it?**

**Answer:**

**Theory:** A `WeakReference<T>` (introduced in .NET 4.5, replaces non-generic `WeakReference`) holds a reference that does NOT prevent the GC from collecting the object. If the GC collects the target, `TryGetTarget(out T?)` returns `false`. Use cases: (1) **caches** — a cache that holds WeakReferences can be collected under memory pressure without explicit eviction logic, (2) **event subscriptions** — the `WeakReferenceMessenger` in MVVM Toolkit uses weak references for subscribers, (3) **large object caches** — images, byte arrays that can be recreated if needed. The trade-off: `WeakReference<T>` has overhead (allocation + finalization tracking). Don't use it for small, short-lived objects — the overhead exceeds the benefit. Also: the GC collects weak references during Gen 2 collections only, not Gen 0/1, so a weak-referenced object might live longer than expected.

**Code Example:**
```csharp
public class ImageCache
{
    private readonly Dictionary<string, WeakReference<Bitmap>> _cache = new();

    public Bitmap? GetImage(string url)
    {
        if (_cache.TryGetValue(url, out var wr) && wr.TryGetTarget(out var bitmap))
            return bitmap;  // Cache hit — still alive

        _cache[url] = new WeakReference<Bitmap>(LoadFromDisk(url));
        return null;
    }
}
```

---

**Q12: What is `GC.AddMemoryPressure` / `GC.RemoveMemoryPressure`?**

**Answer:**

**Theory:** When your managed code wraps an **unmanaged resource** (e.g., a native bitmap, an OS handle), the GC doesn't know about the unmanaged memory consumption — it only sees the managed wrapper object (a few bytes). The unmanaged memory could be hundreds of MB. Without memory pressure, the GC won't trigger collections, leading to `OutOfMemoryException`. `AddMemoryPressure(long bytes)` tells the GC "I'm also using this much unmanaged memory" — the GC factors this into its collection timing and triggers earlier. `RemoveMemoryPressure` tells the GC the unmanaged resource was freed. Always pair these with `IDisposable` — call `RemoveMemoryPressure` in `Dispose()`. Forgetting to call `RemoveMemoryPressure` is a bug: the GC thinks memory is still consumed and may trigger unnecessary collections.

**Code Example:**
```csharp
public class LargeNativeBitmap : IDisposable
{
    private IntPtr _nativeHandle;
    private bool _disposed;

    public LargeNativeBitmap(int width, int height)
    {
        _nativeHandle = CreateNativeBitmap(width, height);
        GC.AddMemoryPressure(width * height * 4);  // ~4 bytes per pixel
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            DestroyNativeBitmap(_nativeHandle);
            _nativeHandle = IntPtr.Zero;
            GC.RemoveMemoryPressure(/* same bytes */);
            _disposed = true;
        }
    }
}
```

---

**Q13: What is `GC.TryStartNoGCRegion` and when is it useful?**

**Answer:**

**Theory:** `TryStartNoGCRegion` temporarily disables GC for latency-sensitive operations. Before entering, you must specify the amount of memory you'll allocate — the GC pre-allocates sufficient heap space so it doesn't NEED to collect during the region. If the region's allocation exceeds the specified budget, a blocking GC occurs anyway (defeating the purpose). Use for: real-time rendering (60fps games), audio processing, financial transactions where a multi-millisecond pause is unacceptable. Don't use for: ordinary API calls, data loading, UI updates — the GC pause is negligible. The region must be short (< 100ms) and bounded — holding it open too long starves the GC and can lead to OOM.

**Code Example:**
```csharp
public void RenderFrame()
{
    if (GC.TryStartNoGCRegion(1024 * 1024))  // 1 MB budget
    {
        try
        {
            // Render one frame without GC interruptions
            // Allocate up to 1 MB — no collections will happen
            DrawScene();
        }
        finally
        {
            GC.EndNoGCRegion();  // GC resumes normal operation
        }
    }
}
```

---

**Q14: What is `fixed` statement and how does it relate to pinning?**

**Answer:**

**Theory:** The `fixed` statement pins a managed object in memory so the GC doesn't move it during compaction. This is necessary when passing managed arrays / strings to unmanaged code via P/Invoke — the native code expects a fixed memory address. Pinning is expensive because: (1) it prevents memory compaction (fragments the heap), (2) objects that are pinned for a long time get promoted to Gen 2, making them harder to collect. Minimize pinning by: using `Marshal.AllocHGlobal` (allocate unmanaged memory, copy data, call native, free) for long-lived operations, or using `GCHandle.Alloc(obj, GCHandleType.Pinned)` with explicit `Free()`. In modern .NET, `Span<T>` and `Memory<T>` reduce the need for `fixed` by providing safe, GC-friendly memory abstractions.

**Code Example:**
```csharp
// Pinning a string to pass to Windows API
[DllImport("user32.dll", CharSet = CharSet.Auto)]
private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

public static void ShowMessage(string text)
{
    // The runtime handles pinning implicitly for string parameters
    MessageBox(IntPtr.Zero, text, "Alert", 0);
}

// Explicit pinning with fixed
unsafe
{
    fixed (int* ptr = &myArray[0])
    {
        // ptr is pinned — GC won't move myArray
        NativeMethod(ptr, myArray.Length);
    }
}  // ptr unpins here — GC can move myArray again
```

---

## 3. JIT, AOT & Runtime Compilation

**Q15: What is the difference between JIT and AOT compilation in .NET?**

**Answer:**

**Theory:** **JIT (Just-In-Time)** compiles IL (Intermediate Language) to native machine code at runtime, method by method, on first call. This means: (1) startup is slower (each method compiles on first use), (2) the JIT can optimize based on the current CPU (SSE, AVX), (3) IL assemblies remain loaded in memory. **AOT (Ahead-Of-Time)** compiles ALL IL to native code at build time. .NET Native AOT (`.NET Native AOT` / `Native AOT`) produces a single native executable with no runtime JIT. Benefits: (1) faster startup (30-50% improvement), (2) smaller memory footprint (no JIT/IL needed), (3) smaller deployment (single .exe, no .NET runtime required for self-contained). Limitations: (1) no dynamic code generation (no `Expression.Compile`, no `Reflection.Emit`), (2) reflection is restricted (types not used at compile time may be trimmed), (3) some libraries don't support AOT. MAUI on Windows supports Native AOT; MAUI on iOS always uses Full AOT (Apple mandates it); on Android, you can choose.

**Code Example:**
```csharp
// Project file for Native AOT (console app)
// <PropertyGroup>
//     <PublishAot>true</PublishAot>
// </PropertyGroup>

// This works in AOT — static reflection data is preserved
var type = typeof(User);
var prop = type.GetProperty("Name");

// This FAILS in AOT — dynamic code generation at runtime
// Expression<Func<User, string>> expr = u => u.Name;
// var compiled = expr.Compile();  // NOT AOT-compatible
```

---

**Q16: What is Tiered Compilation?**

**Answer:**

**Theory:** Tiered Compilation (default since .NET Core 3.0) compiles methods in two tiers. **Tier 0**: quick JIT with minimal optimizations — gets the method running fast at startup. **Tier 1**: re-JIT with full optimizations — runs after the method has been called enough times (threshold). The benefit: startup time improves because methods aren't fully optimized on first call. Over time, frequently-called methods get recompiled with all optimizations (inlining, loop unrolling, bounds-check elimination). You can disable tiered compilation with `DOTNET_TieredCompilation=0` — all methods get Tier 1 immediately, but startup is slower. The trade-off: Tier 0 methods run slower initially, but the app starts faster. For MAUI, tiered compilation helps because startup is critical for user perception.

**Code Example:**
```csharp
// This method starts at Tier 0 (quick JIT) on first call
public int CalculateTotal(List<int> items)
{
    int sum = 0;
    foreach (var item in items)
        sum += item;
    return sum;
}

// After ~30 calls (default threshold), it's re-JITted to Tier 1
// Tier 1 adds: inlining, loop optimization, bounds-check removal

// Disable tiered compilation (for benchmarking):
// Environment.SetEnvironmentVariable("DOTNET_TieredCompilation", "0");
```

---

**Q17: What is `ReadyToRun` (R2R) and how does it differ from Native AOT?**

**Answer:**

**Theory:** ReadyToRun (R2R) is a **hybrid** approach: the build generates native code for methods that are likely hot, but the native code is bundled alongside the IL in the assembly. At runtime, the JIT uses the pre-compiled native code when available (skipping JIT for those methods) but still JIT-compiles methods that weren't pre-compiled. R2R improves startup time (30-40%) without sacrificing the JIT's ability to optimize for the current CPU. It also keeps all IL available for dynamic code generation. Native AOT, by contrast, compiles EVERYTHING upfront — no JIT remains, no IL assemblies. R2R is a "best of both worlds" compromise. Use R2R for ASP.NET Core APIs and desktop apps where startup matters but you still need some JIT flexibility; use Native AOT for minimal deployments (CLI tools, microservices) where the JIT overhead is unacceptable.

**Code Example:**
```csharp
// Project file for R2R
// <PropertyGroup>
//     <PublishReadyToRun>true</PublishReadyToRun>
// </PropertyGroup>

// Project file for Native AOT
// <PropertyGroup>
//     <PublishAot>true</PublishAot>
// </PropertyGroup>

// R2R keeps IL — this still works:
var compiled = someExpression.Compile();

// Native AOT removes IL — Expression.Compile() throws at runtime
```

---

**Q18: What is `MethodImplAttribute` and how can you control inlining?**

**Answer:**

**Theory:** The JIT decides which methods to inline (replace the call site with the method's body). Inlining reduces call overhead but increases code size (can cause instruction cache pressure). `MethodImplOptions.AggressiveInlining` provides a **hint** (not a command) to the JIT that this method SHOULD be inlined, even if it's larger than the default threshold. `MethodImplOptions.NoInlining` prevents inlining — useful when: (1) you want clear stack traces in exception logging, (2) the method is a "cold" path that shouldn't bloat the hot path, (3) the method uses `MethodImplOptions.Synchronized` (which locks on `this` — dangerous! Use `lock` instead). In AOT scenarios, inlining is done at compile time, so `AggressiveInlining` has more predictable effect (the compiler makes the final decision).

**Code Example:**
```csharp
// Force inline — use for tiny, hot-path methods
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static int Square(int x) => x * x;

// Prevent inline — use for exception-logging (keep stack trace clean)
[MethodImpl(MethodImplOptions.NoInlining)]
public static void LogError(string msg)
{
    File.AppendAllText("error.log", msg);
}

// Bad practice — Synchronized locks on 'this', potential deadlock
[MethodImpl(MethodImplOptions.Synchronized)]
public void DoSomething() { }  // Equivalent to: lock(this) { }
```

---

## 4. Reflection, Emit & Dynamic Code

**Q19: How does reflection work in .NET?**

**Answer:**

**Theory:** Reflection allows inspecting and invoking types, methods, properties, and fields at runtime using the metadata stored in assemblies. `typeof(MyClass)` loads the `Type` object from the assembly's metadata tables. `GetMethod()`, `GetProperty()`, and `GetField()` return `MethodInfo`, `PropertyInfo`, `FieldInfo` objects that have metadata about the member. `Invoke()` / `SetValue()` / `GetValue()` access the actual data. Reflection is slow compared to direct calls because: (1) metadata lookups are O(n) scans (unless cached), (2) `Invoke` involves boxing, argument array allocation, and virtual dispatch overhead, (3) null/security checks on every call. For hot paths, cache `MethodInfo` or use **delegates** created via `Delegate.CreateDelegate` (much faster). Reflection is the foundation of serialization, DI containers, ORMs, and unit test frameworks.

**Code Example:**
```csharp
// Reflection inspection
Type type = typeof(User);
PropertyInfo[] props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
foreach (var prop in props)
    Console.WriteLine($"{prop.Name}: {prop.PropertyType.Name}");

// Reflection invocation — slow, allocates arrays
object user = Activator.CreateInstance(typeof(User))!;
type.GetProperty("Name")!.SetValue(user, "Alice");

// FAST alternative — cached delegates
var setName = (Action<User, string>)Delegate.CreateDelegate(
    typeof(Action<User, string>),
    typeof(User).GetMethod("set_Name")!);
var user = new User();
setName(user, "Alice");  // Near-direct-call speed
```

---

**Q20: What is `Expression<T>` and why is it faster than raw reflection?**

**Answer:**

**Theory:** `Expression<T>` represents code as a **data structure** (expression tree) rather than compiled IL. You can analyze, modify, and compile expression trees into delegates at runtime via `.Compile()`. The compiled delegate is nearly as fast as handwritten code because it generates the same IL that the C# compiler would produce. This is the foundation of: (1) **EF Core query translation** — `Where(u => u.Age > 18)` is an expression tree that gets translated to SQL WHERE clause; (2) **AutoMapper** — property mapping expressions compiled to fast delegates; (3) **MVVM Toolkit source generators** — replacing runtime expression work with compile-time generated code. The speed hierarchy: direct call (1x) ≈ compiled expression (1-2x) < Delegate.CreateDelegate (3-5x) < MethodInfo.Invoke (20-50x) < dynamic (50-100x).

**Code Example:**
```csharp
// Build an expression tree and compile it
Expression<Func<User, string>> expr = u => u.Name;
Func<User, string> compiled = expr.Compile();

// Same speed as: (User u) => u.Name
string name = compiled(user);

// Building expressions manually (what AutoMapper does internally)
var param = Expression.Parameter(typeof(User), "u");
var prop = Expression.Property(param, "Name");
var lambda = Expression.Lambda<Func<User, string>>(prop, param);
var fastGetter = lambda.Compile();
```

---

**Q21: What is `Reflection.Emit` and when would you use it?**

**Answer:**

**Theory:** `System.Reflection.Emit` allows generating IL (Intermediate Language) instructions at runtime, creating new types, methods, and assemblies dynamically. This is lower-level than `Expression<T>` — you write IL opcodes directly (`Ldarg_0`, `Callvirt`, `Ret`). Use cases: (1) **dynamic proxies** (Castle.Core, what Moq/NSubstitute use) — creating proxy classes that intercept method calls, (2) **serializers** generating optimized read/write code per type (faster than reflection), (3) **AOP frameworks** injecting code at runtime. `Reflection.Emit` is NOT AOT-compatible — the generated IL must be JIT-compiled. For modern code, prefer **source generators** (compile-time code generation) over `Reflection.Emit` — they're AOT-safe, faster at startup, and easier to debug (you can see generated code).

**Code Example:**
```csharp
// Emit a method that returns the square of an integer
var method = new DynamicMethod("Square", typeof(int),
    new[] { typeof(int) }, typeof(Program).Module);
var il = method.GetILGenerator();
il.Emit(OpCodes.Ldarg_0);        // load the argument
il.Emit(OpCodes.Dup);             // duplicate it (for multiplication)
il.Emit(OpCodes.Mul);             // multiply
il.Emit(OpCodes.Ret);             // return result

var square = (Func<int, int>)method.CreateDelegate(typeof(Func<int, int>));
Console.WriteLine(square(5));  // 25
```

---

**Q22: What is `Covariance` and `Contravariance` in .NET generics?**

**Answer:**

**Theory:** Variance controls how type parameters can be substituted in generic types. **Covariance** (`out T`) means "I only produce T" — you can use a more derived type where a base type is expected. `IEnumerable<Dog>` can be treated as `IEnumerable<Animal>` (you can only read Dogs as Animals). **Contravariance** (`in T`) means "I only consume T" — you can use a less derived type where a more derived type is expected. `Action<Animal>` can be treated as `Action<Dog>` (you can pass any Animal to the action, and a Dog IS an Animal). **Invariance** (no `in`/`out`) means neither — `List<Dog>` is NOT `List<Animal>` because you could add a Cat to it. The compiler enforces variance safety: covariance prevents writes (no `Add(T)`), contravariance prevents reads (no `T Get()`). Arrays are covariant but NOT safe: `Animal[] animals = new Dog[5]; animals[0] = new Cat();` compiles but throws `ArrayTypeMismatchException` at runtime.

**Code Example:**
```csharp
// Covariance — IEnumerable<out T>
IEnumerable<Dog> dogs = new List<Dog>();
IEnumerable<Animal> animals = dogs;  // OK — covariance

// Contravariance — IComparer<in T>
IComparer<Animal> animalComparer = new AnimalComparer();
IComparer<Dog> dogComparer = animalComparer;  // OK — contravariance

// Invariance — List<T> (no in/out)
List<Dog> dogsList = new List<Dog>();
// List<Animal> animalsList = dogsList;  // COMPILE ERROR — invariant
```

---

## 5. Threading, Parallelism & Concurrency

**Q23: What is the difference between `Task`, `Thread`, and `ThreadPool`?**

**Answer:**

**Theory:** **`Thread`** is the lowest-level abstraction — an OS thread with its own stack (~1 MB per thread). Creating threads is expensive (~200µs + 1MB commit), and too many threads cause context-switching overhead. **`ThreadPool`** reuses threads — it maintains a pool of worker threads. When you queue work, a pooled thread picks it up. This avoids create/destroy overhead. **`Task`** represents an **asynchronous operation** — it doesn't require a dedicated thread. A `Task` can be: (1) CPU-bound (runs on ThreadPool), (2) I/O-bound (no thread while waiting — uses overlapped I/O), (3) a continuation (runs when antecedent completes). `Task` is the recommended abstraction for ALL async work. Use `Thread` only for long-running dedicated operations (e.g., a background audio player). Use `Task.Run()` for CPU-bound work on the ThreadPool. Use `async`/`await` for I/O-bound work.

**Code Example:**
```csharp
// Thread — 1MB stack, expensive creation
Thread thread = new Thread(() => DoWork());
thread.Start();

// ThreadPool — reuses threads, avoids creation cost
ThreadPool.QueueUserWorkItem(_ => DoWork());

// Task — lightweight, can be I/O-bound (no thread while waiting)
Task task = Task.Run(() => DoWork());  // CPU-bound, uses ThreadPool

// Async I/O — NO thread while waiting
async Task<string> ReadFileAsync(string path)
{
    using var reader = File.OpenText(path);
    return await reader.ReadToEndAsync();  // No thread holds here
}
```

---

**Q24: What is `async`/`await` and how does it compile?**

**Answer:**

**Theory:** The `async`/`await` keywords are a compiler transformation — NOT a runtime construct. The compiler turns an `async` method into a **state machine struct** implementing `IAsyncStateMachine`. Each `await` becomes a state in a `switch` statement inside `MoveNext()`. When an awaited operation hasn't completed, `MoveNext` returns immediately (the Task is "pending"). When the operation completes, it calls back into `MoveNext` via its `INotifyCompletion` interface, resuming from the saved state. This is why `async` methods don't block threads during I/O — no thread is "parked" waiting; the state machine just resumes when the operation signals completion. The overhead: one `Task` allocation per method call, one boxed state machine if there's an async await (struct state machine gets boxed to heap), and a delegate allocation per continuation.

**Code Example:**
```csharp
// Compiler transforms this:
public async Task<User> GetUserAsync(int id)
{
    var json = await _httpClient.GetStringAsync($"/api/users/{id}");
    return JsonSerializer.Deserialize<User>(json);
}

// Into something conceptually like:
public Task<User> GetUserAsync(int id)
{
    var stateMachine = new GetUserAsyncStateMachine { _this = this, id = id };
    stateMachine._builder = AsyncTaskMethodBuilder<User>.Create();
    stateMachine._builder.Start(ref stateMachine);
    return stateMachine._builder.Task;
}
// Where stateMachine.MoveNext() contains a switch(_state) with cases
// for each await point, saving/restoring locals between states.
```

---

**Q25: What is `ConfigureAwait(false)` and when should you use it?**

**Answer:**

**Theory:** By default, when an `async` method resumes after `await`, the continuation is **marshaled back to the original SynchronizationContext** (UI thread in MAUI/WinForms, ASP.NET request context in ASP.NET). This is essential for ViewModels (must update UI on UI thread). In **library/service code** that doesn't touch UI, this marshaling is wasteful: (1) the continuation must be posted to the UI message pump, (2) it waits for the UI thread to process its queue, (3) it adds overhead per await. `ConfigureAwait(false)` skips this context capture — the continuation runs on any available ThreadPool thread. **Rule of thumb:** every `await` in a library/service/repository should use `ConfigureAwait(false)`. Every `await` in a ViewModel/page should NOT. In ASP.NET Core, `SynchronizationContext` is NOT captured by default (it was removed in ASP.NET Core), so `ConfigureAwait(false)` has no effect — but it's still a good habit.

**Code Example:**
```csharp
// Library code — use ConfigureAwait(false)
public async Task<User?> GetUserAsync(int id)
{
    var json = await _httpClient
        .GetStringAsync($"/api/users/{id}")
        .ConfigureAwait(false);  // Resume on ThreadPool, NOT UI

    return JsonSerializer.Deserialize<User>(json);
}

// ViewModel code — do NOT use ConfigureAwait(false)
[RelayCommand]
async Task LoginAsync()
{
    IsBusy = true;
    var user = await _authService.LoginAsync(Username, Password);
    // Must return to UI thread to set bindable properties
    IsBusy = false;
}
```

---

**Q26: What is `SemaphoreSlim` and how does it differ from `lock`?**

**Answer:**

**Theory:** Both control access to a shared resource, but they differ fundamentally. **`lock`** is a language keyword that uses `Monitor.Enter`/`Exit` — it BLOCKS the current thread until the lock is acquired. A blocked thread cannot be used for anything else. **`SemaphoreSlim`** is a lightweight semaphore with `WaitAsync()` — it AWAITS asynchronously without blocking a thread. If the semaphore is full, `WaitAsync()` returns an incomplete Task and the thread is freed to do other work. The callback resumes when the semaphore releases. SemaphoreSlim also supports (1) initial/max count (e.g., 3 allows 3 concurrent calls), (2) cancellation via `CancellationToken`, (3) no reentrancy (doesn't detect if the same thread tries to acquire twice). Use `lock` for quick, synchronous critical sections (< 1µs). Use `SemaphoreSlim` for async operations (await inside the critical section) and for rate-limiting concurrent access.

**Code Example:**
```csharp
private readonly SemaphoreSlim _gate = new(3, 3);  // Max 3 concurrent

public async Task<Data> FetchAsync(string key)
{
    await _gate.WaitAsync();  // Non-blocking — thread is freed while waiting
    try
    {
        return await _cache.GetOrCreateAsync(key, _ => FetchFromDbAsync());
    }
    finally
    {
        _gate.Release();
    }
}

// Equivalent with lock — would BLOCK the thread:
private readonly object _lock = new();
public Data FetchSync(string key)
{
    lock (_lock)
    {
        return _cache.GetOrCreate(key, _ => FetchFromDb());
    }
}
```

---

**Q27: What is `ConcurrentDictionary<TKey, TValue>` and how does it work internally?**

**Answer:**

**Theory:** `ConcurrentDictionary<TKey, TValue>` is a thread-safe dictionary that uses **fine-grained locking** — it doesn't lock the entire dictionary for each operation. Internally, it's divided into multiple **buckets** (or stripes), each with its own lock. A read/write operation locks only the bucket containing the relevant key, not the whole dictionary. This allows concurrent reads and writes to different buckets without contention. Key atomic operations: `GetOrAdd` (check if exists, add if not — atomic), `AddOrUpdate` (add or update atomically), `TryRemove`, `TryUpdate`. Iteration (`foreach`) is NOT thread-safe — it takes a snapshot of keys/values. Use `ConcurrentDictionary` when multiple threads simultaneously read/write to the same dictionary. For single-threaded access, `Dictionary<TKey, TValue>` is faster and uses less memory.

**Code Example:**
```csharp
private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

public async Task<Data> GetOrCreateAsync(string key, Func<Task<Data>> factory)
{
    // GetOrAdd is atomic — no two threads create the same key's semaphore
    var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

    await semaphore.WaitAsync();
    try
    {
        return await factory();
    }
    finally
    {
        semaphore.Release();
    }
}
```

---

**Q28: What is `Channel<T>` in `System.Threading.Channels`?**

**Answer:**

**Theory:** `Channel<T>` is a high-performance, thread-safe producer/consumer queue introduced in .NET Core 3.0. It replaces `BlockingCollection<T>` with an async-first API. The writer produces items (`channel.Writer.WriteAsync(item)`), the reader consumes them (`await foreach (var item in channel.Reader.ReadAllAsync())`). Channels can be **bounded** (fixed capacity — writer waits when full, providing backpressure) or **unbounded** (infinite capacity — writer never waits). Key features: (1) async read/write (no blocking), (2) completion signaling (`channel.Writer.Complete()` notifies readers no more items), (3) cancellation support, (4) single-reader or multi-reader modes. Use `Channel<T>` for: streaming data pipelines, log processing, WebSocket message broadcasting, or any async producer-consumer scenario.

**Code Example:**
```csharp
// Producer
var channel = Channel.CreateBounded<SwapRequest>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait  // Producer waits when full
});

// Producer writes
await channel.Writer.WriteAsync(new SwapRequest { Id = 1 });

// Consumer reads
await foreach (var request in channel.Reader.ReadAllAsync())
{
    await ProcessSwapAsync(request);
}
```

---

## 6. .NET Runtime & CLR Internals

**Q29: What is the difference between `Stack` and `Heap` in .NET?**

**Answer:**

**Theory:** The **stack** is a per-thread, LIFO memory region (~1MB per thread) that stores value types, method parameters, and return addresses. Allocation is O(1) — just move the stack pointer. Deallocation is automatic when the method returns (stack pointer moves back). The **heap** is a shared region that stores reference type objects. Allocation requires finding a free memory block, and deallocation is handled by the GC (non-deterministic). Value types on the stack are faster because: no allocation overhead, no GC pressure, better cache locality. Value types as fields of reference types live ON the heap (embedded in the parent object). The `ref` keyword and `Span<T>` allow stack-like efficient access to heap arrays. The `stackalloc` keyword explicitly allocates on the stack.

**Code Example:**
```csharp
public void Method()
{
    int x = 5;              // Stack — value type
    string s = "hello";     // Reference on stack, string data on heap
    Point p = new(1, 2);    // Stack — value type (struct)

    // stackalloc — explicitly stack-allocate a span
    Span<int> buffer = stackalloc int[256];  // No heap allocation!
    buffer[0] = 42;
}
```

---

**Q30: What is `AppDomain` and how does it differ in .NET Core vs .NET Framework?**

**Answer:**

**Theory:** In .NET Framework, `AppDomain` provides **process-level isolation** within a single OS process: each AppDomain has its own assembly loader, security boundary, and configuration. ASP.NET Classic used AppDomains to isolate web applications. In .NET Core/.NET 5+, AppDomains are **partially removed** — you cannot create custom AppDomains. The `AppDomain.CurrentDomain` exists only for backward-compatible APIs (like `UnhandledException`). .NET Core uses **AssemblyLoadContext** instead for assembly isolation — it lets you load and unload assemblies (with `Collectible` assemblies), enabling plugin scenarios. The key difference: `AssemblyLoadContext` is lightweight compared to AppDomains (no security boundary, no configuration isolation). For modern plugin systems, use `AssemblyLoadContext`.

**Code Example:**
```csharp
// .NET Core — AssemblyLoadContext for plugin isolation
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly Load(AssemblyName name)
    {
        string? path = _resolver.ResolveAssemblyToPath(name);
        if (path is not null)
            return LoadFromAssemblyPath(path);
        return null!;
    }
}

// Usage — load, use, then UNLOAD
var context = new PluginLoadContext(@"plugins\myplugin.dll");
var assembly = context.LoadFromAssemblyName(new AssemblyName("MyPlugin"));
// ... use the plugin ...
context.Unload();  // Unloads the assembly — NOT possible with AppDomains
```

---

**Q31: What is the `IDisposable` pattern and when do you need `Dispose(bool)`?**

**Answer:**

**Theory:** `IDisposable` provides deterministic cleanup for unmanaged resources (file handles, sockets, database connections, GDI+ objects). The **Dispose pattern** has two paths: (1) **`Dispose()`** — called by user code (`using` statement), cleans up both managed and unmanaged resources, suppresses finalization. (2) **`Finalizer`** (`~ClassName()`) — called by GC if `Dispose()` was never called, cleans up ONLY unmanaged resources (managed objects may already be collected). The `Dispose(bool disposing)` overload distinguishes the two: `disposing = true` means called from `Dispose()` (clean up everything), `disposing = false` means called from finalizer (clean up only unmanaged). Never throw from `Dispose()`. Never call `Dispose()` on objects from `ServiceProvider` (the container owns the lifetime). The modern alternative: `SafeHandle` wraps unmanaged handles safely, reducing the need for custom finalizers.

**Code Example:**
```csharp
public class DatabaseConnection : IDisposable
{
    private IntPtr _nativeHandle;
    private bool _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);  // Don't call finalizer — already cleaned up
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Clean up MANAGED resources (other IDisposables)
            _transaction?.Dispose();
        }

        // Clean up UNMANAGED resources (native handles)
        if (_nativeHandle != IntPtr.Zero)
        {
            CloseNativeHandle(_nativeHandle);
            _nativeHandle = IntPtr.Zero;
        }

        _disposed = true;
    }

    ~DatabaseConnection() => Dispose(false);  // Finalizer — only for unmanaged
}
```

---

**Q32: What is `IAsyncDisposable` and how does it differ from `IDisposable`?**

**Answer:**

**Theory:** `IAsyncDisposable` provides async cleanup for resources that need async operations to release (e.g., closing a network stream, flushing a file buffer). The `DisposeAsync()` method returns a `ValueTask`, and you call it with `await using`. The difference from `IDisposable`: `Dispose()` must be synchronous — no `await` inside. `DisposeAsync()` allows async cleanup, which is essential for I/O-bound resources. The pattern mirrors `IDisposable`: implement `DisposeAsyncCore()` for managed resources and `DisposeAsync()` for unmanaged. The `await using` declaration (C# 8+) compiles to a `try/finally` with `DisposeAsync()`.

**Code Example:**
```csharp
public class NetworkStream : IAsyncDisposable
{
    private Socket _socket;

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_socket is not null)
        {
            await _socket.ShutdownAsync(SocketShutdown.Both);
            _socket.Dispose();
            _socket = null!;
        }
    }
}

// Usage
await using var stream = new NetworkStream();
// ... use ...
// DisposeAsync() called automatically at end of scope
```

---

## 7. Performance & Optimization

**Q33: What is `Span<T>` and `Memory<T>`?**

**Answer:**

**Theory:** `Span<T>` is a **ref struct** — a stack-only type that provides a type-safe, memory-safe view over contiguous memory (arrays, unmanaged memory, stackalloc). It's like a window into a block of memory with a start index and length. Ref structs can't be boxed, used as generic type arguments, or captured in lambdas/async methods. `Memory<T>` solves the async limitation — it's a regular struct that can be stored on the heap, enabling use in async methods and as class fields. Both eliminate allocations for slicing: `"hello world".AsSpan()[0..5]` gives `"hello"` without allocating a new string. Use cases: (1) JSON/BSON parsing, (2) base64 encoding/decoding, (3) string processing, (4) low-allocation streaming. `Span<T>` is the foundation of modern high-performance .NET — it's what makes `System.Text.Json` 2-3x faster than Newtonsoft.

**Code Example:**
```csharp
// Zero-allocation string slicing
ReadOnlySpan<char> email = "user@example.com".AsSpan();
int atIndex = email.IndexOf('@');
ReadOnlySpan<char> name = email[..atIndex];     // "user" — no allocation
ReadOnlySpan<char> domain = email[(atIndex + 1)..];  // "example.com" — no allocation

// Memory<T> for async scenarios
Memory<byte> buffer = new byte[4096];
int bytesRead = await stream.ReadAsync(buffer);  // Span<T> can't be used here

// Span<T> with stackalloc (zero heap allocation)
Span<byte> temp = stackalloc byte[256];
int written = Encoding.UTF8.GetBytes("hello", temp);
```

---

**Q34: What is `ArrayPool<T>` and when should you use it?**

**Answer:**

**Theory:** `ArrayPool<T>` manages a pool of reusable arrays, reducing GC pressure from frequent large-array allocations. Instead of `new byte[1024]` (which allocates on the LOH if > 85KB), you rent from the pool: `ArrayPool<byte>.Shared.Rent(1024)`. The returned array may be larger than requested (pool rounds up). When you return it via `Return(array)`, it goes back to the pool for reuse. The pool has per-size buckets — arrays of similar sizes share a bucket. Use for: (1) network buffers, (2) serialization/deserialization, (3) temporary working arrays in hot paths. Don't use for: small arrays (struct overhead > benefit), long-lived arrays (tying up pool slots), or arrays you modify and hold (returning a modified array causes bugs for the next consumer). The default pool has a maximum of ~2^20 elements per bucket.

**Code Example:**
```csharp
public async Task ProcessDataAsync(NetworkStream stream)
{
    byte[] buffer = ArrayPool<byte>.Shared.Rent(81920);  // Rent, not new
    try
    {
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            ProcessBuffer(buffer, bytesRead);
        }
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);  // Return to pool
    }
}
```

---

**Q35: What is `ValueTask<T>` and how does it differ from `Task<T>`?**

**Answer:**

**Theory:** `Task<T>` is a **reference type** — every async method call allocates a new `Task<T>` object on the heap, even if the result is available synchronously. For hot-path async methods that often complete synchronously (e.g., reading from a memory cache), this allocation is wasteful. `ValueTask<T>` is a **value type** that can wrap either a `T` result (if completed synchronously) or a `Task<T>` (if completed asynchronously). When the operation completes synchronously, no heap allocation occurs. The trade-off: `ValueTask<T>` can only be awaited once (no `Task.WhenAll`), and it's more complex for library authors. Use `ValueTask<T>` for: (1) methods that often complete synchronously (cache hits, in-memory operations), (2) `IAsyncEnumerator<T>.MoveNextAsync()`. Use `Task<T>` for: (1) methods that usually complete asynchronously, (2) return values that need to be awaited multiple times, combined, or cached.

**Code Example:**
```csharp
public class CacheService
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());

    public ValueTask<User?> GetUserAsync(int id)
    {
        // Check cache first — often completes synchronously
        if (_cache.TryGetValue($"user:{id}", out User? cached))
            return new ValueTask<User?>(cached);  // No allocation!

        // Cache miss — need async operation
        return new ValueTask<User?>(FetchFromDbAsync(id));  // Task allocated
    }

    private async Task<User?> FetchFromDbAsync(int id)
    {
        // ... database call ...
    }
}
```

---

**Q36: What is the `StructLayoutAttribute` and how does it affect memory layout?**

**Answer:**

**Theory:** By default, the .NET runtime lays out struct fields in an **auto** layout — it can reorder and pad fields for performance (cache-line alignment). For interop with unmanaged code (P/Invoke), you need `[StructLayout(LayoutKind.Sequential)]` — fields are laid out in declaration order with platform-specific padding. `[StructLayout(LayoutKind.Explicit)]` with `[FieldOffset]` lets you manually control every byte — this is how **unions** are implemented (multiple fields at the same offset). The `Pack` property controls alignment (1 = no padding, 8 = default). Sequential without explicit Pack uses the type's natural alignment (int = 4, long = 8). For performance-critical code, Sequential with Pack=1 eliminates padding bytes (denser but may be slower for unaligned access). For managed-only structs, Auto layout is usually fastest (runtime picks optimal layout).

**Code Example:**
```csharp
// Sequential — for interop with unmanaged code
[StructLayout(LayoutKind.Sequential)]
public struct Point
{
    public int X;
    public int Y;
}

// Explicit — manual byte control (C union equivalent)
[StructLayout(LayoutKind.Explicit)]
public struct Union
{
    [FieldOffset(0)] public int Int;
    [FieldOffset(0)] public float Float;
    [FieldOffset(0)] public uint UInt;
}

// Pack = 1 — no padding (denser)
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PackedHeader
{
    public byte Version;   // 1 byte
    public ushort Length;  // 2 bytes (no gap)
    public uint Checksum;  // 4 bytes (no gap)
}
// Without Pack=1: Version (1) + 1 padding + Length (2) + 4 padding + Checksum (4) = 12 bytes
// With Pack=1: Version (1) + Length (2) + Checksum (4) = 7 bytes
```

---

## 8. Configuration, Logging & Diagnostics

**Q37: How does the `Options` pattern work in .NET?**

**Answer:**

**Theory:** The Options pattern provides strongly-typed, validated configuration access. Define a POCO, register it with `services.Configure<TOptions>(configSection)`, and inject `IOptions<TOptions>` where needed. The framework binds the configuration section to the POCO at startup. Three interfaces: (1) **`IOptions<T>`** — singleton, reads config at startup, doesn't reload on change. (2) **`IOptionsSnapshot<T>`** — scoped, reloads per request (ASP.NET Core) or per scope. (3) **`IOptionsMonitor<T>`** — singleton, reloads on config file change, supports change notifications via `OnChange`. Validation: use `[Required]`, `[Range]`, etc., and call `services.AddOptions<TOptions>().Bind(config).ValidateDataAnnotations().ValidateOnStart()` to catch misconfiguration early.

**Code Example:**
```csharp
public class ApiOptions
{
    public const string Section = "Api";
    [Required, Url]
    public string BaseUrl { get; set; } = "";
    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 30;
}

// Registration
builder.Services.AddOptions<ApiOptions>()
    .Bind(builder.Configuration.GetSection(ApiOptions.Section))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Injection
public class ApiService
{
    public ApiService(IOptions<ApiOptions> options)
    {
        var opts = options.Value;
        _httpClient.BaseAddress = new Uri(opts.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
    }
}
```

---

**Q38: How does `ILogger<T>` handle structured logging?**

**Answer:**

**Theory:** Structured logging captures not just a formatted string but also named properties. `_logger.LogInformation("User {User} logged in from {IP}", username, ipAddress)` creates a log entry with the message AND `User=alice`, `IP=192.168.1.1` as structured data. This enables powerful querying: "show all logins by user" or "count logins by IP" without parsing strings. The template placeholders (`{User}`, `{IP}`) don't use string interpolation — they're separate parameters that logging backends (Serilog, Application Insights) capture as fields. Always use structured logging (with placeholders) instead of string concatenation for log messages. The log level hierarchy: Trace (diagnostic detail), Debug (development debugging), Information (normal flow), Warning (unexpected but handled), Error (failure), Critical (app-crashing failure). Each level can be independently enabled/disabled.

**Code Example:**
```csharp
public class OrderService
{
    private readonly ILogger<OrderService> _logger;

    public async Task<Order> CreateOrderAsync(OrderRequest request)
    {
        // GOOD — structured, queryable
        _logger.LogInformation(
            "Creating order for user {UserId} with {ItemCount} items",
            request.UserId, request.Items.Count);

        try
        {
            var order = await _db.Orders.AddAsync(request);
            _logger.LogInformation("Order {OrderId} created successfully", order.Id);
            return order;
        }
        catch (Exception ex)
        {
            // BAD — string interpolation loses structure
            _logger.LogError($"Failed to create order for {request.UserId}");

            // GOOD — structured exception logging
            _logger.LogError(ex, "Failed to create order for user {UserId}", request.UserId);
            throw;
        }
    }
}
```

---

**Q39: What is `Activity` / `DiagnosticSource` for distributed tracing?**

**Answer:**

**Theory:** `System.Diagnostics.Activity` is .NET's built-in distributed tracing API, part of OpenTelemetry integration. An `Activity` represents a unit of work in a distributed system. It carries a `TraceId` (unique across all services) and `SpanId` (unique within the service). Activities form a hierarchy: an HTTP request activity can have child activities for database calls, external API calls, etc. `DiagnosticSource` is the producer — components like `HttpClient` and `ASP.NET Core` emit `DiagnosticSource` events that listeners (like Application Insights or Jaeger) consume. To trace across services: propagate the `TraceId` via HTTP headers (`traceparent` header follows W3C Trace Context standard). OpenTelemetry .NET SDK automatically collects distributed traces, metrics, and logs.

**Code Example:**
```csharp
// Custom activity
using var activity = DiagnosticsConfig.Source.StartActivity("ProcessOrder");
activity?.SetTag("order.id", orderId);
activity?.SetTag("order.amount", amount);

// Child activity for external call
using var dbActivity = DiagnosticsConfig.Source.StartActivity("SaveToDatabase");
dbActivity?.SetTag("db.table", "Orders");
await _db.SaveChangesAsync();
dbActivity?.SetStatus(ActivityStatusCode.Ok);

// Configure OpenTelemetry in Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());
```

---

## 9. Serialization & Data

**Q40: What is `System.Text.Json` source generation and why does it matter?**

**Answer:**

**Theory:** Normally `System.Text.Json` uses runtime reflection to read/write JSON — it calls `typeof(User).GetProperties()` at runtime. This is slow, prevents type trimming, and breaks on AOT platforms (iOS, Native AOT). **Source generators** solve this: you declare `[JsonSerializable(typeof(User))]` on a partial class deriving from `JsonSerializerContext`, and at compile time the source generator produces explicit read/write code for each property. The generated code is regular IL — no reflection. Benefits: (1) 2-3x faster serialization, (2) AOT compatible, (3) smaller app size (reflection metadata can be trimmed). The trade-off: every serialized type must be explicitly listed. For large models with many DTOs, this is boilerplate. Use the `System.Text.Json` source generator for any MAUI app targeting Windows AOT or iOS.

**Code Example:**
```csharp
// Annotate types to serialize
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(List<User>))]
internal partial class AppJsonContext : JsonSerializerContext { }

// Use it — no reflection!
var options = new JsonSerializerOptions
{
    TypeInfoResolver = AppJsonContext.Default
};

// Fast, AOT-safe serialization
string json = JsonSerializer.Serialize(user, options);
User? deserialized = JsonSerializer.Deserialize<User>(json, options);
```

---

**Q41: How does `XmlSerializer` differ from `DataContractSerializer`?**

**Answer:**

**Theory:** **`XmlSerializer`** (from `System.Xml.Serialization`) is the older, full-featured XML serializer. It supports: (1) attributes like `[XmlElement]`, `[XmlAttribute]`, `[XmlIgnore]`, (2) control over XML element names, namespaces, and order, (3) `IXmlSerializable` for custom serialization. It generates an assembly with serialization code at runtime (or ahead-of-time with `sgen.exe`). **`DataContractSerializer`** (from `System.Runtime.Serialization`) is a newer, simpler serializer. It uses `[DataContract]` and `[DataMember]` attributes. It's typically used in WCF. DataContractSerializer is faster than XmlSerializer for large object graphs and supports serialization of types without a parameterless constructor. For new code, prefer `System.Text.Json` or the newer `XmlSerializer` (still supported in .NET Core). DataContractSerializer should only be used for legacy WCF compatibility.

**Code Example:**
```csharp
[XmlRoot("User")]
public class XmlUser
{
    [XmlElement("FullName")]
    public string Name { get; set; } = "";
    
    [XmlAttribute("Id")]
    public int Id { get; set; }
    
    [XmlIgnore]
    public string Internal { get; set; } = "";
}

// XmlSerializer — full control over XML structure
var serializer = new XmlSerializer(typeof(XmlUser));
using var writer = new StringWriter();
serializer.Serialize(writer, user);
// Output: <User Id="1"><FullName>Alice</FullName></User>
```

---

## 10. Dependency Injection in .NET

**Q42: How does the built-in .NET DI container work?**

**Answer:**

**Theory:** The `Microsoft.Extensions.DependencyInjection` container is a lightweight, built-in IoC container. Three lifetimes: **Singleton** (one instance for the entire app — created on first request or at registration time), **Scoped** (one instance per scope/incoming request — in ASP.NET Core, one per HTTP request), **Transient** (new instance every time it's requested). The container builds a dependency graph — when you request a service, it resolves ALL its constructor dependencies recursively. The container does NOT support: property injection, method injection, named services, or interceptors. For those, you need third-party containers (Autofac, StructureMap). The `IServiceProvider` is the service locator — call `GetRequiredService<T>()` on it. The **Composition Root** is the single place where all services are registered (MauiProgram.cs for MAUI, Program.cs for ASP.NET Core). After `Build()`, the container is sealed.

**Code Example:**
```csharp
// Registration in Composition Root
var services = new ServiceCollection();
services.AddSingleton<IApiService, ApiService>();
services.AddScoped<IAuthService, AuthService>();
services.AddTransient<LoginViewModel>();
services.AddTransient<LoginPage>();

var provider = services.BuildServiceProvider();

// Resolution
var vm = provider.GetRequiredService<LoginViewModel>();
// The container injects IApiService (singleton) into LoginViewModel's constructor

// Transient: each call creates a new instance
var vm1 = provider.GetRequiredService<LoginViewModel>();
var vm2 = provider.GetRequiredService<LoginViewModel>();
// vm1 != vm2 — different instances
```

---

**Q43: What is the `Dispose` behavior of the DI container?**

**Answer:**

**Theory:** The DI container tracks instances it creates and disposes them when the scope or container is disposed. **Singleton** instances are disposed when the container itself is disposed. **Scoped** instances are disposed when the scope is disposed (end of HTTP request in ASP.NET Core). **Transient** instances that are resolved FROM the container are tracked and disposed (this changed in .NET Core 3.0+ — transients from the container are disposed to prevent leaks). Transients that are manually `new`'d or resolved from a service that's not the root container are NOT disposed. Important: the container calls `Dispose()` on all `IDisposable` instances it created — but only if they were resolved from the container. If you create an object with `new`, the container knows nothing about it. Also: registering something as Singleton that implements `IDisposable` will keep it alive for the entire app lifetime — be careful not to register expensive/unmanaged resources as Singleton if they should be short-lived.

**Code Example:**
```csharp
// All disposables created by the container are tracked
services.AddSingleton<ExpensiveService>();  // Disposed when provider is disposed
services.AddScoped<DbContext>();             // Disposed when scope ends
services.AddTransient<StreamWriter>();       // Disposed when scope ends

// NOT disposed by container (created outside)
var writer = new StreamWriter("log.txt");

// BUG: transient registered as singleton — lives forever
services.AddSingleton<FileLogger>();  // File handle held until app exits
```

---

**Q44: What is the difference between `Add*` and `TryAdd*` extension methods?**

**Answer:**

**Theory:** `services.AddSingleton<T>()` adds the service unconditionally — if another registration for the same service already exists, the new one is added (multi-registration — all registered implementations are available via `IEnumerable<T>`). `services.TryAddSingleton<T>()` only adds the service if NO registration for that service type exists yet. Use `TryAdd` in **library code** (extension methods in NuGet packages) so the consumer can override the default registration. Use `Add` in **application code** (Composition Root in Program.cs) where you control the registration order. `TryAddEnumerable` is similar but checks by implementation type — useful for adding to a collection of services (e.g., `IHealthCheck` implementations in ASP.NET Core).

**Code Example:**
```csharp
// Library code — allows consumers to override
public static IServiceCollection AddMyLibrary(this IServiceCollection services)
{
    services.TryAddSingleton<IDefaultService, DefaultService>();  // Won't override if already registered
    services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidator, DefaultValidator>());
    return services;
}

// Application code — explicit registration
services.AddSingleton<IDefaultService, MyCustomService>();  // This runs first

// Library's TryAddSingleton won't replace MyCustomService because IDefaultService is already registered
services.AddMyLibrary();
```

---

## 11. C# Fundamentals — Types & Memory

**Q45: What is boxing and unboxing? How do you avoid it?**

**Answer:**

**Theory:** Boxing wraps a **value type** (struct, int, bool) inside a **reference type object** on the heap. Happens when you assign a value type to `object`, `ValueType`, or an interface. Unboxing extracts the value type back. Both are expensive: boxing allocates heap memory + copy, unboxing copies back + type-check overhead. In hot paths, boxing causes GC pressure. Avoid by: (1) using generics instead of `object` parameters, (2) using `ArrayPool<T>` instead of `ArrayList`, (3) using `string.Concat` vs `string.Format` (which boxes), (4) implementing interfaces on structs carefully (calling `IFoo` on a struct boxes it). .NET's `Span<T>`, `ReadOnlySpan<T>`, and generic math reduce boxing scenarios.

**Code Example:**
```csharp
int number = 42;

// BOXING — allocates heap object
object boxed = number;

// UNBOXING — copies back, checks type
int unboxed = (int)boxed;

// AVOID — ArrayList stores object (boxes every int)
var list = new ArrayList { 1, 2, 3 };

// PREFER — List<T> stores value types directly (no boxing)
var better = new List<int> { 1, 2, 3 };

// Hidden boxing — string.Format boxes value types
string msg = string.Format("Value: {0}", 42); // boxes the int
// Fix: use .ToString() or string interpolation (no boxing in modern .NET)
string msg2 = $"Value: {42}";
```

---

**Q46: What is the difference between `var`, `dynamic`, and `object`?**

**Answer:**

**Theory:** `var` is **compile-time type inference** — the compiler determines the type from the right-hand side. The variable is strongly typed after declaration. `object` is the base type of ALL types — storing a value in `object` boxes it (for value types) and loses compile-time type information (you must cast). `dynamic` bypasses compile-time type checking entirely — method calls, property access, and operators are resolved at runtime using the DLR (Dynamic Language Runtime). `dynamic` is useful for COM interop, dynamic languages (IronPython), and JSON deserialization to `ExpandoObject`. But it's slow (runtime dispatch), has no IntelliSense, and can throw `RuntimeBinderException`. Prefer `var` always, `object` almost never, `dynamic` only for interop scenarios.

**Code Example:**
```csharp
var x = "hello";       // x is string — compile-time, strongly typed
x = 42;                // COMPILE ERROR — can't assign int to string

object obj = "hello";  // obj is object — no compile-time type info
obj = 42;              // OK — but now obj is boxed int
int len = obj.Length;  // COMPILE ERROR — object doesn't have Length

dynamic dyn = "hello"; // dyn is dynamic — resolved at runtime
dyn = 42;
int len2 = dyn.Length; // NO compile error — but RuntimeBinderException at runtime!
```

---

**Q47: What is the difference between `const` and `readonly`?**

**Answer:**

**Theory:** `const` values are **compile-time constants** — their value is baked into the IL at compile time. When you reference a `const` from another assembly, that assembly gets a COPY of the value. If you change the const and rebuild only its assembly, referencing assemblies STILL have the old value until they're recompiled. `readonly` values are **runtime constants** — set once in the constructor or initializer, then immutable. `readonly` fields can be calculated at runtime. `static readonly` combines both: a single value per type, initialized once. Rule: use `const` only for values that are truly universal and never change (math constants like `Math.PI`, enum underlying values). Use `static readonly` for configuration values, connection strings, and anything that might change.

**Code Example:**
```csharp
public class Constants
{
    // Compile-time — burned into IL at compile time
    public const int DefaultPageSize = 20;

    // Runtime — set in static constructor, can use complex logic
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    // Instance readonly — can differ per instance, set in constructor
    public readonly Guid InstanceId;

    public Constants()
    {
        InstanceId = Guid.NewGuid();  // Runtime calculation
    }
}

// const from another assembly:
// Console.WriteLine(ExternalLib.Constants.DefaultPageSize);
// If ExternalLib changes DefaultPageSize to 50 but you don't recompile,
// you still see 20 (the old value burned into YOUR assembly).
```

---

**Q48: What is the difference between `struct` and `class`? When would you use a struct?**

**Answer:**

**Theory:** `struct` is a **value type** — stored inline on the stack (or inside the parent object), copied on assignment, cannot be null (unless nullable), no inheritance (except interfaces), no finalizer. `class` is a **reference type** — stored on the heap, reference copied on assignment, can be null, supports inheritance and finalizers. Microsoft's guidelines: use `struct` for types that (1) represent a single value (< 16-24 bytes), (2) are immutable, (3) don't need to be boxed frequently, (4) have value semantics (two instances with same data are equal). Good struct examples: `DateTime`, `TimeSpan`, `Guid`, `Point`, `Color`, `Complex`. Bad struct examples: `MemoryStream`, `Customer`, anything > 24 bytes (copy overhead exceeds benefit). The .NET runtime handles structs more efficiently than classes for small, temporary data — they don't need GC collection.

**Code Example:**
```csharp
// Struct — value type, stack-allocated (or inline in parent)
public struct Point
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}

// Class — reference type, heap-allocated
public class Person
{
    public string Name { get; set; }
}

void Demonstrate()
{
    Point a = new(1, 2);
    Point b = a;     // COPY — b is independent
    b = new(3, 4);   // a is still (1, 2)

    Person p1 = new() { Name = "Alice" };
    Person p2 = p1;  // REFERENCE — p2 points to same object
    p2.Name = "Bob"; // p1.Name is now "Bob" too!
}
```

---

**Q49: What are Records in C#? How do they differ from classes?**

**Answer:**

**Theory:** Records (C# 9+) are **reference types with value semantics** — two records with the same property values are considered EQUAL, unlike classes (where equality is reference-based by default). Records provide: (1) positional construction (`new Person("Alice", 30)`), (2) `ToString()` that prints all properties, (3) `Deconstruct` for destructuring, (4) `with` expressions for non-destructive mutation (`person with { Age = 31 }`), (5) value-based equality (compares all properties), (6) `IEquatable<T>` implementation automatically. `record struct` (C# 10+) provides the same for value types. Use records for: DTOs, API responses, immutable domain events, configuration objects. Use classes for: services, entities with identity, mutable objects with behavior.

**Code Example:**
```csharp
public record Person(string Name, int Age);

public record UserDto
{
    public string Id { get; init; }  // init-only — set during construction only
    public string Email { get; init; }
}

void RecordExamples()
{
    var p1 = new Person("Alice", 30);
    var p2 = new Person("Alice", 30);

    Console.WriteLine(p1 == p2);  // TRUE — value equality (class would be false)
    Console.WriteLine(p1);        // "Person { Name = Alice, Age = 30 }"

    // Non-destructive mutation
    var p3 = p1 with { Age = 31 };
    Console.WriteLine(p1.Age);    // 30 (unchanged)
    Console.WriteLine(p3.Age);    // 31
}
```

---

**Q50: What are Delegates and how do they relate to events?**

**Answer:**

**Theory:** A **delegate** is a type-safe function pointer — it defines a signature and can hold a reference to any matching method (static or instance). Delegates are multicast: one delegate can hold multiple method references and invoke them all in sequence. The built-in delegate types are `Action` (returns void), `Func<TResult>` (returns a value), `Predicate<T>` (returns bool). **Events** are wrappers around delegates — they expose only `+=` and `-=` (subscribe/unsubscribe) from outside the declaring class. The declaring class controls when the event is raised (`OnXxx()` pattern). The key difference: a public delegate field can be invoked by anyone (assigned, invoked, replaced); an event can only be raised by its declaring class. Events follow the **Observer pattern**.

**Code Example:**
```csharp
// Delegate declaration
public delegate void ProgressHandler(int percent);

public class FileUploader
{
    // Event — only this class can raise it
    public event ProgressHandler? OnProgress;

    // Event using built-in EventHandler<T>
    public event EventHandler<FileUploadedEventArgs>? OnComplete;

    public async Task UploadAsync(Stream file)
    {
        long total = file.Length;
        long read = 0;
        byte[] buffer = new byte[8192];

        while ((read = await file.ReadAsync(buffer)) > 0)
        {
            int percent = (int)(read * 100 / total);
            OnProgress?.Invoke(percent);  // Raise event
        }

        OnComplete?.Invoke(this, new FileUploadedEventArgs { Success = true });
    }
}

// Subscribing
var uploader = new FileUploader();
uploader.OnProgress += p => Console.WriteLine($"{p}%");
uploader.OnComplete += (s, e) => Console.WriteLine($"Done: {e.Success}");

// Multicast — multiple subscribers
uploader.OnProgress += p => UpdateProgressBar(p);
```

---

**Q51: What are Lambda Expressions and Closures?**

**Answer:**

**Theory:** A **lambda expression** is an anonymous method: `(x, y) => x + y`. Lambdas can be compiled to delegates (`Func<int, int, int> add = (a, b) => a + b`) or expression trees (`Expression<Func<int, int, int>>`). A **closure** occurs when a lambda captures a variable from its enclosing scope — the compiler creates a hidden class to hold the captured variable, extending its lifetime beyond the method's scope. This is how `Where(x => x.Age > minAge)` works — `minAge` is captured. Closures allocate (the hidden class + delegate instance) and can cause memory leaks if the delegate outlives the captured object. Use captured variables carefully in hot paths and long-lived callbacks.

**Code Example:**
```csharp
int threshold = 18;
// Closure — 'threshold' is captured by the lambda
Func<int, bool> isAdult = age => age >= threshold;

// Compiler generates something like:
// private class <>c__DisplayClass0
// {
//     public int threshold;
//     public bool <Main>b__0(int age) => age >= threshold;
// }

// Memory leak risk: if 'isAdult' is stored in a static/ long-lived collection,
// the captured 'threshold' (and 'this' if instance member is captured)
// prevents GC from collecting the object that created the lambda.

// Avoid capturing 'this' (instance members) in long-lived delegates
public class Service
{
    private int _minAge;

    public void Process(List<User> users)
    {
        // Captures 'this' — if the delegate outlives the Service, memory leak
        var adults = users.Where(u => u.Age >= _minAge);
    }
}
```

---

## 12. Exception Handling

**Q52: What is the difference between `throw` and `throw ex`?**

**Answer:**

**Theory:** `throw` (bare, without an exception object) rethrows the CURRENT exception preserving its **original stack trace**. `throw ex` throws the same exception object but RESETS the stack trace to the current line — you lose the original call site. The original line number, method name, and call chain are destroyed. Debugging becomes guesswork: you know an exception happened but not where in the original method. The correct patterns: (1) `throw` — rethrow with full context, (2) `throw new Exception("context", ex)` — wrap with additional context, preserving original as `InnerException`, (3) `ExceptionDispatchInfo.Capture(ex).Throw()` — preserve stack trace across threads. Never use `throw ex` in production code.

**Code Example:**
```csharp
try
{
    await _api.PostAsync(endpoint, data);
}
catch (HttpRequestException ex)
{
    // GOOD — preserves original stack trace
    throw;

    // BAD — resets stack trace to this line
    throw ex;

    // GOOD — wraps with context, preserves original
    throw new ApiException("API call failed", ex);
}

// Cross-thread preservation
ExceptionDispatchInfo? captured = null;
try { DoWork(); }
catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }

// Later, on another thread:
captured?.Throw();  // Original stack trace preserved
```

---

**Q53: How do you implement global exception handling in ASP.NET Core?**

**Answer:**

**Theory:** ASP.NET Core provides `IExceptionHandler` (new in .NET 8) and `UseExceptionHandler()` middleware for global error handling. The middleware catches unhandled exceptions, logs them, and returns a standardized error response. The **Exception Handling Middleware** should be registered EARLY in the pipeline (before any other middleware). A well-designed global handler: (1) logs the full exception details (stack trace, correlation ID, request path), (2) returns a structured error response (`ProblemDetails` RFC 7807), (3) maps known exception types to appropriate HTTP status codes (e.g., `NotFoundException` → 404, `ValidationException` → 400), (4) returns a generic message for security (don't leak stack traces in production). For MAUI, subscribe to `AppDomain.CurrentDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException`.

**Code Example:**
```csharp
// .NET 8+ — IExceptionHandler
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception");

        var problemDetails = new ProblemDetails
        {
            Status = exception switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                ValidationException => StatusCodes.Status400BadRequest,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                _ => StatusCodes.Status500InternalServerError
            },
            Title = "An error occurred",
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return ValueTask.FromResult(true); // Exception handled
    }
}

// Program.cs
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
```

---

**Q54: What are exception filters (`when` keyword)?**

**Answer:**

**Theory:** Exception filters (`catch (Exception ex) when (condition)`) let you conditionally catch an exception based on a runtime check WITHOUT unwinding the stack first. If the condition is false, the catch block is skipped and the exception continues propagating. The stack trace is preserved because the catch hasn't actually executed yet. Without `when`, you'd catch, check, and rethrow — which either loses the stack trace (if `throw ex`) or preserves it but still executes the catch logic. Exception filters are especially useful for: (1) catching only specific error codes, (2) logging exceptions without handling them (always returns false), (3) distinguishing between transient and fatal errors.

**Code Example:**
```csharp
// Catch only 404 errors, let 500s propagate
try
{
    return await _httpClient.GetAsync(url);
}
catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    return null;  // Handle 404 gracefully
}

// Log without catching — filter always returns false
try
{
    return await _db.Users.FindAsync(id);
}
catch (Exception ex) when (Log(ex, "Database error"))
{
    // Never reached — Log() returns false
    // But the exception continues propagating with stack trace intact
}

static bool Log(Exception ex, string message)
{
    Console.WriteLine($"{message}: {ex.Message}");
    return false;  // Don't catch
}
```

---

## 13. Collections & Generics

**Q55: What are Generic Constraints in C#?**

**Answer:**

**Theory:** Generic constraints restrict what types can be used as type arguments. `where T : class` (reference type only), `where T : struct` (value type only), `where T : notnull` (non-nullable), `where T : IComparable<T>` (must implement interface), `where T : new()` (must have parameterless constructor), `where T : U` (must inherit from U). Constraints enable you to use members of the constrained type inside the generic method — e.g., `where T : IComparable<T>` allows calling `x.CompareTo(y)`. Without constraints, you can only use `object` members. Multiple constraints apply: `where T : class, IDisposable, new()`. Constraints also affect runtime behavior — `List<int>` and `List<string>` are different closed types with different JIT-compiled code.

**Code Example:**
```csharp
// T must be a reference type with parameterless constructor
public class Factory<T> where T : class, new()
{
    public T Create() => new T();  // Allowed because of new() constraint
}

// T must implement IComparable<T>
public T Max<T>(T a, T b) where T : IComparable<T>
{
    return a.CompareTo(b) > 0 ? a : b;  // Allowed because of constraint
}

// T must be a value type (non-nullable)
public struct Nullable<T> where T : struct { }

// Multiple constraints
public class Repository<T> where T : class, IEntity, new() { }
```

---

**Q56: What is the difference between `List<T>`, `HashSet<T>`, and `Dictionary<TKey, TValue>`?**

**Answer:**

**Theory:** `List<T>` is an ordered, indexable collection with O(1) access by index. Add/Remove are O(1) at the end, O(n) in the middle (shift). `HashSet<T>` is an **unordered** collection with O(1) add/remove/lookup — it uses a hash table internally. No duplicates allowed. No indexing. `Dictionary<TKey, TValue>` is a key-value hash table with O(1) key lookup. Each key maps to one value. Choose: `List` when you need order/index/duplicates. `HashSet` when you need fast membership tests (`contains`) and no duplicates. `Dictionary` when you need key-value mapping. `HashSet` and `Dictionary` have overhead: they call `GetHashCode()` and `Equals()` on every operation. For small collections (< 20 items), `List` with LINQ `Any()`/`First()` may be faster than hash structure overhead.

**Code Example:**
```csharp
// List — ordered, duplicates allowed
var list = new List<int> { 3, 1, 2, 1 };
Console.WriteLine(list[2]);        // 2 — index access
bool hasFive = list.Contains(5);   // O(n) — scans entire list

// HashSet — unordered, unique, O(1) membership
var set = new HashSet<int> { 3, 1, 2, 1 };
Console.WriteLine(set.Count);       // 3 — duplicates removed
bool hasTwo = set.Contains(2);      // O(1) — hash lookup

// Dictionary — key-value mapping
var dict = new Dictionary<string, int>
{
    ["Alice"] = 30,
    ["Bob"] = 25
};
int age = dict["Alice"];  // O(1) — hash lookup
bool exists = dict.TryGetValue("Charlie", out int charlieAge);
```

---

**Q57: What is the difference between `IEnumerable`, `ICollection`, `IList`, and `IQueryable`?**

**Answer:**

**Theory:** These interfaces form a hierarchy of collection capabilities. **`IEnumerable<T>`** is the most basic — forward-only iteration (`foreach`). No count, no index, no modification. **`ICollection<T>`** adds Count, Add, Remove, Clear — a modifiable collection. **`IList<T>`** adds index-based access (insert/remove at index) — the most capable in-memory interface. **`IQueryable<T>`** is DIFFERENT — it represents a query that can be executed against a data source (database). It builds an expression tree that gets translated to SQL. `IQueryable` inherits from `IEnumerable` but behaves very differently: `Where()` on `IEnumerable` filters in memory; `Where()` on `IQueryable` adds a WHERE clause to SQL. The general rule: accept the least capable interface you need. If you only iterate, accept `IEnumerable`. If you need add/remove, accept `ICollection`. If you need indexed access, accept `IList`.

**Code Example:**
```csharp
// IEnumerable — iterate only
public void PrintAll(IEnumerable<string> items)
{
    foreach (var item in items)
        Console.WriteLine(item);
}

// ICollection — iterate, count, add, remove
public void AddIfNotExists<T>(ICollection<T> collection, T item)
{
    if (!collection.Contains(item))
        collection.Add(item);
}

// IList — indexed access
public T GetMiddle<T>(IList<T> list)
{
    return list[list.Count / 2];
}

// IQueryable — builds query, executes on server
public IQueryable<User> GetAdults(IQueryable<User> query, int minAge)
{
    return query.Where(u => u.Age >= minAge);  // SQL: WHERE Age >= @minAge
}
```

---

## 14. LINQ Deep Dive

**Q58: What is the difference between `FirstOrDefault()` and `SingleOrDefault()`?**

**Answer:**

**Theory:** `FirstOrDefault()` returns the **first** element that matches the condition — it stops iterating as soon as it finds a match. If there are multiple matches, it returns the first one. If no match, returns default. **Always O(1) best case, O(n) worst case.** `SingleOrDefault()` returns the **only** element that matches — it MUST iterate the entire sequence to verify there's exactly one match. If there are zero or more than one, it throws (or returns default for zero in the OrDefault variant). **Always O(n).** The difference is the CONTRACT: `FirstOrDefault` says "I want the first one or nothing" — safe assumption. `SingleOrDefault` says "there MUST be exactly one" — it's an assertion. Use `FirstOrDefault` for paginated/top-N queries and search results. Use `SingleOrDefault` only when you're CERTAIN the data source enforces uniqueness (e.g., querying by primary key where the DB guarantees uniqueness).

**Code Example:**
```csharp
// FirstOrDefault — stops at first match
var user = await db.Users
    .Where(u => u.Email == "test@test.com")
    .FirstOrDefaultAsync();
// SQL: SELECT TOP 1 * FROM Users WHERE Email = @p0
// If 3 users match, only the first is returned

// SingleOrDefault — verifies uniqueness
var user = await db.Users
    .Where(u => u.Email == "test@test.com")
    .SingleOrDefaultAsync();
// SQL: SELECT * FROM Users WHERE Email = @p0
// Then client-side: if 0 → default, 1 → return it, >1 → throw InvalidOperationException

// Best for PK lookups — uniqueness is guaranteed
var user = await db.Users.FindAsync(42);  // Use FindAsync, not SingleOrDefault!
```

---

**Q59: What is `SelectMany` and how is it different from `Select`?**

**Answer:**

**Theory:** `Select` projects each element into a single result — `IEnumerable<A> → IEnumerable<B>`. `SelectMany` projects each element into an **enumerable** and **flattens** the result — `IEnumerable<A> → IEnumerable<B>` where each A can produce 0-to-many B's. Think of `SelectMany` as "for each parent, return all children, flatten into a single list." Use cases: (1) flattening nested collections (orders → order items), (2) cross-join-style queries, (3) `SelectMany` with `Where` is equivalent to `from a in As from b in a.Bs where ...`. In LINQ query syntax: `from o in orders from item in o.Items select item`.

**Code Example:**
```csharp
var orders = new List<Order>
{
    new Order { Id = 1, Items = new[] { "Apple", "Banana" } },
    new Order { Id = 2, Items = new[] { "Cherry" } }
};

// Select — returns list of arrays: [["Apple","Banana"], ["Cherry"]]
var nested = orders.Select(o => o.Items);

// SelectMany — flattens: ["Apple", "Banana", "Cherry"]
var flat = orders.SelectMany(o => o.Items);

// EF Core equivalent — eager load with SelectMany
var allItems = await db.Orders
    .SelectMany(o => o.OrderItems)
    .ToListAsync();
// SQL: SELECT oi.* FROM Orders o JOIN OrderItems oi ON o.Id = oi.OrderId
```

---

**Q60: What is the difference between `Any()` and `Count() > 0`?**

**Answer:**

**Theory:** `Any()` returns `true` as soon as it finds the FIRST matching element. It stops iterating immediately — O(1) best case. `.Count() > 0` must iterate the **entire** sequence to count ALL elements — O(n) always. For a query with millions of records, `Any()` returns in microseconds (SQL: `IF EXISTS(SELECT 1 ...)`), while `Count() > 0` scans everything. The difference is critical in database queries: `Any()` translates to `EXISTS` (short-circuits on first match), `Count()` translates to `COUNT(*)` (full scan). Always prefer `Any()` over `Count() > 0` for existence checks. The same applies to `All()` (returns false on first mismatch) vs `Count() == length`.

**Code Example:**
```csharp
// GOOD — Any() short-circuits
bool hasAdults = users.Any(u => u.Age >= 18);
// SQL: SELECT CASE WHEN EXISTS(SELECT 1 FROM Users WHERE Age >= 18) THEN 1 ELSE 0 END

// BAD — Count() must scan ALL rows to count
bool hasAdults = users.Count(u => u.Age >= 18) > 0;
// SQL: SELECT COUNT(*) FROM Users WHERE Age >= 18  — scans EVERY row

// For in-memory collections, Any() is also faster
var millionItems = Enumerable.Range(0, 1_000_000);
bool any = millionItems.Any(x => x > 999_999);  // Stops at element 1,000,000
bool count = millionItems.Count(x => x > 999_999) > 0;  // Iterates ALL 1,000,000
```

---

**Q61: What is `GroupBy` and how does it work in LINQ?**

**Answer:**

**Theory:** `GroupBy` groups elements by a key, returning `IEnumerable<IGrouping<TKey, TElement>>` where each `IGrouping` has a `Key` property and is itself an `IEnumerable` of its members. Groups are computed lazily. In EF Core, `GroupBy` translates to SQL `GROUP BY` — the grouping happens in the database, not in memory. Each group can then be aggregated using `Count()`, `Sum()`, `Average()`, `Min()`, `Max()`, or custom projections. The `into` keyword in query syntax lets you continue querying after a group. For nested grouping (group by A then by B), chain `GroupBy` with `Select` to transform each group.

**Code Example:**
```csharp
var orders = await db.Orders.ToListAsync();

// Group by customer, count orders per customer
var customerOrderCounts = orders
    .GroupBy(o => o.CustomerId)
    .Select(g => new { CustomerId = g.Key, OrderCount = g.Count() });

// EF Core translates this to:
// SELECT o.CustomerId, COUNT(*) AS OrderCount
// FROM Orders o
// GROUP BY o.CustomerId

// Group by composite key
var monthlyStats = orders
    .GroupBy(o => new { o.CustomerId, o.OrderDate.Year })
    .Select(g => new
    {
        g.Key.CustomerId,
        g.Key.Year,
        Total = g.Sum(o => o.Amount),
        Count = g.Count()
    });

// Group with into (query syntax)
var grouped = from o in orders
              group o by o.CustomerId into g
              where g.Count() > 5
              select new { Customer = g.Key, Count = g.Count() };
```

---

## 15. Multithreading & Synchronization

**Q62: What is the difference between `lock`, `Monitor`, and `Mutex`?**

**Answer:**

**Theory:** `lock` is a C# syntactic sugar for `Monitor.Enter`/`Monitor.Exit`. It's the simplest and most common synchronization primitive — use it for thread-safe access within a SINGLE process. `Monitor` provides more control: `Monitor.TryEnter(timeout)` for non-blocking attempts, `Monitor.Wait()`/`Pulse()` for signaling between threads. `Mutex` is an OS-level synchronization primitive that works ACROSS processes — you can use it to ensure only one instance of your application runs. Both `lock`/`Monitor` and `Mutex` are **blocking** — the thread waits (can't do other work). For async scenarios, use `SemaphoreSlim.WaitAsync()` (non-blocking). Rule: use `lock` for simple intra-process critical sections. Use `Monitor` when you need timeout or signaling. Use `Mutex` only for cross-process synchronization (single-instance apps).

**Code Example:**
```csharp
// lock — simplest intra-process
private readonly object _lock = new();
private int _counter;

public void Increment()
{
    lock (_lock) { _counter++; }
}

// Monitor — with timeout and signaling
public bool TryAcquire(int timeoutMs)
{
    return Monitor.TryEnter(_lock, timeoutMs);
}

// Mutex — cross-process (single-instance enforcement)
using var mutex = new Mutex(true, "Global\\EVSwapAppMutex");
if (!mutex.WaitOne(TimeSpan.Zero, true))
{
    Console.WriteLine("Another instance is already running.");
    return;
}
// Run application...
```

---

**Q63: What is `ReaderWriterLockSlim` and when should you use it?**

**Answer:**

**Theory:** `ReaderWriterLockSlim` allows MULTIPLE concurrent readers OR ONE exclusive writer — but NOT both simultaneously. When a thread holds the **read lock**, other threads can ALSO acquire the read lock (they don't block each other). When a thread requests the **write lock**, it waits until all readers release, then blocks ALL new readers/writers until it releases. This is more efficient than `lock` for data structures with frequent reads and rare writes (like a configuration cache read by every request but updated once a day). With `lock`, readers would block each other unnecessarily. The trade-off: `ReaderWriterLockSlim` is more complex (deadlock-prone if you upgrade from read to write), and for very short critical sections (< 1µs), `lock` may be faster due to the overhead of the reader-writer logic.

**Code Example:**
```csharp
public class ConfigCache
{
    private readonly ReaderWriterLockSlim _rwLock = new();
    private Dictionary<string, string> _cache = new();

    public string? Get(string key)
    {
        _rwLock.EnterReadLock();
        try
        {
            return _cache.TryGetValue(key, out var val) ? val : null;
        }
        finally { _rwLock.ExitReadLock(); }
    }

    public void Update(Dictionary<string, string> newCache)
    {
        _rwLock.EnterWriteLock();
        try
        {
            _cache = newCache;  // Only blocks when someone is updating
        }
        finally { _rwLock.ExitWriteLock(); }
    }
}
```

---

**Q64: What is PLINQ and when would you use it?**

**Answer:**

**Theory:** PLINQ (Parallel LINQ) automatically parallelizes LINQ queries across multiple CPU cores using `AsParallel()`. It partitions the data source, processes each partition on a separate thread, and merges the results. Use for CPU-bound operations on large in-memory collections — image processing, complex calculations, data transforms. Do NOT use for I/O-bound operations (database calls, HTTP requests) — you'll saturate the thread pool and get worse performance. Do NOT use for small collections (parallelization overhead > benefit). `AsOrdered()` preserves the original ordering at a performance cost. `WithDegreeOfParallelism(N)` controls concurrency. PLINQ uses `Task` internally and `ThreadPool` threads.

**Code Example:**
```csharp
// Sequential
var results = data.Where(IsComplex).Select(Transform).ToList();

// Parallel — may process items out of order
var parallelResults = data
    .AsParallel()
    .Where(IsComplex)
    .Select(Transform)
    .ToList();

// Preserve order — slightly slower
var orderedResults = data
    .AsParallel()
    .AsOrdered()
    .Where(IsComplex)
    .Select(Transform)
    .ToList();

// Control degree of parallelism
var controlled = data
    .AsParallel()
    .WithDegreeOfParallelism(4)  // Max 4 threads
    .Select(ExpensiveOperation)
    .ToList();
```

---

## 16. ASP.NET Core Fundamentals

**Q65: What is the ASP.NET Core Middleware Pipeline?**

**Answer:**

**Theory:** The middleware pipeline is a series of components (middleware) that handle HTTP requests and responses in sequence. Each middleware can: (1) process the request, (2) pass to the next middleware via `await next(context)`, (3) process the response. The pipeline is a **chain of responsibility** — order matters. Common middleware order: ExceptionHandler → HSTS → HTTPS → StaticFiles → Routing → Authentication → Authorization → CORS → Endpoint. Middleware is registered in `Program.cs` with `Use*` methods. Custom middleware can be written as a class with `InvokeAsync(HttpContext, RequestDelegate)` method, or inline with `app.Use(async (context, next) => ...)`. Middleware is the foundation of ASP.NET Core's extensibility — everything (auth, CORS, static files, MVC) is implemented as middleware.

**Code Example:**
```csharp
// Program.cs — typical pipeline order
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/error");

app.UseHttpsRedirection();
app.UseStaticFiles();          // Serve wwwroot files
app.UseRouting();              // Enable routing
app.UseAuthentication();       // JWT auth middleware
app.UseAuthorization();        // Policy enforcement
app.UseCors("AllowSpecific");  // CORS policy
app.MapControllers();          // Map endpoints

app.Run();

// Custom middleware — request logging
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    await next(context);
    sw.Stop();
    _logger.LogInformation("{Method} {Path} → {Status} in {Elapsed}ms",
        context.Request.Method, context.Request.Path,
        context.Response.StatusCode, sw.ElapsedMilliseconds);
});
```

---

**Q66: What is the difference between Middleware and Filters in ASP.NET Core?**

**Answer:**

**Theory:** **Middleware** operates at the HTTP pipeline level — it sees ALL requests/responses regardless of controller/action. It runs BEFORE routing (for early-in-pipeline middleware) and AFTER routing (for later middleware). **Filters** operate WITHIN the MVC/Controller pipeline — they run AFTER routing has selected the controller/action. Filter types: (1) `AuthorizationFilter` (runs first — auth check), (2) `ResourceFilter` (model binding caching), (3) `ActionFilter` (before/after action execution), (4) `ExceptionFilter` (exception handling), (5) `ResultFilter` (before/after result execution). Filters have access to action-specific context (action arguments, model state, controller instance). Middleware only has `HttpContext`. Use middleware for: cross-cutting concerns that don't need action context (logging, HSTS, CORS, static files). Use filters for: concerns tied to specific actions/controllers (input validation, action logging, result formatting).

**Code Example:**
```csharp
// Middleware — sees ALL requests, no action context
app.Use(async (context, next) =>
{
    // context.Request, context.Response only
    await next(context);
});

// ActionFilter — sees action-specific context
public class LoggingActionFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Access: controller name, action name, arguments, model state
        _logger.LogInformation("Executing {Action} with {Args}",
            context.ActionDescriptor.DisplayName,
            context.ActionArguments);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        _logger.LogInformation("Executed {Action} → {Result}",
            context.ActionDescriptor.DisplayName,
            context.Result?.GetType().Name);
    }
}
```

---

**Q67: What is Kestrel and how does it relate to IIS?**

**Answer:**

**Theory:** **Kestrel** is the cross-platform, high-performance web server built into ASP.NET Core. It processes HTTP requests directly — no IIS dependency. On Windows, it can run standalone or behind IIS (or Nginx/Apache on Linux). **IIS** acts as a **reverse proxy** when used with ASP.NET Core: it receives the request, optionally handles Windows auth/SSL termination, then forwards it to Kestrel via the ASP.NET Core Module (ANCM). The request goes: Internet → IIS (port 80/443) → ANCM → Kestrel (port 5000). Kestrel should NOT be exposed directly to the internet in production (it lacks enterprise-grade features like request filtering, rate limiting). Use a reverse proxy (IIS, Nginx, YARP) in front. For development and simple microservices, Kestrel standalone is fine.

**Code Example:**
```csharp
// Program.cs — Kestrel configuration
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Loopback, 5000);  // HTTP
    options.Listen(IPAddress.Any, 5001, listenOptions =>
    {
        listenOptions.UseHttps("certificate.pfx", "password");  // HTTPS
        listenOptions.Limits.MaxRequestBodySize = 10 * 1024 * 1024;  // 10MB
        listenOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    });
});

// appsettings.json — Kestrel limits
// "Kestrel": {
//   "Limits": {
//     "MaxConcurrentConnections": 100,
//     "MaxConcurrentUpgradedConnections": 10
//   }
// }
```

---

## 17. Entity Framework Core Deep Dive

**Q68: What is the difference between `AsNoTracking()` and `AsTracking()`?**

**Answer:**

**Theory:** By default, EF Core queries **track** all returned entities — it stores a snapshot of each entity's values in the `ChangeTracker`. When you modify properties and call `SaveChangesAsync()`, EF Core detects changes by comparing current values to the snapshot. `AsNoTracking()` disables tracking — entities are returned without the ChangeTracker overhead, making queries 30-50% faster and using less memory. Use `AsNoTracking()` for READ-ONLY queries (display data, reports). The entities can't be updated or deleted through the context. Use `AsTracking()` (default) when you intend to modify entities. For bulk read-only operations, always use `AsNoTracking()`. For identity resolution (two queries returning the same entity should return the same instance), use tracking or `AsNoTrackingWithIdentityResolution()` (EF Core 5+).

**Code Example:**
```csharp
// Tracking — use for UPDATE operations
var user = await db.Users.FirstAsync(u => u.Id == 1);
user.Name = "New Name";  // ChangeTracker detects this
await db.SaveChangesAsync();  // Generates UPDATE SQL

// NoTracking — use for READ operations (faster, less memory)
var users = await db.Users
    .AsNoTracking()
    .Where(u => u.IsActive)
    .ToListAsync();
// Changes to 'users' are NOT saved — no ChangeTracker overhead

// NoTrackingWithIdentityResolution — no tracking but same PK = same instance
var data = await db.Orders
    .AsNoTrackingWithIdentityResolution()
    .Include(o => o.Customer)
    .ToListAsync();
// Each Customer appears once even if multiple orders reference it
```

---

**Q69: What is the difference between `Find()` and `FirstOrDefault()` in EF Core?**

**Answer:**

**Theory:** `Find()` first checks the **ChangeTracker** (local cache) for an entity with the given primary key — if found, it returns instantly WITHOUT a database query. If not found, it queries the database. `FirstOrDefault()` always queries the database (unless the query is already cached/completed). `Find()` only works with primary key lookups — no filtering, no includes. `FirstOrDefault()` works with any filter. `Find()` is faster when the same entity is queried multiple times within the same `DbContext` scope (the second query avoids a DB round trip). `FirstOrDefault()` is better for non-PK lookups or when you need related data loaded with Include. Also: `Find()` returns `null` if not found and you can't use `Include` with it.

**Code Example:**
```csharp
// Find — checks ChangeTracker first, then DB
var user = await db.Users.FindAsync(42);
// First call: SELECT * FROM Users WHERE Id = 42
// Second call (same context): returns cached entity — NO database query

// FirstOrDefault — always queries DB
var user = await db.Users
    .Include(u => u.Orders)
    .FirstOrDefaultAsync(u => u.Id == 42);
// Always: SELECT u.*, o.* FROM Users u JOIN Orders o ... WHERE u.Id = 42

// Find with no tracked entity — also queries DB
var user = await new AppDbContext().Users.FindAsync(42);
// Always queries — different context, no cache
```

---

**Q70: What is `Include` vs `ThenInclude` vs `Select` for loading related data?**

**Answer:**

**Theory:** `Include` loads a navigation property (eager loading): `Include(o => o.Customer)`. `ThenInclude` loads a SECOND-level navigation: `Include(o => o.Customer).ThenInclude(c => c.Address)`. Multiple `Include` calls load multiple relationships. `Select` (projection) loads ONLY specific columns — more efficient than loading full entities. The SQL difference: `Include` generates JOINs that return ALL columns. `Select` generates joins returning ONLY needed columns. For performance: prefer `Select` with a DTO/anonymous type over `Include` when you only need a few fields. `Include` is simpler for read-modify-write scenarios (you need the full entity for tracking). `ThenInclude` is needed for grandchild relationships.

**Code Example:**
```csharp
// Include — loads full Customer with all columns
var orders = await db.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items)
    .ToListAsync();

// ThenInclude — loads Customer, then Customer.Address
var orders = await db.Orders
    .Include(o => o.Customer)
        .ThenInclude(c => c.Address)
    .ToListAsync();

// Select — loads only needed columns (MORE EFFICIENT)
var orderDtos = await db.Orders
    .Select(o => new OrderDto
    {
        Id = o.Id,
        CustomerName = o.Customer.Name,  // JOINs but only fetches Name
        TotalItems = o.Items.Count
    })
    .ToListAsync();
```

---

## 18. SQL Server

**Q71: What is the difference between Clustered and Non-Clustered Index?**

**Answer:**

**Theory:** A **clustered index** determines the PHYSICAL order of data in the table — the leaf nodes ARE the actual data rows. A table can have ONLY ONE clustered index (data can only be sorted one way). Primary keys create clustered indexes by default. A **non-clustered index** is a SEPARATE structure that contains a copy of the indexed columns plus a pointer to the actual data row. A table can have MANY non-clustered indexes (up to 999). Clustered indexes are fastest for range queries (`BETWEEN`, `>`), sorting, and returning all columns (no extra lookup). Non-clustered indexes are best for precise lookups and covering queries (all needed columns in the index). The trade-off: indexes speed up reads but slow down writes (each INSERT/UPDATE must update the index). Choose clustered index on the most-frequently-queried column (often the primary key or a date column).

**Code Example:**
```sql
-- Clustered index — data is physically sorted by Id
CREATE CLUSTERED INDEX IX_Orders_Id ON Orders(Id);
-- SELECT * FROM Orders WHERE Id BETWEEN 100 AND 200 → fast range scan

-- Non-clustered index — separate structure pointing to data
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId ON Orders(CustomerId)
    INCLUDE (OrderDate, Amount);  -- Covering index — all needed data in index

-- Composite index
CREATE INDEX IX_Orders_CustomerDate ON Orders(CustomerId, OrderDate DESC);
-- WHERE CustomerId = 5 ORDER BY OrderDate DESC → single index seek

-- Index with included columns (covering index, no key lookup needed)
CREATE INDEX IX_Orders_Status ON Orders(Status)
    INCLUDE (Id, CustomerId, Amount);
-- SELECT Id, CustomerId, Amount FROM Orders WHERE Status = 'Pending'
-- → All data in index, no table access needed
```

---

**Q72: What is a CTE (Common Table Expression) and when would you use it?**

**Answer:**

**Theory:** A CTE is a **temporary named result set** that exists within the scope of a single SELECT/INSERT/UPDATE/DELETE statement. Unlike a subquery, CTEs can be referenced MULTIPLE times within the same query and can be recursive. Use CTEs for: (1) recursive queries (org hierarchy, bill of materials), (2) breaking complex queries into readable steps, (3) referencing the same subquery result multiple times (avoid duplication), (4) window function aggregations. CTEs don't improve performance over subqueries (they're not materialized — same execution plan). `WITH TIES` combined with window functions enables powerful pagination with ties.

**Code Example:**
```sql
-- Recursive CTE — org hierarchy
WITH OrgHierarchy AS (
    -- Anchor: top-level manager
    SELECT Id, Name, ManagerId, 0 AS Level
    FROM Employees WHERE ManagerId IS NULL

    UNION ALL

    -- Recursive: direct reports
    SELECT e.Id, e.Name, e.ManagerId, oh.Level + 1
    FROM Employees e
    INNER JOIN OrgHierarchy oh ON e.ManagerId = oh.Id
)
SELECT * FROM OrgHierarchy ORDER BY Level, Name;

-- Multiple CTEs for readability
WITH CustomerOrders AS (
    SELECT CustomerId, COUNT(*) AS OrderCount, SUM(Amount) AS TotalSpent
    FROM Orders GROUP BY CustomerId
),
TopCustomers AS (
    SELECT * FROM CustomerOrders
    WHERE TotalSpent > 10000
)
SELECT c.Name, tc.OrderCount, tc.TotalSpent
FROM TopCustomers tc
JOIN Customers c ON c.Id = tc.CustomerId
ORDER BY tc.TotalSpent DESC;
```

---

**Q73: What is the difference between `UNION` and `UNION ALL`?**

**Answer:**

**Theory:** Both combine result sets from multiple SELECT statements. `UNION` removes **duplicate rows** (performs a DISTINCT sort) — this is SLOWER because it requires sorting/comparing all columns. `UNION ALL` returns ALL rows including duplicates — it's FASTER because it just appends results. Use `UNION ALL` when you KNOW there are no duplicates or duplicates are acceptable (the common case). Use `UNION` only when you specifically need deduplication. The columns must match in number, order, and compatible data types. For large datasets, the performance difference is significant — `UNION` requires an extra sort operation on the full combined result set.

**Code Example:**
```sql
-- UNION ALL — fast, includes duplicates
SELECT City FROM Customers WHERE Country = 'USA'
UNION ALL
SELECT City FROM Suppliers WHERE Country = 'USA';
-- 10 customers in NY + 5 suppliers in NY = 15 rows (NY appears twice)

-- UNION — slower, no duplicates
SELECT City FROM Customers WHERE Country = 'USA'
UNION
SELECT City FROM Suppliers WHERE Country = 'USA';
-- 10 customers in NY + 5 suppliers in NY = 11 rows (NY appears once)

-- UNION for distinct list
SELECT Email FROM ActiveUsers
UNION
SELECT Email FROM InactiveUsers;
-- Unique email addresses from both tables
```

---

## 19. Design Patterns

**Q74: Explain the Singleton pattern and its thread-safe implementation in .NET.**

**Answer:**

**Theory:** Singleton ensures a class has exactly ONE instance and provides a global access point. In .NET, `Lazy<T>` provides the simplest thread-safe implementation — it guarantees single initialization and thread safety without explicit locking. The classic double-check locking pattern is also valid but more verbose. Common Singleton use cases: logging, configuration, caching, service clients (HttpClient). In modern .NET, DI containers handle Singleton lifecycle (`AddSingleton<T>`) — you rarely write Singleton pattern yourself. The pattern's criticism: it introduces global state, makes unit testing harder (can't mock the instance), and hides dependencies (`Logger.Instance` vs injecting `ILogger<T>`). Prefer DI-managed singletons over the classic pattern.

**Code Example:**
```csharp
// Modern .NET — Lazy<T> thread-safe singleton
public sealed class CacheService
{
    private static readonly Lazy<CacheService> _instance =
        new(() => new CacheService());

    public static CacheService Instance => _instance.Value;

    private CacheService() { }  // Private constructor prevents external instantiation
}

// Via DI — prefer this approach
services.AddSingleton<ICacheService, CacheService>();

// Classic double-check locking (still valid)
public sealed class ClassicSingleton
{
    private static readonly object _lock = new();
    private static ClassicSingleton? _instance;

    public static ClassicSingleton Instance
    {
        get
        {
            if (_instance is null)
            {
                lock (_lock)
                {
                    _instance ??= new ClassicSingleton();
                }
            }
            return _instance;
        }
    }

    private ClassicSingleton() { }
}
```

---

**Q75: Explain the Repository and Unit of Work patterns.**

**Answer:**

**Theory:** The **Repository pattern** abstracts data access behind an interface — controllers/ViewModels depend on `IUserRepository`, not `DbContext`. This enables: (1) unit testing with mock repositories, (2) swapping data sources (SQL → API → in-memory), (3) encapsulating complex queries. The **Unit of Work pattern** groups multiple repository operations into a single transaction — either ALL succeed or ALL are rolled back. In EF Core, `DbContext` IS both a Repository (exposes `DbSet<T>`) and a Unit of Work (tracks changes, `SaveChangesAsync()` commits). You can add a custom `IUnitOfWork` interface wrapping `DbContext` for testability. The key: repositories expose domain-focused methods (`GetActiveUsers()`, `FindByEmail()`), not generic CRUD. For simple CRUD apps, use `DbContext` directly — the abstraction overhead isn't justified.

**Code Example:**
```csharp
// Repository interface
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<List<User>> GetActiveUsersAsync();
    Task AddAsync(User user);
    Task SaveChangesAsync();
}

// Implementation
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public async Task<User?> GetByEmailAsync(string email)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<List<User>> GetActiveUsersAsync()
        => await _db.Users.Where(u => u.IsActive).ToListAsync();

    // ... other methods
}

// Unit of Work
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IOrderRepository Orders { get; }
    Task<int> CommitAsync();  // Saves all changes in one transaction
    Task RollbackAsync();
}
```

---

**Q76: Explain the CQRS pattern and when would you use it?**

**Answer:**

**Theory:** CQRS (Command Query Responsibility Segregation) separates **read** operations (Queries) from **write** operations (Commands). Queries return data without modifying state — use a separate read model optimized for display. Commands modify state without returning data — use a separate write model optimized for consistency. Benefits: (1) independent scaling — reads can use cached/denormalized data, writes use normalized OLTP, (2) different data models — read model can be denormalized flat views, write model stays normalized, (3) security — commands can have different authorization than queries. Costs: (1) complexity — two models, eventual consistency, (2) not suitable for simple CRUD apps. Use CQRS for: high-traffic systems with complex domains (e-commerce, banking, IoT), where read and write workloads differ significantly. Don't use for: simple CRUD APIs, internal tools.

**Code Example:**
```csharp
// Command — modifies state, returns nothing meaningful
public record CreateOrderCommand(int UserId, List<OrderItemDto> Items);

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        var order = new Order { UserId = cmd.UserId, /* ... */ };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order.Id;
    }
}

// Query — reads data, no side effects
public record GetOrderQuery(Guid OrderId) : IRequest<OrderDto?>;

public class GetOrderHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderQuery query, CancellationToken ct)
    {
        return await _db.Orders
            .AsNoTracking()
            .Where(o => o.Id == query.OrderId)
            .Select(o => new OrderDto(/* ... */))
            .FirstOrDefaultAsync(ct);
    }
}
```

---

## 20. SOLID Principles

**Q77: Explain the SOLID principles with code examples.**

**Answer:**

**Theory:** **S**ingle Responsibility — a class should have ONE reason to change. `AuthService` handles auth only; if you change token logic, you change only `AuthService`. **O**pen/Closed — open for extension, closed for modification. Add new `IPaymentProcessor` implementations without modifying existing code. **L**iskov Substitution — derived classes must be substitutable for base. A `Square` inheriting `Rectangle` should work where `Rectangle` is expected (it doesn't — violates LSP). **I**nterface Segregation — many small, focused interfaces > one large one. Separate `IPrinter`, `IScanner`, `IFax` instead of one `IMultiFunctionDevice`. **D**ependency Inversion — depend on abstractions, not concretions. ViewModels depend on `IAuthService`, not `AuthService`. SOLID prevents code rot: without it, classes become god objects, changes cascade, and testing is impossible.

**Code Example:**
```csharp
// S: Single Responsibility
public class OrderProcessor  // ONE responsibility: process orders
{
    private readonly INotificationService _notifier;
    public async Task ProcessAsync(Order order) { /* ... */ }
}

// O: Open/Closed
public interface IPaymentProcessor
{
    Task<bool> ProcessPaymentAsync(decimal amount);
}
public class CreditCardProcessor : IPaymentProcessor { /* ... */ }
public class UpiProcessor : IPaymentProcessor { /* ... */ }
// New payment methods = new implementations, not modifications

// L: Liskov Substitution
public class Rectangle
{
    public virtual int Width { get; set; }
    public virtual int Height { get; set; }
    public int Area() => Width * Height;
}
public class Square : Rectangle  // VIOLATES LSP!
{
    public override int Width { set { base.Width = base.Height = value; } }
    public override int Height { set { base.Width = base.Height = value; } }
}
// Rectangle r = new Square(); r.Width = 5; r.Height = 10; → Area = 100 (expected 50)

// I: Interface Segregation
public interface IPrinter { void Print(string doc); }
public interface IScanner { void Scan(string doc); }
// Not: public interface IAllInOne { void Print(); void Scan(); void Fax(); }

// D: Dependency Inversion
public class OrderService
{
    private readonly IPaymentProcessor _payment;  // Abstraction, not concrete
    public OrderService(IPaymentProcessor payment) => _payment = payment;
}
```

---

## 21. Architecture & Microservices

**Q78: What is the difference between Clean Architecture and N-Tier Architecture?**

**Answer:**

**Theory:** **N-Tier** physically separates layers (Presentation → Business → Data Access) where each layer may run on different servers. The dependency direction is top-down: Presentation depends on Business, Business depends on Data Access. The problem: the Data Access layer (SQL, EF Core) is at the BOTTOM, and Business depends on it — changing the database affects business logic. **Clean Architecture** (and Onion/Hexagonal) reverses this: the DOMAIN (business logic) is at the CENTER with NO external dependencies. Infrastructure (database, API, file system) depends on the Domain, not vice versa. The Domain defines INTERFACES; Infrastructure IMPLEMENTS them (Dependency Inversion). This means: (1) the domain has zero NuGet dependencies (no EF Core, no Newtonsoft), (2) you can swap the database without touching business logic, (3) the domain is purely unit-testable. Clean Architecture adds more projects (Domain, Application, Infrastructure, Presentation) but pays off in long-lived, complex applications.

**Code Example:**
```
// N-Tier (traditional):
// Presentation → Business Layer → Data Access Layer (EF Core)
// Business layer depends on EF Core — hard to test, hard to swap DB

// Clean Architecture:
// ┌─────────────┐  Domain (Entities, Interfaces) — NO dependencies
// │   Domain     │
// └──────┬───────┘
// ┌──────┴───────┐  Application (Use Cases, DTOs) — depends on Domain
// │ Application  │
// └──────┬───────┘
// ┌──────┴───────┐  Infrastructure (EF Core, Repositories, Email) — implements Domain interfaces
// │Infrastructure│
// └──────┬───────┘
// ┌──────┴───────┐  Presentation (API Controllers, MAUI) — depends on Application
// │ Presentation │
// └─────────────┘

// Domain layer — pure C#, no EF Core dependency
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id);
}

// Infrastructure layer — implements Domain interface
public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;  // EF Core lives HERE
    public async Task<Order?> GetByIdAsync(Guid id)
        => await _db.Orders.FindAsync(id);
}
```

---

**Q79: What is the Saga pattern in microservices?**

**Answer:**

**Theory:** The Saga pattern coordinates distributed transactions across multiple microservices without using distributed transactions (2PC). Instead of all-or-nothing ACID, Saga uses eventual consistency with COMPENSATING transactions (rollback). Two implementation styles: (1) **Choreography** — each service publishes events, other services react and emit their own events. No central coordinator. (2) **Orchestration** — a central Saga orchestrator tells each service what to do and handles failures by calling compensating actions. For an EV fleet app: CreateSwapRequest → DeductWalletBalance → NotifyStationOperator → ConfirmSwap. If DeductWalletBalance fails, the orchestrator calls ReverseSwapRequest (compensating action). Sagas are complex — use them only when a single service can't handle the workflow and eventual consistency is acceptable.

**Code Example:**
```csharp
// Orchestration Saga example
public class SwapSagaOrchestrator
{
    public async Task<SwapResult> ExecuteSwapAsync(SwapRequest request)
    {
        try
        {
            // Step 1: Reserve battery
            await _stationService.ReserveBatteryAsync(request.StationId, request.BatteryId);

            // Step 2: Deduct wallet
            await _walletService.DeductAsync(request.UserId, request.Amount);

            // Step 3: Notify station operator
            await _notificationService.NotifyOperatorAsync(request.StationId);

            return SwapResult.Success;
        }
        catch (Exception ex)
        {
            // Compensating actions — rollback in reverse order
            await _walletService.RefundAsync(request.UserId, request.Amount);  // Compensate step 2
            await _stationService.ReleaseBatteryAsync(request.StationId, request.BatteryId);  // Compensate step 1
            _logger.LogError(ex, "Swap saga failed");
            return SwapResult.Failure;
        }
    }
}
```

---

## 22. Caching

**Q80: What is the Cache-Aside pattern and how do you implement it?**

**Answer:**

**Theory:** Cache-Aside is the most common caching pattern. The application code checks the cache first: (1) cache HIT → return cached data, (2) cache MISS → load from database, store in cache, return. Benefits: simple, the cache doesn't own the data (loose coupling). The cache stores data as key-value pairs with TTL (Time-To-Live). Challenges: (1) **cache invalidation** — when data changes, you must remove/update the cache entry; (2) **stampede** — when cache expires and thousands of requests hit the DB simultaneously, use a lock/mutex or cache the "old" data while refreshing; (3) **thundering herd** — same as stampede, solved by `GetOrCreate` atomic operations. In-memory cache (`IMemoryCache`) is fast but per-server. Redis (`IDistributedCache`) is shared across servers. The `options.CancellationToken` enables premature eviction for stale data.

**Code Example:**
```csharp
public class CachedStationService
{
    private readonly IMemoryCache _cache;
    private readonly IStationRepository _repo;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<List<StationModel>> GetNearbyStationsAsync(double lat, double lng)
    {
        string cacheKey = $"stations:{lat:F2}:{lng:F2}";

        // Cache-Aside — check cache first
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            // Cache miss — load from database
            var stations = await _repo.GetNearbyAsync(lat, lng);

            // Prevent caching null/empty results (cache penetration)
            if (stations is null || stations.Count == 0)
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1); // Short TTL for empties

            return stations ?? new List<StationModel>();
        });
    }

    // Invalidate cache when data changes
    public async Task UpdateStationAsync(StationModel station)
    {
        await _repo.UpdateAsync(station);
        _cache.Remove($"stations:{station.Latitude:F2}:{station.Longitude:F2}");
    }
}
```

---

## 23. API Security

**Q81: How does JWT authentication work?**

**Answer:**

**Theory:** JWT (JSON Web Token) is a stateless authentication mechanism. The token has three parts separated by dots: `header.payload.signature`. **Header** contains the algorithm (HS256, RS256). **Payload** contains claims (sub, exp, iat, roles). **Signature** is a cryptographic hash of header+payload using a secret key (HMAC) or private key (RSA/ECDSA). The flow: (1) user logs in with credentials, (2) server validates, generates a JWT signed with a secret, (3) client stores the JWT (localStorage/SecureStorage), (4) client sends JWT in `Authorization: Bearer <token>` header, (5) server validates the signature (no DB lookup needed — stateless), (6) server reads claims from payload (user ID, roles). JWTs should have short expiry (15-30 min) with refresh tokens for renewal. RS256 (asymmetric) is preferred over HS256 (symmetric) when multiple services need to validate tokens — use the public key to verify, private key to sign.

**Code Example:**
```csharp
// Program.cs — JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "EVSwap",
            ValidAudience = "EVSwap.Mobile",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("your-256-bit-secret-here-minimum-32-chars"))
        };
    });

// Token generation
public string GenerateToken(User user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var token = new JwtSecurityToken(
        issuer: "EVSwap",
        audience: "EVSwap.Mobile",
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(30),
        signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256)
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

---

## 24. Unit Testing

**Q82: What is the difference between Stub, Mock, and Fake?**

**Answer:**

**Theory:** All three are **test doubles** but serve different purposes. A **Stub** provides PRE-CONFIGURED return values — you test the SUT's (System Under Test) STATE. A **Mock** verifies INTERACTIONS — you test whether the SUT called the right methods with the right arguments. A **Fake** has a working (but simplified) implementation — like an in-memory database instead of a real SQL Server. The distinction: with a Stub, you assert on the RESULT (e.g., `Assert.Equal(42, result)`). With a Mock, you assert on INTERACTIONS (e.g., `mock.Verify(x => x.Save(), Times.Once)`). A Fake is somewhere between — it acts like the real thing but runs in-process. Moq supports both: `Setup().Returns()` for stubbing, `Verify()` for mocking. The key: test the SUT's behavior, not the mock's setup — over-verifying makes tests brittle.

**Code Example:**
```csharp
[Test]
public void CreateOrder_ValidRequest_SavesToDatabase()
{
    // Arrange — Stub: provide data
    var userRepo = new Mock<IUserRepository>();
    userRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Name = "Alice" });

    // Arrange — Mock: verify interaction
    var orderRepo = new Mock<IOrderRepository>();

    var service = new OrderService(userRepo.Object, orderRepo.Object);

    // Act
    var order = await service.CreateOrderAsync(1, items);

    // Assert — STATE-based (Stub)
    Assert.That(order, Is.Not.Null);
    Assert.That(order.UserId, Is.EqualTo(1));

    // Assert — INTERACTION-based (Mock)
    orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
    orderRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
}

// Fake — in-memory database for integration tests
public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    public Task<User?> GetByIdAsync(int id)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    // ...
}
```

---

## 25. Scenario-Based Questions

**Q83: How would you optimize a slow API endpoint?**

**Answer:**

**Theory:** Optimization follows a systematic process: (1) **Measure** — use Application Insights, MiniProfiler, or stopwatch to identify WHERE time is spent (database, serialization, external API, CPU). (2) **Database** — check for N+1 queries (missing Include), missing indexes (use SQL Server Execution Plan), large result sets (add pagination), unnecessary columns (use Select projections), blocking queries (check `sys.dm_exec_requests`). (3) **Serialization** — large JSON payloads? Use compression, return fewer fields, use `System.Text.Json` source generators. (4) **Caching** — add in-memory or Redis cache for data that doesn't change frequently. (5) **Async** — ensure all I/O is truly async (no `.Result` or `.Wait()`). (6) **Parallelism** — independent API calls can run concurrently with `Task.WhenAll`. (7) **CDN** — serve static files and images from CDN. The 80/20 rule: fixing the database query (N+1, missing index, pagination) solves 80% of API performance issues.

**Code Example:**
```csharp
// BEFORE — slow version
[HttpGet("orders")]
public async Task<IActionResult> GetOrders()
{
    var orders = await _db.Orders.ToListAsync();  // BUG 1: No pagination

    var result = orders.Select(o => new OrderDto
    {
        CustomerName = o.Customer.Name,  // BUG 2: N+1 — each access queries DB
        TotalItems = o.Items.Count       // BUG 3: Another N+1 per order
    });

    return Ok(result);
}

// AFTER — optimized
[HttpGet("orders")]
public async Task<IActionResult> GetOrders(
    [FromQuery] int page = 1, [FromQuery] int size = 20)
{
    var orders = await _db.Orders
        .AsNoTracking()                    // Read-only — no tracking overhead
        .OrderByDescending(o => o.CreatedAt)
        .Skip((page - 1) * size)
        .Take(size)
        .Select(o => new OrderDto          // Single query with JOIN
        {
            Id = o.Id,
            CustomerName = o.Customer.Name,  // Loaded in one JOIN
            TotalItems = o.Items.Count       // Loaded in one JOIN
        })
        .ToListAsync();

    return Ok(new PagedResult<OrderDto>
    {
        Items = orders,
        Page = page,
        TotalCount = await _db.Orders.CountAsync()  // Separate count query
    });
}
```

---

**Q84: How would you handle millions of records in a single table?**

**Answer:**

**Theory:** Large tables (millions+ rows) need specific strategies: (1) **Indexing** — ensure query predicates use indexed columns. Covering indexes (INCLUDE non-key columns) avoid key lookups. Filtered indexes for common WHERE conditions. (2) **Partitioning** — split the table by a key (date, region) into multiple filegroups. Queries with partition elimination only scan relevant partitions. (3) **Paging** — always use keyset pagination (`WHERE Id > @lastId ORDER BY Id`) instead of `OFFSET/FETCH` for large offsets. (4) **Archiving** — move old/unused data to archive tables or cold storage. (5) **Read replicas** — separate read and write workloads. (6) **Materialized views** — pre-aggregated summary tables for reporting. (7) **Elasticsearch** — for full-text search on large datasets. The key insight: OFFSET pagination gets SLOWER as the offset grows (SQL Server must skip N rows). Keyset pagination stays O(1) regardless of page number.

**Code Example:**
```csharp
// BAD — OFFSET pagination (slows down with large offset)
var page = await db.Orders
    .OrderBy(o => o.Id)
    .Skip(100000)  // SQL must scan first 100,000 rows
    .Take(20)
    .ToListAsync();

// GOOD — Keyset pagination (always fast)
var page = await db.Orders
    .Where(o => o.Id > lastId)  // lastId from previous page
    .OrderBy(o => o.Id)
    .Take(20)
    .ToListAsync();

// Table partitioning script (SQL Server)
// CREATE PARTITION FUNCTION pf_DateRange (datetime2)
// AS RANGE RIGHT FOR VALUES ('2024-01-01', '2024-04-01', '2024-07-01');
// CREATE PARTITION SCHEME ps_DateRange AS PARTITION pf_DateRange ALL TO ([PRIMARY]);
// CREATE TABLE Orders (Id int, ...) ON ps_DateRange(OrderDate);
```

---

## Quick Reference: Complete .NET Interview Topics

| Section | Key Topics |
|---------|-----------|
| **Loading Strategies** | Eager (Include/ThenInclude), Lazy (proxies), Explicit (LoadAsync), Lazy<T>, deferred execution, IQueryable vs IEnumerable |
| **Memory Management** | Generational GC, Gen0/1/2, LOH, WeakReference, GC modes, pinned objects, GCHandle |
| **JIT/AOT** | JIT vs AOT, Tiered Compilation, ReadyToRun, Native AOT, inlining control, R2R hybrid |
| **Reflection** | Type metadata, MethodInfo.Invoke, Expression.Compile, Reflection.Emit, source generators |
| **Threading** | Task vs Thread vs ThreadPool, async/await state machine, ConfigureAwait(false), ValueTask |
| **Concurrency** | SemaphoreSlim, ConcurrentDictionary, Channel<T>, Interlocked, lock, Monitor, Mutex, PLINQ |
| **C# Fundamentals** | Boxing, var vs dynamic vs object, const vs readonly, struct vs class, records, delegates, lambdas |
| **Exception Handling** | throw vs throw ex, exception filters, global handlers, IExceptionHandler, ProblemDetails |
| **Collections** | List vs HashSet vs Dictionary, IEnumerable vs ICollection vs IList vs IQueryable, generic constraints |
| **LINQ** | FirstOrDefault vs SingleOrDefault, SelectMany, deferred vs immediate, Any vs Count, GroupBy |
| **ASP.NET Core** | Middleware pipeline, Filters vs Middleware, Kestrel, hosting, configuration, options pattern |
| **EF Core** | AsNoTracking vs AsTracking, Find vs FirstOrDefault, Include vs ThenInclude vs Select, migrations |
| **SQL Server** | Clustered vs Non-Clustered index, CTE, UNION vs UNION ALL, window functions, execution plan |
| **Design Patterns** | Singleton, Repository/Unit of Work, CQRS, Factory, Strategy, Decorator, Facade |
| **SOLID** | Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion |
| **Architecture** | N-Tier vs Clean Architecture, Microservices, Saga pattern, event-driven, CQRS |
| **Caching** | Cache-Aside, IMemoryCache, IDistributedCache (Redis), cache stampede, cache invalidation |
| **API Security** | JWT structure and validation, CORS, XSS, CSRF, SQL injection, rate limiting |
| **Unit Testing** | Stub vs Mock vs Fake, xUnit/NUnit, Moq, TDD, integration testing |
| **Serialization** | System.Text.Json source generators, XML vs DataContract, String vs StringBuilder |
| **Performance** | Span<T>/Memory<T>, ArrayPool<T>, ValueTask<T>, struct layout, stackalloc, connection pooling |

---

> **Tip:** These questions probe deep .NET runtime knowledge that separates senior from mid-level developers. If you can explain how async compiles, how GC generations work, and the difference between ValueTask and Task, you're demonstrating senior-level understanding of the platform.
