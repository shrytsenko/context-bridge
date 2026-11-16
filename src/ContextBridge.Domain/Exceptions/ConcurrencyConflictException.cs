namespace ContextBridge.Domain.Exceptions;

public sealed class ConcurrencyConflictException : DomainException
{
    public ConcurrencyConflictException(string entityType, object id, int expectedVersion, int actualVersion)
        : base($"{entityType} '{id}' version conflict: expected {expectedVersion}, actual {actualVersion}.")
    {
    }
}
