using EVSwap.API.Core.DTOs.Fleet;

namespace EVSwap.API.Core.Interfaces.Services;

public interface IFleetService
{
    Task<IEnumerable<FleetDto>> GetVehiclesAsync(int managerId);
    Task<FleetDto> AssignDriverAsync(int managerId, FleetDto fleetDto);
}
