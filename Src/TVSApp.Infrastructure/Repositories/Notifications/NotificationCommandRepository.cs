using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Data;
using TVS_App.Infrastructure.Exceptions;

namespace TVS_App.Infrastructure.Repositories.Notifications;

public class NotificationCommandRepository : INotificationCommandRepository
{
    private readonly ApplicationDataContext _context;

    public NotificationCommandRepository(ApplicationDataContext context)
    {
        _context = context;
    }
    
    public async Task<BaseResponse<Notification>> Create(string title, string message)
    {
        try
        {
            var notification = new Notification(title, message);
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return new BaseResponse<Notification>(notification, 201, "Notificação criada com sucesso");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<Notification>(ex);
        }
    }

    public async Task<BaseResponse<Notification>> MarkAsRead(long id)
    {
        try
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return new BaseResponse<Notification>(null, 404, "Notificação não encontrada");

            notification.MarkAsRead();
            await _context.SaveChangesAsync();
            return new BaseResponse<Notification>(notification, 200, "Notificação marcada como lida");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<Notification>(ex);
        }
    }
}