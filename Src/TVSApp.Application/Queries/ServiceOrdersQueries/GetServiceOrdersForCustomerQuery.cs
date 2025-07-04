using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Queries.ServiceOrdersQueries;

public record GetServiceOrdersForCustomerQuery(long ServiceOrderId, string SecurityCode) : IQuery
{
    public string NormalizedSecurityCode => SecurityCode.Trim().ToUpper();
    
    public void Validate()
    {
        if (string.IsNullOrEmpty(SecurityCode))
            throw new QueryException<GetServiceOrdersForCustomerQuery>("O código de segurança não pode ser vazio");
    }
}