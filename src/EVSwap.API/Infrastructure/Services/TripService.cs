using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Core.DTOs.Trip;
using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Repositories;
using EVSwap.API.Infrastructure.Utilities;

namespace EVSwap.API.Infrastructure.Services;

public class TripService : ITripService
{
    private readonly ITripRepository _tripRepository;

    public TripService(ITripRepository tripRepository)
    {
        _tripRepository = tripRepository;
    }

    public async Task<TripDto> StartTripAsync(int userId, StartTripRequest request)
    {
        var activeTrip = await _tripRepository.GetActiveTripAsync(userId);
        if (activeTrip != null)
            throw new InvalidOperationException("You already have an active trip");

        var trip = new Trip
        {
            RiderId = userId,
            VehicleId = request.VehicleId,
            StartTime = DateTime.UtcNow,
            StartLat = request.StartLat,
            StartLng = request.StartLng
        };

        trip = await _tripRepository.AddAsync(trip);

        return new TripDto
        {
            Id = trip.Id,
            RiderId = trip.RiderId,
            VehicleId = trip.VehicleId,
            StartTime = trip.StartTime,
            StartLat = trip.StartLat,
            StartLng = trip.StartLng
        };
    }

    public async Task<TripDto> EndTripAsync(int userId, EndTripRequest request)
    {
        var trip = await _tripRepository.GetByIdAsync(request.TripId)
            ?? throw new KeyNotFoundException("Trip not found");

        if (trip.RiderId != userId)
            throw new UnauthorizedAccessException("This trip does not belong to you");

        if (trip.EndTime != null)
            throw new InvalidOperationException("Trip already ended");

        trip.EndTime = DateTime.UtcNow;
        trip.EndLat = request.EndLat;
        trip.EndLng = request.EndLng;
        trip.DistanceKm = DistanceHelper.CalculateDistance(trip.StartLat, trip.StartLng, request.EndLat, request.EndLng);

        await _tripRepository.UpdateAsync(trip);

        return MapToDto(trip);
    }

    public async Task<IEnumerable<TripDto>> GetHistoryAsync(int userId)
    {
        var trips = await _tripRepository.GetByUserAsync(userId);
        return trips.Select(MapToDto);
    }

    public async Task<TripDto?> GetActiveTripAsync(int userId)
    {
        var trip = await _tripRepository.GetActiveTripAsync(userId);
        return trip != null ? MapToDto(trip) : null;
    }

    private static TripDto MapToDto(Trip t) => new()
    {
        Id = t.Id,
        RiderId = t.RiderId,
        VehicleId = t.VehicleId,
        VehicleRegNumber = t.Vehicle?.RegNumber ?? "",
        StartTime = t.StartTime,
        EndTime = t.EndTime,
        StartLat = t.StartLat,
        StartLng = t.StartLng,
        EndLat = t.EndLat,
        EndLng = t.EndLng,
        DistanceKm = t.DistanceKm
    };
}
