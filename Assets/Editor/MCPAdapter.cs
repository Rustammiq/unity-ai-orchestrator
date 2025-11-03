using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// MCP Adapter that implements ILLMProvider interface for integration with the orchestrator.
/// Connects to MCP servers via stdio or HTTP and forwards prompts.
/// </summary>
public class MCPAdapter : ILLMProvider
{
    private readonly MCPClient client;
    private readonly string serverName;
    public string ProviderName => $"MCP-{serverName}";

    public MCPAdapter(string serverCommand = null, string serverArgs = null, string serverUrl = null, string serverName = "Server", Dictionary<string, string> envVars = null)
    {
        this.serverName = serverName;
        this.client = new MCPClient(serverCommand, serverArgs, serverUrl, envVars);
    }

    public async Task<string> SendPromptAsync(string prompt, CancellationToken ct = default)
    {
        try
        {
            // Ensure connection is initialized
            if (!client.IsConnected)
            {
                var connected = await client.InitializeAsync(ct);
                if (!connected)
                {
                    return $"Error: Failed to connect to MCP server {serverName}";
                }
            }

            // Send prompt to MCP server
            var response = await client.SendPromptAsync(prompt, ct);
            
            if (string.IsNullOrEmpty(response))
            {
                return $"No response from MCP server {serverName}";
            }

            return response;
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPAdapter error ({serverName}): {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }

    public void Disconnect()
    {
        client?.Disconnect();
    }
}
