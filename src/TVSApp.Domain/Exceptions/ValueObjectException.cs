using TVS_App.Domain.ValueObjects;

namespace TVS_App.Domain.Exceptions;

public class ValueObjectException<T> : ValueObjectException where T : ValueObject
{
    public ValueObjectException(string message) : base(message) { }
}

public abstract class ValueObjectException : Exception
{
    protected ValueObjectException(string message) : base(message) { }
}