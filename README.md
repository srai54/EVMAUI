# EV Swap - Battery Swap Management System

A full-stack electric vehicle battery swap management application built with .NET 10. The system manages battery swap stations, riders, fleet vehicles, maintenance, and wallet transactions.

## Project Structure

```
src/
├── EVSwap.API/          # Backend Web API (ASP.NET Core)
│   ├── Controllers/     # API endpoints (Auth, Report, Swap, Trip, Wallet, Station, etc.)
│   ├── Core/            # Business logic, DTOs, interfaces, services
│   ├── Infrastructure/  # Database, repositories, external services
│   └── Program.cs       # App startup & configuration
│
└── EVSwap.Mobile/       # MAUI mobile app (Windows, Android, iOS)
    ├── Views/           # XAML pages (Login, Dashboard, Stations, Wallet, etc.)
    ├── ViewModels/      # UI logic per page
    ├── Models/          # Data models matching API responses
    ├── Services/        # API calls, auth, navigation, local DB
    ├── Interfaces/      # Service contracts
    └── Resources/       # Styles, colors, fonts
```

## Features

- **Role-based login** - Admin, Rider roles with JWT authentication
- **Dashboard** - Battery health, wallet balance, trip stats, recent activity (30+ KPIs for admin)
- **Battery Swap** - Request swaps, view swap history, QR scanning
- **Stations** - Find nearby stations, view station details
- **Wallet** - Check balance, add money, transaction history
- **Trips** - Start/end trips, trip history
- **Notifications** - Real-time and historical notifications
- **Profile** - View/edit user profile, settings
- **Admin Panel** - User management, fleet dashboard, maintenance dashboard

## Quick Start

### Prerequisites
- .NET 10 SDK
- SQL Server (or update connection string in appsettings.json)
- Visual Studio 2022+ (recommended) or VS Code

### Run the API
```bash
cd src/EVSwap.API
dotnet run
```
API runs at `http://localhost:5238`. First run applies migrations and seeds demo data automatically.

### Run the Mobile App
```bash
cd src/EVSwap.Mobile
dotnet run -f net10.0-windows10.0.19041.0
```

### Demo Credentials
| Role  | Username | Password    |
|-------|----------|-------------|
| Admin | admin    | Admin@123   |
| Rider | john_doe | Rider@123   |

## Login Issue Workaround

If the API is not running or login fails, use the **"Bypass Login (Dev)"** button on the login screen to enter the app with a dummy admin account. All features will show sample data when the API is unavailable.

## Tech Stack

- **Backend:** ASP.NET Core, Entity Framework Core, SQL Server/PostgreSQL
- **Mobile:** .NET MAUI, CommunityToolkit.Mvvm, CommunityToolkit.Maui
- **Auth:** JWT tokens with refresh tokens
- **Real-time:** SignalR for live updates
