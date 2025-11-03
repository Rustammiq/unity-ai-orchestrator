using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// LM Studio Adapter - connects to local LM Studio server (typically running on localhost:1234)
/// Uses OpenAI-compatible API endpoint for seamless integration
/// </summary>
public class LMStudioAdapter : ILLMProvider
{
    private readonly string apiUrl;
    private readonly string model;
    private readonly string authHeader;
    public string ProviderName => $"LM Studio ({model ?? "default"})";

    public LMStudioAdapter(string apiUrl = "http://localhost:1234/v1/chat/completions", string model = null, string authHeader = null)
    {
        this.apiUrl = apiUrl;
        this.model = model; // If null, LM Studio will use the currently loaded model
        this.authHeader = authHeader;
    }

    public async Task<string> SendPromptAsync(string prompt, CancellationToken ct = default)
    {
        var payload = new LMStudioRequest
        {
            model = model ?? "", // Empty string means use default/loaded model
            messages = new List<LMStudioMessage> 
            { 
                new LMStudioMessage { role = "user", content = prompt } 
            },
            temperature = 0.7f,
            max_tokens = 2048,
            stream = false
        };

        var body = JsonUtility.ToJson(payload);
        
        using (var uwr = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            
            // LM Studio doesn't require auth, but some setups might
            if (!string.IsNullOrEmpty(authHeader))
            {
                uwr.SetRequestHeader("Authorization", authHeader);
            }

            var op = uwr.SendWebRequest();
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested) 
                { 
                    uwr.Abort(); 
                    ct.ThrowIfCancellationRequested(); 
                }
                await Task.Yield();
            }

#if UNITY_2020_1_OR_NEWER
            if (uwr.result == UnityWebRequest.Result.Success)
#else
            if (!uwr.isNetworkError && !uwr.isHttpError)
#endif
            {
                var responseText = uwr.downloadHandler.text;
                
                // Parse OpenAI-compatible response
                var response = JsonUtility.FromJson<LMStudioResponse>(responseText);
                if (response?.choices != null && response.choices.Count > 0)
                {
                    var firstChoice = response.choices[0];
                    if (firstChoice.message != null)
                    {
                        return firstChoice.message.content;
                    }
                }
                
                // Fallback: return raw response if parsing fails
                return responseText;
            }
            else
            {
                var errorMsg = $"LM Studio request failed: {uwr.responseCode} {uwr.error}";
                if (!string.IsNullOrEmpty(uwr.downloadHandler.text))
                {
                    errorMsg += $"\nResponse: {uwr.downloadHandler.text}";
                }
                Debug.LogError(errorMsg);
                
                // Provide helpful error message
                if (uwr.responseCode == 0 || uwr.error.Contains("connection"))
                {
                    return "Error: Could not connect to LM Studio. Make sure LM Studio is running on " + apiUrl;
                }
                return $"Error: {uwr.responseCode} - {uwr.error}";
            }
        }
    }

    [System.Serializable]
    private class LMStudioRequest
    {
        public string model;
        public List<LMStudioMessage> messages;
        public float temperature;
        public int max_tokens;
        public bool stream;
    }

    [System.Serializable]
    private class LMStudioMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    private class LMStudioResponse
    {
        public List<LMStudioChoice> choices;
    }

    [System.Serializable]
    private class LMStudioChoice
    {
        public LMStudioMessage message;
        public string finish_reason;
    }
}
