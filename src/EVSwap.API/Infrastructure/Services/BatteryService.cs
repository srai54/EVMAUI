using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Core.DTOs.Battery;
using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Repositories;

namespace EVSwap.API.Infrastructure.Services;

public class BatteryService : IBatteryService
{
    private readonly IBatteryRepository _batteryRepository;

    public BatteryService(IBatteryRepository batteryRepository)
    {
        _batteryRepository = batteryRepository;
    }

    public async Task<BatteryDto> GetByIdAsync(int id)
    {
        var battery = await _batteryRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Battery not found");
        return MapToDto(battery);
    }

    public async Task<IEnumerable<BatteryDto>> GetAllAsync()
    {
        var batteries = await _batteryRepository.GetAllAsync();
        return batteries.Select(MapToDto);
    }

    public async Task<IEnumerable<BatteryDto>> GetNearbyAsync(int stationId)
    {
        var batteries = await _batteryRepository.GetByStationAsync(stationId);
        return batteries.Select(MapToDto);
    }

    public async Task<BatteryDto> UpdateStatusAsync(int id, string status)
    {
        var battery = await _batteryRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Battery not found");
        battery.Status = status;
        await _batteryRepository.UpdateAsync(battery);
        return MapToDto(battery);
    }

    public async Task<BatteryHealthDto> RecordHealthAsync(int batteryId, BatteryHealthDto healthDto)
    {
        var battery = await _batteryRepository.GetByIdAsync(batteryId)
            ?? throw new KeyNotFoundException("Battery not found");

        var health = new BatteryHealth
        {
            BatteryId = batteryId,
            Timestamp = DateTime.UtcNow,
            ChargeLevel = healthDto.ChargeLevel,
            Temperature = healthDto.Temperature,
            Voltage = healthDto.Voltage,
            CycleCount = healthDto.CycleCount,
            Notes = healthDto.Notes
        };

        battery.Temperature = healthDto.Temperature;
        battery.Voltage = healthDto.Voltage;
        battery.ChargeLevel = healthDto.ChargeLevel;
        battery.LastMaintenance = DateTime.UtcNow;
        await _batteryRepository.UpdateAsync(battery);

        return healthDto;
    }

    private static BatteryDto MapToDto(Battery b) => new()
    {
        Id = b.Id,
        SerialNumber = b.SerialNumber,
        QRCode = b.QRCode,
        Manufacturer = b.Manufacturer,
        Capacity = b.Capacity,
        Status = b.Status,
        ChargeLevel = b.ChargeLevel,
        ChargeCycles = b.ChargeCycles,
        Temperature = b.Temperature,
        Voltage = b.Voltage,
        InstallDate = b.InstallDate,
        WarrantyExpiry = b.WarrantyExpiry,
        LastMaintenance = b.LastMaintenance
    };
}
