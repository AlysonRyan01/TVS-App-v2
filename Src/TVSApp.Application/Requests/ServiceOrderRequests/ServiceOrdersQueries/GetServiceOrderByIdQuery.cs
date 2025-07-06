using TVS_App.Application.Exceptions;
using TVS_App.Application.Requests;

namespace TVS_App.Application.Queries.ServiceOrdersQueries;

public record GetServiceOrderByIdQuery(long Id) : IQuery
{
    public void Validate()
    {
        if (Id == 0)
            throw new QueryException<GetServiceOrderByIdQuery>("O Id da ordem de serviço não pode ser 0");
    }
}
