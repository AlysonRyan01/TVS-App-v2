using Microsoft.EntityFrameworkCore;
using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Shared;
using TVS_App.Infrastructure.Data;

namespace TVS_App.Infrastructure.Repositories.Notifications;

public class NotificationQueryRepository : INotificationQueryRepository
{
    private readonly ApplicationDataContext _context;

    public NotificationQueryRepository(ApplicationDataContext context)
    {
        _context = context;
    }
    
    public async Task<BaseResponse<List<Notification>>> GetUnread()
    {
        var fiveDaysAgo = DateTime.Now.AddDays(-5);

        var unread = await _context.Notifications
            .AsNoTracking()
            .Where(n => !n.IsRead && n.CreatedAt >= fiveDaysAgo)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return new BaseResponse<List<Notification>>(unread, 200, "Notificações não lidas obtidas com sucesso");
    }
}