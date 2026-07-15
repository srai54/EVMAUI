using Microsoft.EntityFrameworkCore;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task Initialize(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (context.Roles.Any())
            return;

        var roles = new List<Role>
        {
            new() { Name = "Admin" },
            new() { Name = "EVRider" },
            new() { Name = "StationOperator" },
            new() { Name = "FleetManager" },
            new() { Name = "ServiceEngineer" }
        };
        context.Roles.AddRange(roles);
        await context.SaveChangesAsync();

        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@evswap.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Phone = "0000000000",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        context.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = context.Roles.First(r => r.Name == "Admin").Id });
        context.Wallets.Add(new Wallet { UserId = adminUser.Id, Balance = 50000 });

        var riders = new List<User>();
        var riderNames = new[] { "john_doe", "jane_smith", "mike_wilson", "sarah_parker", "tom_brown" };
        foreach (var name in riderNames)
        {
            var rider = new User
            {
                Username = name,
                Email = $"{name}@evswap.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Rider@123"),
                Phone = $"{Random.Shared.Next(1000000000, 1999999999)}",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30))
            };
            context.Users.Add(rider);
            riders.Add(rider);
        }
        await context.SaveChangesAsync();

        foreach (var rider in riders.Take(4))
            context.UserRoles.Add(new UserRole { UserId = rider.Id, RoleId = context.Roles.First(r => r.Name == "EVRider").Id });
        context.UserRoles.Add(new UserRole { UserId = riders.Last().Id, RoleId = context.Roles.First(r => r.Name == "StationOperator").Id });
        await context.SaveChangesAsync();

        foreach (var rider in riders)
        {
            context.Wallets.Add(new Wallet
            {
                UserId = rider.Id,
                Balance = Random.Shared.Next(100, 5000)
            });
        }
        await context.SaveChangesAsync();

        var stations = new List<Station>
        {
            new() { Name = "Downtown EV Hub", Address = "123 Main St, Downtown", Latitude = 40.7128, Longitude = -74.0060, Status = "Active", OperatorId = riders.Last().Id },
            new() { Name = "Green Energy Plaza", Address = "456 Oak Ave, Midtown", Latitude = 40.7282, Longitude = -73.7949, Status = "Active", OperatorId = riders.Last().Id },
            new() { Name = "EcoCharge Station", Address = "789 Pine Rd, Uptown", Latitude = 40.7489, Longitude = -73.9680, Status = "Active", OperatorId = riders.Last().Id },
            new() { Name = "PowerSwap Center", Address = "321 Elm St, Suburbs", Latitude = 40.7061, Longitude = -74.0087, Status = "Active", OperatorId = riders.Last().Id },
            new() { Name = "QuickCharge Depot", Address = "555 River Blvd, Harbor", Latitude = 40.6892, Longitude = -74.0445, Status = "Inactive", OperatorId = riders.Last().Id }
        };
        context.Stations.AddRange(stations);
        await context.SaveChangesAsync();

        var batteries = new List<Battery>();
        for (int i = 0; i < 30; i++)
        {
            var statuses = new[] { "Available", "Available", "InUse", "Available", "Maintenance", "Available", "InUse", "Available", "InUse", "Disposed" };
            var battery = new Battery
            {
                SerialNumber = $"EV-BAT-{1000 + i}",
                QRCode = $"QR-{1000 + i}",
                Manufacturer = new[] { "Tesla", "LG Chem", "Panasonic", "CATL", "BYD" }[Random.Shared.Next(5)],
                Capacity = 60 + Random.Shared.Next(5, 40),
                Status = statuses[Random.Shared.Next(statuses.Length)],
                ChargeLevel = Random.Shared.Next(10, 100),
                ChargeCycles = Random.Shared.Next(0, 500),
                Temperature = 22 + Random.Shared.NextDouble() * 18,
                Voltage = 48.0 + Random.Shared.NextDouble() * 4,
                InstallDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365)),
                WarrantyExpiry = DateTime.UtcNow.AddYears(2),
                LastMaintenance = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 60))
            };
            context.Batteries.Add(battery);
            batteries.Add(battery);
        }
        await context.SaveChangesAsync();

        var vehicles = new List<Vehicle>();
        var regNumbers = new[] { "EV-1001", "EV-1002", "EV-1003", "EV-1004", "EV-1005" };
        for (int i = 0; i < 5; i++)
        {
            var vehicle = new Vehicle
            {
                UserId = riders[i].Id,
                RegNumber = regNumbers[i],
                Model = new[] { "Model S", "Leaf", "IONIQ 5", "EV6", "Mustang Mach-E" }[i],
                Manufacturer = new[] { "Tesla", "Nissan", "Hyundai", "Kia", "Ford" }[i],
                BatteryId = batteries[i].Id
            };
            context.Vehicles.Add(vehicle);
            vehicles.Add(vehicle);
        }
        await context.SaveChangesAsync();

        for (int i = 0; i < 25; i++)
        {
            var daysAgo = Random.Shared.Next(0, 14);
            var startHour = Random.Shared.Next(6, 22);
            var durationHours = Random.Shared.NextDouble() * 2 + 0.5;
            var endTime = DateTime.UtcNow.AddDays(-daysAgo).Date.AddHours(startHour);
            var startTime = endTime.AddHours(-durationHours);
            var distance = 5 + Random.Shared.NextDouble() * 45;

            context.Trips.Add(new Trip
            {
                RiderId = riders[i % 5].Id,
                VehicleId = vehicles[i % 5].Id,
                StartTime = startTime,
                EndTime = endTime,
                StartLat = 40.70 + Random.Shared.NextDouble() * 0.05,
                StartLng = -74.02 + Random.Shared.NextDouble() * 0.04,
                EndLat = 40.70 + Random.Shared.NextDouble() * 0.05,
                EndLng = -74.02 + Random.Shared.NextDouble() * 0.04,
                DistanceKm = Math.Round(distance, 1)
            });
        }
        context.Trips.Add(new Trip
        {
            RiderId = riders[0].Id,
            VehicleId = vehicles[0].Id,
            StartTime = DateTime.UtcNow.AddHours(-1),
            EndTime = null,
            StartLat = 40.7128,
            StartLng = -74.0060,
            DistanceKm = 0
        });
        await context.SaveChangesAsync();

        for (int i = 0; i < 15; i++)
        {
            var rider = riders[i % 5];
            var station = stations[i % 5];
            var oldBattery = batteries[i % 30];
            var newBattery = batteries[(i + 3) % 30];
            var daysAgo = Random.Shared.Next(0, 10);

            context.BatterySwapRequests.Add(new BatterySwapRequest
            {
                RiderId = rider.Id,
                StationId = station.Id,
                VehicleId = vehicles[i % 5].Id,
                OldBatteryId = oldBattery.Id,
                RequestedBatteryType = "Standard",
                Status = i < 12 ? "Completed" : "Pending",
                CreatedAt = DateTime.UtcNow.AddDays(-daysAgo)
            });
            await context.SaveChangesAsync();

            if (i < 12)
            {
                var swapReq = context.BatterySwapRequests.OrderBy(r => r.Id).Last();
                context.BatterySwapHistories.Add(new BatterySwapHistory
                {
                    SwapRequestId = swapReq.Id,
                    RiderId = rider.Id,
                    StationId = station.Id,
                    OldBatteryId = oldBattery.Id,
                    NewBatteryId = newBattery.Id,
                    CompletedAt = DateTime.UtcNow.AddDays(-daysAgo).AddHours(Random.Shared.Next(1, 3))
                });
            }
        }
        await context.SaveChangesAsync();

        var wallets = await context.Wallets.ToListAsync();
        foreach (var w in wallets)
        {
            for (int i = 0; i < Random.Shared.Next(3, 8); i++)
            {
                context.Transactions.Add(new Transaction
                {
                    WalletId = w.Id,
                    Amount = Random.Shared.Next(10, 500),
                    Type = Random.Shared.Next(2) == 0 ? "Credit" : "Debit",
                    Reference = new[] { "Trip fare", "Swap fee", "Wallet top-up", "Refund", "Bonus credit" }[Random.Shared.Next(5)],
                    Timestamp = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 14))
                });
            }
        }
        await context.SaveChangesAsync();

        foreach (var rider in riders)
        {
            for (int i = 0; i < Random.Shared.Next(2, 6); i++)
            {
                context.Notifications.Add(new Notification
                {
                    UserId = rider.Id,
                    Title = new[] { "Battery ready", "Swap completed", "Trip saved", "Welcome!", "Low battery alert" }[Random.Shared.Next(5)],
                    Message = "This is a sample notification message for demonstration purposes.",
                    IsRead = Random.Shared.Next(2) == 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 7)),
                    Type = new[] { "Info", "Warning", "Success", "Alert" }[Random.Shared.Next(4)]
                });
            }
        }
        await context.SaveChangesAsync();

        context.FleetAssignments.Add(new FleetAssignment
        {
            FleetManagerId = adminUser.Id,
            VehicleId = vehicles[0].Id,
            DriverId = riders[0].Id,
            AssignedAt = DateTime.UtcNow.AddDays(-10),
            Status = "Active"
        });
        context.FleetAssignments.Add(new FleetAssignment
        {
            FleetManagerId = adminUser.Id,
            VehicleId = vehicles[1].Id,
            DriverId = riders[1].Id,
            AssignedAt = DateTime.UtcNow.AddDays(-7),
            Status = "Active"
        });
        context.FleetAssignments.Add(new FleetAssignment
        {
            FleetManagerId = adminUser.Id,
            VehicleId = vehicles[2].Id,
            DriverId = riders[2].Id,
            AssignedAt = DateTime.UtcNow.AddDays(-5),
            Status = "Inactive"
        });
        await context.SaveChangesAsync();

        context.MaintenanceRequests.Add(new MaintenanceRequest
        {
            BatteryId = batteries[4].Id,
            EngineerId = adminUser.Id,
            IssueType = "Voltage drop",
            Description = "Battery voltage dropping below threshold during discharge cycle.",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        });
        context.MaintenanceRequests.Add(new MaintenanceRequest
        {
            BatteryId = batteries[9].Id,
            EngineerId = adminUser.Id,
            IssueType = "Overheating",
            Description = "Battery temperature exceeding 45°C during fast charging.",
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        context.MaintenanceRequests.Add(new MaintenanceRequest
        {
            BatteryId = batteries[2].Id,
            EngineerId = adminUser.Id,
            IssueType = "Routine check",
            Description = "Scheduled quarterly maintenance inspection.",
            Status = "Resolved",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            ResolvedAt = DateTime.UtcNow.AddDays(-3)
        });
        await context.SaveChangesAsync();

        context.SupportTickets.Add(new SupportTicket
        {
            UserId = riders[1].Id,
            Subject = "App login issue",
            Description = "Unable to log in after password reset.",
            Status = "Open",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        context.SupportTickets.Add(new SupportTicket
        {
            UserId = riders[2].Id,
            Subject = "Battery not swapping",
            Description = "Station reported no batteries available but app shows 5.",
            Status = "InProgress",
            CreatedAt = DateTime.UtcNow.AddHours(-6)
        });
        context.SupportTickets.Add(new SupportTicket
        {
            UserId = riders[0].Id,
            Subject = "Payment failed",
            Description = "Wallet deduction failed during swap at Downtown station.",
            Status = "Resolved",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            ResolvedAt = DateTime.UtcNow.AddDays(-2)
        });
        await context.SaveChangesAsync();
    }
}
