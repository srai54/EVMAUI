using EVSwap.API.Core.DTOs.Report;

namespace EVSwap.API.Core.Interfaces.Services;

public interface IReportService
{
    Task<DashboardDto> GetDashboardAsync();
    Task<UserDashboardDto> GetUserDashboardAsync(int userId);
}
