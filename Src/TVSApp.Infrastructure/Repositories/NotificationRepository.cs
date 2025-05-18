using Microsoft.EntityFrameworkCore;
using TVS_App.Application.DTOs;
using TVS_App.Application.Repositories;
using TVS_App.Domain.Entities;
using TVS_App.Infrastructure.Data;
using TVS_App.Infrastructure.Exceptions;

namespace TVS_App.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDataContext _context;

    public NotificationRepository(ApplicationDataContext context)
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

    public async Task<BaseResponse<List<Notification>>> GetUnread()
    {
        try
        {
            var fiveDaysAgo = DateTime.Now.AddDays(-5);

            var unread = await _context.Notifications
                .Where(n => !n.IsRead && n.CreatedAt >= fiveDaysAgo)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return new BaseResponse<List<Notification>>(unread, 200, "Notificações não lidas obtidas com sucesso");
        }
        catch (Exception ex)
        {
            return DbExceptionHandler.Handle<List<Notification>>(ex);
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