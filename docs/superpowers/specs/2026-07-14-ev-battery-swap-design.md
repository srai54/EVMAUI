# EV Battery Swap Management Application — Design Spec

## Overview

A full-stack EV Battery Swap Management system with a .NET MAUI mobile app (MVVM) and ASP.NET Core Web API (Controller → Service → Repository). Supports five roles: Admin, EV Rider, Station Operator, Fleet Manager, Service Engineer.

## Tech Stack

| Layer | Technology |
|---|---|
| Mobile | .NET MAUI, CommunityToolkit.Mvvm, Shell Navigation |
| Backend | ASP.NET Core Web API, EF Core |
| Database | SQLite (both backend and mobile offline) |
| Auth | JWT + Refresh Tokens, Role-based, Biometric |
| Realtime | SignalR |
| Notifications | Firebase Cloud Messaging |

## Solution Structure

```
/FullStackEVMvuiApp
├── EVSwap.sln
├── src/
│   ├── EVSwap.API/
│   └── EVSwap.Mobile/
```

### EVSwap.API

```
Controllers/     — AuthController, UserController, BatteryController, StationController,
                   SwapController, TripController, WalletController, NotificationController,
                   FleetController, MaintenanceController, ReportController
Services/        — Interfaces + Implementations
Repositories/    — Interfaces + Implementations
Models/          — Entity Framework entities
DTOs/            — Request/Response DTOs
Data/            — AppDbContext, Migrations
Middleware/      — ExceptionMiddleware, JwtMiddleware
Authentication/  — JwtService, TokenProvider
Authorization/   — Custom policies/requirements
SignalR/         — Hubs (BatteryHub, StationHub, NotificationHub, DashboardHub)
Utilities/       — Helpers, Extensions
```

### EVSwap.Mobile

```
Views/           — XAML pages grouped by feature
ViewModels/      — MVVM ViewModels using CommunityToolkit
Models/          — POCO models matching API DTOs
Services/        — ApiService, AuthService, NavigationService, etc.
Interfaces/      — Service contracts
Helpers/         — Constants, Converters, Extensions
Resources/       — Fonts, Images, Styles, Colors
LocalDatabase/   — SQLite tables for offline cache and sync queue
SignalR/         — SignalR client service
Validators/      — Input validation helpers
```

## Database Schema

### Identity & Auth
- **Users**: Id, Username, Email, PasswordHash, Phone, IsActive, CreatedAt
- **Roles**: Id, Name
- **UserRoles**: UserId, RoleId

### Battery & Vehicle
- **Vehicles**: Id, UserId, RegNumber, Model, Manufacturer, BatteryId (FK)
- **Batteries**: Id, SerialNumber, QRCode, Manufacturer, Capacity, Status, ChargeLevel, ChargeCycles, Temperature, Voltage, InstallDate, WarrantyExpiry, LastMaintenance
- **BatteryHealth**: Id, BatteryId, Timestamp, ChargeLevel, Temperature, Voltage, CycleCount, Notes

### Swap Operations
- **BatterySwapRequests**: Id, RiderId, StationId, VehicleId, OldBatteryId, RequestedBatteryType, Status, CreatedAt
- **BatterySwapHistory**: Id, SwapRequestId, RiderId, StationId, OldBatteryId, NewBatteryId, CompletedAt

### Station & Trips
- **Stations**: Id, Name, Address, Latitude, Longitude, OperatorId, Status
- **Trips**: Id, RiderId, VehicleId, StartTime, EndTime, StartLat, StartLng, EndLat, EndLng, DistanceKm

### Wallet & Finance
- **Wallets**: Id, UserId, Balance
- **Transactions**: Id, WalletId, Amount, Type, Reference, Timestamp

### Notifications & Support
- **Notifications**: Id, UserId, Title, Message, IsRead, CreatedAt, Type
- **SupportTickets**: Id, UserId, Subject, Description, Status, CreatedAt, ResolvedAt

### Fleet & Maintenance
- **FleetAssignments**: Id, FleetManagerId, VehicleId, DriverId, AssignedAt, Status
- **MaintenanceRequests**: Id, BatteryId, EngineerId, IssueType, Description, Status, CreatedAt, ResolvedAt
- **AuditLogs**: Id, UserId, Action, EntityType, EntityId, Timestamp, Details

## API Controllers & Endpoints

| Controller | Endpoints |
|---|---|
| AuthController | POST login, register, refresh, forgot-password, verify-otp |
| UserController | GET/PUT profile, GET by id, GET all (admin) |
| BatteryController | GET all, GET by id, GET nearby, PUT status |
| StationController | GET nearby, GET by id, POST/PUT/DELETE (admin) |
| SwapController | POST request, POST approve, GET history |
| TripController | GET all, POST start, PUT end |
| WalletController | GET balance, POST add, GET transactions |
| NotificationController | GET all, PUT read, POST register-device |
| FleetController | GET vehicles, POST assign, GET reports |
| MaintenanceController | GET requests, POST request, PUT resolve |
| ReportController | GET dashboard, swaps, revenue |

## SignalR Hubs

- **BatteryHub**: Live battery status, swap request updates
- **StationHub**: Live inventory changes
- **NotificationHub**: Real-time notifications
- **DashboardHub**: Admin dashboard live refresh

## Authentication Flow

1. User logs in → API returns JWT + RefreshToken
2. Tokens stored in SecureStorage (MAUI)
3. On app start → check SecureStorage → validate JWT → if expired, use RefreshToken
4. Biometric → store encrypted password → authenticate via platform biometric → silent login call
5. All API calls include `Authorization: Bearer {jwt}` header

## Offline Support (MAUI SQLite)

- Local tables: LocalUser, CachedStation, CachedBattery, PendingSync
- App reads from local cache when offline
- PendingSync table queues mutations for replay when connectivity returns
- ConnectivityService monitors network state and triggers sync

## Notification Flow

- App registers FCM device token on login → stored on server
- Server sends push via Firebase Admin SDK for critical alerts
- SignalR delivers in-app notifications for real-time updates when connected
- Notification center stores history both server-side and locally

## MVVM Pattern (MAUI)

- Each ViewModel extends `ObservableObject` from CommunityToolkit.Mvvm
- Properties use `[ObservableProperty]` attribute
- Commands use `[RelayCommand]` attribute
- Services injected via constructor DI
- Views bind via `{Binding}` in XAML
- Navigation handled via `Shell.Current.GoToAsync()`

## Roles & Features

- **Admin**: Full dashboard, user/role/battery/station management, reports
- **EV Rider**: Dashboard (battery %, range), find stations, request swap, QR scan, trips, wallet, notifications
- **Station Operator**: Station dashboard, approve swaps, verify QR, assign batteries
- **Fleet Manager**: Fleet dashboard, vehicle monitoring, driver assignment, reports
- **Service Engineer**: Maintenance dashboard, battery diagnostics (health, temp, voltage), service history
