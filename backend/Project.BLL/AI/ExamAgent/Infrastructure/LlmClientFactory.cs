using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Project.BLL.AI.ExamAgent.Interfaces;
using Project.BLL.AI.ExamAgent.Models;

namespace Project.BLL.AI.ExamAgent.Infrastructure;

public static class LlmClientFactory
{
    public static void RegisterLlmClient(this IServiceCollection services, IConfiguration config)
    {
        var providerStr = config["LlmSettings:Provider"] ?? "OpenRouter";
        var provider = Enum.Parse<LlmProvider>(providerStr, ignoreCase: true);

        switch (provider)
        {
            case LlmProvider.OpenRouter:
                services.AddHttpClient<ILlmClient, OpenRouterLlmClient>(client =>
                {
                    var apiKey = config["LlmSettings:OpenRouter:ApiKey"]
                                 ?? throw new InvalidOperationException("OpenRouter ApiKey is missing");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    client.DefaultRequestHeaders.Add("HTTP-Referer", "https://examagent.local");
                });
                break;

            case LlmProvider.HuggingFace:
                services.AddHttpClient<ILlmClient, HuggingFaceLlmClient>(client =>
                {
                    var apiKey = config["LlmSettings:HuggingFace:ApiKey"]
                                 ?? throw new InvalidOperationException("HuggingFace ApiKey is missing");
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                });
                break;

            case LlmProvider.CloudflareAI:
                services.AddHttpClient<ILlmClient, CloudflareAILlmClient>(client =>
                {
                    var apiKey = config["LlmSettings:CloudflareAI:ApiKey"]
                                 ?? throw new InvalidOperationException("CloudflareAI ApiKey is missing");
                    client.DefaultRequestHeaders.TryAddWithoutValidation("cf-aig-authorization", $"Bearer {apiKey}");
                });
                break;

            case LlmProvider.OpenCodeAI:
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
