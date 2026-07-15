namespace EVSwap.Mobile.Models;

public class TripModel
{
    public int Id { get; set; }
    public int RiderId { get; set; }
    public int VehicleId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double StartLat { get; set; }
    public double StartLng { get; set; }
    public double? EndLat { get; set; }
    public double? EndLng { get; set; }
    public double DistanceKm { get; set; }
}
