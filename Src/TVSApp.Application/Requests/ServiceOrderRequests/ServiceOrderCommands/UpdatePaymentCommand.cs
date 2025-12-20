using MediatR;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record UpdatePaymentCommand(long ServiceOrderId, long PaymentId, decimal Amount, DateTime PaymentDate) : IRequest<BaseResponse<string>>;