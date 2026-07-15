using EVSwap.API.Core.Entities;

namespace EVSwap.API.Core.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserUnreadAsync(int userId);
}
