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
    private readonly Dictionary<string, string> environmentVariables;
    private System.Diagnostics.Process stdioProcess;
    private readonly Queue<string> responseQueue = new Queue<string>();
    private readonly Queue<string> errorQueue = new Queue<string>();
    private int requestIdCounter = 1;
    private readonly object lockObject = new object();

    public bool IsConnected { get; private set; }
    public string ClientName => "Unity-AI-Orchestrator";

    public MCPClient(string serverCommand = null, string serverArgs = null, string serverUrl = null, Dictionary<string, string> envVars = null)
    {
        this.serverCommand = serverCommand;
        this.serverArgs = serverArgs;
        this.serverUrl = serverUrl;
        this.environmentVariables = envVars ?? new Dictionary<string, string>();
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
            // Expand environment variables in command path if needed
            var command = serverCommand;
            if (command.Contains("$"))
            {
                command = Environment.ExpandEnvironmentVariables(command);
            }

            // Parse arguments - support both single string and space-separated arguments
            var arguments = ParseArguments(serverArgs ?? "");

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // Set environment variables
            foreach (var envVar in environmentVariables)
            {
                startInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
            }

            // Also pass through important system environment variables
            var importantEnvVars = new[] { "PATH", "HOME", "USER", "SHELL", "PYTHONPATH" };
            foreach (var envVar in importantEnvVars)
            {
                var value = Environment.GetEnvironmentVariable(envVar);
                if (!string.IsNullOrEmpty(value) && !startInfo.EnvironmentVariables.ContainsKey(envVar))
                {
                    startInfo.EnvironmentVariables[envVar] = value;
                }
            }

            Debug.Log($"MCPClient: Starting process '{command}' with args '{arguments}'");
            if (environmentVariables.Count > 0)
            {
                Debug.Log($"MCPClient: Environment variables: {string.Join(", ", environmentVariables.Keys)}");
            }

            stdioProcess = System.Diagnostics.Process.Start(startInfo);
            if (stdioProcess == null)
            {
                Debug.LogError("MCPClient: Failed to start stdio process");
                return false;
            }

            // Start reading output and error streams
            _ = Task.Run(() => ReadStdioOutput(), ct);
            _ = Task.Run(() => ReadStdioError(), ct);

            Debug.Log($"MCPClient: Process started successfully (PID: {stdioProcess.Id})");

            // Wait a bit for process to initialize
            await Task.Delay(500, ct);

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

                if (!string.IsNullOrWhiteSpace(line))
                {
                    Debug.Log($"MCPClient: Received output: {line}");
                    lock (lockObject)
                    {
                        responseQueue.Enqueue(line);
                    }
                }
            }
            
            if (stdioProcess.HasExited)
            {
                Debug.LogWarning($"MCPClient: Process exited with code {stdioProcess.ExitCode}");
                IsConnected = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient stdio read error: {ex.Message}");
            IsConnected = false;
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

                if (!string.IsNullOrWhiteSpace(line))
                {
                    Debug.LogWarning($"MCPClient: Process error: {line}");
                    lock (lockObject)
                    {
                        errorQueue.Enqueue(line);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"MCPClient stderr read error: {ex.Message}");
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
                        if (firstItem is Dictionary<string, object> contentDict)
                        {
                            if (contentDict.ContainsKey("text"))
                            {
                                return contentDict["text"].ToString();
                            }
                            else if (contentDict.ContainsKey("type") && contentDict["type"].ToString() == "text")
                            {
                                // Try to get any text field
                                foreach (var kvp in contentDict)
                                {
                                    if (kvp.Key != "type" && kvp.Value is string)
                                    {
                                        return kvp.Value.ToString();
                                    }
                                }
                            }
                        }
                        else if (firstItem is string)
                        {
                            return firstItem.ToString();
                        }
                    }
                    else if (content is string)
                    {
                        return content.ToString();
                    }
                }
                
                // Fallback: try to get text directly
                if (response.result.ContainsKey("text"))
                {
                    return response.result["text"].ToString();
                }
                
                // Try to find any string value in result
                foreach (var kvp in response.result)
                {
                    if (kvp.Value is string str && !string.IsNullOrEmpty(str))
                    {
                        return str;
                    }
                }
                
                // Last resort: return JSON representation of result
                var resultJson = new System.Text.StringBuilder();
                resultJson.Append("{");
                var first = true;
                foreach (var kvp in response.result)
                {
                    if (!first) resultJson.Append(", ");
                    resultJson.Append($"\"{kvp.Key}\": {kvp.Value}");
                    first = false;
                }
                resultJson.Append("}");
                return resultJson.ToString();
            }

            if (response?.error != null)
            {
                return $"MCP Error [{response.error.code}]: {response.error.message}";
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
        // Check if process is still running
        if (stdioProcess.HasExited)
        {
            Debug.LogError($"MCPClient: Process has exited, cannot send request");
            IsConnected = false;
            return null;
        }

        // Build JSON manually for Unity compatibility (JsonUtility doesn't handle Dictionary<string, object> well)
        var json = BuildJsonRequest(request);
        var id = request.id;

        Debug.Log($"MCPClient: Sending request (id={id}, method={request.method}): {json}");

        lock (lockObject)
        {
            try
            {
                stdioProcess.StandardInput.WriteLine(json);
                stdioProcess.StandardInput.Flush();
            }
            catch (Exception ex)
            {
                Debug.LogError($"MCPClient: Failed to write to stdin: {ex.Message}");
                IsConnected = false;
                return null;
            }
        }

        // Wait for response
        var timeout = DateTime.Now.AddSeconds(30);
        var lastErrorCheck = DateTime.Now;
        
        while (DateTime.Now < timeout && !ct.IsCancellationRequested)
        {
            await Task.Yield();
            
            // Check for process errors periodically
            if ((DateTime.Now - lastErrorCheck).TotalSeconds > 2)
            {
                lastErrorCheck = DateTime.Now;
                if (stdioProcess.HasExited)
                {
                    Debug.LogError($"MCPClient: Process exited while waiting for response (id={id})");
                    IsConnected = false;
                    
                    // Check if there are error messages
                    lock (lockObject)
                    {
                        var errors = new List<string>();
                        while (errorQueue.Count > 0)
                        {
                            errors.Add(errorQueue.Dequeue());
                        }
                        if (errors.Count > 0)
                        {
                            Debug.LogError($"MCPClient: Process errors: {string.Join("\n", errors)}");
                        }
                    }
                    return null;
                }
            }
            
            lock (lockObject)
            {
                if (responseQueue.Count > 0)
                {
                    var responseJson = responseQueue.Dequeue();
                    Debug.Log($"MCPClient: Received response: {responseJson}");
                    
                    try
                    {
                        // Try to parse JSON response
                        var response = ParseJsonResponse(responseJson);
                        if (response?.id == id)
                        {
                            Debug.Log($"MCPClient: Matched response for id={id}");
                            return response;
                        }
                        else if (response?.id != null && response.id != id)
                        {
                            // Response for different ID - put it back at the front to process later
                            var tempQueue = new Queue<string>();
                            tempQueue.Enqueue(responseJson);
                            while (responseQueue.Count > 0)
                            {
                                tempQueue.Enqueue(responseQueue.Dequeue());
                            }
                            while (tempQueue.Count > 0)
                            {
                                responseQueue.Enqueue(tempQueue.Dequeue());
                            }
                            Debug.Log($"MCPClient: Response id mismatch (expected {id}, got {response.id}), queued for later");
                        }
                        else
                        {
                            Debug.LogWarning($"MCPClient: Response with no or null ID, ignoring");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"MCPClient: Failed to parse response: {ex.Message}\nJSON: {responseJson}");
                    }
                }
            }
        }

        if (DateTime.Now >= timeout)
        {
            Debug.LogError($"MCPClient: Timeout waiting for response (id={id}, method={request.method})");
        }

        return null;
    }

    private string BuildJsonRequest(MCPRequest request)
    {
        // Improved JSON builder for Unity compatibility that handles nested structures
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
            sb.Append(SerializeValue(request.@params));
        }
        sb.Append("}");
        return sb.ToString();
    }

    private string SerializeValue(object value)
    {
        if (value == null) return "null";
        
        if (value is string str)
        {
            // Escape special characters
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
        else if (value is float f)
        {
            return f.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else if (value is double d)
        {
            return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        else if (value is System.Collections.ICollection collection)
        {
            var sb = new System.Text.StringBuilder("[");
            var first = true;
            foreach (var item in collection)
            {
                if (!first) sb.Append(",");
                sb.Append(SerializeValue(item));
                first = false;
            }
            sb.Append("]");
            return sb.ToString();
        }
        else if (value is Dictionary<string, object> dict)
        {
            var sb = new System.Text.StringBuilder("{");
            var first = true;
            foreach (var kvp in dict)
            {
                if (!first) sb.Append(",");
                sb.Append($"\"{kvp.Key}\":");
                sb.Append(SerializeValue(kvp.Value));
                first = false;
            }
            sb.Append("}");
            return sb.ToString();
        }
        else
        {
            // Fallback: try to serialize as string or use ToString
            return SerializeValue(value.ToString());
        }
    }

    private MCPResponse ParseJsonResponse(string json)
    {
        var response = new MCPResponse();
        
        try
        {
            // Use Unity's JsonUtility where possible, but we need to handle nested structures manually
            // First extract basic fields
            var idMatch = System.Text.RegularExpressions.Regex.Match(json, @"""id""\s*:\s*(\d+)");
            if (idMatch.Success)
            {
                response.id = int.Parse(idMatch.Groups[1].Value);
            }

            // Extract jsonrpc version
            var jsonrpcMatch = System.Text.RegularExpressions.Regex.Match(json, @"""jsonrpc""\s*:\s*""([^""]+)""");
            if (jsonrpcMatch.Success)
            {
                response.jsonrpc = jsonrpcMatch.Groups[1].Value;
            }

            // Parse result object if present
            if (json.Contains("\"result\""))
            {
                response.result = new Dictionary<string, object>();
                
                // Try to extract the result object using regex
                var resultMatch = System.Text.RegularExpressions.Regex.Match(json, @"""result""\s*:\s*(\{.*?\})(?=,""|})", System.Text.RegularExpressions.RegexOptions.Singleline);
                if (resultMatch.Success)
                {
                    var resultJson = resultMatch.Groups[1].Value;
                    response.result = ParseJsonObject(resultJson);
                }
                else
                {
                    // Try simpler extraction - look for content array
                    if (json.Contains("\"content\""))
                    {
                        // Extract content array
                        var contentMatch = System.Text.RegularExpressions.Regex.Match(json, @"""content""\s*:\s*(\[.*?\])", System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (contentMatch.Success)
                        {
                            response.result["content"] = ParseJsonArray(contentMatch.Groups[1].Value);
                        }
                    }
                    
                    // Also try to get tools array if present
                    if (json.Contains("\"tools\""))
                    {
                        var toolsMatch = System.Text.RegularExpressions.Regex.Match(json, @"""tools""\s*:\s*(\[.*?\])", System.Text.RegularExpressions.RegexOptions.Singleline);
                        if (toolsMatch.Success)
                        {
                            response.result["tools"] = ParseJsonArray(toolsMatch.Groups[1].Value);
                        }
                    }
                }
            }

            // Parse error if present
            if (json.Contains("\"error\""))
            {
                response.error = new MCPError();
                var codeMatch = System.Text.RegularExpressions.Regex.Match(json, @"""code""\s*:\s*(-?\d+)");
                if (codeMatch.Success)
                {
                    response.error.code = int.Parse(codeMatch.Groups[1].Value);
                }
                var messageMatch = System.Text.RegularExpressions.Regex.Match(json, @"""message""\s*:\s*""([^""]+)""");
                if (messageMatch.Success)
                {
                    response.error.message = messageMatch.Groups[1].Value;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MCPClient: Failed to parse JSON response: {ex.Message}");
            Debug.LogWarning($"JSON was: {json}");
        }

        return response;
    }

    private Dictionary<string, object> ParseJsonObject(string json)
    {
        var result = new Dictionary<string, object>();
        
        try
        {
            // Improved parser that handles nested objects and arrays
            var keyPattern = @"""([^""]+)""\s*:\s*";
            var matches = System.Text.RegularExpressions.Regex.Matches(json, keyPattern);
            
            for (int i = 0; i < matches.Count; i++)
            {
                var keyMatch = matches[i];
                var key = keyMatch.Groups[1].Value;
                
                // Find the start position of the value
                var valueStart = keyMatch.Index + keyMatch.Length;
                
                // Find where the value ends (either comma, closing brace, or end of string)
                int valueEnd = valueStart;
                int depth = 0;
                bool inString = false;
                bool escaped = false;
                
                for (int j = valueStart; j < json.Length; j++)
                {
                    char c = json[j];
                    
                    if (escaped)
                    {
                        escaped = false;
                        continue;
                    }
                    
                    if (c == '\\')
                    {
                        escaped = true;
                        continue;
                    }
                    
                    if (c == '"')
                    {
                        inString = !inString;
                        continue;
                    }
                    
                    if (!inString)
                    {
                        if (c == '{' || c == '[')
                            depth++;
                        else if (c == '}' || c == ']')
                            depth--;
                        else if ((c == ',' || c == '}') && depth == 0)
                        {
                            valueEnd = j;
                            break;
                        }
                    }
                }
                
                if (valueEnd == valueStart)
                    valueEnd = json.Length;
                
                var valueStr = json.Substring(valueStart, valueEnd - valueStart).Trim();
                
                // Parse the value based on its type
                if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
                {
                    // String value - unescape
                    result[key] = valueStr.Substring(1, valueStr.Length - 2)
                        .Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\\", "\\");
                }
                else if (valueStr.StartsWith("[") && valueStr.EndsWith("]"))
                {
                    // Array value
                    result[key] = ParseJsonArray(valueStr);
                }
                else if (valueStr.StartsWith("{") && valueStr.EndsWith("}"))
                {
                    // Nested object
                    result[key] = ParseJsonObject(valueStr);
                }
                else if (valueStr == "true" || valueStr == "false")
                {
                    result[key] = valueStr == "true";
                }
                else if (valueStr == "null")
                {
                    result[key] = null;
                }
                else if (int.TryParse(valueStr, out int intVal))
                {
                    result[key] = intVal;
                }
                else if (float.TryParse(valueStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
                {
                    result[key] = floatVal;
                }
                else
                {
                    result[key] = valueStr;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MCPClient: Error parsing JSON object: {ex.Message}");
        }
        
        return result;
    }

    private List<object> ParseJsonArray(string json)
    {
        var result = new List<object>();
        
        try
        {
            // Remove outer brackets
            if (json.StartsWith("[") && json.EndsWith("]"))
            {
                json = json.Substring(1, json.Length - 2).Trim();
            }
            
            if (string.IsNullOrEmpty(json))
                return result;
            
            // Parse array elements - they can be objects, strings, numbers, etc.
            int depth = 0;
            int start = 0;
            bool inString = false;
            bool escaped = false;
            
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                
                if (escaped)
                {
                    escaped = false;
                    continue;
                }
                
                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }
                
                if (c == '"')
                {
                    inString = !inString;
                    continue;
                }
                
                if (!inString)
                {
                    if (c == '{' || c == '[')
                        depth++;
                    else if (c == '}' || c == ']')
                        depth--;
                    else if (c == ',' && depth == 0)
                    {
                        // Found an element boundary
                        var elementStr = json.Substring(start, i - start).Trim();
                        if (!string.IsNullOrEmpty(elementStr))
                        {
                            result.Add(ParseArrayElement(elementStr));
                        }
                        start = i + 1;
                    }
                }
            }
            
            // Add the last element
            if (start < json.Length)
            {
                var elementStr = json.Substring(start).Trim();
                if (!string.IsNullOrEmpty(elementStr))
                {
                    result.Add(ParseArrayElement(elementStr));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MCPClient: Error parsing JSON array: {ex.Message}");
        }
        
        return result;
    }
    
    private object ParseArrayElement(string elementStr)
    {
        elementStr = elementStr.Trim();
        
        if (elementStr.StartsWith("{") && elementStr.EndsWith("}"))
        {
            return ParseJsonObject(elementStr);
        }
        else if (elementStr.StartsWith("\"") && elementStr.EndsWith("\""))
        {
            return elementStr.Substring(1, elementStr.Length - 2)
                .Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\\", "\\");
        }
        else if (elementStr == "true" || elementStr == "false")
        {
            return elementStr == "true";
        }
        else if (int.TryParse(elementStr, out int intVal))
        {
            return intVal;
        }
        else if (float.TryParse(elementStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatVal))
        {
            return floatVal;
        }
        else
        {
            return elementStr;
        }
    }

    private async Task<MCPResponse> SendHttpRequestAsync(MCPRequest request, CancellationToken ct)
    {
        using (var uwr = new UnityEngine.Networking.UnityWebRequest(serverUrl, "POST"))
        {
            var json = BuildJsonRequest(request);
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

    /// <summary>
    /// Parse arguments string - handles both simple space-separated and quoted arguments
    /// Properly escapes arguments with spaces or special characters
    /// </summary>
    private string ParseArguments(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return "";

        // If args already contains properly quoted arguments, return as-is
        // Otherwise, we need to properly quote arguments that contain spaces
        
        // Check if args is already a JSON array string (from mcp.json format)
        if (args.TrimStart().StartsWith("["))
        {
            // Try to parse as JSON array and convert to command-line arguments
            try
            {
                // Simple parsing: remove brackets and split by comma, handling quotes
                var cleanArgs = args.Trim().TrimStart('[').TrimEnd(']');
                var parts = new List<string>();
                bool inQuotes = false;
                int start = 0;
                
                for (int i = 0; i < cleanArgs.Length; i++)
                {
                    char c = cleanArgs[i];
                    
                    if (c == '"')
                    {
                        inQuotes = !inQuotes;
                    }
                    else if (c == ',' && !inQuotes)
                    {
                        var part = cleanArgs.Substring(start, i - start).Trim();
                        if (!string.IsNullOrEmpty(part))
                        {
                            // Remove surrounding quotes if present
                            if (part.StartsWith("\"") && part.EndsWith("\""))
                                part = part.Substring(1, part.Length - 2);
                            parts.Add(part);
                        }
                        start = i + 1;
                    }
                }
                
                // Add last part
                if (start < cleanArgs.Length)
                {
                    var part = cleanArgs.Substring(start).Trim();
                    if (!string.IsNullOrEmpty(part))
                    {
                        if (part.StartsWith("\"") && part.EndsWith("\""))
                            part = part.Substring(1, part.Length - 2);
                        parts.Add(part);
                    }
                }
                
                if (parts.Count > 0)
                {
                    // Escape and join arguments properly
                    var escapedParts = parts.Select(p => EscapeArgument(p));
                    return string.Join(" ", escapedParts);
                }
            }
            catch
            {
                // Fall through to default handling
            }
        }
        
        // Default: return as-is, ProcessStartInfo.Arguments will handle basic escaping
        return args;
    }

    /// <summary>
    /// Escape a single argument for command-line usage
    /// </summary>
    private string EscapeArgument(string arg)
    {
        if (string.IsNullOrEmpty(arg))
            return "\"\"";

        // If argument contains spaces or special characters, quote it
        if (arg.Contains(" ") || arg.Contains("\t") || arg.Contains("\"") || arg.Contains("&") || arg.Contains("|"))
        {
            // Escape inner quotes and wrap in quotes
            var escaped = arg.Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }

        return arg;
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
