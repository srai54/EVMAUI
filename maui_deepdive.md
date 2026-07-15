# .NET MAUI Deep Dive — 150+ Interview Questions

> **Pure .NET MAUI questions** covering architecture, rendering, Shell navigation, layout, data binding, MVVM, performance, custom controls, platform interop, testing, deployment, and advanced MAUI concepts. No ASP.NET/EF Core — just MAUI.

---

## How to Use This Guide

Each question has two parts:
- **Theory** — The conceptual foundation. Understand the "why" behind the concept.
- **Code Example** — Practical implementation. See how the theory applies in real code.

---

## 1. MAUI Architecture & Rendering Pipeline

**Q1: What is the difference between Handlers and Renderers in .NET MAUI?**

**Answer:**

**Theory:** .NET MAUI's architecture has two eras: **Renderers** (Xamarin.Forms legacy) and **Handlers** (MAUI's modern approach). **Renderers** are heavyweight — each renderer is a `ViewRenderer<TView, TPlatformView>` that creates the platform control, handles property changes, and manages the entire lifecycle. There's a 1:1:1 mapping: one MAUI control → one renderer → one platform view. Renderers are classes that you must subclass to customize, leading to deep inheritance hierarchies. **Handlers** are lightweight — they separate the mapping into a `PropertyMapper` (a dictionary mapping MAUI properties to platform update actions) and a `CommandMapper` (for actions like focus/layout). Handlers use composition over inheritance: the handler is a simple bridge that connects abstract properties to concrete platform behavior. MAUI ships handlers for all first-party controls. If you need to customize, you modify the handler's mapper rather than subclassing. This makes Handlers faster (less indirection), more memory-efficient (shorter object graphs), and easier to extend.

**Code Example:**
```csharp
// Handler approach — PropertyMapper drives updates
public class MyEntryHandler : ViewHandler<IEntry, PlatformEntry>
{
    public static IPropertyMapper<IEntry, MyEntryHandler> Mapper = new PropertyMapper<IEntry, MyEntryHandler>(ViewHandler.ViewMapper)
    {
        [nameof(IEntry.Text)] = (handler, entry) => handler.PlatformView.Text = entry.Text,
        [nameof(IEntry.TextColor)] = (handler, entry) => handler.PlatformView.TextColor = entry.TextColor.ToPlatform(),
    };

    public MyEntryHandler() : base(Mapper) { }

    protected override PlatformEntry CreatePlatformView() => new PlatformEntry(Context);
}
```

---

**Q2: How does the MAUI rendering pipeline work from XAML to pixels?**

**Answer:**

**Theory:** The rendering pipeline has six stages. (1) **XAML Parsing** — `MauiXamlReader` parses XAML into an object tree using reflection and `CreateContent` from `ContentPage`. Bindings are stored as `BindingExpression` objects, not resolved values. (2) **Logical Tree** — The parsed objects form the logical tree (Page → Layout → Controls). The tree is lazy: nothing is measured or arranged yet. (3) **Handler Creation** — When a `VisualElement` is added to a `VisualTree`, MAUI's `VisualDiagnostics` fires, and the element's handler is created via `IViewHandler`. Each handler maps to a platform view. (4) **Measure** — The root page calls `MeasureOverride` on itself, which propagates down the tree. Each layout asks children for their desired size. MAUI uses double-pass: measure (size available), then arrange (position). (5) **Arrange** — The root calls `ArrangeOverride`, positioning each child at its final location. (6) **Platform Rendering** — Each handler's `PlatformView` (e.g., `UIView` on iOS, `View` on Android) is laid out using native APIs (`Auto Layout` on iOS, `View#layout` on Android). The platform takes over from here — Core Animation (iOS) or Canvas (Android) paints pixels.

**Key insight:** The MAUI tree and the platform tree coexist. The handler is the bridge. If you access `handler.PlatformView`, you're working with the native control directly.

---

**Q3: How does MAUI map a single XAML element to different platform controls?**

**Answer:**

**Theory:** MAUI uses **handler registration** at startup to map abstract controls to platform-specific implementations. In `MauiProgram.cs`, `UseMauiApp<T>` calls `ConfigureMauiHandlers(handlers => ...)`. Each control type has a handler registered per platform. For example, `Button` maps to `ButtonHandler`, which creates `UIButton` on iOS, `AppCompatButton` on Android, `Button` on WinUI, and `NSButton` on Mac Catalyst. The handler's `CreatePlatformView()` returns the appropriate native control for the current platform. `PropertyMapper` converts abstract property changes into platform-specific calls. This decouples the MAUI API from any single platform — adding a new platform just requires a new handler registration.

**Code Example:**
```csharp
// In MauiProgram.cs — handler registration
builder.ConfigureMauiHandlers(handlers =>
{
    handlers.AddHandler<Button, ButtonHandler>();
    handlers.AddHandler<Entry, EntryHandler>();
});
// Each handler internally does:
// iOS:  new UIKit.UIButton()
// Android: new AndroidX.AppCompat.Widget.AppCompatButton()
// Windows: new Microsoft.UI.Xaml.Controls.Button()
```

---

**Q4: What is the `VisualElement` hierarchy in MAUI?**

**Answer:**

**Theory:** `VisualElement` is the base class for anything that appears on screen. The hierarchy: `VisualElement` → `View` → `Layout` → `Layout<T>` → `Grid/VerticalStackLayout/HorizontalStackLayout/FlexLayout/AbsoluteLayout`. `Page` also inherits from `VisualElement` (indirectly). Key properties defined at `VisualElement` level: `Background`, `IsVisible`, `Opacity`, `Rotation`, `Scale`, `TranslationX/Y`, `AnchorX/Y`, `InputTransparent`, `WidthRequest`, `HeightRequest`, `MinimumWidthRequest`, `MinimumHeightRequest`, `FlowDirection`, `LayoutFlags` (for `AbsoluteLayout`). `View` adds `GestureRecognizers` and shadow/frame properties. `Layout` adds `Padding`, `IsClippedToBounds`, and `CascadeInputTransparent`. Understanding this hierarchy is critical for knowing what properties are available at each level and what triggers re-layout vs. re-render.

---

## 2. Shell Navigation

**Q5: How does Shell routing work under the hood?**

**Answer:**

**Theory:** Shell uses **URI-based navigation** with a `Routing` system. When you call `Shell.Current.GoToAsync("//dashboard/profile?id=42")`, Shell parses the URI into segments: route, query parameters, and fragments. Each route segment maps to a registered `Route` — either a hierarchical route (`//tab/page`) or a relative route (`page`). The `Routing` class maintains a static `RouteDictionary` mapping URI segments to `Page` types. When a route is resolved, Shell creates the page using DI. If route parameters are present (e.g., `?id=42`), Shell uses `IQueryAttributable` to set them on the target `Page` or `ViewModel`. Shell maintains a **navigation stack** per tab (hierarchical navigation) and a **root stack** for modal / absolute routes. `GoToAsync` returns a `Task` that completes when the navigation animation finishes. Shell also supports **deep linking**: `OnAppLinkRequestReceived` can intercept URIs from outside the app.

**Code Example:**
```csharp
// Register routes in AppShell.xaml.cs or App.xaml.cs
Routing.RegisterRoute("stationdetail", typeof(StationDetailPage));
Routing.RegisterRoute("swaprequest", typeof(SwapRequestPage));

// Navigate with query parameters
await Shell.Current.GoToAsync($"stationdetail?stationId={station.Id}");

// Target page receives query params via IQueryAttributable
public partial class StationDetailPage : IQueryAttributable
{
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        StationId = (int)query["stationId"];
    }
}
```

---

**Q6: What are the differences between absolute (`//`) and relative routes in Shell?**

**Answer:**

**Theory:** Shell distinguishes three route types. (1) **Absolute routes** (`//route/tab/page`) — start with `//` and navigate from the root. They switch the active tab if needed and reset that tab's navigation stack. For example, `//dashboard/stations` navigates to the dashboard tab's stations sub-page, replacing the back stack entirely. (2) **Relative routes** (`page` or `tab/page`) — push onto the current navigation stack without switching tabs. `await Shell.Current.GoToAsync("stationdetail")` pushes `StationDetailPage` onto the current tab's stack; the back button pops it. (3) **Global routes** registered via `Routing.RegisterRoute` — can be navigated to from any tab as long as no absolute prefix overrides them. The rule: `//` means "from root, switch context" — use it for tab switches and primary navigation. Routes without `//` mean "push onto current stack" — use it for drill-downs. Mixing them incorrectly causes navigation animations to skip or stacks to corrupt.

---

**Q7: How does Shell manage its navigation stack per tab?**

**Answer:**

**Theory:** Shell internally maintains a **navigation stack per Tab** (`ShellSection`). Each tab has its own `INavigation` stack — the same `NavigationPage`-like behavior but managed by Shell's `ShellNavigationManager`. When you navigate to a relative route within a tab, Shell pushes onto that tab's stack, and the back button pops it. When you navigate to an absolute route (`//othertab`), Shell switches the active `ShellSection` and shows that tab's root content — the previous tab's stack is preserved intact (its state is not lost). Shell also supports **modal navigation** — `await Shell.Current.GoToAsync("modalPage")` with `modal=true` attribute pushes onto a separate modal stack that overlays the entire Shell. The modal stack persists across tab switches. `Shell.Current.Navigation` returns the current tab's navigation. The stack depth can be inspected via `Shell.Current.Navigation.NavigationStack.Count`. Shell enforces a minimum stack depth of 1 (the root page) — you cannot `GoBack` from the root.

---

**Q8: How do you pass complex objects between Shell pages?**

**Answer:**

**Theory:** URI-based navigation supports only **primitive query parameters** (strings, numbers, booleans) because parameters are serialized in the URI. For complex objects, you have three strategies: (1) **IQueryAttributable** + serialization — serialize the object to JSON, pass it as a query parameter, and deserialize it in the target page. This works but bloats URLs for large objects. (2) **Shared service** — store the object in a singleton service (e.g., `NavigationDataService` with a `Dictionary<string, object>`) before navigation, and the target page retrieves it. This is the most common MAUI pattern. (3) **MessagingCenter / WeakReferenceMessenger** — send the object via a message before navigating. The target page subscribes to the message in `OnAppearing`. Option 2 (shared service) is preferred because it's explicit, testable, and doesn't pollute the URI.

