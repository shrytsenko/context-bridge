namespace ContextBridge.Api.Authentication;

public sealed class ApiKeyMiddleware
{
    private const string HeaderName = "X-Api-Key";

    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var configuredKey = configuration["Authentication:ApiKey"];
        var providedKey = context.Request.Headers[HeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(configuredKey) || providedKey != configuredKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await _next(context);
    }
}
