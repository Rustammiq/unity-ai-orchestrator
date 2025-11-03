using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class GeminiAdapter : ILLMProvider
{
    private readonly string apiKey;
    private readonly string apiUrl;
    private readonly string model;
    public string ProviderName => "Gemini-2.5-Pro";

    public GeminiAdapter(string apiKey, string model = "gemini-2.5-pro", string apiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent")
    {
        this.apiKey = apiKey;
        this.apiUrl = apiUrl;
        this.model = model;
    }

    public async Task<string> SendPromptAsync(string prompt, CancellationToken ct = default)
    {
        var bodyObj = new { prompt = prompt, model = model, maxOutputTokens = 1024 };
        var body = JsonUtility.ToJson(bodyObj);
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
                Debug.LogError($"Gemini request failed: {uwr.responseCode} {uwr.error} - {uwr.downloadHandler.text}");
                return null;
            }
        }
    }
}
