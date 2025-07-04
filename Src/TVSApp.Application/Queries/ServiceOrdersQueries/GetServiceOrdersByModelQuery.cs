using TVS_App.Application.Exceptions;

namespace TVS_App.Application.Queries.ServiceOrdersQueries;

public record GetServiceOrdersByModelQuery(string Model) : IQuery
{
    public string NormalizedModel => Model.Trim().ToUpper();

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Model))
            throw new QueryException<GetServiceOrdersByModelQuery>("O modelo não pode ser vazio.");
    }
}