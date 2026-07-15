namespace EVSwap.API.Core.DTOs.Trip;

public class TripDto
{
    public int Id { get; set; }
    public int RiderId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleRegNumber { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double StartLat { get; set; }
    public double StartLng { get; set; }
    public double? EndLat { get; set; }
    public double? EndLng { get; set; }
    public double? DistanceKm { get; set; }
    public bool IsActive => EndTime == null;
}
