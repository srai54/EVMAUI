# EVSwap API Reorganization Implementation Plan

> **For agentic workers:** Use subagent-driven-development or executing-plans.

**Goal:** Reorganize EVSwap.API into Core/Infrastructure layered structure within a single project.

**Architecture:** Single project with `Core/` (entities, interfaces, DTOs, constants) and `Infrastructure/` (data, repositories, services, utilities) folders. No logic changes.

**Tech Stack:** .NET 10.0, C#

## Global Constraints
- Namespace convention: `EVSwap.API.Core.*`, `EVSwap.API.Infrastructure.*`
- No logic changes — pure restructuring
- All files must compile after move

---
### Task 1: Create new directory structure

**Files:**
- No code changes

- [ ] **Step 1: Create all required directories**

```powershell
$base = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
$dirs = @(
    "Core\Entities",
    "Core\Interfaces\Repositories",
    "Core\Interfaces\Services",
    "Core\DTOs\Auth",
    "Core\DTOs\Battery",
    "Core\DTOs\Station",
    "Core\DTOs\Swap",
    "Core\DTOs\Trip",
    "Core\DTOs\Wallet",
    "Core\DTOs\Notification",
    "Core\DTOs\Fleet",
    "Core\DTOs\Maintenance",
    "Core\DTOs\Report",
    "Core\Constants",
    "Infrastructure\Data",
    "Infrastructure\Repositories",
    "Infrastructure\Services",
    "Infrastructure\Utilities"
)
foreach ($d in $dirs) {
    New-Item -ItemType Directory -Path "$base\$d" -Force | Out-Null
}
```

- [ ] **Step 2: Verify directories created**

Run: `Get-ChildItem C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API -Recurse -Directory | Where-Object { $_.FullName -match '\\Core\\|\\Infrastructure\\' } | Select-Object FullName`

---

### Task 2: Move Models → Core/Entities (update namespace)

**Files:** 17 entity files in `Models/` → `Core/Entities/`

- [ ] **Step 1: Move and update namespaces for all entity files**

```powershell
$src = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
Get-ChildItem "$src\Models\*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $content = $content -replace 'namespace EVSwap\.API\.Models', 'namespace EVSwap.API.Core.Entities'
    $dest = "$src\Core\Entities\$($_.Name)"
    Set-Content -Path $dest -Value $content -NoNewline
    Remove-Item $_.FullName
}
```

---

### Task 3: Move DTOs → Core/DTOs/{Feature}/

**Files:** 26 DTO files

- [ ] **Step 1: Move DTO files to feature subfolders**

```powershell
$src = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
$maps = @{
    "LoginRequest.cs" = "Auth"
    "RegisterRequest.cs" = "Auth"
    "RefreshTokenRequest.cs" = "Auth"
    "ForgotPasswordRequest.cs" = "Auth"
    "VerifyOtpRequest.cs" = "Auth"
    "AssignRoleRequest.cs" = "Auth"
    "UpdateProfileRequest.cs" = "Auth"
    "AuthResponse.cs" = "Auth"
    "UserProfileDto.cs" = "Auth"
    "BatteryDto.cs" = "Battery"
    "BatteryHealthDto.cs" = "Battery"
    "StationDto.cs" = "Station"
    "NearbyStationQuery.cs" = "Station"
    "SwapRequestDto.cs" = "Swap"
    "SwapHistoryDto.cs" = "Swap"
    "TripDto.cs" = "Trip"
    "StartTripRequest.cs" = "Trip"
    "EndTripRequest.cs" = "Trip"
    "WalletDto.cs" = "Wallet"
    "AddMoneyRequest.cs" = "Wallet"
    "TransactionDto.cs" = "Wallet"
    "NotificationDto.cs" = "Notification"
    "FleetDto.cs" = "Fleet"
    "MaintenanceDto.cs" = "Maintenance"
    "DashboardDto.cs" = "Report"
}
$maps.GetEnumerator() | ForEach-Object {
    $file = "$src\DTOs\$($_.Key)"
    if (Test-Path $file) {
        $content = Get-Content $file -Raw
        $content = $content -replace 'namespace EVSwap\.API\.DTOs', "namespace EVSwap.API.Core.DTOs.$($_.Value)"
        $dest = "$src\Core\DTOs\$($_.Value)\$($_.Key)"
        Set-Content -Path $dest -Value $content -NoNewline
        Remove-Item $file
    }
}
```

