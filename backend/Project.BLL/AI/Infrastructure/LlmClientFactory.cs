using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Infrastructure;

public enum LlmProviderType { OpenRouter, HuggingFace, CloudflareAI, OpenCodeAI }

public static class LlmClientFactory
{
    public static void RegisterLlmClient(this IServiceCollection services, IConfiguration config)
    {
        var providerStr = config["LlmSettings:Provider"] ?? "OpenRouter";
        var provider = Enum.Parse<LlmProviderType>(providerStr, ignoreCase: true);

        switch (provider)
        {
            case LlmProviderType.OpenRouter:
                services.AddHttpClient<ILlmClient, OpenRouterLlmClient>(client =>
                {
                    var apiKey = config["LlmSettings:OpenRouter:ApiKey"]
                                 ?? throw new InvalidOperationException("OpenRouter ApiKey is missing");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    client.DefaultRequestHeaders.Add("HTTP-Referer", "https://examagent.local");
                });
                break;

            case LlmProviderType.HuggingFace:
                services.AddHttpClient<ILlmClient, HuggingFaceLlmClient>(client =>
                {
                    var apiKey = config["LlmSettings:HuggingFace:ApiKey"]
                                 ?? throw new InvalidOperationException("HuggingFace ApiKey is missing");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                });
                break;

            case LlmProviderType.CloudflareAI:
                services.AddHttpClient<ILlmClient, CloudflareAILlmClient>(client =>
                {
                    var apiKey = config["LlmSettings:CloudflareAI:ApiKey"]
                                 ?? throw new InvalidOperationException("CloudflareAI ApiKey is missing");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("cf-aig-authorization", $"Bearer {apiKey}");
                });
                break;

            case LlmProviderType.OpenCodeAI:
                services.AddHttpClient<ILlmClient, OpenCodeAILlmClient>(client =>
                {
                    var apiKey = config["LlmSettings:OpenCodeAI:ApiKey"]
                                 ?? throw new InvalidOperationException("OpenCodeAI ApiKey is missing");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                });
                break;

            default:
                throw new InvalidOperationException($"Unknown LLM provider: {provider}");
        }
    }
}
