using EVSwap.API.Core.Interfaces.Services;
using EVSwap.API.Core.DTOs.Notification;
using EVSwap.API.Core.Entities;
using EVSwap.API.Core.Interfaces.Repositories;

namespace EVSwap.API.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int userId)
    {
        var notifications = await _notificationRepository.FindAsync(n => n.UserId == userId);
        return notifications.OrderByDescending(n => n.CreatedAt).Select(MapToDto);
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId)
            ?? throw new KeyNotFoundException("Notification not found");
        notification.IsRead = true;
        await _notificationRepository.UpdateAsync(notification);
    }

    public async Task<NotificationDto> CreateNotificationAsync(int userId, string title, string message, string type)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };
        notification = await _notificationRepository.AddAsync(notification);
        return MapToDto(notification);
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        Title = n.Title,
        Message = n.Message,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
        Type = n.Type
    };
}
