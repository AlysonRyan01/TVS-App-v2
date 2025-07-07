using MediatR;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record RegenerateAndReturnPdfCommand(long Id) : IRequest<BaseResponse<byte[]>>;