# EVSwap API - Single Project Reorganization

## Overview
Reorganize `EVSwap.API` from flat folders into an industry-standard single-project structure with clear separation between Core (domain) and Infrastructure (implementation) layers.

## Current Structure
```
EVSwap.API/
├── Authentication/     # mixed interfaces + implementations
├── Authorization/      # policy constants + role requirement
├── Controllers/        # ✅ keep
├── Data/               # ✅ keep
├── DTOs/               # 26 flat files
├── Middleware/          # ✅ keep
├── Models/             # 17 flat entity files
├── Repositories/       # interfaces + implementations interleaved
├── Services/           # interfaces + implementations interleaved
├── SignalR/            # ✅ keep
├── Utilities/          # 2 files
├── Program.cs          # ✅ keep
├── appsettings.json    # ✅ keep
```

## Target Structure
```
EVSwap.API/
├── Controllers/        # unchanged
├── Middleware/          # unchanged
├── SignalR/            # unchanged
├── Program.cs          # unchanged
├── appsettings.json    # unchanged
├── appsettings.Development.json
│
├── Core/               # Dependencies: none (pure domain)
│   ├── Entities/       # renamed from Models/
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   └── Services/
│   ├── DTOs/
│   │   ├── Auth/
│   │   ├── Battery/
│   │   ├── Station/
│   │   ├── Swap/
│   │   ├── Trip/
│   │   ├── Wallet/
│   │   ├── Notification/
│   │   ├── Fleet/
│   │   ├── Maintenance/
│   │   └── Report/
│   └── Constants/
│
├── Infrastructure/     # Dependencies: Core
│   ├── Data/
│   ├── Repositories/
│   ├── Services/
│   └── Utilities/
```

## Namespace Convention
- `EVSwap.API.Core.Entities`
- `EVSwap.API.Core.Interfaces.Repositories`
- `EVSwap.API.Core.Interfaces.Services`
- `EVSwap.API.Core.DTOs.Auth`
- `EVSwap.API.Core.Constants`
- `EVSwap.API.Infrastructure.Data`
- `EVSwap.API.Infrastructure.Repositories`
- `EVSwap.API.Infrastructure.Services`
- `EVSwap.API.Infrastructure.Utilities`

## File Moves
| Current Location | New Location |
|---|---|
| `Models/*.cs` | `Core/Entities/*.cs` |
| `DTOs/*.cs` | `Core/DTOs/{Feature}/*.cs` |
| `Authentication/IJwtService.cs` | `Core/Interfaces/Services/IJwtService.cs` |
| `Authentication/JwtService.cs` | `Infrastructure/Services/JwtService.cs` |
| `Authorization/Policies.cs` | `Core/Constants/Policies.cs` |
| `Authorization/RoleRequirement.cs` | `Infrastructure/Services/RoleRequirement.cs` |
| `Services/I*Service.cs` | `Core/Interfaces/Services/I*Service.cs` |
| `Services/*Service.cs` | `Infrastructure/Services/*Service.cs` |
| `Repositories/I*Repository.cs` | `Core/Interfaces/Repositories/I*Repository.cs` |
| `Repositories/*Repository.cs` | `Infrastructure/Repositories/*Repository.cs` |
| `Utilities/*.cs` | `Infrastructure/Utilities/*.cs` |
| `Data/*.cs` | `Infrastructure/Data/*.cs` |

## Required Updates
- All namespaces in every moved .cs file
- All `using` statements referencing the old namespaces
- No logic changes — pure restructuring
