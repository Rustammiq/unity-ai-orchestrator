using System.Threading;
using System.Threading.Tasks;

public interface ILLMProvider
{
    // Returns raw string response (usually JSON). Orchestrator may parse further.
    Task<string> SendPromptAsync(string prompt, CancellationToken ct = default);
    string ProviderName { get; }
}
