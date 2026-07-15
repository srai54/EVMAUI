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
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        var adminRole = context.Roles.First(r => r.Name == "Admin");
        context.UserRoles.Add(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id });

        var wallet = new Wallet
        {
            UserId = adminUser.Id,
            Balance = 10000
        };
        context.Wallets.Add(wallet);

        await context.SaveChangesAsync();
    }
}
