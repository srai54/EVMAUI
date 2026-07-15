using EVSwap.API.Core.Entities;

namespace EVSwap.API.Core.Interfaces.Repositories;

public interface IFleetRepository : IRepository<FleetAssignment>
{
    Task<IEnumerable<FleetAssignment>> GetByManagerAsync(int managerId);
}
