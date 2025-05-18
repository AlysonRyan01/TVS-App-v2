using TVS_App.Domain.ValueObjects;

namespace TVS_App.Domain.Exceptions;

public class ValueObjectException<T>  :  Exception where T : ValueObject
{
    public ValueObjectException(string message) : base(message)
    {
        
    }
}