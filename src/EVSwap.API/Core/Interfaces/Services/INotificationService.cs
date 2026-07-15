using EVSwap.API.Core.DTOs.Notification;

namespace EVSwap.API.Core.Interfaces.Services;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int userId);
    Task MarkAsReadAsync(int notificationId);
    Task<NotificationDto> CreateNotificationAsync(int userId, string title, string message, string type);
}
