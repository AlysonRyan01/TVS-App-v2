using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Queries.CustomersQueries;

public record GetCustomerByIdQuery(long Id) : IQuery
{
    public void Validate()
    {
        if (Id == 0)
            throw new QueryException<GetCustomerByIdQuery>("O Id do cliente não pode ser 0");
    }
}