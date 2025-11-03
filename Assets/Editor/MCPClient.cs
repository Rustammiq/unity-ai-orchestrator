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
            // Resolve absolute path if serverArgs is a relative path
            var args = serverArgs ?? "";
            if (!string.IsNullOrEmpty(args) && !System.IO.Path.IsPathRooted(args) && !args.StartsWith("-"))
            {
                // Try to resolve relative path
                var workspacePath = System.IO.Directory.GetCurrentDirectory();
                var absolutePath = System.IO.Path.Combine(workspacePath, args);
                if (System.IO.File.Exists(absolutePath) || System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(absolutePath)))
                {
                    args = absolutePath;
                    Debug.Log($"[MCPClient] Resolved path: {args}");
                }
            }

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = serverCommand,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = System.IO.Directory.GetCurrentDirectory()
            };

            Debug.Log($"[MCPClient] Starting process: {serverCommand} {args}");

            stdioProcess = System.Diagnostics.Process.Start(startInfo);
            if (stdioProcess == null)
            {
                Debug.LogError("MCPClient: Failed to start stdio process");
                return false;
            }

            // Start reading output and error streams
            _ = Task.Run(() => ReadStdioOutput(), ct);
            _ = Task.Run(() => ReadStdioError(), ct);

            // Wait a bit for the process to start
            await Task.Delay(100, ct);

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
                Debug.Log("[MCPClient] Initialization successful");
                
                // Send initialized notification
                await SendNotificationAsync(new MCPRequest
                {
                    jsonrpc = "2.0",
                    method = "notifications/initialized"
                }, ct);

                IsConnected = true;
                return true;
            }
            else if (initResponse?.error != null)
            {
                Debug.LogError($"[MCPClient] Initialization error: {initResponse.error.code} - {initResponse.error.message}");
            }
            else
            {
                Debug.LogWarning("[MCPClient] Initialization returned null response");
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient stdio init failed: {ex.Message}");
            Debug.LogError($"StackTrace: {ex.StackTrace}");
            return false;
        }
    }
    
    private async Task ReadStdioError()
    {
        try
        {
            while (!stdioProcess.HasExited && stdioProcess.StandardError != null)
            {
                var line = await stdioProcess.StandardError.ReadLineAsync();
                if (line == null) break;
                
                line = line.Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    Debug.LogWarning($"[MCPClient] Error output: {line}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient stdio error read failed: {ex.Message}");
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
                
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                // JSON-RPC 2.0 messages are one complete JSON object per line
                lock (lockObject)
                {
                    responseQueue.Enqueue(line);
                }
                
                Debug.Log($"[MCPClient] Received: {line}");
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
            var tools = new List<MCPTool>();
            var toolsValue = response.result["tools"];
            
            // Tools can be a string (JSON) or a List
            if (toolsValue is string toolsJsonStr)
            {
                // Parse JSON string representation of tools array
                // Format: [{"name":"...","description":"...","inputSchema":{...}}]
                var toolMatches = System.Text.RegularExpressions.Regex.Matches(toolsJsonStr,
                    @"\{[^\}]+\}", System.Text.RegularExpressions.RegexOptions.Singleline);
                
                foreach (System.Text.RegularExpressions.Match toolMatch in toolMatches)
                {
                    var toolObj = ParseJsonObject("{" + toolMatch.Value + "}", "\"name\"");
                    if (toolObj != null && toolObj.ContainsKey("name"))
                    {
                        tools.Add(new MCPTool
                        {
                            name = toolObj["name"].ToString(),
                            description = toolObj.ContainsKey("description") ? toolObj["description"].ToString() : "",
                            inputSchema = toolObj.ContainsKey("inputSchema") && toolObj["inputSchema"] is Dictionary<string, object> schema 
                                ? schema 
                                : new Dictionary<string, object>()
                        });
                    }
                }
            }
            else if (toolsValue is System.Collections.IList toolsList)
            {
                foreach (var toolItem in toolsList)
                {
                    if (toolItem is Dictionary<string, object> toolDict)
                    {
                        tools.Add(new MCPTool
                        {
                            name = toolDict.ContainsKey("name") ? toolDict["name"].ToString() : "",
                            description = toolDict.ContainsKey("description") ? toolDict["description"].ToString() : "",
                            inputSchema = toolDict.ContainsKey("inputSchema") && toolDict["inputSchema"] is Dictionary<string, object> schema
                                ? schema
                                : new Dictionary<string, object>()
                        });
                    }
                }
            }
            
            Debug.Log($"[MCPClient] Found {tools.Count} tools");
            return tools;
        }
        
        Debug.LogWarning("[MCPClient] No tools found in response");
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
                // Try to extract content from response (MCP tool responses use content array)
                if (response.result.ContainsKey("content_text"))
                {
                    return response.result["content_text"].ToString();
                }
                
                if (response.result.ContainsKey("content"))
                {
                    var content = response.result["content"];
                    // If content is a string that contains JSON array, try to parse it
                    if (content is string contentStr && contentStr.Contains("text"))
                    {
                        // Extract text from content string
                        var textMatch = System.Text.RegularExpressions.Regex.Match(contentStr, @"""text""\s*:\s*""([^""]+)""");
                        if (textMatch.Success)
                        {
                            return textMatch.Groups[1].Value;
                        }
                    }
                    // If content is a List or array-like structure
                    else if (content is System.Collections.IList contentList && contentList.Count > 0)
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
                
                // Last resort: serialize the result as JSON-like string
                var resultStr = new System.Text.StringBuilder();
                foreach (var kvp in response.result)
                {
                    if (resultStr.Length > 0) resultStr.Append(", ");
                    resultStr.Append($"{kvp.Key}: {kvp.Value}");
                }
                return resultStr.Length > 0 ? resultStr.ToString() : "Empty response from MCP server";
            }
            
            if (response?.error != null)
            {
                return $"MCP Error ({response.error.code}): {response.error.message}";
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

        Debug.Log($"[MCPClient] Sending: {json}");

        lock (lockObject)
        {
            stdioProcess.StandardInput.WriteLine(json);
            stdioProcess.StandardInput.Flush();
        }

        // Wait for response with proper matching
        var timeout = DateTime.Now.AddSeconds(30);
        var tempQueue = new Queue<string>();
        
        while (DateTime.Now < timeout && !ct.IsCancellationRequested)
        {
            await Task.Delay(10, ct); // Small delay to avoid busy waiting
            
            lock (lockObject)
            {
                // Move all responses to temp queue to check them
                while (responseQueue.Count > 0)
                {
                    tempQueue.Enqueue(responseQueue.Dequeue());
                }
            }
            
            // Check responses outside the lock
            while (tempQueue.Count > 0)
            {
                var responseJson = tempQueue.Dequeue();
                try
                {
                    // Try to parse JSON response
                    var response = ParseJsonResponse(responseJson);
                    
                    // Match by ID (null ID means notification response)
                    if (response != null)
                    {
                        if (id == null || response.id == id)
                        {
                            return response;
                        }
                        // If ID doesn't match, put it back
                        lock (lockObject)
                        {
                            responseQueue.Enqueue(responseJson);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"MCPClient: Failed to parse response: {ex.Message}");
                    Debug.LogWarning($"Response JSON: {responseJson}");
                }
            }
            
            // Put unmatched responses back
            lock (lockObject)
            {
                while (tempQueue.Count > 0)
                {
                    responseQueue.Enqueue(tempQueue.Dequeue());
                }
            }
        }

        Debug.LogWarning($"MCPClient: Timeout waiting for response (ID: {id})");
        return null;
    }

    private string BuildJsonRequest(MCPRequest request)
    {
        // Improved JSON builder for Unity compatibility - handles nested objects and arrays
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
            sb.Append(",\"params\":");
            sb.Append(BuildJsonValue(request.@params));
        }
        sb.Append("}");
        return sb.ToString();
    }
    
    private string BuildJsonValue(object value)
    {
        if (value == null)
            return "null";
            
        if (value is string str)
        {
            // Escape special characters in strings
            str = str.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
            return $"\"{str}\"";
        }
        else if (value is bool b)
        {
            return b ? "true" : "false";
        }
        else if (value is int || value is long || value is short || value is byte)
        {
            return value.ToString();
        }
        else if (value is float || value is double || value is decimal)
        {
            return value.ToString().Replace(",", ".");
        }
        else if (value is Dictionary<string, object> dict)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            var first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{kvp.Key}\":");
                sb.Append(BuildJsonValue(kvp.Value));
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
        else if (value is System.Collections.IList list)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[");
            var first = true;
            foreach (var item in list)
            {
                if (!first) sb.Append(",");
                sb.Append(BuildJsonValue(item));
                first = false;
            }
            sb.Append("]");
            return sb.ToString();
        }
        else
        {
            // Fallback: convert to string and escape
            var str = value.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{str}\"";
        }
    }

    private MCPResponse ParseJsonResponse(string json)
    {
        // Improved JSON parser for Unity - handles basic JSON-RPC 2.0 responses
        var response = new MCPResponse();
        
        try
        {
            // Extract jsonrpc version
            var jsonrpcMatch = System.Text.RegularExpressions.Regex.Match(json, @"""jsonrpc""\s*:\s*""([^""]+)""");
            if (jsonrpcMatch.Success)
            {
                response.jsonrpc = jsonrpcMatch.Groups[1].Value;
            }
            
            // Extract id (can be number or null)
            var idMatch = System.Text.RegularExpressions.Regex.Match(json, @"""id""\s*:\s*(\d+|null)");
            if (idMatch.Success && idMatch.Groups[1].Value != "null")
            {
                response.id = int.Parse(idMatch.Groups[1].Value);
            }

            // Extract result - try to find the result object
            if (json.Contains("\"result\""))
            {
                response.result = ParseJsonObject(json, "\"result\"");
                if (response.result == null)
                {
                    response.result = new Dictionary<string, object>();
                }
            }

            // Extract error if present
            if (json.Contains("\"error\""))
            {
                var errorObj = ParseJsonObject(json, "\"error\"");
                if (errorObj != null)
                {
                    response.error = new MCPError();
                    if (errorObj.ContainsKey("code") && errorObj["code"] != null)
                    {
                        response.error.code = int.Parse(errorObj["code"].ToString());
                    }
                    if (errorObj.ContainsKey("message") && errorObj["message"] != null)
                    {
                        response.error.message = errorObj["message"].ToString();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MCPClient: Failed to parse JSON response: {ex.Message}");
            Debug.LogWarning($"JSON: {json}");
        }

        return response;
    }
    
    private Dictionary<string, object> ParseJsonObject(string json, string key)
    {
        try
        {
            var keyPattern = key.Replace("\"", "\\\"");
            var match = System.Text.RegularExpressions.Regex.Match(json, keyPattern + @"\s*:\s*(\{.*?\})", 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            
            if (match.Success)
            {
                var objJson = match.Groups[1].Value;
                var result = new Dictionary<string, object>();
                
                // Extract key-value pairs from the object
                var kvpPattern = @"""([^""]+)""\s*:\s*(""[^""]*""|\d+\.?\d*|true|false|null|\{.*?\}|\[.*?\])";
                var kvpMatches = System.Text.RegularExpressions.Regex.Matches(objJson, kvpPattern, 
                    System.Text.RegularExpressions.RegexOptions.Singleline);
                
                foreach (System.Text.RegularExpressions.Match kvpMatch in kvpMatches)
                {
                    var k = kvpMatch.Groups[1].Value;
                    var v = kvpMatch.Groups[2].Value;
                    
                    // Parse value
                    if (v.StartsWith("\"") && v.EndsWith("\""))
                    {
                        result[k] = v.Substring(1, v.Length - 2); // Remove quotes
                    }
                    else if (v == "true" || v == "false")
                    {
                        result[k] = v == "true";
                    }
                    else if (v == "null")
                    {
                        result[k] = null;
                    }
                    else if (System.Text.RegularExpressions.Regex.IsMatch(v, @"^\d+\.?\d*$"))
                    {
                        if (v.Contains("."))
                            result[k] = float.Parse(v);
                        else
                            result[k] = int.Parse(v);
                    }
                    else if (v.StartsWith("{") || v.StartsWith("["))
                    {
                        // Nested object or array - store as string for now, can be improved
                        result[k] = v;
                    }
                    else
                    {
                        result[k] = v;
                    }
                }
                
                // Special handling for content arrays in tool responses (MCP format)
                if (result.ContainsKey("content"))
                {
                    var contentValue = result["content"];
                    if (contentValue is string contentStr)
                    {
                        // Content is a JSON string representing an array
                        if (contentStr.StartsWith("["))
                        {
                            // Try to extract text from content array: [{"type":"text","text":"..."}]
                            var textMatch = System.Text.RegularExpressions.Regex.Match(contentStr, 
                                @"""text""\s*:\s*""([^""]*)""", System.Text.RegularExpressions.RegexOptions.Singleline);
                            if (textMatch.Success)
                            {
                                result["content_text"] = textMatch.Groups[1].Value;
                            }
                            // Also try unescaped JSON
                            else
                            {
                                var unescapedMatch = System.Text.RegularExpressions.Regex.Match(contentStr,
                                    @"""text""\s*:\s*""([^""\\]+(?:\\.[^""\\]*)*)""", System.Text.RegularExpressions.RegexOptions.Singleline);
                                if (unescapedMatch.Success)
                                {
                                    result["content_text"] = unescapedMatch.Groups[1].Value.Replace("\\\"", "\"")
                                        .Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
                                }
                            }
                        }
                    }
                }
                
                return result;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MCPClient: Failed to parse JSON object: {ex.Message}");
        }
        
        return null;
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
                var json = BuildJsonRequest(request);
                stdioProcess.StandardInput.WriteLine(json);
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
