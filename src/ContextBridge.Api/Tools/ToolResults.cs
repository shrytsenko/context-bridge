using System.Text.Json;
using ContextBridge.Domain.Exceptions;
using ModelContextProtocol.Protocol;

namespace ContextBridge.Api.Tools;

internal static class ToolResults
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static CallToolResult Success<T>(T payload)
    {
        return new CallToolResult
        {
            StructuredContent = JsonSerializer.SerializeToElement(payload, JsonOptions),
            Content = [new TextContentBlock { Text = JsonSerializer.Serialize(payload, JsonOptions) }]
        };
    }

    public static CallToolResult FromDomainException(DomainException ex)
    {
        var code = ex switch
        {
            EntityNotFoundException => "NOT_FOUND",
            ConcurrencyConflictException => "VERSION_CONFLICT",
            DomainValidationException => "VALIDATION_ERROR",
            _ => "VALIDATION_ERROR"
        };

        var payload = new { error = new { code, message = ex.Message } };
        return new CallToolResult
        {
            IsError = true,
            Content = [new TextContentBlock { Text = JsonSerializer.Serialize(payload, JsonOptions) }]
        };
    }
}
