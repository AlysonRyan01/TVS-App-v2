using TVS_App.Domain.Entities;

namespace TVS_App.Domain.Exceptions;

public class EntityException<T> : EntityException where T : Entity
{
    public EntityException(string message) : base(message) { }
}

public abstract class EntityException : Exception
{
    protected EntityException(string message) : base(message) { }
}