**Code Example:**
```csharp
// Approach 2: Shared service (recommended)
public class NavigationDataService
{
    private readonly Dictionary<string, object> _data = new();
    public void Set(string key, object value) => _data[key] = value;
    public T Get<T>(string key) => (T)_data[key];
}

// Sender
_navData.Set("selectedStation", station);
await Shell.Current.GoToAsync("stationdetail");

// Receiver — StationDetailViewModel
[QueryProperty(nameof(StationId), "stationId")]
public partial class StationDetailViewModel : BaseViewModel
{
    public void OnNavigatedTo()
    {
        var station = _navData.Get<StationModel>("selectedStation");
        // Use station object
    }
}
```

---

## 3. MAUI Layout System

**Q9: How does the MAUI layout measurement pass work?**

**Answer:**

**Theory:** MAUI layout uses a **two-pass system** inherited from WPF/UWP: **Measure** then **Arrange**. In the **Measure pass**, the root layout calls `MeasureOverride(availableSize)` on each child. Each child returns its `DesiredSize` — the minimum size it needs. Layouts like `Grid` allocate available space according to row/column definitions (Auto, Star, Absolute). `StackLayout` asks children for their size in the stacking direction and returns the accumulated size. In the **Arrange pass**, the parent calls `ArrangeOverride(finalSize)` on each child, giving the child its allocated bounds. Children then position themselves. Constraints flow down; sizes flow up. MAUI uses `double.PositiveInfinity` for unconstrained dimensions (e.g., `ScrollView` in the scrolling direction). A key performance insight: the entire tree re-measures when any child requests a different size (e.g., text changing). This is why `VerticalStackLayout` is faster than `StackLayout` — it doesn't re-measure siblings when content changes.

**Code Example:**
```csharp
public class MyLayout : Layout<View>
{
    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        var totalHeight = 0.0;
        foreach (var child in Children)
        {
            child.Measure(widthConstraint, double.PositiveInfinity);
            totalHeight += child.DesiredSize.Height;
        }
        return new Size(widthConstraint, totalHeight);
    }

    protected override Size ArrangeOverride(Rect bounds)
    {
        var y = bounds.Y;
        foreach (var child in Children)
        {
            child.Arrange(new Rect(bounds.X, y, bounds.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return bounds.Size;
    }
}
```

---

**Q10: When should you use `VerticalStackLayout` vs `StackLayout`?**

**Answer:**

**Theory:** `VerticalStackLayout` (and `HorizontalStackLayout`) are **new in .NET MAUI** as replacements for `StackLayout`. The old `StackLayout` is legacy from Xamarin.Forms. The key difference: `StackLayout` uses `ILayout` and measures children using the **standard measure/arrange** pass, but it re-measures all children whenever any child changes size (height in `StackOrientation.Vertical`). `VerticalStackLayout` uses `IContentLayout` in **vertical-only** mode — it tells MAUI that only the vertical dimension affects layout, so when a child's height changes, only that child and the parent get re-measured, not siblings. This reduces measure pass costs from O(n) to O(1) for height changes. `VerticalStackLayout` also supports `Spacing` directly. **Recommendation:** always prefer `VerticalStackLayout` / `HorizontalStackLayout` over `StackLayout`. The old `StackLayout` exists only for backward compatibility with Xamarin.Forms code.

**Code Example:**
```xml
<!-- Prefer this (MAUI-native, faster) -->
<VerticalStackLayout Spacing="10">
    <Label Text="Item 1" />
    <Label Text="Item 2" />
</VerticalStackLayout>

<!-- Avoid this (legacy, slower) -->
<StackLayout Orientation="Vertical" Spacing="10">
    <Label Text="Item 1" />
    <Label Text="Item 2" />
</StackLayout>
```

---

**Q11: How does `FlexLayout` differ from `Grid` and when would you use it?**

**Answer:**

**Theory:** `FlexLayout` implements the **CSS Flexbox** layout model in MAUI. Unlike `Grid` (row/column based 2D positioning), `FlexLayout` arranges children in a single direction (horizontal or vertical) and wraps them into multiple rows/columns when they overflow. Key properties: `Wrap` (wrap/no wrap), `JustifyContent` (alignment in main axis: Start, Center, End, SpaceBetween, SpaceAround, SpaceEvenly), `AlignItems` (alignment in cross axis: Stretch, Center, Start, End), `AlignContent` (multi-line alignment), `Direction` (row/column/reverse). Use `FlexLayout` for: wrap panels (tags, chips), toolbars, responsive layouts where children change size dynamically, and centering content in both axes (combine `JustifyContent="Center"` with `AlignItems="Center"`). Use `Grid` for: 2D data entry forms, dashboard layouts with fixed columns, any layout where children need specific row/column positions. `Grid` is more performant for known-size grids; `FlexLayout` is better for dynamic wrap scenarios.

**Code Example:**
```xml
<!-- FlexLayout wrapping chips -->
<FlexLayout Wrap="Wrap" JustifyContent="Start" AlignItems="Center">
    <Button Text="Chip 1" CornerRadius="16" />
    <Button Text="Chip 2" CornerRadius="16" />
    <Button Text="Chip 3" CornerRadius="16" />
</FlexLayout>

<!-- Equivalent Grid for 2-column form -->
<Grid ColumnDefinitions="Auto,*" RowDefinitions="Auto,Auto">
    <Label Text="Name" Grid.Row="0" Grid.Column="0" />
    <Entry Grid.Row="0" Grid.Column="1" />
    <Label Text="Email" Grid.Row="1" Grid.Column="0" />
    <Entry Grid.Row="1" Grid.Column="1" />
</Grid>
```

---

**Q12: What causes unnecessary layout passes in MAUI and how do you avoid them?**

**Answer:**

**Theory:** Layout passes are expensive because they cascade through the entire visual tree. Common causes: (1) **Binding to layout-affecting properties** without constraints — `HeightRequest`, `WidthRequest`, `Margin`, `Padding`, `IsVisible` changes trigger re-measure. (2) **Dynamic text** — `Label` with no `WidthRequest` re-measures whenever text changes, affecting parent layouts. Fix: set `MaximumWidthRequest` or constrain the width via `Grid` columns. (3) **Using `StackLayout`** instead of `VerticalStackLayout` — legacy re-measure semantics. (4) **Unnecessary `LayoutOptions.CenterAndExpand`** — `Expand` flags tell the layout to give extra space, which triggers re-measure on siblings. Use `LayoutOptions.Center` instead. (5) **Frequent `IsVisible` toggles** — use `Opacity` + `InputTransparent` for fade transitions instead of `IsVisible` to avoid re-layout. (6) **`AbsoluteLayout` with proportional positioning** — every child re-measures on any change. Use percent-based `Grid` instead. Profile layout performance using the MAUI `Layout` diagnostic tool: set environment variable `DOTNET_MAUI_LAYOUT_CYCLES=1` to log layout pass counts.

---

## 4. Data Binding & Compiled Bindings

**Q13: What is the difference between `x:Bind` and `Binding` in MAUI?**

**Answer:**

**Theory:** `Binding` (runtime binding) uses reflection at runtime to resolve property paths. It's flexible — supports converters, string paths, ancestor bindings, fallbacks — but slower because path resolution, conversion, and change notification happen via reflection each time. `x:Bind` (compiled binding) resolves property paths **at compile time** using source generators. The XAML compiler generates strongly-typed code that directly accesses the ViewModel's properties and calls `OnPropertyChanged` handlers. This gives: (1) compile-time type checking — broken bindings are build errors, not runtime crashes, (2) ~5-10x faster binding resolution, (3) smaller binary size (no reflection metadata). Constraints: `x:Bind` requires `x:DataType` to be set on the page or element — the compiler needs to know the ViewModel type. `x:Bind` doesn't support `StringFormat`, `FallbackValue`, `TargetNullValue`, `RelativeSource`, or `IValueConverter` directly (use `x:Bind` with converter functions instead). **Best practice:** use `x:Bind` everywhere with `x:DataType` set on every page. Fall back to `Binding` only when you need `RelativeSource` or dynamic converters.

**Code Example:**
```xml
<!-- Compiled binding — compile-time checked, ~10x faster -->
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:vm="clr-namespace:MyApp.ViewModels"
             x:DataType="vm:DashboardViewModel">
    <Label Text="{x:Bind UserName}" />
    <Label Text="{x:Bind BatteryLevel, Converter={StaticResource LevelToColor}}" />
</ContentPage>

<!-- Runtime binding — reflection-based, flexible but slower -->
<Label Text="{Binding UserName}" />
```

---

**Q14: How do you use `x:DataType` with compiled bindings and why is it important?**

**Answer:**

**Theory:** `x:DataType` tells the XAML compiler what type the binding context will be. It enables compiled bindings (`x:Bind`). When set on a `ContentPage`, the compiler knows that `{x:Bind UserName}` maps to `DashboardViewModel.UserName`. If `UserName` doesn't exist or the type is wrong, the build fails instead of silently failing at runtime. `x:DataType` is inherited by child elements — you can override it for specific sections. For example, in a `CollectionView.ItemTemplate`, set `x:DataType` to the item type. The compiler generates `global::MyApp.ViewModels.DashboardViewModel.get_UserName()` IL directly, with no reflection. This also enables IDE features (Go To Definition, Rename) for bindings. **Critical:** without `x:DataType`, `x:Bind` falls back to runtime binding — you lose all compile-time benefits.

**Code Example:**
```xml
<ContentPage x:DataType="vm:DashboardViewModel">
    <!-- Inherits DashboardViewModel -->
    <Label Text="{x:Bind Title}" />

    <CollectionView ItemsSource="{x:Bind RecentActivity}">
        <CollectionView.ItemTemplate>
            <DataTemplate x:DataType="models:RecentActivityModel">
                <!-- Override for item type -->
                <Label Text="{x:Bind Description}" />
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>
</ContentPage>
```

---

**Q15: How does `INotifyPropertyChanged` work and how does CommunityToolkit.Mvvm simplify it?**

**Answer:**

**Theory:** `INotifyPropertyChanged` is the interface that enables data binding. It has a single event: `PropertyChanged(object sender, PropertyChangedEventArgs e)`. When a property setter fires `PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PropertyName)))`, the binding engine re-reads the property value and updates the UI. Manually implementing this is verbose: each property needs a backing field, a setter with a null-check, and an event invocation. **CommunityToolkit.Mvvm** uses **source generators** (`[ObservableProperty]`) to generate this boilerplate at compile time. The `[ObservableProperty]` attribute on a field generates: (1) a public property, (2) `PropertyChanged` notification in the setter, (3) optional `OnXxxChanged` partial method that fires on change. The source generator emits IL directly, so there's zero runtime overhead.

