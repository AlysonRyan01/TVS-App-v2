using System.Threading.Channels;
using TVS_App.Application.Requests.EmailQueueRequests;

namespace TVS_App.Application.Interfaces.Services;

public interface IEmailQueue
{
    bool Enqueue(IEmailServiceRequest job);
    ChannelReader<IEmailServiceRequest> Reader { get; }
}