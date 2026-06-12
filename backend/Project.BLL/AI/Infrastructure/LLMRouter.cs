using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Infrastructure;

public class LLMRouter : ILLMRouter
{
    private readonly IEnumerable<ILLMProvider> _providers;
    private readonly ILogger<LLMRouter> _logger;
    private readonly string _defaultProvider;

    public LLMRouter(IEnumerable<ILLMProvider> providers, ILogger<LLMRouter> logger, string defaultProvider = "OpenCodeAI")
    {
        _providers = providers;
        _logger = logger;
        _defaultProvider = defaultProvider;
    }

    public Task<string> GenerateAsync(string systemPrompt, string userMessage, string? preferredProvider = null, CancellationToken ct = default)
    {
        var provider = ResolveProvider(preferredProvider);
        return provider.GenerateAsync(systemPrompt, userMessage, ct);
    }

    public Task<string> GenerateChatAsync(string systemPrompt, List<ChatMessage> messages, string? preferredProvider = null, CancellationToken ct = default)
    {
        var provider = ResolveProvider(preferredProvider);
        return provider.GenerateChatAsync(systemPrompt, messages, ct);
    }

    public Task<string> GenerateWithToolsAsync(string systemPrompt, string userMessage, List<AiTool> tools, string? preferredProvider = null, CancellationToken ct = default)
    {
        var provider = ResolveProvider(preferredProvider);
        return provider.GenerateWithToolsAsync(systemPrompt, userMessage, tools, ct);
    }

    private ILLMProvider ResolveProvider(string? preferred)
    {
        var name = preferred ?? _defaultProvider;
        var provider = _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            _logger.LogWarning("Provider {Name} not found, falling back to default {Default}", name, _defaultProvider);
            provider = _providers.FirstOrDefault()
                ?? throw new InvalidOperationException("No LLM providers registered");
        }

        return provider;
    }
}