**Code Example:**
```csharp
// Manual — 15+ lines per property
private string _title;
public string Title
{
    get => _title;
    set
    {
        if (_title != value)
        {
            _title = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
        }
    }
}

// CommunityToolkit.Mvvm — 1 line per property
[ObservableProperty]
private string _title;

// Source generator produces the equivalent manual code
```

---

**Q16: How do `IValueConverter` and `IMultiValueConverter` work?**

**Answer:**

**Theory:** `IValueConverter` transforms a binding value from the source type to the target type (and optionally back for two-way binding). MAUI passes the value, target type, converter parameter, and culture. Common uses: `bool` to `Visibility` (e.g., `InverseBoolConverter`), battery-level number to color, status string to icon. `ConvertBack` is needed for two-way bindings like `Entry.Text`. `IMultiValueConverter` takes multiple binding values (via `MultiBinding`) and combines them. For example, combine `Latitude` + `Longitude` into a map coordinate string. Both converters should be stateless (no instance fields) for performance — register them as singletons in `ResourceDictionary`. MAUI doesn't wire converters to `x:Bind` directly — for compiled bindings, use converter functions in the ViewModel instead.

**Code Example:**
```csharp
public class BatteryLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var level = (int)value;
        return level switch
        {
            <= 20 => Colors.Red,
            <= 50 => Colors.Orange,
            _ => Colors.Green
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// In XAML
<Label Text="Battery" TextColor="{Binding BatteryLevel, Converter={StaticResource BatteryLevelToColor}}" />
```

---

## 5. MVVM with CommunityToolkit

**Q17: How does `[RelayCommand]` work and how is it different from `ICommand`?**

**Answer:**

**Theory:** `[RelayCommand]` is a source generator attribute from `CommunityToolkit.Mvvm` that generates `ICommand` properties from methods. When you annotate a method with `[RelayCommand]`, the source generator creates: (1) a `RelayCommand` property named `MethodNameCommand`, (2) a `CanExecute` method if the method returns `bool`, (3) `NotifyCanExecuteChanged()` support. This eliminates boilerplate: manually you'd need a class that implements `ICommand`, with `CanExecute`, `Execute`, and `CanExecuteChanged`. The generated `RelayCommand` wraps `ICommand` with `Action`/`Func<T>` delegates using `WeakReference` to prevent memory leaks. Async variants: `[RelayCommand]` on an `async Task` method generates `AsyncRelayCommand`, which tracks `IsRunning` and prevents re-entry. `AsyncRelayCommand` calls `CommandManager.InvalidateRequerySuggested` when execution completes.

**Code Example:**
```csharp
// Manual ICommand — 20+ lines
public class LoginCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;
    public event EventHandler CanExecuteChanged;
    public LoginCommand(Action execute, Func<bool> canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object parameter) => _execute();
}

// CommunityToolkit — 3 lines
[RelayCommand]
private async Task LoginAsync()
{
    await _authService.LoginAsync(Username, Password);
}

// Generator produces: LoginCommand of type AsyncRelayCommand
// XAML: <Button Command="{Binding LoginCommand}" />
```

---

**Q18: How does `WeakReferenceMessenger` work and when should you use it?**

**Answer:**

**Theory:** `WeakReferenceMessenger` is a **weak-referenced message broker** — it allows loosely-coupled communication between objects without creating strong references that prevent garbage collection. Senders send messages (any type), recipients register to receive them. The messenger holds `WeakReference<IRecipient>` to each subscription, so when a ViewModel is no longer referenced elsewhere, it can be GC'd without explicitly unregistering. Use cases: (1) cross-ViewModel communication (e.g., "user logged in" → dashboard should refresh), (2) cross-page events ("swap completed" → return to dashboard), (3) sending data from a service to an active page. **Don't use** for: ViewModel-to-View binding (use data binding), or scenarios better served by DI/shared services. Overusing Messenger makes data flow hard to trace — prefer explicit service dependencies for essential communication.

**Code Example:**
```csharp
// Define message type
public record SwapCompletedMessage(int StationId, string BatteryType);

// Sender (SwapRequestViewModel)
WeakReferenceMessenger.Default.Send(new SwapCompletedMessage(stationId, batteryType));

// Receiver (DashboardViewModel)
public partial class DashboardViewModel : BaseViewModel, IRecipient<SwapCompletedMessage>
{
    public void Receive(SwapCompletedMessage message)
    {
        // Refresh dashboard data
        LoadDashboardCommand.Execute(null);
    }
}

// Registration
WeakReferenceMessenger.Default.RegisterAll(this);
```

---

**Q19: What is the lifecycle of a ViewModel in MAUI?**

**Answer:**

**Theory:** ViewModel lifecycle is tied to its page lifecycle: (1) **Construction** — DI creates the ViewModel when the page is navigated to. Constructor injection resolves all dependencies. (2) **`OnAppearing`** — the page fires `OnAppearing`, which the ViewModel uses to load initial data. Typically calls `LoadDataAsync()` or `InitializeAsync()`. (3) **Active** — the user interacts with the page, calling RelayCommands. Data binding keeps the UI in sync. (4) **`OnDisappearing`** — the page goes off-screen (another page pushed or tab switched). The ViewModel remains alive in the navigation stack. (5) **Navigation removal** — when the page is popped from the stack, the page and its ViewModel become eligible for GC. In Shell, tabs cache their root pages but not pushed pages. Shell caches `ShellContent` pages by default — they're created once and stay alive. To release resources, implement `IAppearingAware` or use `OnDisappearing` to cancel subscriptions. For long-lived services that outlive the ViewModel, use `WeakReferenceMessenger` or register services as Singletons.

---

## 6. Performance & Optimization

**Q20: What are the most impactful MAUI performance optimizations?**

**Answer:**

**Theory:** The top 10 performance optimizations ranked by impact: (1) **Compiled bindings** — use `x:Bind` with `x:DataType` on every page. Runtime bindings are ~10x slower. (2) **`CollectionView` over `ListView`** — `CollectionView` uses virtualization by default; `ListView` uses a heavier cell adapter model. (3) **Startup trimming** — enable `<PublishTrimmed>true</PublishTrimmed>` and `<TrimMode>partial</TrimMode>` to reduce assembly size and JIT time. (4) **AOT compilation** — on iOS and Android, enable `<RunAOTCompilation>true</RunAOTCompilation>` in Release builds. AOT compiles IL to native code before deployment, eliminating JIT pauses. (5) **Image optimization** — use `ResizedImage` source, set `CacheValidity` on `UriImageSource`, avoid base64 images, prefer `jpg` over `png` for photos. (6) **Layout reduction** — flatten nested layouts. Each `Layout` adds measure/arrange overhead. Replace `Grid > StackLayout > Grid > StackLayout` with a single `Grid`. (7) **`VerticalStackLayout` over `StackLayout`** — see Q10. (8) **Minimize bindings** — each binding adds a `PropertyChanged` handler. 1000 bindings = 1000 listeners. Use `OneTime` mode for static values. (9) **Lazy loading** — defer loading of tabs and complex views using `ShellContent` with `ContentTemplate` (lazy creation). (10) **Disable form metrics** — set `XamlCompilationOptions.Compile` on all XAML files.

**Code Example:**
```xml
<!-- Enable XAML compilation -->
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MyApp.Views.DashboardPage"
             x:DataType="vm:DashboardViewModel">
    <!-- Compiled bindings for performance -->
    <CollectionView ItemsSource="{x:Bind Items}">
        <CollectionView.ItemTemplate>
            <DataTemplate x:DataType="models:ItemModel">
                <Label Text="{x:Bind Name}" />
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>
</ContentPage>

<!-- Lazy tab loading -->
<ShellContent Title="Dashboard"
              ContentTemplate="{DataTemplate views:DashboardPage}" />
```

---

**Q21: How does `CollectionView` virtualization work in MAUI?**

**Answer:**

**Theory:** `CollectionView` uses **UI virtualization** — it creates only enough platform views to fill the visible area plus a small buffer, reusing them as the user scrolls. When an item scrolls off-screen, its view is recycled and bound to the next incoming item. The `CollectionView`'s layout (Linear, Grid, Horizontal) determines the arrangement. The `ItemsLayout` property sets `LinearItemsLayout` (vertical list) or `GridItemsLayout` (grid with `Span`). Virtualization works only when the `CollectionView` has a fixed height (constrained by parent layout or `HeightRequest`). If `CollectionView.Height` is `Auto` (inside a `StackLayout`), virtualization is disabled — all items are created upfront, destroying performance for large data sets. Always wrap `CollectionView` in a layout that constrains its height (e.g., `Grid` with `*` row). `RemainingItemsThreshold` triggers incremental loading. `ItemSizingStrategy` controls whether items have uniform (`MeasureFirstItem`) or variable (`MeasureAllItems`) sizes — `MeasureAllItems` is slower but necessary for varying item sizes.

**Code Example:**
```xml
<!-- CollectionView with virtualization enabled (height constrained by Grid) -->
<Grid RowDefinitions="*">
    <CollectionView ItemsSource="{x:Bind Stations}"
                    RemainingItemsThreshold="5"
                    RemainingItemsThresholdReachedCommand="{x:Bind LoadMoreCommand}">
        <CollectionView.ItemsLayout>
            <LinearItemsLayout Orientation="Vertical" ItemSpacing="8" />
        </CollectionView.ItemsLayout>
        <CollectionView.ItemTemplate>
            <DataTemplate x:DataType="models:StationModel">
                <Frame Padding="16" Margin="8,0">
                    <Grid ColumnDefinitions="*,Auto">
                        <Label Text="{x:Bind Name}" />
                        <Label Text="{x:Bind Distance, StringFormat='{0:F1} km'}"
                               Grid.Column="1" />
                    </Grid>
                </Frame>
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>
</Grid>
```

---

**Q22: How do you reduce MAUI app startup time?**

**Answer:**

