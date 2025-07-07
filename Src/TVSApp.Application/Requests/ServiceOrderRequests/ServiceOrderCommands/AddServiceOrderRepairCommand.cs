using MediatR;
using TVS_App.Application.DTOs.ServiceOrder;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.ServiceOrderRequests.ServiceOrderCommands;

public record AddServiceOrderRepairCommand(long Id) : IRequest<BaseResponse<ServiceOrderDto?>>;