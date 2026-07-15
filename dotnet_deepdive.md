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

## Quick Reference: Key .NET Interview Topics Summary

| Topic | Key Points |
|-------|-----------|
| **Loading Strategies** | Eager (Include/ThenInclude), Lazy (proxies/tracking), Explicit (LoadAsync), Lazy<T> (thread-safe deferral), deferred execution |
| **Memory Management** | Generational GC, Gen0/1/2, LOH, WeakReference, GC modes, pinned objects |
| **JIT/AOT** | JIT vs AOT, Tiered Compilation, ReadyToRun, Native AOT, inlining control |
| **Reflection** | Type metadata, MethodInfo.Invoke, Expression.Compile, Reflection.Emit, source generators |
| **Threading** | Task vs Thread vs ThreadPool, async/await state machine, ConfigureAwait(false) |
| **Concurrency** | SemaphoreSlim, ConcurrentDictionary, Channel<T>, Interlocked, lock |
| **Serialization** | System.Text.Json, source generators, XmlSerializer, DataContractSerializer |
| **DI** | Lifetimes (Singleton/Scoped/Transient), composition root, container disposal |
| **Performance** | Span<T>/Memory<T>, ValueTask<T>, ArrayPool<T>, struct layout, stackalloc |

---

> **Tip:** These questions probe deep .NET runtime knowledge that separates senior from mid-level developers. If you can explain how async compiles, how GC generations work, and the difference between ValueTask and Task, you're demonstrating senior-level understanding of the platform.
