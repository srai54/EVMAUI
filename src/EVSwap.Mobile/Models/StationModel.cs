namespace EVSwap.Mobile.Models;

public class StationModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int OperatorId { get; set; }
    public string Status { get; set; } = string.Empty;
    public double? DistanceKm { get; set; }
}
