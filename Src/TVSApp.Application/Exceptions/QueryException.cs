namespace TVS_App.Application.Exceptions;

public class QueryException <T> : QueryException
{
    public QueryException(string message) : base (message) { }
}

public abstract class QueryException : Exception
{
    protected QueryException(string message) : base (message) { }
}