---

### Task 4: Move Authentication + Authorization files

**Files:** IJwtService.cs (Core/Interfaces/Services), JwtService.cs (Infrastructure/Services), Policies.cs (Core/Constants), RoleRequirement.cs (Infrastructure/Services)

- [ ] **Step 1: Move and rename namespaces**

```powershell
$src = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
# IJwtService → Core/Interfaces/Services
$content = Get-Content "$src\Authentication\IJwtService.cs" -Raw
$content = $content -replace 'namespace EVSwap\.API\.Authentication', 'namespace EVSwap.API.Core.Interfaces.Services'
Set-Content -Path "$src\Core\Interfaces\Services\IJwtService.cs" -Value $content -NoNewline
Remove-Item "$src\Authentication\IJwtService.cs"

# JwtService → Infrastructure/Services
$content = Get-Content "$src\Authentication\JwtService.cs" -Raw
$content = $content -replace 'namespace EVSwap\.API\.Authentication', 'namespace EVSwap.API.Infrastructure.Services'
Set-Content -Path "$src\Infrastructure\Services\JwtService.cs" -Value $content -NoNewline
Remove-Item "$src\Authentication\JwtService.cs"

# Policies → Core/Constants
$content = Get-Content "$src\Authorization\Policies.cs" -Raw
$content = $content -replace 'namespace EVSwap\.API\.Authorization', 'namespace EVSwap.API.Core.Constants'
Set-Content -Path "$src\Core\Constants\Policies.cs" -Value $content -NoNewline
Remove-Item "$src\Authorization\Policies.cs"

# RoleRequirement → Infrastructure/Services
$content = Get-Content "$src\Authorization\RoleRequirement.cs" -Raw
$content = $content -replace 'namespace EVSwap\.API\.Authorization', 'namespace EVSwap.API.Infrastructure.Services'
Set-Content -Path "$src\Infrastructure\Services\RoleRequirement.cs" -Value $content -NoNewline
Remove-Item "$src\Authorization\RoleRequirement.cs"

# Remove empty folders
Remove-Item "$src\Authentication" -Force -ErrorAction SilentlyContinue
Remove-Item "$src\Authorization" -Force -ErrorAction SilentlyContinue
```

---

### Task 5: Move interface files to Core/Interfaces/

**Files:** I*Repository.cs → Core/Interfaces/Repositories, I*Service.cs → Core/Interfaces/Services

- [ ] **Step 1: Move repository interfaces**

```powershell
$src = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
foreach ($file in Get-ChildItem "$src\Repositories\I*.cs") {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'namespace EVSwap\.API\.Repositories', 'namespace EVSwap.API.Core.Interfaces.Repositories'
    Set-Content -Path "$src\Core\Interfaces\Repositories\$($file.Name)" -Value $content -NoNewline
    Remove-Item $file.FullName
}
```

- [ ] **Step 2: Move service interfaces**

```powershell
$src = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
foreach ($file in Get-ChildItem "$src\Services\I*.cs") {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'namespace EVSwap\.API\.Services', 'namespace EVSwap.API.Core.Interfaces.Services'
    Set-Content -Path "$src\Core\Interfaces\Services\$($file.Name)" -Value $content -NoNewline
    Remove-Item $file.FullName
}
```

---

### Task 6: Move repository implementations to Infrastructure/Repositories

**Files:** *Repository.cs (non-interface) → Infrastructure/Repositories

- [ ] **Step 1: Move repository implementations**

```powershell
$src = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
foreach ($file in Get-ChildItem "$src\Repositories\*.cs") {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'namespace EVSwap\.API\.Repositories', 'namespace EVSwap.API.Infrastructure.Repositories'
    Set-Content -Path "$src\Infrastructure\Repositories\$($file.Name)" -Value $content -NoNewline
    Remove-Item $file.FullName
}
Remove-Item "$src\Repositories" -Force -ErrorAction SilentlyContinue
```

