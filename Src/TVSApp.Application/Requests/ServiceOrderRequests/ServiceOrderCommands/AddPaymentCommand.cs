using MediatR;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record AddPaymentCommand(long ServiceOrderId, decimal Amount, DateTime PaymentDate) : IRequest<BaseResponse<string>>;