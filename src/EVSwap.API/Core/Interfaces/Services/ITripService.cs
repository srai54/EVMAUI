using EVSwap.API.Core.DTOs.Trip;

namespace EVSwap.API.Core.Interfaces.Services;

public interface ITripService
{
    Task<TripDto> StartTripAsync(int userId, StartTripRequest request);
    Task<TripDto> EndTripAsync(int userId, EndTripRequest request);
    Task<IEnumerable<TripDto>> GetHistoryAsync(int userId);
    Task<TripDto?> GetActiveTripAsync(int userId);
}
