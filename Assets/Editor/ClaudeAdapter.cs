using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ClaudeAdapter : ILLMProvider
{
    private readonly string apiKey;
    private readonly string apiUrl;
    private readonly string model;
    public string ProviderName => "Claude";

    public ClaudeAdapter(string apiKey, string model = "claude-3.5-sonnet", string apiUrl = "https://api.anthropic.com/v1/messages")
    {
        this.apiKey = apiKey;
        this.apiUrl = apiUrl;
        this.model = model;
    }

    public async Task<string> SendPromptAsync(string prompt, CancellationToken ct = default)
    {
        var payload = new ClaudeRequest
        {
            model = model,
            messages = new List<Message> { new Message { role = "user", content = prompt } },
            max_tokens = 1024
        };
        var body = JsonUtility.ToJson(payload);
        using (var uwr = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("x-api-key", apiKey);
            var op = uwr.SendWebRequest();
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested) { uwr.Abort(); ct.ThrowIfCancellationRequested(); }
                await Task.Yield();
            }
#if UNITY_2020_1_OR_NEWER
            if (uwr.result == UnityWebRequest.Result.Success)
#else
            if (!uwr.isNetworkError && !uwr.isHttpError)
#endif
            {
                return uwr.downloadHandler.text;
            }
            else
            {
                Debug.LogError($"Claude request failed: {uwr.responseCode} {uwr.error} - {uwr.downloadHandler.text}");
                return null;
            }
        }
    }

    [System.Serializable]
    private class ClaudeRequest
    {
        public string model;
        public System.Collections.Generic.List<Message> messages;
        public int max_tokens;
    }
    [System.Serializable]
    private class Message
    {
        public string role;
        public string content;
    }
}
