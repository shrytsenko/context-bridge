using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContextBridge.Import;
using ModelContextProtocol.Client;

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: ContextBridge.Import <bundle.json> [--url <mcp-url>] [--api-key <key>]");
    return 1;
}

var bundlePath = args[0];
var serverUrl = GetOption(args, "--url") ?? Environment.GetEnvironmentVariable("CONTEXTBRIDGE_URL") ?? "http://localhost:8081/mcp";
var apiKey = GetOption(args, "--api-key") ?? Environment.GetEnvironmentVariable("CONTEXTBRIDGE_API_KEY")
    ?? throw new InvalidOperationException("No API key given. Pass --api-key or set CONTEXTBRIDGE_API_KEY.");

var bundleJson = await File.ReadAllTextAsync(bundlePath);
var bundle = JsonSerializer.Deserialize<ImportBundle>(bundleJson, new JsonSerializerOptions(JsonSerializerDefaults.Web))
    ?? throw new InvalidOperationException($"Could not parse '{bundlePath}' as an import bundle.");

using var httpClient = new HttpClient();
var transport = new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Endpoint = new Uri(serverUrl),
        TransportMode = HttpTransportMode.StreamableHttp,
        AdditionalHeaders = new Dictionary<string, string> { ["X-Api-Key"] = apiKey }
    },
    httpClient,
    null,
    false);

await using var mcp = await McpClient.CreateAsync(transport);

Console.WriteLine($"Importing into project '{bundle.Project.Slug}' via {serverUrl} ...");

foreach (var entry in bundle.ContextEntries ?? [])
{
    var key = IdempotencyKey(bundle.Project.Slug, "context", entry.Title);
    var result = await mcp.CallToolAsync("save_context", new Dictionary<string, object?>
    {
        ["project"] = bundle.Project.Slug,
        ["type"] = entry.Type,
        ["title"] = entry.Title,
        ["content"] = entry.Content,
        ["actor"] = entry.Actor,
        ["conversationReference"] = entry.ConversationReference,
        ["idempotencyKey"] = key
    });
    Report("context entry", entry.Title, result);
}

foreach (var decision in bundle.Decisions ?? [])
{
    var key = IdempotencyKey(bundle.Project.Slug, "decision", decision.Title);
    var result = await mcp.CallToolAsync("save_decision", new Dictionary<string, object?>
    {
        ["project"] = bundle.Project.Slug,
        ["title"] = decision.Title,
        ["rationale"] = decision.Rationale,
        ["status"] = decision.Status,
        ["actor"] = decision.Actor,
        ["conversationReference"] = decision.ConversationReference,
        ["idempotencyKey"] = key
    });
    Report("decision", decision.Title, result);
}

foreach (var handoff in bundle.Handoffs ?? [])
{
    var key = IdempotencyKey(bundle.Project.Slug, "handoff", handoff.Title);
    var result = await mcp.CallToolAsync("create_handoff", new Dictionary<string, object?>
    {
        ["project"] = bundle.Project.Slug,
        ["title"] = handoff.Title,
        ["objective"] = handoff.Objective,
        ["acceptanceCriteria"] = handoff.AcceptanceCriteria,
        ["sourceAgent"] = handoff.SourceAgent,
        ["targetAgent"] = handoff.TargetAgent,
        ["idempotencyKey"] = key
    });
    Report("handoff", handoff.Title, result);
}

Console.WriteLine("Import complete.");

var snapshot = await mcp.CallToolAsync("get_project_context", new Dictionary<string, object?>
{
    ["project"] = bundle.Project.Slug
});
Console.WriteLine();
Console.WriteLine($"Current project context for '{bundle.Project.Slug}' (what Claude Code would see via get_project_context):");
Console.WriteLine(snapshot.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().First().Text);

return 0;

static string? GetOption(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string IdempotencyKey(string projectSlug, string kind, string title)
{
    var raw = $"import:{projectSlug}:{kind}:{title}";
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    return Convert.ToHexStringLower(hash)[..32];
}

static void Report(string kind, string title, ModelContextProtocol.Protocol.CallToolResult result)
{
    var isError = result.IsError ?? false;
    var text = string.Join(" ", result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Select(c => c.Text));
    var status = isError ? "FAILED" : "ok";
    Console.WriteLine($"  [{status}] {kind} '{title}': {(isError ? text : "saved")}");
}
