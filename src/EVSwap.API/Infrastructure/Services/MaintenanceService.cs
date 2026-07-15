using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Core.DTOs.Maintenance;
using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Repositories;

namespace EVSwap.API.Infrastructure.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IBatteryRepository _batteryRepository;

    public MaintenanceService(IMaintenanceRepository maintenanceRepository, IBatteryRepository batteryRepository)
    {
        _maintenanceRepository = maintenanceRepository;
        _batteryRepository = batteryRepository;
    }

    public async Task<IEnumerable<MaintenanceDto>> GetRequestsAsync()
    {
        var requests = await _maintenanceRepository.GetAllAsync();
        return requests.Select(MapToDto);
    }

    public async Task<MaintenanceDto> CreateRequestAsync(MaintenanceDto dto)
    {
        var battery = await _batteryRepository.GetByIdAsync(dto.BatteryId)
            ?? throw new KeyNotFoundException("Battery not found");

        var request = new MaintenanceRequest
        {
            BatteryId = dto.BatteryId,
            IssueType = dto.IssueType,
            Description = dto.Description,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        request = await _maintenanceRepository.AddAsync(request);
        return MapToDto(request);
    }

    public async Task<MaintenanceDto> ResolveRequestAsync(int id)
    {
        var request = await _maintenanceRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Maintenance request not found");

        request.Status = "Resolved";
        request.ResolvedAt = DateTime.UtcNow;
        await _maintenanceRepository.UpdateAsync(request);

        return MapToDto(request);
    }

    public async Task<IEnumerable<MaintenanceDto>> GetDiagnosticsAsync(int batteryId)
    {
        var requests = await _maintenanceRepository.GetByBatteryAsync(batteryId);
        return requests.Select(MapToDto);
    }

    private static MaintenanceDto MapToDto(MaintenanceRequest m) => new()
    {
        Id = m.Id,
        BatteryId = m.BatteryId,
        BatterySerial = m.Battery?.SerialNumber ?? "",
        EngineerId = m.EngineerId,
        EngineerName = "",
        IssueType = m.IssueType,
        Description = m.Description,
        Status = m.Status,
        CreatedAt = m.CreatedAt,
        ResolvedAt = m.ResolvedAt
    };
}
