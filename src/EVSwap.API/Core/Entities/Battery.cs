namespace EVSwap.API.Core.Entities;

public class Battery
{
    public int Id { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string QRCode { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public double Capacity { get; set; }
    public string Status { get; set; } = "Available";
    public double ChargeLevel { get; set; }
    public int ChargeCycles { get; set; }
    public double Temperature { get; set; }
    public double Voltage { get; set; }
    public DateTime InstallDate { get; set; }
    public DateTime WarrantyExpiry { get; set; }
    public DateTime? LastMaintenance { get; set; }

    public ICollection<BatteryHealth> HealthRecords { get; set; } = new List<BatteryHealth>();
}
