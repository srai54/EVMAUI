namespace EVSwap.API.Core.DTOs.Trip;

public class StartTripRequest
{
    public int VehicleId { get; set; }
    public double StartLat { get; set; }
    public double StartLng { get; set; }
}
