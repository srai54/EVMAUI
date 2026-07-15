namespace EVSwap.API.Core.DTOs.Trip;

public class EndTripRequest
{
    public int TripId { get; set; }
    public double EndLat { get; set; }
    public double EndLng { get; set; }
}
