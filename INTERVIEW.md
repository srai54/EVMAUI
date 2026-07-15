# Interview Questions — .NET MAUI & EV Swap App

## 1. Application-Specific (EV Swap Project)

1. Walk through the app architecture — how does a login request flow from UI to API?
2. Why does the app use both `IApiService` and `IAuthService` instead of one service?
3. How does dependency injection work in this MAUI app? Where are services registered?
4. What happens when the API is down and a user clicks "Bypass Login"?
5. How does the `ApiService.HandleResponse<T>` method handle errors?
6. Why was `CloneRequestAsync` removed from the original `ApiService`?
7. How does `AuthService.BypassLogin()` work and why is it useful?
8. The app has `catch {}` blocks in ViewModels — is that good practice? How would you improve it?
9. How does the `LoginViewModel` communicate with the `DashboardViewModel` after login?
10. What is `Constants.ApiBaseUrl` and how is it configured?
11. How does the app handle JWT token storage and retrieval using `SecureStorage`?
12. Why does `App.xaml.cs` check for a stored token on startup and navigate accordingly?
13. How does `BaseViewModel` reduce code duplication across all ViewModels?
14. What is the role of the `ISecureStorageService` in this app?
15. How would you add offline support for swap requests when the API is unreachable?

## 2. .NET MAUI Fundamentals

16. What is the difference between `ContentPage`, `Shell`, and `NavigationPage` in MAUI?
17. How does Shell routing work? Give an example from the app.
18. What are the differences between `Transient` and `Singleton` service lifetimes in MAUI DI?
19. How would you pass complex data between pages in MAUI?
20. What is `AppThemeBinding` and how is it used in `Styles.xaml`?
21. How does MAUI handle platform-specific code (Windows vs Android vs iOS)?
22. What layout panels are available in MAUI and when would you use each?
23. Explain how `CollectionView` differs from `ListView`. Which is better for performance?
24. How does data binding work in MAUI XAML? Give an example from the app.
25. What is `x:DataType` and why is it important for compiled bindings?
26. How do you handle large lists in MAUI without freezing the UI?
27. What is `VisualStateManager` and how is it used in button styles?
28. How would you implement pull-to-refresh in a MAUI app?
29. Explain the MAUI lifecycle: `OnAppearing`, `OnDisappearing`, and page navigation events.
30. How do you style an app globally using `ResourceDictionary`?

## 3. C# Language & .NET Runtime

31. Explain `async` / `await` — what happens on the call stack when you await a task?
32. What is `Task<T>` vs `ValueTask<T>`? When would you use each?
33. What is a deadlock with `async` / `await` and how do you avoid it in MAUI?
34. Explain `ConfigureAwait(false)` — should you use it in MAUI apps?
35. How does `JsonSerializer.Deserialize<T>` handle missing or extra JSON properties?
36. What is the difference between `record` and `class` in C#? Would you use records for DTOs?
37. Explain nullable reference types. Why does `UserModel?` use `?` but `string` is already nullable?
38. What is pattern matching in C# and give an example using switch expressions?
39. How does `List<T>.ForEach` differ from a `foreach` loop in terms of LINQ and lambda captures?
40. What is `FireAndForget` and why is it used in `SettingsViewModel`?
41. Explain the `IDisposable` pattern — when should you implement it in a MAUI app?
42. What is `Span<T>` and how does it differ from array slices?
43. How does `yield return` work internally?
44. What is the difference between `StringBuilder` and string concatenation in a loop?
45. Explain covariance and contravariance in C# generics.

## 4. Entity Framework Core & Database

46. How does `DbInitializer.SeedData()` work? When does it run?
47. What is Code-First migration in EF Core? How would you add a new column to the Batteries table?
48. Explain `Include()` and `ThenInclude()` in EF Core — when do you need them?
49. What is the difference between `FirstOrDefault()` and `SingleOrDefault()`?
50. How does EF Core change tracking work? What is `AsNoTracking()` for?
51. What is the difference between SQL Server and PostgreSQL with EF Core?
52. How would you handle a million battery swap records efficiently in a query?
53. What is a migration bundle and when would you use it?
54. How does EF Core map C# `enum` types to the database?
55. What is the `IQueryable<T>` vs `IEnumerable<T>` difference in database queries?

## 5. REST API & HTTP Communication

