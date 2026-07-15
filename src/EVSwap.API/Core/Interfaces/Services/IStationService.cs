using EVSwap.API.Core.DTOs.Station;

namespace EVSwap.API.Core.Interfaces.Services;

public interface IStationService
{
    Task<IEnumerable<StationDto>> GetNearbyAsync(NearbyStationQuery query);
    Task<StationDto> GetByIdAsync(int id);
    Task<StationDto> CreateAsync(StationDto stationDto);
    Task<StationDto> UpdateAsync(int id, StationDto stationDto);
    Task DeleteAsync(int id);
}
