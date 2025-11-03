using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class OpenAIAdapter : ILLMProvider
{
    private readonly string apiKey;
    private readonly string apiUrl;
    private readonly string model;
    public string ProviderName => "OpenAI-GPT";

    public OpenAIAdapter(string apiKey, string model = "gpt-4o-mini", string apiUrl = "https://api.openai.com/v1/chat/completions")
    {
        this.apiKey = apiKey;
        this.apiUrl = apiUrl;
        this.model = model;
    }

    public async Task<string> SendPromptAsync(string prompt, CancellationToken ct = default)
    {
        var payload = new OpenAIRequest
        {
            model = model,
            messages = new System.Collections.Generic.List<OpenAIMessage> { new OpenAIMessage { role = "user", content = prompt } },
            max_tokens = 1024
        };
        var body = JsonUtility.ToJson(payload);
        using (var uwr = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
            uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Authorization", $"Bearer {apiKey}");
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
                Debug.LogError($"OpenAI request failed: {uwr.responseCode} {uwr.error} - {uwr.downloadHandler.text}");
                return null;
            }
        }
    }

    [System.Serializable]
    private class OpenAIRequest
    {
        public string model;
        public System.Collections.Generic.List<OpenAIMessage> messages;
        public int max_tokens;
    }
    [System.Serializable]
    private class OpenAIMessage
    {
        public string role;
        public string content;
    }
}
