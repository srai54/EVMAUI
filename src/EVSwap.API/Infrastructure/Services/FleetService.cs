using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Core.DTOs.Fleet;
using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Repositories;

namespace EVSwap.API.Infrastructure.Services;

public class FleetService : IFleetService
{
    private readonly IFleetRepository _fleetRepository;

    public FleetService(IFleetRepository fleetRepository)
    {
        _fleetRepository = fleetRepository;
    }

    public async Task<IEnumerable<FleetDto>> GetVehiclesAsync(int managerId)
    {
        var assignments = await _fleetRepository.GetByManagerAsync(managerId);
        return assignments.Select(a => new FleetDto
        {
            Id = a.Id,
            FleetManagerId = a.FleetManagerId,
            FleetManagerName = "",
            VehicleId = a.VehicleId,
            VehicleRegNumber = a.Vehicle?.RegNumber ?? "",
            DriverId = a.DriverId,
            DriverName = a.Driver?.Username ?? "",
            AssignedAt = a.AssignedAt,
            Status = a.Status
        });
    }

    public async Task<FleetDto> AssignDriverAsync(int managerId, FleetDto fleetDto)
    {
        var assignment = new FleetAssignment
        {
            FleetManagerId = managerId,
            VehicleId = fleetDto.VehicleId,
            DriverId = fleetDto.DriverId,
            AssignedAt = DateTime.UtcNow,
            Status = "Active"
        };

        assignment = await _fleetRepository.AddAsync(assignment);

        return new FleetDto
        {
            Id = assignment.Id,
            FleetManagerId = assignment.FleetManagerId,
            VehicleId = assignment.VehicleId,
            DriverId = assignment.DriverId,
            AssignedAt = assignment.AssignedAt,
            Status = assignment.Status
        };
    }
}
