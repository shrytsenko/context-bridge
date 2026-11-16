using ContextBridge.Domain.Enums;

namespace ContextBridge.Domain.ValueObjects;

public sealed record Source(Actor Actor, string? ConversationReference, DateTimeOffset CapturedAt);
