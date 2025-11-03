using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Simple orchestrator: run providers in parallel and merge results.
public class LLMOrchestrator : ILLMProvider
{
    private readonly List<ILLMProvider> providers;
    public string ProviderName => "Orchestrator(" + string.Join(",", providers.Select(p => p.ProviderName)) + ")";

    public enum Mode { Solo, Dual, Tri }

    public LLMOrchestrator(List<ILLMProvider> providers)
    {
        this.providers = providers;
    }

    public async Task<string> SendPromptAsync(string prompt, CancellationToken ct = default)
    {
        var tasks = new List<Task<string>>();
        foreach (var p in providers) tasks.Add(p.SendPromptAsync(prompt, ct));
        var results = await Task.WhenAll(tasks);
        // Simple merge strategy:
        // - If multiple non-empty results, join with separators. A better strategy would parse and vote.
        var nonEmpty = results.Where(r => !string.IsNullOrEmpty(r)).ToArray();
        if (nonEmpty.Length == 0) return "";

        if (nonEmpty.Length == 1) return nonEmpty[0];

        // choose longest plus merge
        var longest = nonEmpty.OrderByDescending(s => s.Length).First();
        return string.Join("\n---MERGED---\n", nonEmpty) + "\n\n=== Longest chosen as primary ===\n" + longest;
    }
}