**Theory:** Startup time is the time from app launch to the first frame being interactive. Key strategies: (1) **AOT compilation** (`<RunAOTCompilation>true</RunAOTCompilation>`) — pre-compiles IL to native code. Adds ~20% to binary size but eliminates JIT pauses. On iOS, AOT is mandatory for App Store submission anyway. (2) **Trimming** (`<PublishTrimmed>true</PublishTrimmed>`) — removes unused assemblies and IL code. Combine with `<TrimMode>partial</TrimMode>` and `ILLink.Substitutions.xml` for fine control. (3) **Lazy initialization** — `UseMauiApp<T>` registers many services; some aren't needed at startup. Use `ContentTemplate="{DataTemplate views:Page}"` on `ShellContent` to defer page creation until the tab is selected. (4) **Minimize main-thread work** — move heavy initialization (DB setup, file loading) to background tasks with `Task.Run` or `Task.WhenAll`. (5) **Reduce XAML parsing** — use `XamlCompilationOptions.Compile` on all pages. (6) **Splash screen** — use a native splash (window background color + centered image) that shows instantly, then transition to the first page. (7) **Pre-jitting** — on Android, `AndroidEnablePreloadAssemblies` and `AndroidEnableProfiledAot` can pre-compile assemblies during installation. Profile startup with the MAUI startup trace: `dotnet trace collect --profile maui-startup`.

---

**Q23: How do you handle memory management in MAUI to avoid leaks?**

**Answer:**

**Theory:** Common MAUI memory leak patterns and fixes: (1) **Event handler leaks** — subscribing to static events (`MessagingCenter`, `WeakReferenceMessenger`) without unregistering. Fix: implement `IDisposable` in ViewModels and unregister in `OnDisappearing`. (2) **Binding leaks** — bindings on pages that are never unloaded keep references to ViewModels. Fix: set `BindingContext = null` in code-behind when the page is removed. (3) **Handler circular references** — handlers hold references to platform views, which hold references to handlers. Fix: ensure handlers are detached on page unload — call `Handler?.Disconnect()`. (4) **Image cache bloat** — `UriImageSource` caches images in memory without bounds. Fix: set `CacheValidity` and use `DownsampleWidth`/`DownsampleHeight`. (5) **Timer/Animation leaks** — `Task.Delay` or `Animation` callbacks capture the page reference. Fix: cancel timers in `OnDisappearing`. Use `CancellationTokenSource` linked to page lifecycle. (6) **Shell page caching** — `ShellContent` pages are cached by default. For pages with heavy resources, set `ShellContent.ContentTemplate` to force lazy creation. Monitor memory with `dotnet counters monitor` on desktop or Xcode Instruments on iOS.

**Code Example:**
```csharp
// Prevent leaks: cancel tasks on disappear
private CancellationTokenSource _cts = new();

public void OnDisappearing()
{
    _cts.Cancel();
    _cts = new(); // Reset for next appearance
}

// Unregister messenger to prevent leaks
public void Dispose()
{
    WeakReferenceMessenger.Default.UnregisterAll(this);
}
```

---

## 7. Custom Controls, Handlers & Behaviors

**Q24: How do you create a custom MAUI control using Handlers?**

**Answer:**

**Theory:** Creating a custom control involves three parts: (1) **MAUI control class** — extends `View`, defines bindable properties that users set in XAML. (2) **Platform handler** — extends `ViewHandler<TVirtual, TPlatform>`, implements `CreatePlatformView()` and `PropertyMapper` to map MAUI properties to native control properties. (3) **Handler registration** — in `MauiProgram.cs`, call `handlers.AddHandler<MyControl, MyControlHandler>()`. The handler is per-platform — you need separate handler classes for each target platform (iOS, Android, Windows, Mac). MAUI resolves the handler at runtime by looking up the registered type. This is the **correct** way to create custom controls — do NOT subclass existing controls and override platform methods. Use handler customization instead. For simpler scenarios (extending an existing control), use `Behaviors` or `Effects`.

**Code Example:**
```csharp
// 1. MAUI control
public class RatingBar : View
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(int), typeof(RatingBar), 0);

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
}

// 2. Android handler
public class RatingBarHandler : ViewHandler<RatingBar, Android.Widget.RatingBar>
{
    public static IPropertyMapper<RatingBar, RatingBarHandler> Mapper = new PropertyMapper<RatingBar, RatingBarHandler>(ViewMapper)
    {
        [nameof(RatingBar.Value)] = (h, v) => h.PlatformView.Rating = v.Value
    };

    public RatingBarHandler() : base(Mapper) { }

    protected override Android.Widget.RatingBar CreatePlatformView()
        => new Android.Widget.RatingBar(Context) { NumStars = 5, StepSize = 1 };
}

// 3. Registration
builder.ConfigureMauiHandlers(handlers =>
{
    handlers.AddHandler<RatingBar, RatingBarHandler>();
});
```

---

**Q25: What are `Behaviors` in MAUI and when should you use them over custom controls?**

**Answer:**

**Theory:** `Behaviors` are reusable **attached logic** that extends an existing control without subclassing. A `Behavior<T>` has `OnAttachedTo` (attach event handlers, set up state) and `OnDetachingFrom` (cleanup). Behaviors are ideal for: input validation (attach to `Entry`, change border color on invalid), numeric-only input (intercept `TextChanged`), masking (phone number formatting), auto-complete, and analytics tracking. Use Behaviors **instead** of custom controls when: you need to augment an existing control's behavior without changing its appearance. Use custom controls (with Handlers) when: you need a new visual element that doesn't exist in MAUI (e.g., a chart, a signature pad, a camera preview). The rule: Behavior = add behavior to existing control; Handler control = create new rendering from scratch.

**Code Example:**
```csharp
public class NumericEntryBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry entry)
    {
        entry.TextChanged += OnTextChanged;
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.TextChanged -= OnTextChanged;
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is Entry entry && !string.IsNullOrEmpty(e.NewTextValue))
        {
            entry.Text = new string(e.NewTextValue.Where(char.IsDigit).ToArray());
        }
    }
}

// XAML usage
<Entry Placeholder="Enter age">
    <Entry.Behaviors>
        <local:NumericEntryBehavior />
    </Entry.Behaviors>
</Entry>
```

---

**Q26: What are `Effects` and how are they different from Behaviors?**

**Answer:**

**Theory:** `Effects` are **platform-specific visual or behavioral modifications** that are lightweight and don't require handler customization. Unlike Behaviors (pure managed code), Effects have a platform-specific implementation. A MAUI `Effect` is a wrapper around platform effects: `PlatformEffect` on Android, `PlatformEffect` on iOS. The MAUI side sends routing info; the platform side applies native effect APIs (e.g., iOS `UIView.Layer.Shadow`, Android `View.SetElevation`). Effects are resolved by `Effect.Resolve($"{group}.{name}")`. Differences from Behaviors: (1) Effects can modify **native properties** (shadows, elevation, blur) that aren't exposed in MAUI. (2) Effects are resolved per-platform — you write platform code. (3) Effects are attached via `Effects` collection (`entry.Effects.Add(Effect.Resolve(...))`). Behaviors are better for cross-platform logic (validation, formatting). Effects are better for platform-specific visual tweaks (iOS shadow, Android ripple).

**Code Example:**
```csharp
// MAUI-side effect (routing)
public class ShadowEffect : RoutingEffect
{
    public ShadowEffect() : base("MyApp.ShadowEffect") { }
}

// Android-side implementation
public class ShadowPlatformEffect : PlatformEffect
{
    protected override void OnAttached()
    {
        Control.SetElevation(10);
        Control.OutlineProvider = ViewOutlineProvider.Bounds;
    }
    protected override void OnDetached() { }
}

// Usage
entry.Effects.Add(new ShadowEffect());
```

---

## 8. Graphics & Animations

**Q27: How does `GraphicsView` work with `IDrawable`?**

**Answer:**

**Theory:** `GraphicsView` is a high-performance drawing surface in MAUI that uses `Microsoft.Maui.Graphics` — a cross-platform 2D drawing API. You implement `IDrawable` and set it on the `GraphicsView.Drawable` property. The `Draw(ICanvas canvas, RectF dirtyRect)` method receives a canvas object with methods for: `DrawLine`, `DrawRectangle`, `DrawRoundedRectangle`, `DrawCircle`, `DrawEllipse`, `DrawArc`, `DrawPath`, `DrawString`, and `DrawImage`. MAUI graphics uses a **immediate mode** API — you issue draw commands each frame. For animations, call `Invalidate()` on the `GraphicsView` to trigger a redraw. `GraphicsView` is hardware-accelerated on all platforms (uses `SKCanvas` on Android via SkiaSharp, `CoreGraphics` on iOS, `Win2D` on Windows). Use `GraphicsView` for: custom charts, gauges, signatures, real-time waveforms, and any custom drawing that doesn't need full GPU acceleration.

**Code Example:**
```csharp
public class BatteryGaugeDrawable : IDrawable
{
    public int Level { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var center = new PointF(dirtyRect.Width / 2, dirtyRect.Height / 2);
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2 - 10;

        // Background arc
        canvas.StrokeColor = Colors.LightGray;
        canvas.StrokeSize = 20;
        canvas.DrawArc(center.X - radius, center.Y - radius,
                       center.X + radius, center.Y + radius,
                       135, 270, false, false);

        // Fill arc based on battery level
        canvas.StrokeColor = Level switch
        {
            <= 20 => Colors.Red,
            <= 50 => Colors.Orange,
            _ => Colors.Green
        };
        var sweepAngle = 270.0 * Level / 100;
        canvas.DrawArc(center.X - radius, center.Y - radius,
                       center.X + radius, center.Y + radius,
                       135, (float)sweepAngle, false, false);

        // Center text
        canvas.FontColor = Colors.Black;
        canvas.FontSize = 24;
        canvas.DrawString($"{Level}%", center.X, center.Y,
                          HorizontalAlignment.Center);
    }
}

// Usage in XAML
<GraphicsView x:Name="BatteryGauge" HeightRequest="200" WidthRequest="200" />

// In code-behind
var drawable = new BatteryGaugeDrawable { Level = 75 };
BatteryGauge.Drawable = drawable;
```

---

**Q28: How do you animate elements in MAUI?**

**Answer:**

