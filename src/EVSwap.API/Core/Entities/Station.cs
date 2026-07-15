namespace EVSwap.API.Core.Entities;

public class Station
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? OperatorId { get; set; }
    public string Status { get; set; } = "Active";

    public User? Operator { get; set; }
}
