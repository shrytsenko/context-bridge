using System.ComponentModel;
using ModelContextProtocol.Server;

namespace ContextBridge.Api.Tools;

[McpServerToolType]
public static class SmokeTestTool
{
    [McpServerTool(Name = "ping")]
    [Description("Smoke-test tool: confirms the ContextBridge MCP server is reachable.")]
    public static string Ping() => "pong";
}