**Theory:** MAUI provides **implicit animations** (via extension methods) and **explicit animations** (via `Animation` class). Implicit methods: `FadeTo(opacity, length, easing)`, `TranslateTo(x, y, length, easing)`, `ScaleTo(factor, length, easing)`, `RotateTo(degrees, length, easing)`, `RotateXTo`/`RotateYTo`, and `LayoutTo(rect, length, easing)`. Each returns a `Task<bool>` that completes when the animation finishes (true) or is cancelled (false). For complex multi-property animations, use the `Animation` class with children, or `Task.WhenAll` for parallel animations. **Easing functions** control acceleration: `Easing.Linear`, `Easing.SpringIn`, `Easing.SpringOut`, `Easing.CubicIn`, `Easing.CubicOut`, `Easing.CubicInOut`, `Easing.SinIn`, `Easing.SinOut`, `Easing.BounceIn`, `Easing.BounceOut`. For 60fps animations with custom drawing, use `GraphicsView` with `Invalidate()` in a `Task.Delay` loop. **Key performance rule:** animate only **layout-independent properties** — `Opacity`, `Scale`, `Rotation`, `TranslationX/Y`. Animating `WidthRequest`/`HeightRequest` triggers re-layout which is expensive.

**Code Example:**
```csharp
// Fade in with bounce
await card.FadeTo(1, 500, Easing.BounceOut);

// Parallel scale + rotation
await Task.WhenAll(
    icon.ScaleTo(1.2, 300, Easing.SpringIn),
    icon.RotateTo(360, 500, Easing.CubicOut)
);

// Sequential: fade out, then translate
await card.FadeTo(0, 200);
await card.TranslateTo(0, -50, 300);

// Custom animation loop with GraphicsView
while (!_cancelled)
{
    angle += 5;
    graphicsView.Invalidate();
    await Task.Delay(16); // ~60fps
}
```

---

**Q29: How does `Easing` work in MAUI animations?**

**Answer:**

**Theory:** `Easing` controls the rate of change of an animation over time. Mathematically, it's a function `f(t)` where `t` goes from 0 to 1, and `f(t)` maps to the animation progress. Linear easing: `f(t) = t` — constant speed. CubicIn: `f(t) = t³` — slow start, fast end. CubicOut: `f(t) = (t-1)³ + 1` — fast start, slow end. BounceIn: simulates a bouncing ball landing — overshoots and rebounds at the start. SpringIn: overshoots the target and settles back. You can define custom easings via `Easing.CubicIn` or a custom `Func<double, double>`. Custom easing: `new Easing(t => t * t * (3 - 2 * t))` (smoothstep). Easing affects **user perception** of animations — `CubicInOut` (smooth start and end) feels most natural for UI transitions. For feedback animations (button press), use `SpringOut` for a playful feel. For screen transitions, use `SinInOut` for smooth, professional motion.

**Code Example:**
```csharp
// Built-in easings
await view.FadeTo(1, 500, Easing.CubicInOut);
await view.ScaleTo(1.2, 300, Easing.SpringOut);
await view.TranslateTo(0, 100, 400, Easing.BounceOut);

// Custom easing function (smoothstep)
var smoothStep = new Easing(t => t * t * (3 - 2 * t));
await view.RotateTo(180, 1000, smoothStep);
```

---

## 9. Platform-Specific Code

**Q30: How does MAUI handle platform-specific code?**

**Answer:**

**Theory:** MAUI provides four mechanisms for platform-specific code. (1) **Partial classes + platform folders** — place files in `Platforms/Android/`, `Platforms/iOS/`, `Platforms/Windows/`, `Platforms/MacCatalyst/`. MSBuild compiles only the files matching the current target framework. (2) **`#if` conditional compilation** — `#if ANDROID`, `#if IOS`, `#if MACCATALYST`, `#if WINDOWS`. Use for small platform-specific blocks within shared files. (3) **`OnPlatform` / `OnIdiom`** — XAML markup extensions for per-platform styling. `OnPlatform` selects values based on platform; `OnIdiom` selects based on device form factor (Phone, Tablet, Desktop, TV, Watch). (4) **Platform handlers** — for custom controls, per-platform handler implementations provide platform-specific rendering. For accessing platform APIs (e.g., `Android.Content.Context`), MAUI provides `Platform.CurrentActivity`, `Platform.AppContext`, and `Microsoft.Maui.ApplicationModel.Platform`. Best practice: prefer partial classes in platform folders over `#if` — they're cleaner and don't clutter shared code.

**Code Example:**
```xml
<!-- OnPlatform/OnIdiom in XAML -->
<Label Text="Welcome">
    <Label.FontSize>
        <OnPlatform Default="16">
            <On Platform="iOS" Value="18" />
            <On Platform="Android" Value="14" />
        </OnPlatform>
    </Label.FontSize>
</Label>

<Grid ColumnDefinitions="*">
    <OnIdiom Phone="1" Tablet="2" Desktop="3">
        <ColumnDefinition Width="*" />
    </OnIdiom>
</Grid>
```

```csharp
// #if conditional
#if ANDROID
using Android.Net;
var cm = (ConnectivityManager)Platform.AppContext.GetSystemService(Context.ConnectivityService);
var networkInfo = cm.ActiveNetworkInfo;
#elif IOS
using CoreTelephony;
var info = new CTTelephonyNetworkInfo();
#endif
```

---

**Q31: How do you access platform-specific services like GPS or Camera in MAUI?**

**Answer:**

**Theory:** MAUI provides a **unified API** for common device capabilities via `Microsoft.Maui.Essentials` (now built into MAUI itself). Key APIs: `Geolocation.Default.GetLocationAsync()` for GPS, `MediaPicker.Default.PickPhotoAsync()` for camera/gallery, `Accelerometer.Default.ReadingChanged` for motion, `Battery.Default.ChargeLevel` for battery state, `Connectivity.Current.NetworkAccess` for network status, `Vibration.Default.Vibrate()` for haptics. These APIs handle **permission requests** internally on Android 13+ (runtime permissions). For permissions, use `Permission.CheckStatusAsync<TPermission>()` and `Permission.RequestAsync<TPermission>()`. If you need platform APIs not covered by Essentials, use the platform-specific mechanisms above. Essentials APIs are designed to throw `FeatureNotSupportedException`, `PermissionException`, or `FeatureNotEnabledException` — always handle these in try/catch.

**Code Example:**
```csharp
// GPS location
try
{
    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest
    {
        DesiredAccuracy = GeolocationAccuracy.High,
        Timeout = TimeSpan.FromSeconds(10)
    });
    Console.WriteLine($"Lat: {location.Latitude}, Lng: {location.Longitude}");
}
catch (PermissionException) { await ShowAlertAsync("Permission denied"); }
catch (FeatureNotSupportedException) { await ShowAlertAsync("GPS not available"); }

// Camera/Gallery
var photo = await MediaPicker.Default.CapturePhotoAsync();
if (photo is not null)
{
    var stream = await photo.OpenReadAsync();
    // Use stream
}

// Check/request permissions
var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
if (status != PermissionStatus.Granted)
{
    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
}
```

---

**Q32: What are target frameworks in a MAUI `.csproj` and what does each mean?**

**Answer:**

**Theory:** A MAUI project uses **multi-targeting** — it compiles the same code for multiple platforms simultaneously. The `<TargetFrameworks>` property lists the platforms: `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows10.0.19041.0`. Each target framework combines the BCL (Base Class Library) with platform-specific APIs. `net10.0-android` includes `Mono.Android` (full Android API). `net10.0-ios` includes `Xamarin.iOS` (iOS APIs). `net10.0-windows10.0.19041.0` includes WinUI 3 APIs. The minimum Windows version (`19041`) means Windows 10 2004 or later. You can conditionally include platform-specific NuGet packages using `<ItemGroup Condition="$(TargetFramework.Contains('android'))">`. MSBuild uses the target framework to choose the right SDK, runtime, and platform assemblies. Debug builds typically target only the current platform (`net10.0-windows10.0.19041.0` on Windows); Release builds target all.

**Code Example:**
```xml
<PropertyGroup>
    <!-- Multi-targeting all supported platforms -->
    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0-windows10.0.19041.0</TargetFrameworks>
    <SingleProject>true</SingleProject>
    <UseMaui>true</UseMaui>
    <OutputType>Exe</OutputType>
</PropertyGroup>

<!-- Platform-specific packages -->
<ItemGroup Condition="$(TargetFramework.Contains('android'))">
    <PackageReference Include="Xamarin.AndroidX.Core" Version="1.15.0" />
</ItemGroup>
<ItemGroup Condition="$(TargetFramework.Contains('windows'))">
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.0" />
</ItemGroup>
```

---

## 10. Dependency Injection in MAUI

**Q33: How does DI work in MAUI and what are the lifetime scopes?**

**Answer:**

