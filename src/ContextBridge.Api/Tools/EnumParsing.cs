using ContextBridge.Domain.Exceptions;

namespace ContextBridge.Api.Tools;

internal static class EnumParsing
{
    public static TEnum Parse<TEnum>(string value, string fieldName) where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
        {
            var allowed = string.Join(", ", Enum.GetNames<TEnum>());
            throw new DomainValidationException($"'{value}' is not a valid {fieldName}. Allowed values: {allowed}.");
        }

        return result;
    }
}
