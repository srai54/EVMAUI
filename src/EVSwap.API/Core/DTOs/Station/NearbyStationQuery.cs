namespace EVSwap.API.Core.DTOs.Station;

public class NearbyStationQuery
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusKm { get; set; } = 10;
}