**Theory:** MAUI uses the **generic host pattern** — `MauiApp.CreateBuilder()` creates a `HostApplicationBuilder` with a `ServiceCollection` DI container (same as ASP.NET Core). Services are registered in `MauiProgram.cs` with three lifetimes: (1) **Singleton** — one instance for the app's lifetime. Created on first resolve, disposed when the app shuts down. Use for: `HttpClient`, `IApiService`, `IAuthService`, database connections. (2) **Transient** — new instance every time it's resolved. Created each time a ViewModel or page is constructed. Use for: ViewModels, pages, short-lived services. (3) **Scoped** — behaves like Singleton in MAUI (there's no request scope like ASP.NET Core). Best to avoid Scoped in MAUI and use Singleton or Transient explicitly. ViewModels and Pages are typically registered as Transient — each navigation creates a fresh instance. Shell's cached pages use the same ViewModel instance across appearances. `AddSingleton` + `AddTransient` covers 99% of MAUI scenarios.

**Code Example:**
```csharp
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder.UseMauiApp<App>()
           .UseMauiCommunityToolkit()
           .ConfigureFonts(fonts =>
           {
               fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
               fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
           });

    // Singletons — one instance for app lifetime
    builder.Services.AddSingleton<IApiService, ApiService>();
    builder.Services.AddSingleton<IAuthService, AuthService>();
    builder.Services.AddSingleton<HttpClient>(sp =>
    {
        var client = new HttpClient { BaseAddress = new Uri(Constants.ApiBaseUrl) };
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    });

    // Transients — new instance per navigation
    builder.Services.AddTransient<DashboardViewModel>();
    builder.Services.AddTransient<DashboardPage>();
    builder.Services.AddTransient<LoginViewModel>();
    builder.Services.AddTransient<LoginPage>();

    return builder.Build();
}
```

---

**Q34: How does constructor injection work with Shell pages?**

**Answer:**

**Theory:** Shell creates pages using `IMauiHandlersFactory` internally, which resolves pages from the DI container if they're registered. When Shell navigates to a route, it calls `IServiceProvider.GetService(pageType)`. If the page is registered, DI constructs it by resolving all constructor parameters. The page's constructor receives its ViewModel and any other services. The ViewModel's constructor, in turn, receives its dependencies. Shell **does not** automatically inject into ViewModel constructors — the page must pass DI-resolved services to the ViewModel. The pattern: page constructor takes ViewModel, sets `BindingContext = viewModel`. ViewModel constructor takes services. To pass query parameters, the ViewModel implements `IQueryAttributable` or uses `[QueryProperty]` attribute. Key insight: if a page is NOT registered in DI, Shell creates it using the **default constructor** — no injections happen. Always register pages and ViewModels as Transient.

**Code Example:**
```csharp
// Page — DI resolves the ViewModel
public partial class DashboardPage : ContentPage
{
    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}

// ViewModel — DI resolves its dependencies
public partial class DashboardViewModel : BaseViewModel
{
    private readonly IApiService _apiService;

    public DashboardViewModel(IApiService apiService, INavigationService navService)
        : base(navService)
    {
        _apiService = apiService;
    }
}

// Registration
builder.Services.AddTransient<DashboardViewModel>();
builder.Services.AddTransient<DashboardPage>();
```

---

**Q35: What is the Composition Root in MAUI?**

**Answer:**

**Theory:** The **Composition Root** is the single place in the application where the dependency graph is assembled. In MAUI, it's `MauiProgram.cs` — the `CreateMauiApp` method. All service registrations, handler configurations, font registrations, and middleware setup happen here. The Composition Root principle states: the entire object graph should be composed (wired together) in one place, as close to the application's entry point as possible. This means: (1) no `new ViewModel()` anywhere except the Composition Root, (2) no service locator pattern (`App.ServiceProvider.GetService<T>()`), (3) no `Application.Current.FindByName` for resolving services. Everything flows through constructor injection. The Composition Root is also where you'd replace real services with test doubles for integration testing. Violating the Composition Root principle leads to scattered `new` calls, making the app hard to test and refactor.

**Code Example:**
```csharp
// ✅ Correct — Composition Root in MauiProgram.cs
builder.Services.AddSingleton<IApiService, ApiService>();
builder.Services.AddTransient<DashboardViewModel>();
builder.Services.AddTransient<DashboardPage>();

// ❌ Wrong — service locator pattern
var vm = App.ServiceProvider.GetService<DashboardViewModel>();

// ❌ Wrong — scattered new calls
var vm = new DashboardViewModel(new ApiService(new HttpClient()));
```

---

## 11. Storage & Data Persistence

**Q36: How does `SecureStorage` work across platforms in MAUI?**

**Answer:**

**Theory:** `SecureStorage` provides encrypted-at-rest storage using each platform's native security infrastructure. **Android** (API 23+): uses `EncryptedSharedPreferences` with AES-256 encryption. The key is stored in the **Android KeyStore** (hardware-backed if available). On older API < 23, falls back to `AndroidKeyStore` (less secure). **iOS**: uses the **KeyChain** with `kSecAttrAccessibleWhenUnlockedThisDeviceOnly` — data is encrypted with the device's hardware key. If the device is locked, the data is inaccessible. **Windows**: uses **DPAPI** (Data Protection API) — `CryptProtectData` with the user's login credentials. Data is tied to the user account. **Mac Catalyst**: uses the **macOS Keychain** (same as iOS). All platforms: data survives app uninstall on Windows (DPAPI is machine-level) but NOT on Android/iOS (KeyStore/Keychain is per-app). `SecureStorage` is for **small secrets** (tokens, keys, passwords). Max recommended size: ~1KB. For larger encrypted data, use `FileSystem.AppDataDirectory` with manual encryption (`System.Security.Cryptography.ProtectedData` on Windows, or platform-specific AES).

**Code Example:**
```csharp
// Store a secret
await SecureStorage.Default.SetAsync("auth_token", jwtToken);

// Retrieve
var token = await SecureStorage.Default.GetAsync("auth_token");

// Remove
SecureStorage.Default.Remove("auth_token");

// Remove all
SecureStorage.Default.RemoveAll();

// Platform-specific behavior
// Android: encrypts with KeyStore-backed AES key
// iOS: encrypts with KeyChain (device-locked)
// Windows: encrypts with DPAPI (user-specific)
```

---

**Q37: How do you use `Preferences` in MAUI and when should you use it vs `SecureStorage`?**

**Answer:**

**Theory:** `Preferences` stores data as **plain text key-value pairs** in platform-specific storage: `SharedPreferences` (Android XML file), `NSUserDefaults` (iOS plist), `ApplicationDataContainer` (Windows `LocalSettings`). Unlike `SecureStorage`, data is NOT encrypted — it's visible in file system backups and to other apps on rooted/jailbroken devices. Use `Preferences` for: user preferences (theme, language), feature flags, last-seen timestamps, onboarding completion flags — anything that's not sensitive. Use `SecureStorage` for: JWT tokens, API keys, passwords, biometric data — anything that would cause harm if exposed. `Preferences` supports typed getters: `Get<string>`, `Get<int>`, `Get<bool>`, `Get<double>`, `Get<long>` with default values. It also supports `Set<T>` for the same types. Both `Preferences` and `SecureStorage` support a shared name (`sharedName`) for app groups/widgets.

**Code Example:**
```csharp
// Preferences — NOT encrypted, for non-sensitive data
Preferences.Default.Set("theme", "dark");
var theme = Preferences.Default.Get("theme", "light");
Preferences.Default.Set("onboarding_complete", true);
var isComplete = Preferences.Default.Get("onboarding_complete", false);

// SecureStorage — encrypted, for sensitive data
await SecureStorage.Default.SetAsync("auth_token", token);
```

---

**Q38: How do you access the file system in MAUI?**

**Answer:**

**Theory:** MAUI provides `FileSystem.Current` with three directories: (1) `AppDataDirectory` — the app's private data folder. Use for: databases, user-generated content, caches that should persist across app updates. Path example: `C:\Users\<user>\AppData\Local\Packages\<package>\LocalState` on Windows, `/data/data/<package>/files` on Android, `Application Support` on iOS. (2) `CacheDirectory` — temporary data that the OS can purge. Use for: downloaded images, temporary files, response caches. Path example: `C:\Users\<user>\AppData\Local\Packages\<package>\LocalCache` on Windows. (3) `AppPackageRoot` — the read-only app bundle directory. Use for: shipped assets, bundled SQLite databases, config files. Each returns a `string` path. Create files with standard `File.WriteAllTextAsync(path, content)` and `File.ReadAllTextAsync(path)`. For structured data, use `sqlite-net-pcl` with a database file in `AppDataDirectory`. For serialization, `System.Text.Json` works directly with file streams.

**Code Example:**
```csharp
// Get directories
var appData = FileSystem.Current.AppDataDirectory;      // DB, user files
var cache = FileSystem.Current.CacheDirectory;          // Temp files
var appPackage = FileSystem.Current.AppPackageRoot;      // Bundled files

// Write/read files
var dbPath = Path.Combine(appData, "evswap.db");
await File.WriteAllTextAsync(dbPath, jsonData);
var data = await File.ReadAllTextAsync(dbPath);

// Copy bundled file to app data
using var stream = await FileSystem.Current.OpenAppPackageFileAsync("seed.json");
using var fileStream = File.Create(Path.Combine(appData, "seed.json"));
await stream.CopyToAsync(fileStream);
```

---

## 12. MAUI Concurrency & Threading

**Q39: What is `IDispatcher` and how is it used in MAUI?**

**Answer:**

**Theory:** `IDispatcher` is the MAUI abstraction over the platform's main thread dispatcher. On MAUI, **all UI updates must happen on the main thread**. `IDispatcher` provides `Dispatch(Action)` to execute code on the main thread, and a `DispatchAsync(Action)` for async scenarios. It replaces the older `Device.BeginInvokeOnMainThread` from Xamarin.Forms. Each `Dispatcher` is tied to a specific element's synchronization context. `Dispatcher.GetForCurrentThread()` gets the dispatcher for the current thread. `MainThread.IsMainThread` checks if you're on the UI thread. In ViewModels, you typically don't need to dispatch manually because data binding automatically happens on the main thread (bindings marshal property changes to the UI thread). However, when setting `IsBusy` or `Title` from a background task, use `MainThread.BeginInvokeOnMainThread(() => IsBusy = false)` to ensure the property change notification fires on the right thread. The `[ObservableProperty]` source generator handles this if the property change affects UI-bound bindings.

**Code Example:**
```csharp
// Check if on main thread
if (!MainThread.IsMainThread)
{
    MainThread.BeginInvokeOnMainThread(() =>
    {
        IsBusy = false;
        Title = "Data loaded";
    });
}
else
{
    IsBusy = false;
    Title = "Data loaded";
}

// Using IDispatcher from a View
var dispatcher = Dispatcher.GetForCurrentThread() ?? Application.Current.Dispatcher;
dispatcher.Dispatch(() => myLabel.Text = "Updated");

// Async dispatch
await dispatcher.DispatchAsync(() => myLabel.Text = "Updated");
```

---

**Q40: What is `ConfigureAwait(false)` and should you use it in MAUI apps?**

**Answer:**

**Theory:** `ConfigureAwait(false)` tells the `Task` not to marshal the continuation back to the original `SynchronizationContext`. In MAUI apps, the original context is the **main UI thread**. When you `await` without `ConfigureAwait(false)`, the continuation runs on the main thread (via `SynchronizationContext.Post`). With `ConfigureAwait(false)`, the continuation runs on any available `ThreadPool` thread. **In MAUI libraries and ViewModel code**: use `ConfigureAwait(false)` for **all** `await` calls that don't touch UI elements — it prevents unnecessary thread switching and reduces main thread pressure. For example, `await httpClient.GetAsync(url).ConfigureAwait(false)` — the HTTP response doesn't need the main thread. Then marshal only the final result back: `Title = result.Title` (the binding engine will marshal this). **In MAUI event handlers and page code-behind**: do NOT use `ConfigureAwait(false)` if you access `this` or any UI element after the await — you must be on the main thread. The rule: `ConfigureAwait(false)` in ViewModels, no `ConfigureAwait(false)` in code-behind that accesses UI.

**Code Example:**
```csharp
// ✅ ViewModel — safe to use ConfigureAwait(false)
public async Task LoadDataAsync()
{
    var json = await _httpClient.GetStringAsync(url).ConfigureAwait(false);
    var data = JsonSerializer.Deserialize<DashboardData>(json);
    // Back on main thread via binding
    Title = data.Title; // ObservableProperty marshals automatically
}

// ❌ Code-behind — do NOT use ConfigureAwait(false)
private async void OnButtonClicked(object sender, EventArgs e)
{
    var result = await _service.DoSomethingAsync(); // Don't add ConfigureAwait(false)
    myLabel.Text = result; // Must be on main thread
}
```

---

**Q41: How do you handle fire-and-forget async tasks in MAUI without crashing?**

**Answer:**

**Theory:** Fire-and-forget (`async void` or unobserved `Task`) is dangerous in MAUI because unhandled exceptions throw in the finalizer thread and crash the app. In ViewModels, `async void` event handlers (like `OnAppearing`) should be minimized. For fire-and-forget calls, use the `FireAndForget` extension pattern: wrap the `Task` in a try/catch, log the exception, and optionally show a user-facing error. Never let an async void propagate unhandled. For background operations that must complete even if the page disappears, use `Task.Run` with `CancellationToken` and handle completion carefully. MAUI has an `UnobservedTaskException` event on `TaskScheduler` that catches unobserved exceptions — log them there as a safety net. For timers, use `PeriodicTimer` (new in .NET 6+) which supports async properly without the `async void` pattern.

**Code Example:**
```csharp
// Safe fire-and-forget extension
public static void FireAndForget(this Task task, Action<Exception> onError = null)
{
    task.ContinueWith(t =>
    {
        if (t.Exception is not null)
        {
            onError?.Invoke(t.Exception.InnerException ?? t.Exception);
            // Log to crash reporting
            Logger.LogError(t.Exception, "Unhandled fire-and-forget exception");
        }
    }, TaskContinuationOptions.OnlyOnFaulted);
}

// Usage
_ = LoadDataAsync().FireAndForget(ex => ShowAlertAsync("Error", ex.Message));

// Safe async void — only for event handlers
protected override async void OnAppearing()
{
    base.OnAppearing();
    try
    {
        await LoadDataAsync();
    }
    catch (Exception ex)
    {
        await ShowAlertAsync("Error", ex.Message);
    }
}
```

---

## 13. Testing MAUI Applications

**Q42: How do you unit test MAUI ViewModels?**

**Answer:**

**Theory:** MAUI ViewModels with CommunityToolkit.Mvvm are **testable** because they use dependency injection and `[ObservableProperty]`/`[RelayCommand]` source generators that produce standard .NET types. To test: (1) Create a test project (xUnit, NUnit, or MSTest). (2) Mock all dependencies using Moq, NSubstitute, or FakeItEasy. (3) Create the ViewModel with mocked dependencies. (4) Call commands directly as methods (e.g., `viewModel.LoginCommand.Execute(null)` maps to `viewModel.LoginAsync()`). (5) Assert property changes, navigation calls, and service interactions. Do NOT test UI-specific behavior (layout, rendering) in unit tests — those belong in UI tests. Key patterns: test `CanExecute` returns false when form is invalid, test `IsBusy` is true during async operations, test that errors set `ErrorMessage` instead of crashing. For ViewModels with `BaseViewModel`, mock `INavigationService` and `IConnectivityService` to avoid real navigation.

**Code Example:**
```csharp
[TestClass]
public class LoginViewModelTests
{
    private Mock<IAuthService> _authMock;
    private LoginViewModel _vm;

    [TestInitialize]
    public void Setup()
    {
        _authMock = new Mock<IAuthService>();
        var navMock = new Mock<INavigationService>();
        var connMock = new Mock<IConnectivityService>();
        connMock.Setup(c => c.IsConnected).Returns(true);

        _vm = new LoginViewModel(_authMock.Object, navMock.Object, connMock.Object);
    }

    [TestMethod]
    public async Task Login_Success_NavigatesToDashboard()
    {
        _authMock.Setup(a => a.LoginAsync("admin", "pass"))
                 .ReturnsAsync(new AuthResponse { Token = "abc", Success = true });

        _vm.Username = "admin";
        _vm.Password = "pass";
        await _vm.LoginCommand.ExecuteAsync(null);

        Assert.IsFalse(_vm.IsBusy);
        _authMock.Verify(a => a.LoginAsync("admin", "pass"), Times.Once);
    }

    [TestMethod]
    public void Login_EmptyCredentials_CanExecuteIsFalse()
    {
        _vm.Username = "";
        _vm.Password = "";

        Assert.IsFalse(_vm.LoginCommand.CanExecute(null));
    }
}
```

---

**Q43: How do you test MAUI Shell navigation?**

**Answer:**

**Theory:** Shell navigation is difficult to unit test because `Shell.Current` is a platform-bound singleton. Instead, **abstract navigation behind `INavigationService`** that wraps Shell calls. Then mock the service in ViewModel tests. The production `NavigationService` calls `Shell.Current.GoToAsync`, which you verify by checking that the mock received the correct route. For integration tests, use `Microsoft.Maui.Testing` with `CreateMauiApp()` to spin up the full DI container and test Shell routing by directly resolving and navigating pages. For UI tests, use `Appium` or `Xamarin.UITest` (now `Microsoft.Maui.UITesting`) to automate the app and verify navigation flows end-to-end. The key: never call `Shell.Current` directly from ViewModels — always go through `INavigationService`. This single abstraction makes the entire navigation system testable.

**Code Example:**
```csharp
// Production implementation
public class NavigationService : INavigationService
{
    public async Task NavigateToAsync(string route, IDictionary<string, object> parameters = null)
    {
        if (parameters is not null)
            await Shell.Current.GoToAsync(route, parameters);
        else
            await Shell.Current.GoToAsync(route);
    }
}

// Test
[TestMethod]
public async Task Login_Success_NavigatesToDashboard()
{
    var navMock = new Mock<INavigationService>();
    // ... setup ViewModel with navMock ...
    await _vm.LoginCommand.ExecuteAsync(null);

    navMock.Verify(n => n.NavigateToAsync("//dashboard", It.IsAny<IDictionary<string, object>>()));
}
```

---

**Q44: What UI testing tools are available for MAUI?**

**Answer:**

**Theory:** MAUI supports multiple UI testing approaches: (1) **Xamarin.UITest** (now Test Cloud) — uses REPL for test scripting, supports Android and iOS. Requires Xamarin Test Cloud agent NuGet. Being phased out in favor of newer tools. (2) **Appium** — cross-platform UI automation using WebDriver protocol. MAUI apps appear as native apps to Appium. Use `dotnet test` with Appium drivers (`WinAppDriver` for Windows, `XCTest` for iOS, `UiAutomator2` for Android). (3) **Microsoft.Maui.Testing** — experimental Microsoft-provided testing with `Harness` for unit-level Shell/page resolution tests. (4) **`Microsoft.Playwright.Maui`** — for Blazor Hybrid MAUI apps, Playwright automates the webview. (5) **Manual screenshot comparison** — use `Microsoft.VisualStudio.TestTools.UnitTesting` and `RenderTargetBitmap` on Windows for pixel-level verification. Most mature option: **Appium** with the `DotNetSeleniumExtras.PageObjects` pattern for Page Object Model. Appium tests are slow but catch real layout and rendering issues.

---

## 14. Deployment & CI/CD

**Q45: How do you deploy a MAUI app for each platform?**

**Answer:**

**Theory:** Each platform has its own deployment pipeline: **Windows** — create an MSIX package (`dotnet publish -f net10.0-windows10.0.19041.0 -c Release /p:WindowsPackageType=MSIX`). Sign with a certificate, then distribute via the Store or sideload. **Android** — create an AAB (Android App Bundle) via `dotnet publish -f net10.0-android -c Release`. Sign with a keystore (`.jks`). Upload to Google Play Console. For sideloading, also generate APK with `-p:AndroidPackageFormat=apk`. **iOS** — archive via `dotnet publish -f net10.0-ios -c Release` on macOS. The output is an `.ipa` file. Submit via App Store Connect using `Transporter` or Xcode. **Mac Catalyst** — produce a `.app` bundle via `dotnet publish`. Notarize with Apple and distribute via App Store or Developer ID. **Common step**: code signing. Each platform requires a certificate: Android uses a JKS keystore, iOS/macOS uses Apple Developer certificates, Windows uses a PFX or Azure Key Vault. CI/CD with GitHub Actions or Azure DevOps can automate all four platform builds using matrix strategies.

**Code Example:**
```bash
# Windows — MSIX package
dotnet publish -f net10.0-windows10.0.19041.0 -c Release `
  /p:WindowsPackageType=MSIX `
  /p:AppxPackageSigningEnabled=true `
  /p:PackageCertificateThumbprint="thumbprint"

# Android — AAB (Google Play)
dotnet publish -f net10.0-android -c Release `
  /p:AndroidKeyStore=true `
  /p:AndroidSigningKeyStore=evswap.keystore `
  /p:AndroidSigningKeyAlias=evswap `
  /p:AndroidSigningKeyPass=password `
  /p:AndroidSigningStorePass=password

# iOS — requires macOS
dotnet publish -f net10.0-ios -c Release `
  /p:ArchiveOnBuild=true `
  /p:CodesignProvision="EV Swap Distribution" `
  /p:CodesignKey="Apple Distribution: Company Name"
```

---

**Q46: What is trimming and how does it affect MAUI apps?**

**Answer:**

**Theory:** **Trimming** removes unused IL code from assemblies to reduce binary size. In MAUI, enable with `<PublishTrimmed>true</PublishTrimmed>` in the `.csproj`. The linker analyzes your code's reachable types and methods, then removes everything else. Trimming modes: (1) **partial** (`<TrimMode>partial</TrimMode>`) — trims only the MAUI framework assemblies, not your app code. Safer, less size reduction. (2) **full** (`<TrimMode>full</TrimMode>`) — trims everything including your app. Maximum size reduction but can break reflection-based code. (3) **link** — legacy Xamarin linker mode, not recommended. **Risks**: trimming can break code that uses reflection (`JsonSerializer`, `Activator.CreateInstance`, `Assembly.GetTypes`). Fix by adding `[DynamicallyAccessedMembers]` attributes or using a `ILLink.Substitutions.xml` file. For CommunityToolkit.Mvvm, ensure the linker doesn't strip `[ObservableProperty]` generated code by setting `<TrimmerSingleWarn>false</TrimmerSingleWarn>` and testing thoroughly. Typical size reduction: 30-50%.

**Code Example:**
```xml
<PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
    <TrimMode>partial</TrimMode>
    <TrimmerSingleWarn>false</TrimmerSingleWarn>
</PropertyGroup>

<!-- Preserve reflection-needed types -->
<ItemGroup>
    <TrimmerRootAssembly Include="CommunityToolkit.Mvvm" />
    <TrimmerRootAssembly Include="System.Text.Json" />
</ItemGroup>
```

---

## 15. Advanced MAUI Topics

**Q47: What is the `Handler` lifecycle and when is it created/disconnected?**

**Answer:**

**Theory:** A MAUI `Handler` has a well-defined lifecycle tied to the `VisualElement`. (1) **Creation** — when a `View` is added to the visual tree (set as `Content` or added to a `Layout`'s Children collection), MAUI calls `IViewHandler.SetParent` and creates the handler. The handler's `CreatePlatformView()` is called. (2) **Connected** — the handler is set on the view's `Handler` property. Property values are synchronized via `PropertyMapper`. (3) **Update** — whenever a bindable property changes, `PropertyMapper` is invoked to update the platform view. (4) **Disconnected** — when the view is removed from the visual tree, `Handler.Disconnect()` is called. This removes native view references, unsubscribes from native events, and calls `DisconnectHandler` if implemented. (5) **Recycle** — MAUI can reuse handlers if the same type is added again (via `HandlerRecycling`). Most apps don't need to handle this lifecycle explicitly, but it's important for custom controls to clean up native resources in `DisconnectHandler`. Failure to disconnect handler event subscriptions is a common memory leak source.

**Code Example:**
```csharp
public class MyCustomHandler : ViewHandler<MyView, PlatformView>
{
    protected override PlatformView CreatePlatformView() => new PlatformView();

    protected override void ConnectHandler(PlatformView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.SomeEvent += OnPlatformEvent;
    }

    protected override void DisconnectHandler(PlatformView platformView)
    {
        platformView.SomeEvent -= OnPlatformEvent;
        base.DisconnectHandler(platformView);
    }
}
```

---

**Q48: How do you implement background tasks in MAUI?**

**Answer:**

**Theory:** MAUI supports background tasks through multiple mechanisms depending on the platform: (1) **`BackgroundService`** (cross-platform) — .NET's hosted service pattern. Create a class extending `BackgroundService` with `ExecuteAsync(CancellationToken)`. Register with `builder.Services.AddHostedService<MyService>()`. Works while the app is in the foreground. (2) **Platform-specific background modes** — iOS: `beginBackgroundTask` for short tasks (~30s), `BGTaskScheduler` for longer tasks. Android: `WorkManager` (Java) or `AndroidX.Work.Runtime` for reliable background work. Windows: `BackgroundTaskBuilder` with triggers. (3) **PeriodicTimer** (cross-platform) — for recurring foreground tasks (e.g., refreshing data every 30s). `await new PeriodicTimer(TimeSpan.FromSeconds(30)).WaitForNextTickAsync(ct)`. (4) **Push notifications** — for server-triggered background work. (5) **SignalR persistent connection** — stays alive as long as the app is active. For truly cross-platform background work, abstract platform-specific implementations behind `IBackgroundTaskService` and register per-platform handlers. Most MAUI apps use `BackgroundService` for foreground background work and push notifications for app-closed scenarios.

**Code Example:**
```csharp
// Cross-platform foreground background service
public class BatteryRefreshService : BackgroundService
{
    private readonly IApiService _api;

    public BatteryRefreshService(IApiService api) => _api = api;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var status = await _api.GetBatteryStatusAsync();
            WeakReferenceMessenger.Default.Send(new BatteryStatusMessage(status));
        }
    }
}

