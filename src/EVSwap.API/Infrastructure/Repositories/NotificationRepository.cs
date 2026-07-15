using EVSwap.API.Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using EVSwap.API.Infrastructure.Data;
using EVSwap.API.Core.Entities;

namespace EVSwap.API.Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Notification>> GetByUserUnreadAsync(int userId)
        => await _dbSet.Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
}
