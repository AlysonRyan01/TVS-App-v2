using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Queries.ServiceOrdersQueries;

public record GetServiceOrdersBySerialNumberQuery(string SerialNumber) : IQuery
{
    public string NormalizedSerialNumber => SerialNumber.Trim().ToUpper();
    
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SerialNumber))
            throw new QueryException<GetServiceOrdersBySerialNumberQuery>("O número de série não pode ser vazio.");
    }
}