// Register
builder.Services.AddHostedService<BatteryRefreshService>();
```

---

**Q49: How does `MauiProgram.cs` handle app lifecycle events?**

**Answer:**

**Theory:** The MAUI app lifecycle is managed through `App.xaml.cs` which extends `Application`. Lifecycle events: (1) **`OnStart`** — called when the app launches. Initialize services, check auth token, set the main page. (2) **`OnSleep`** — called when the app goes to the background. Save state, pause active operations, release resources. (3) **`OnResume`** — called when the app returns to the foreground. Refresh data, check token expiry, resume operations. (4) **`OnAppLinkRequestReceived`** — for deep linking from URIs. Additionally, each `Page` has `OnAppearing` and `OnDisappearing`. For finer-grained control, MAUI provides `IApplication` and platform-specific lifecycle hooks. On Windows, you can access `Microsoft.UI.Xaml.Application.Current` for `Suspending`/`Resuming` events. The `Application` class also provides `RequestedThemeChanged` for dark/light mode changes. Best practice: keep `App.xaml.cs` lean — delegate lifecycle logic to services (e.g., `AuthService.CheckSessionOnResume()`).

---

**Q50: How do you implement custom fonts in MAUI?**

**Answer:**

**Theory:** Custom fonts are registered in `MauiProgram.cs` via `ConfigureFonts`. Steps: (1) Add font files (`.ttf` or `.otf`) to `Resources/Fonts/`. Set Build Action to `MauiFont`. (2) Register with `fonts.AddFont(filename.ttf, "FontAlias")`. (3) Use the alias in XAML: `FontFamily="FontAlias"` or `FontFamily="{x:Static fonts:Fonts.Bold}"`. MAUI supports font weights via `FontAttributes="Bold"` if the font family has bold variants. For icon fonts (FontAwesome, Material Icons), add the `.ttf` and use the Unicode character: `Text="&#xf007;"`. Font fallback: MAUI's native text platforms handle missing glyphs gracefully. On Android, font files go to `Resources/Fonts/` and are compiled into the asset directory. On iOS, they're bundled in the app package. On Windows, fonts are loaded from the app's `Fonts` folder. Performance tip: subset icon fonts to only the characters you use to reduce binary size (use tools like `glyphhanger` or `fonttools`).

**Code Example:**
```csharp
// MauiProgram.cs
builder.ConfigureFonts(fonts =>
{
    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
    fonts.AddFont("FontAwesome6-Regular.ttf", "FARegular");
    fonts.AddFont("FontAwesome6-Solid.ttf", "FASolid");
});
```

```xml
<!-- XAML usage -->
<Label Text="&#xf007;" FontFamily="FASolid" FontSize="24" />
<Label Text="Bold Title" FontFamily="OpenSansSemibold" FontSize="18" />
```

---

## 16. MAUI Performance Profiling & Tools

**Q51: What tools are available for profiling MAUI apps?**

**Answer:**

**Theory:** MAUI provides several profiling tools across platforms: (1) **`dotnet-trace`** — cross-platform tracing tool. Use `dotnet trace collect --profile maui-startup` to capture startup profiles, `dotnet trace collect --profile gc-verbose` for GC analysis. Output is a `.nettrace` file viewable in PerfView or Visual Studio. (2) **Visual Studio Diagnostic Tools** — CPU Usage, Memory Usage, and Network tabs. Connect to a running MAUI app (Windows) for real-time profiling. (3) **Xcode Instruments** — for iOS/macOS. Use the `Time Profiler`, `Allocations`, and `Leaks` instruments. Attach to the MAUI app process on a simulator or device. (4) **Android Studio Profiler** — for Android. Monitor CPU, memory, network, and energy usage. (5) **MAUI Layout Inspector** — set env var `DOTNET_MAUI_LAYOUT_CYCLES=1` to log layout pass counts to debug output. (6) **`dotnet-counters`** — real-time monitoring for GC, JIT, and thread pool metrics. `dotnet counters monitor -p <pid>`. (7) **`dotnet-dump`** — capture memory dumps for offline analysis. `dotnet dump collect -p <pid>`. Best practice: profile on the target device (not emulator), use Release build, and always take baseline measurements before optimizing.

---

**Q52: How do you identify and fix MAUI layout cycles?**

**Answer:**

**Theory:** A **layout cycle** (or layout pass storm) happens when a layout change triggers another layout change, causing infinite or excessive re-measures. Symptoms: UI freezes, high CPU, slow page transitions. To identify: set environment variable `DOTNET_MAUI_LAYOUT_CYCLES=1` — MAUI logs each layout pass to the debug console. Normal pages should have 2-4 passes (measure, arrange). If you see 20+ passes, there's a cycle. Common causes: (1) **`SizeChanged` event handler triggers layout change** — e.g., adjusting `HeightRequest` in response to `SizeChanged`. Fix: constrain layout with `Grid` or `AbsoluteLayout` instead. (2) **Binding to `Height`/`Width` in a layout-affecting way** — a child's height change causes parent to re-measure, which changes child's height, etc. Fix: use `MaximumHeightRequest`. (3) **`StackLayout` with dynamic content** — legacy stack re-measures all children. Use `VerticalStackLayout`. (4) **`AbsoluteLayout` with proportional flags** — every child re-measures on any layout change. Use `Grid` percentages instead. To fix: flatten nested layouts, constrain sizes explicitly, use `VerticalStackLayout`/`HorizontalStackLayout`, and avoid layout-affecting properties in bound setters.

---

## Quick Reference: Key MAUI Interview Topics

| Topic | Key Points |
|-------|------------|
| Architecture | Handlers > Renderers, PropertyMapper, CommandMapper |
| Shell | URI routing, `//absolute`, `relative`, `IQueryAttributable` |
| Layout | VerticalStackLayout > StackLayout, Grid > AbsoluteLayout |
| Data Binding | x:Bind + x:DataType preferred, compiled > runtime |
| MVVM | [ObservableProperty], [RelayCommand], WeakReferenceMessenger |
| Performance | CollectionView virtualization, AOT, trimming, compiled bindings |
| Custom Controls | View + Handler per platform + registration |
| Storage | SecureStorage for secrets, Preferences for settings |
| Testing | Mock INavigationService, unit test RelayCommands |
| Graphics | GraphicsView + IDrawable for custom charts/gauges |
| Threading | IDispatcher, ConfigureAwait(false) in ViewModels |
| Deployment | MSIX (Win), AAB (Android), IPA (iOS) |
