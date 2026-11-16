namespace ContextBridge.Domain.Exceptions;

public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityType, object id)
        : base($"{entityType} '{id}' was not found.")
    {
    }
}
