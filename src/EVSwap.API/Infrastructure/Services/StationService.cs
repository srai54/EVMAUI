using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Core.DTOs.Station;
using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Repositories;

namespace EVSwap.API.Infrastructure.Services;

public class StationService : IStationService
{
    private readonly IStationRepository _stationRepository;

    public StationService(IStationRepository stationRepository)
    {
        _stationRepository = stationRepository;
    }

    public async Task<IEnumerable<StationDto>> GetNearbyAsync(NearbyStationQuery query)
    {
        var stations = await _stationRepository.GetNearbyAsync(query.Latitude, query.Longitude, query.RadiusKm);
        return stations.Select(MapToDto);
    }

    public async Task<StationDto> GetByIdAsync(int id)
    {
        var station = await _stationRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Station not found");
        return MapToDto(station);
    }

    public async Task<StationDto> CreateAsync(StationDto stationDto)
    {
        var station = new Station
        {
            Name = stationDto.Name,
            Address = stationDto.Address,
            Latitude = stationDto.Latitude,
            Longitude = stationDto.Longitude,
            OperatorId = stationDto.OperatorId,
            Status = stationDto.Status
        };
        station = await _stationRepository.AddAsync(station);
        return MapToDto(station);
    }

    public async Task<StationDto> UpdateAsync(int id, StationDto stationDto)
    {
        var station = await _stationRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Station not found");

        station.Name = stationDto.Name;
        station.Address = stationDto.Address;
        station.Latitude = stationDto.Latitude;
        station.Longitude = stationDto.Longitude;
        station.OperatorId = stationDto.OperatorId;
        station.Status = stationDto.Status;

        await _stationRepository.UpdateAsync(station);
        return MapToDto(station);
    }

    public async Task DeleteAsync(int id)
    {
        await _stationRepository.DeleteAsync(id);
    }

    private static StationDto MapToDto(Station s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Address = s.Address,
        Latitude = s.Latitude,
        Longitude = s.Longitude,
        OperatorId = s.OperatorId,
        Status = s.Status
    };
}
