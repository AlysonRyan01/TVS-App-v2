using TVS_App.Application.Queries;

namespace TVS_App.Application.Exceptions;

public class QueryException <T> : QueryException where T : IQuery
{
    public QueryException(string message) : base (message) { }
}

public abstract class QueryException : Exception
{
    protected QueryException(string message) : base (message) { }
}