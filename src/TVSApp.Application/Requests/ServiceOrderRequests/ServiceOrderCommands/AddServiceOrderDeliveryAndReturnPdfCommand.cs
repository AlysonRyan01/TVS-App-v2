using MediatR;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record AddServiceOrderDeliveryAndReturnPdfCommand(long Id) : IRequest<BaseResponse<byte[]>>;