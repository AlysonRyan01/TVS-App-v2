using System.Threading.Channels;
using TVS_App.Application.Requests.WhatsAppQueueRequests;

namespace TVS_App.Application.Interfaces.Services;

public interface IWhatsAppQueue
{
    bool Enqueue(IWhatsAppServiceRequest job);
    ChannelReader<IWhatsAppServiceRequest> Reader { get; }
}