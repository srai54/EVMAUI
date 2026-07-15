using Microsoft.EntityFrameworkCore;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Battery> Batteries => Set<Battery>();
    public DbSet<BatteryHealth> BatteryHealthRecords => Set<BatteryHealth>();
    public DbSet<BatterySwapRequest> BatterySwapRequests => Set<BatterySwapRequest>();
    public DbSet<BatterySwapHistory> BatterySwapHistories => Set<BatterySwapHistory>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<FleetAssignment> FleetAssignments => Set<FleetAssignment>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Battery>()
            .HasIndex(b => b.SerialNumber)
            .IsUnique();

        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Battery)
            .WithMany()
            .HasForeignKey(v => v.BatteryId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<BatterySwapRequest>()
            .HasOne(r => r.Rider)
            .WithMany()
            .HasForeignKey(r => r.RiderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BatterySwapRequest>()
            .HasOne(r => r.Station)
            .WithMany()
            .HasForeignKey(r => r.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BatterySwapRequest>()
            .HasOne(r => r.Vehicle)
            .WithMany()
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BatterySwapHistory>()
            .HasOne(h => h.Rider)
            .WithMany()
            .HasForeignKey(h => h.RiderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BatterySwapHistory>()
            .HasOne(h => h.Station)
            .WithMany()
            .HasForeignKey(h => h.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BatterySwapHistory>()
            .HasOne(h => h.OldBattery)
            .WithMany()
            .HasForeignKey(h => h.OldBatteryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BatterySwapHistory>()
            .HasOne(h => h.NewBattery)
            .WithMany()
            .HasForeignKey(h => h.NewBatteryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Rider)
            .WithMany()
            .HasForeignKey(t => t.RiderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Trip>()
            .HasOne(t => t.Vehicle)
            .WithMany()
            .HasForeignKey(t => t.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Wallet>()
            .HasOne(w => w.User)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(w => w.UserId);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Wallet)
            .WithMany(w => w.Transactions)
            .HasForeignKey(t => t.WalletId);

        modelBuilder.Entity<Station>()
            .HasOne(s => s.Operator)
            .WithMany()
            .HasForeignKey(s => s.OperatorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FleetAssignment>()
            .HasOne(f => f.FleetManager)
            .WithMany()
            .HasForeignKey(f => f.FleetManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FleetAssignment>()
            .HasOne(f => f.Vehicle)
            .WithMany()
            .HasForeignKey(f => f.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FleetAssignment>()
            .HasOne(f => f.Driver)
            .WithMany()
            .HasForeignKey(f => f.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaintenanceRequest>()
            .HasOne(m => m.Battery)
            .WithMany()
            .HasForeignKey(m => m.BatteryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaintenanceRequest>()
            .HasOne(m => m.Engineer)
            .WithMany()
            .HasForeignKey(m => m.EngineerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SupportTicket>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
