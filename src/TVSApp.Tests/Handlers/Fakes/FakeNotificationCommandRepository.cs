using TVS_App.Domain.Entities;
using TVS_App.Domain.Repositories.Notifications;
using TVS_App.Domain.Shared;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeNotificationCommandRepository : INotificationCommandRepository
{
    public List<(string Title, string Message)> Created { get; } = new();

    public Task<BaseResponse<Notification>> Create(string title, string message)
    {
        Created.Add((title, message));
        return Task.FromResult(new BaseResponse<Notification>(null));
    }

    public Task<BaseResponse<Notification>> MarkAsRead(long id) => throw new NotImplementedException();
}
