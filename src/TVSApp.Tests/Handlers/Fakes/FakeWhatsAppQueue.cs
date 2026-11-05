using System.Threading.Channels;
using TVS_App.Application.Interfaces.Services;
using TVS_App.Application.Requests.WhatsAppQueueRequests;

namespace TVSApp.Tests.Handlers.Fakes;

public class FakeWhatsAppQueue : IWhatsAppQueue
{
    public List<IWhatsAppServiceRequest> Enqueued { get; } = new();

    public bool Enqueue(IWhatsAppServiceRequest job)
    {
        Enqueued.Add(job);
        return true;
    }

    public ChannelReader<IWhatsAppServiceRequest> Reader => throw new NotImplementedException();
}
