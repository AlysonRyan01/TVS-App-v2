using MediatR;
using TVS_App.Application.DTOs;
using TVS_App.Domain.Shared;

namespace TVS_App.Application.Requests.CustomerRequests.CustomersQueries;

public record GetCustomerByNameQuery(string Name) : IRequest<BaseResponse<List<CustomerDto>>>;