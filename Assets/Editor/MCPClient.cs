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
    private readonly Dictionary<int, TaskCompletionSource<MCPResponse>> pendingRequests = new Dictionary<int, TaskCompletionSource<MCPResponse>>();
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
            var buffer = new char[4096];
            var sb = new StringBuilder();
            
            while (!stdioProcess.HasExited && stdioProcess.StandardOutput != null)
            {
                var charsRead = await stdioProcess.StandardOutput.ReadAsync(buffer, 0, buffer.Length);
                if (charsRead == 0) break;

                sb.Append(buffer, 0, charsRead);
                var text = sb.ToString();
                
                // Try to extract complete JSON responses
                var responses = ExtractJsonResponses(text, out var remaining);
                sb.Clear();
                sb.Append(remaining);

                lock (lockObject)
                {
                    foreach (var response in responses)
                    {
                        ProcessJsonResponse(response);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient stdio read error: {ex.Message}");
            Debug.LogError(ex.StackTrace);
        }
    }

    private List<string> ExtractJsonResponses(string text, out string remaining)
    {
        var responses = new List<string>();
        remaining = "";
        
        var depth = 0;
        var inString = false;
        var escape = false;
        var start = -1;
        
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            
            if (escape)
            {
                escape = false;
                continue;
            }
            
            if (c == '\\')
            {
                escape = true;
                continue;
            }
            
            if (c == '"')
            {
                inString = !inString;
                continue;
            }
            
            if (inString) continue;
            
            if (c == '{')
            {
                if (start == -1) start = i;
                depth++;
            }
            else if (c == '}')
            {
                depth--;
                if (depth == 0 && start != -1)
                {
                    var json = text.Substring(start, i - start + 1);
                    responses.Add(json);
                    start = -1;
                }
            }
        }
        
        if (start != -1)
        {
            remaining = text.Substring(start);
        }
        else if (depth > 0)
        {
            remaining = text;
        }
        
        return responses;
    }

    private void ProcessJsonResponse(string json)
    {
        try
        {
            var response = ParseJsonResponse(json);
            
            if (response.id.HasValue && pendingRequests.ContainsKey(response.id.Value))
            {
                var tcs = pendingRequests[response.id.Value];
                pendingRequests.Remove(response.id.Value);
                tcs.SetResult(response);
            }
            else
            {
                // Store in queue for old-style waiting
                responseQueue.Enqueue(json);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MCPClient: Failed to process response: {ex.Message}");
            Debug.LogWarning($"JSON: {json}");
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
                // Try to extract content from response (MCP standard format)
                if (response.result.ContainsKey("content"))
                {
                    var content = response.result["content"];
                    if (content is List<object> contentList && contentList.Count > 0)
                    {
                        var firstItem = contentList[0];
                        if (firstItem is Dictionary<string, object> contentDict)
                        {
                            if (contentDict.ContainsKey("text"))
                            {
                                var textValue = contentDict["text"];
                                return textValue?.ToString() ?? "";
                            }
                            // If no "text" key, try to get the value directly
                            if (contentDict.ContainsKey("value"))
                            {
                                return contentDict["value"]?.ToString() ?? "";
                            }
                        }
                        else
                        {
                            return firstItem?.ToString() ?? "";
                        }
                    }
                    else if (content is Dictionary<string, object> contentDict)
                    {
                        // Content might be a dict instead of array
                        if (contentDict.ContainsKey("text"))
                        {
                            return contentDict["text"]?.ToString() ?? "";
                        }
                    }
                }
                
                // Fallback: try to get text directly
                if (response.result.ContainsKey("text"))
                {
                    var textValue = response.result["text"];
                    if (textValue is Dictionary<string, object> textDict && textDict.ContainsKey("value"))
                    {
                        return textDict["value"]?.ToString() ?? "";
                    }
                    return textValue?.ToString() ?? "";
                }
                
                // Try to get any string value from result
                foreach (var kvp in response.result)
                {
                    if (kvp.Value is string str && !string.IsNullOrEmpty(str))
                    {
                        return str;
                    }
                    if (kvp.Value is Dictionary<string, object> dict && dict.ContainsKey("value"))
                    {
                        return dict["value"]?.ToString() ?? "";
                    }
                }
                
                // Last resort: return the full result as JSON-like string
                var resultStr = string.Join(", ", response.result.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                return $"MCP Result: {resultStr}";
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
        var id = request.id.Value;

        // Create task completion source for async waiting
        var tcs = new TaskCompletionSource<MCPResponse>();
        
        lock (lockObject)
        {
            pendingRequests[id] = tcs;
            stdioProcess.StandardInput.WriteLine(json);
            stdioProcess.StandardInput.Flush();
            Debug.Log($"MCPClient: Sent request ID {id}: {json}");
        }

        // Wait for response with timeout
        try
        {
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), ct);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                lock (lockObject)
                {
                    if (pendingRequests.ContainsKey(id))
                    {
                        pendingRequests.Remove(id);
                    }
                }
                Debug.LogError($"MCPClient: Request {id} timed out");
                return null;
            }

            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            lock (lockObject)
            {
                if (pendingRequests.ContainsKey(id))
                {
                    pendingRequests.Remove(id);
                }
            }
            return null;
        }
    }

    private string BuildJsonRequest(MCPRequest request)
    {
        // Better JSON builder for Unity compatibility
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
            sb.Append(SerializeJsonValue(request.@params));
        }
        sb.Append("}");
        return sb.ToString();
    }

    private string SerializeJsonValue(object value)
    {
        if (value == null) return "null";
        
        if (value is string str)
        {
            return $"\"{EscapeJsonString(str)}\"";
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
            return value.ToString().Replace(',', '.');
        }
        else if (value is Dictionary<string, object> dict)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            var first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{EscapeJsonString(kvp.Key)}\":");
                sb.Append(SerializeJsonValue(kvp.Value));
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
        else if (value is System.Collections.IList list)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[");
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(",");
                sb.Append(SerializeJsonValue(list[i]));
            }
            sb.Append("]");
            return sb.ToString();
        }
        else if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[");
            var first = true;
            foreach (var item in enumerable)
            {
                if (!first) sb.Append(",");
                sb.Append(SerializeJsonValue(item));
                first = false;
            }
            sb.Append("]");
            return sb.ToString();
        }
        else
        {
            // Fallback to string representation
            return $"\"{EscapeJsonString(value.ToString())}\"";
        }
    }

    private string EscapeJsonString(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        
        var sb = new System.Text.StringBuilder();
        foreach (var c in str)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20)
                    {
                        sb.Append($"\\u{((int)c):X4}");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    private MCPResponse ParseJsonResponse(string json)
    {
        // Better JSON parser that handles nested objects
        var response = new MCPResponse();
        
        try
        {
            // Extract jsonrpc
            var jsonrpcMatch = System.Text.RegularExpressions.Regex.Match(json, @"""jsonrpc""\s*:\s*""([^""]+)""");
            if (jsonrpcMatch.Success)
            {
                response.jsonrpc = jsonrpcMatch.Groups[1].Value;
            }

            // Extract id (can be null, number, or missing)
            var idMatch = System.Text.RegularExpressions.Regex.Match(json, @"""id""\s*:\s*(null|\d+)");
            if (idMatch.Success && idMatch.Groups[1].Value != "null")
            {
                response.id = int.Parse(idMatch.Groups[1].Value);
            }

            // Extract result object - need to handle nested objects properly
            var resultStart = json.IndexOf("\"result\"");
            if (resultStart >= 0)
            {
                var colonPos = json.IndexOf(':', resultStart);
                if (colonPos >= 0)
                {
                    colonPos++; // Skip colon
                    // Skip whitespace
                    while (colonPos < json.Length && char.IsWhiteSpace(json[colonPos]))
                        colonPos++;
                    
                    if (colonPos < json.Length)
                    {
                        var resultJson = ExtractJsonValue(json, colonPos, out var endPos);
                        if (!string.IsNullOrEmpty(resultJson))
                        {
                            response.result = ParseJsonObject(resultJson);
                        }
                    }
                }
            }

            // Extract error object
            var errorMatch = System.Text.RegularExpressions.Regex.Match(json, @"""error""\s*:\s*(\{[^}]*\})");
            if (errorMatch.Success)
            {
                var errorJson = errorMatch.Groups[1].Value;
                var errorDict = ParseJsonObject(errorJson);
                if (errorDict != null)
                {
                    response.error = new MCPError();
                    if (errorDict.ContainsKey("code") && errorDict["code"] is int code)
                    {
                        response.error.code = code;
                    }
                    if (errorDict.ContainsKey("message") && errorDict["message"] is string msg)
                    {
                        response.error.message = msg;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient: JSON parse error: {ex.Message}");
            Debug.LogError($"JSON: {json}");
        }

        return response;
    }

    private Dictionary<string, object> ParseJsonObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
        
        var result = new Dictionary<string, object>();
        
        // Handle simple string values
        if (json.StartsWith("\"") && json.EndsWith("\""))
        {
            // This is a string, not an object - return as string value in a dict
            return new Dictionary<string, object> { ["value"] = json.Substring(1, json.Length - 2) };
        }

        // Handle numbers
        if (System.Text.RegularExpressions.Regex.IsMatch(json, @"^-?\d+(\.\d+)?$"))
        {
            if (json.Contains("."))
            {
                if (float.TryParse(json, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f))
                {
                    return new Dictionary<string, object> { ["value"] = f };
                }
            }
            else if (int.TryParse(json, out int i))
            {
                return new Dictionary<string, object> { ["value"] = i };
            }
        }

        // Handle booleans
        if (json == "true" || json == "false")
        {
            return new Dictionary<string, object> { ["value"] = json == "true" };
        }

        // Handle arrays (improved)
        if (json.StartsWith("[") && json.EndsWith("]"))
        {
            var list = new List<object>();
            var content = json.Substring(1, json.Length - 2).Trim();
            if (!string.IsNullOrEmpty(content))
            {
                var items = SplitJsonArray(content);
                foreach (var item in items)
                {
                    var trimmedItem = item.Trim();
                    if (trimmedItem.StartsWith("{") && trimmedItem.EndsWith("}"))
                    {
                        // Parse nested object
                        var parsed = ParseJsonObject(trimmedItem);
                        if (parsed != null && parsed.Count > 0)
                        {
                            list.Add(parsed);
                        }
                        else
                        {
                            list.Add(trimmedItem);
                        }
                    }
                    else
                    {
                        // Parse as value
                        var parsed = ParseJsonObject(trimmedItem);
                        if (parsed != null && parsed.ContainsKey("value"))
                        {
                            list.Add(parsed["value"]);
                        }
                        else
                        {
                            list.Add(trimmedItem.Trim('"'));
                        }
                    }
                }
            }
            return new Dictionary<string, object> { ["value"] = list };
        }

        // Handle objects - extract key-value pairs
        if (json.StartsWith("{") && json.EndsWith("}"))
        {
            var content = json.Substring(1, json.Length - 2).Trim();
            if (string.IsNullOrEmpty(content)) return result;

            var pairs = SplitJsonPairs(content);
            foreach (var pair in pairs)
            {
                var colonIndex = pair.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = pair.Substring(0, colonIndex).Trim().Trim('"');
                    var valueJson = pair.Substring(colonIndex + 1).Trim();
                    
                    var valueObj = ParseJsonObject(valueJson);
                    if (valueObj != null && valueObj.ContainsKey("value"))
                    {
                        result[key] = valueObj["value"];
                    }
                    else if (valueObj != null)
                    {
                        result[key] = valueObj;
                    }
                    else
                    {
                        result[key] = valueJson.Trim('"');
                    }
                }
            }
        }

        return result;
    }

    private List<string> SplitJsonArray(string content)
    {
        var items = new List<string>();
        var depth = 0;
        var inString = false;
        var escape = false;
        var start = 0;

        for (int i = 0; i < content.Length; i++)
        {
            var c = content[i];
            
            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '{' || c == '[') depth++;
            else if (c == '}' || c == ']') depth--;
            else if (c == ',' && depth == 0)
            {
                items.Add(content.Substring(start, i - start));
                start = i + 1;
            }
        }

        if (start < content.Length)
        {
            items.Add(content.Substring(start));
        }

        return items;
    }

    private string ExtractJsonValue(string json, int startPos, out int endPos)
    {
        endPos = startPos;
        if (startPos >= json.Length) return "";
        
        var c = json[startPos];
        
        // Handle object
        if (c == '{')
        {
            var depth = 0;
            var inString = false;
            var escape = false;
            for (int i = startPos; i < json.Length; i++)
            {
                c = json[i];
                if (escape)
                {
                    escape = false;
                    continue;
                }
                if (c == '\\')
                {
                    escape = true;
                    continue;
                }
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }
                if (inString) continue;
                if (c == '{') depth++;
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        endPos = i + 1;
                        return json.Substring(startPos, endPos - startPos);
                    }
                }
            }
        }
        // Handle array
        else if (c == '[')
        {
            var depth = 0;
            var inString = false;
            var escape = false;
            for (int i = startPos; i < json.Length; i++)
            {
                c = json[i];
                if (escape)
                {
                    escape = false;
                    continue;
                }
                if (c == '\\')
                {
                    escape = true;
                    continue;
                }
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }
                if (inString) continue;
                if (c == '[') depth++;
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        endPos = i + 1;
                        return json.Substring(startPos, endPos - startPos);
                    }
                }
            }
        }
        // Handle string
        else if (c == '"')
        {
            var escape = false;
            for (int i = startPos + 1; i < json.Length; i++)
            {
                c = json[i];
                if (escape)
                {
                    escape = false;
                    continue;
                }
                if (c == '\\')
                {
                    escape = true;
                    continue;
                }
                if (c == '"')
                {
                    endPos = i + 1;
                    return json.Substring(startPos, endPos - startPos);
                }
            }
        }
        // Handle number, boolean, null
        else
        {
            var end = startPos;
            while (end < json.Length && (char.IsLetterOrDigit(json[end]) || json[end] == '-' || json[end] == '.' || json[end] == '+' || json[end] == 'e' || json[end] == 'E'))
                end++;
            endPos = end;
            return json.Substring(startPos, endPos - startPos);
        }
        
        endPos = startPos;
        return "";
    }

    private List<string> SplitJsonPairs(string content)
    {
        var pairs = new List<string>();
        var depth = 0;
        var inString = false;
        var escape = false;
        var start = 0;

        for (int i = 0; i < content.Length; i++)
        {
            var c = content[i];
            
            if (escape)
            {
                escape = false;
                continue;
            }

            if (c == '\\')
            {
                escape = true;
                continue;
            }

            if (c == '"')
            {
                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '{' || c == '[') depth++;
            else if (c == '}' || c == ']') depth--;
            else if (c == ',' && depth == 0)
            {
                pairs.Add(content.Substring(start, i - start));
                start = i + 1;
            }
        }

        if (start < content.Length)
        {
            pairs.Add(content.Substring(start));
        }

        return pairs;
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
