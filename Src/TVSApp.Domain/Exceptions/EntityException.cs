using TVS_App.Domain.Entities;

namespace TVS_App.Domain.Exceptions;

public class EntityException<T> : Exception where T : Entity
{
    public EntityException(string message) : base(message)
    {

    }
}