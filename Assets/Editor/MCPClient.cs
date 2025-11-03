using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

/// <summary>
/// MCP (Model Context Protocol) Client implementation for Unity.
/// Supports JSON-RPC 2.0 communication with MCP servers via stdio or HTTP.
/// </summary>
public class MCPClient
{
    private readonly string serverCommand;
    private readonly string serverArgs;
    private readonly string serverUrl; // For HTTP-based MCP servers
    private readonly bool useStdio;
    private System.Diagnostics.Process stdioProcess;
    private readonly Queue<string> responseQueue = new Queue<string>();
    private int requestIdCounter = 1;
    private readonly object lockObject = new object();

    public bool IsConnected { get; private set; }
    public string ClientName => "Unity-AI-Orchestrator";

    public MCPClient(string serverCommand = null, string serverArgs = null, string serverUrl = null)
    {
        this.serverCommand = serverCommand;
        this.serverArgs = serverArgs;
        this.serverUrl = serverUrl;
        this.useStdio = !string.IsNullOrEmpty(serverCommand);
    }

    /// <summary>
    /// Initialize MCP connection (stdio or HTTP)
    /// </summary>
    public async Task<bool> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            if (useStdio)
            {
                return await InitializeStdioAsync(ct);
            }
            else if (!string.IsNullOrEmpty(serverUrl))
            {
                return await InitializeHttpAsync(ct);
            }
            else
            {
                Debug.LogError("MCPClient: No server command or URL provided");
                return false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient initialization failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> InitializeStdioAsync(CancellationToken ct)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = serverCommand,
                Arguments = serverArgs ?? "",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            stdioProcess = System.Diagnostics.Process.Start(startInfo);
            if (stdioProcess == null)
            {
                Debug.LogError("MCPClient: Failed to start stdio process");
                return false;
            }

            // Start reading output
            _ = Task.Run(() => ReadStdioOutput(), ct);

            // Initialize protocol handshake
            var initRequest = new MCPRequest
            {
                jsonrpc = "2.0",
                id = requestIdCounter++,
                method = "initialize",
                @params = new Dictionary<string, object>
                {
                    ["protocolVersion"] = "2024-11-05",
                    ["capabilities"] = new Dictionary<string, object>
                    {
                        ["tools"] = new Dictionary<string, object>(),
                        ["resources"] = new Dictionary<string, object>()
                    },
                    ["clientInfo"] = new Dictionary<string, object>
                    {
                        ["name"] = ClientName,
                        ["version"] = "1.0.0"
                    }
                }
            };

            var initResponse = await SendRequestAsync(initRequest, ct);
            if (initResponse?.result != null)
            {
                // Send initialized notification
                await SendNotificationAsync(new MCPRequest
                {
                    jsonrpc = "2.0",
                    method = "notifications/initialized"
                }, ct);

                IsConnected = true;
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient stdio init failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> InitializeHttpAsync(CancellationToken ct)
    {
        // For HTTP-based MCP servers (simpler implementation)
        IsConnected = true;
        return true;
    }

    private async Task ReadStdioOutput()
    {
        try
        {
            while (!stdioProcess.HasExited && stdioProcess.StandardOutput != null)
            {
                var line = await stdioProcess.StandardOutput.ReadLineAsync();
                if (line == null) break;

                lock (lockObject)
                {
                    responseQueue.Enqueue(line);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient stdio read error: {ex.Message}");
        }
    }

    /// <summary>
    /// List available tools from MCP server
    /// </summary>
    public async Task<List<MCPTool>> ListToolsAsync(CancellationToken ct = default)
    {
        var request = new MCPRequest
        {
            jsonrpc = "2.0",
            id = requestIdCounter++,
            method = "tools/list"
        };

        var response = await SendRequestAsync(request, ct);
        if (response?.result != null && response.result.ContainsKey("tools"))
        {
            // Parse tools (simplified)
            var tools = new List<MCPTool>();
            // In a full implementation, you'd parse the JSON properly
            return tools;
        }
        return new List<MCPTool>();
    }

    /// <summary>
    /// Call a tool on the MCP server
    /// </summary>
    public async Task<MCPResponse> CallToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken ct = default)
    {
        var request = new MCPRequest
        {
            jsonrpc = "2.0",
            id = requestIdCounter++,
            method = "tools/call",
            @params = new Dictionary<string, object>
            {
                ["name"] = toolName,
                ["arguments"] = arguments ?? new Dictionary<string, object>()
            }
        };

        return await SendRequestAsync(request, ct);
    }

    /// <summary>
    /// Send prompt to MCP server (if it supports prompts)
    /// </summary>
    public async Task<string> SendPromptAsync(string prompt, CancellationToken ct = default)
    {
        try
        {
            // Try to list tools first to see what's available
            var tools = await ListToolsAsync(ct);
            
            // Try calling a generic prompt processing tool
            // Many MCP servers support a "prompt" or "process" tool
            var toolNames = new[] { "prompt", "process_prompt", "generate", "chat" };
            MCPResponse response = null;
            
            foreach (var toolName in toolNames)
            {
                try
                {
                    response = await CallToolAsync(toolName, new Dictionary<string, object> { ["prompt"] = prompt }, ct);
                    if (response != null) break;
                }
                catch
                {
                    // Try next tool
                    continue;
                }
            }
            
            if (response == null)
            {
                // If no tool found, try direct prompt via tools/call with a generic approach
                response = await CallToolAsync("prompt", new Dictionary<string, object> { ["message"] = prompt }, ct);
            }
            
            if (response?.result != null)
            {
                // Try to extract content from response
                if (response.result.ContainsKey("content"))
                {
                    var content = response.result["content"];
                    if (content is List<object> contentList && contentList.Count > 0)
                    {
                        var firstItem = contentList[0];
                        if (firstItem is Dictionary<string, object> contentDict && contentDict.ContainsKey("text"))
                        {
                            return contentDict["text"].ToString();
                        }
                    }
                }
                
                // Fallback: try to get text directly
                if (response.result.ContainsKey("text"))
                {
                    return response.result["text"].ToString();
                }
                
                // Last resort: return the full result as string
                return response.result.ToString();
            }

            return response != null ? "MCP server responded but no content found" : "No response from MCP server";
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient SendPromptAsync error: {ex.Message}");
            return $"Error sending prompt to MCP server: {ex.Message}";
        }
    }

    private async Task<MCPResponse> SendRequestAsync(MCPRequest request, CancellationToken ct)
    {
        try
        {
            if (useStdio)
            {
                return await SendStdioRequestAsync(request, ct);
            }
            else
            {
                return await SendHttpRequestAsync(request, ct);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient send request failed: {ex.Message}");
            return null;
        }
    }

    private async Task<MCPResponse> SendStdioRequestAsync(MCPRequest request, CancellationToken ct)
    {
        // Build JSON manually for Unity compatibility (JsonUtility doesn't handle Dictionary<string, object> well)
        var json = BuildJsonRequest(request);
        var id = request.id;

        lock (lockObject)
        {
            stdioProcess.StandardInput.WriteLine(json);
            stdioProcess.StandardInput.Flush();
        }

        // Wait for response
        var timeout = DateTime.Now.AddSeconds(30);
        while (DateTime.Now < timeout && !ct.IsCancellationRequested)
        {
            await Task.Yield();
            
            lock (lockObject)
            {
                if (responseQueue.Count > 0)
                {
                    var responseJson = responseQueue.Dequeue();
                    try
                    {
                        // Try to parse JSON response
                        var response = ParseJsonResponse(responseJson);
                        if (response?.id == id)
                        {
                            return response;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"MCPClient: Failed to parse response: {ex.Message}");
                    }
                }
            }
        }

        return null;
    }

    private string BuildJsonRequest(MCPRequest request)
    {
        // Simple JSON builder for Unity compatibility
        var sb = new System.Text.StringBuilder();
        sb.Append("{");
        sb.Append($"\"jsonrpc\":\"{request.jsonrpc}\"");
        if (request.id.HasValue)
        {
            sb.Append($",\"id\":{request.id.Value}");
        }
        sb.Append($",\"method\":\"{request.method}\"");
        if (request.@params != null && request.@params.Count > 0)
        {
            sb.Append(",\"params\":{");
            var first = true;
            foreach (var kvp in request.@params)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{kvp.Key}\":");
                if (kvp.Value is string str)
                {
                    sb.Append($"\"{str}\"");
                }
                else if (kvp.Value is Dictionary<string, object> dict)
                {
                    sb.Append("{");
                    var dictFirst = true;
                    foreach (var dkvp in dict)
                    {
                        if (!dictFirst) sb.Append(",");
                        sb.Append($"\"{dkvp.Key}\":");
                        if (dkvp.Value is string dstr)
                        {
                            sb.Append($"\"{dstr}\"");
                        }
                        else
                        {
                            sb.Append(dkvp.Value?.ToString() ?? "null");
                        }
                        dictFirst = false;
                    }
                    sb.Append("}");
                }
                else
                {
                    sb.Append(kvp.Value?.ToString() ?? "null");
                }
                first = false;
            }
            sb.Append("}");
        }
        sb.Append("}");
        return sb.ToString();
    }

    private MCPResponse ParseJsonResponse(string json)
    {
        // Simple JSON parser for Unity - in a production environment, consider using a proper JSON library
        var response = new MCPResponse();
        
        // Extract id
        var idMatch = System.Text.RegularExpressions.Regex.Match(json, @"""id""\s*:\s*(\d+)");
        if (idMatch.Success)
        {
            response.id = int.Parse(idMatch.Groups[1].Value);
        }

        // Extract result (simplified - assumes result is a simple object or string)
        if (json.Contains("\"result\""))
        {
            response.result = new Dictionary<string, object>();
            // For full implementation, you'd want proper JSON parsing here
            // This is a simplified version for Unity compatibility
        }

        return response;
    }

    private async Task<MCPResponse> SendHttpRequestAsync(MCPRequest request, CancellationToken ct)
    {
        using (var uwr = new UnityEngine.Networking.UnityWebRequest(serverUrl, "POST"))
        {
            var json = JsonUtility.ToJson(request);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            uwr.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            var op = uwr.SendWebRequest();
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested) { uwr.Abort(); ct.ThrowIfCancellationRequested(); }
                await Task.Yield();
            }

#if UNITY_2020_1_OR_NEWER
            if (uwr.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
#else
            if (!uwr.isNetworkError && !uwr.isHttpError)
#endif
            {
                var responseJson = uwr.downloadHandler.text;
                return ParseJsonResponse(responseJson);
            }
        }

        return null;
    }

    private async Task SendNotificationAsync(MCPRequest request, CancellationToken ct)
    {
        // Notifications don't expect responses
        if (useStdio)
        {
            lock (lockObject)
            {
                stdioProcess.StandardInput.WriteLine(JsonUtility.ToJson(request));
                stdioProcess.StandardInput.Flush();
            }
        }
        await Task.CompletedTask;
    }

    public void Disconnect()
    {
        if (stdioProcess != null && !stdioProcess.HasExited)
        {
            try
            {
                stdioProcess.Kill();
                stdioProcess.Dispose();
            }
            catch { }
        }
        IsConnected = false;
    }
}

[Serializable]
public class MCPRequest
{
    public string jsonrpc = "2.0";
    public int? id;
    public string method;
    public Dictionary<string, object> @params;
}

[Serializable]
public class MCPResponse
{
    public string jsonrpc;
    public int? id;
    public Dictionary<string, object> result;
    public MCPError error;
}

[Serializable]
public class MCPError
{
    public int code;
    public string message;
}

[Serializable]
public class MCPTool
{
    public string name;
    public string description;
    public Dictionary<string, object> inputSchema;
}
