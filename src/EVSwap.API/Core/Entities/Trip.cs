namespace EVSwap.API.Core.Entities;

public class Trip
{
    public int Id { get; set; }
    public int RiderId { get; set; }
    public int VehicleId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public double StartLat { get; set; }
    public double StartLng { get; set; }
    public double? EndLat { get; set; }
    public double? EndLng { get; set; }
    public double? DistanceKm { get; set; }

    public User Rider { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
}