56. Explain the full HTTP request/response lifecycle of a login call in this app.
57. What status codes does `ApiService.HandleResponse` consider successful?
58. How does JWT authentication work? What is the difference between access token and refresh token?
59. How would you intercept all HTTP requests to add logging in `ApiService`?
60. What is the `HttpClient` lifetime in a MAUI app? Should you use `IHttpClientFactory`?
61. How does `PostAsJsonAsync<T>` serialize your request object?
62. What is `EnsureSuccessStatusCode()` and what happens when it fails?
63. How would you implement request retry with exponential backoff on network failure?
64. What is `MultipartFormDataContent` used for in `PostMultipartAsync`?
65. How do you handle file uploads in a MAUI REST client?

## 6. MVVM & Data Binding with CommunityToolkit

66. Explain the MVVM pattern — how do Model, View, and ViewModel interact in this app?
67. What does `[ObservableProperty]` generate behind the scenes?
68. What is the difference between `[RelayCommand]` and `ICommand`?
69. How does `INotifyPropertyChanged` work and why is it essential for MVVM?
70. What is `ObservableCollection<T>` and when should you use it instead of `List<T>`?
71. How does two-way binding work for an `Entry` field like the Username in `LoginPage`?
72. What is `QueryProperty` and how is it used in `SwapRequestViewModel`?
73. How does `BaseViewModel.ShowAlertAsync` work without a direct reference to the page?
74. What is the role of `IValueConverter`? Give an example use case.
75. How would you bind a button's `IsEnabled` to a ViewModel property?
76. Explain `x:Bind` vs `Binding` in MAUI XAML.
77. What is `x:Load` and how does it improve performance?
78. How does the `CommunityToolkit.Mvvm` source generator work?
79. What are `partial` methods and how does `OnIsBiometricEnabledChanged` get called?
80. How would you implement master-detail navigation in MVVM with Shell?

## 7. Dependency Injection

81. What is dependency injection and why is it used in this app?
82. What is the difference between `AddSingleton`, `AddTransient`, and `AddScoped` in MAUI?
83. How does the DI container resolve `LoginViewModel` which depends on `IAuthService`?
84. What happens if you register the same interface twice?
85. How would you register a service that needs to be different in Debug vs Release builds?
86. What is the Service Locator anti-pattern and how does it differ from DI?
87. How does constructor injection work with MAUI Shell pages?
88. What is `IServiceProvider` and how was it previously used in `ApiService` (before refactoring)?
89. How would you inject configuration like URLs into a service?
90. What is the composition root in a MAUI application?

## 8. Testing & Debugging

91. How would you unit test `LoginViewModel.LoginAsync()` without calling the real API?
92. What is mocking and how would you mock `IApiService` in a test?
93. How would you test the `ApiService.HandleResponse<T>` method?
94. What debugging tools are available for MAUI on Windows?
95. How would you diagnose why a data-bound label isn't updating in the UI?
96. What is UI testing in MAUI and how does `Microsoft.Maui.Testing` work?
97. How would you test async methods that use `await`?
98. What is the Arrange-Act-Assert pattern?
99. How would you verify a navigation call happened in a ViewModel test?
100. How do you test code that depends on `SecureStorage` or platform APIs?

## 9. Performance & Security

101. What MAUI-specific performance concerns would you consider for a battery swap app?
102. How does `CollectionView` with `DataTemplate` get recycled and why is that important?
103. What is AOT compilation and how does it relate to MAUI on Windows?
104. How would you reduce app startup time in a MAUI app?
105. What is the risk of storing JWT tokens in `SecureStorage` vs `Preferences`?
106. How does `HttpClient` timeout protect the app from hanging?
107. What is input validation and how is it done in `AddMoneyViewModel`?
108. How would you prevent SQL injection in an API endpoint?
109. What is XSS and how does MAUI protect against it?
110. How would you secure the API if the app communicates over public internet?

## 10. General Software Engineering

111. What is SOLID? Give an example of each principle from this codebase.
112. What is the difference between composition and inheritance? Which does MAUI prefer?
113. Explain the Repository pattern — is it used in this app's API backend?
114. What is a clean architecture layer? Map the layers of `EVSwap.API` to clean architecture terms.
115. What is technical debt and how would you identify it in this codebase?
116. How would you implement logging across the app for production monitoring?
117. What is the difference between integration testing and unit testing?
118. How would you version a REST API?
119. What is CORS and when would you need to configure it?
120. How would you deploy a MAUI Windows app to end users?

---

> **Tip:** For each question, try to connect it back to this specific project. Interviewers value candidates who can discuss concrete examples from real code rather than just textbook definitions.