---

### Task 7: Move service implementations to Infrastructure/Services

**Files:** *Service.cs (non-interface) → Infrastructure/Services

- [ ] **Step 1: Move service implementations**

```powershell
$src = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
foreach ($file in Get-ChildItem "$src\Services\*.cs") {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'namespace EVSwap\.API\.Services', 'namespace EVSwap.API.Infrastructure.Services'
    Set-Content -Path "$src\Infrastructure\Services\$($file.Name)" -Value $content -NoNewline
    Remove-Item $file.FullName
}
Remove-Item "$src\Services" -Force -ErrorAction SilentlyContinue
```

---

### Task 8: Move Data and Utilities to Infrastructure/

**Files:** AppDbContext.cs, DbInitializer.cs → Infrastructure/Data; DistanceHelper.cs, AppConstants.cs → Infrastructure/Utilities

- [ ] **Step 1: Move Data files**

```powershell
$src = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
foreach ($file in Get-ChildItem "$src\Data\*.cs") {
    $content = Get-Content $file.FullName -Raw
    $content = $content -replace 'namespace EVSwap\.API\.Data', 'namespace EVSwap.API.Infrastructure.Data'
    Set-Content -Path "$src\Infrastructure\Data\$($file.Name)" -Value $content -NoNewline
    Remove-Item $file.FullName
}
Remove-Item "$src\Data" -Force -ErrorAction SilentlyContinue
```

- [ ] **Step 2: Move Utilities files**

```powershell
$src = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
# DistanceHelper
$content = Get-Content "$src\Utilities\DistanceHelper.cs" -Raw
$content = $content -replace 'namespace EVSwap\.API\.Utilities', 'namespace EVSwap.API.Infrastructure.Utilities'
Set-Content -Path "$src\Infrastructure\Utilities\DistanceHelper.cs" -Value $content -NoNewline
Remove-Item "$src\Utilities\DistanceHelper.cs"

# AppConstants
$content = Get-Content "$src\Utilities\AppConstants.cs" -Raw
$content = $content -replace 'namespace EVSwap\.API\.Utilities', 'namespace EVSwap.API.Infrastructure.Utilities'
Set-Content -Path "$src\Infrastructure\Utilities\AppConstants.cs" -Value $content -NoNewline
Remove-Item "$src\Utilities\AppConstants.cs"

Remove-Item "$src\Utilities" -Force -ErrorAction SilentlyContinue
```

---

### Task 9: Update Program.cs to use new namespaces

**Files:**
- Modify: `Program.cs`

- [ ] **Step 1: Replace Program.cs using statements**

```powershell
$src = "C:\Users\55\source\repos\FullStackEVMvuiApp\src\EVSwap.API"
$content = Get-Content "$src\Program.cs" -Raw
$replacements = @(
    @{old = 'using EVSwap.API.Data;'; new = 'using EVSwap.API.Infrastructure.Data;'}
    @{old = 'using EVSwap.API.Authentication;'; new = 'using EVSwap.API.Core.Interfaces.Services;'}
    @{old = 'using EVSwap.API.Repositories;'; new = 'using EVSwap.API.Infrastructure.Repositories;'}
    @{old = 'using EVSwap.API.Services;'; new = 'using EVSwap.API.Infrastructure.Services;'}
    @{old = 'using EVSwap.API.Middleware;'; new = 'using EVSwap.API.Middleware;'}
    @{old = 'using EVSwap.API.Utilities;'; new = 'using EVSwap.API.Infrastructure.Utilities;'}
)
foreach ($r in $replacements) {
    $content = $content -replace [regex]::Escape($r.old), $r.new
}
Set-Content -Path "$src\Program.cs" -Value $content -NoNewline
```

---

### Task 10: Build and verify

- [ ] **Step 1: Build the solution**

Run: `dotnet build C:\Users\55\source\repos\FullStackEVMvuiApp\EVSwap.slnx 2>&1`

- [ ] **Step 2: Fix any remaining namespace issues**

Check build output for namespace errors and fix them.
