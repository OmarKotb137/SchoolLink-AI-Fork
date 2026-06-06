using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Project.BLL.AI.Providers;

public class OpenRouterProvider : OpenAICompatibleProvider
{
    public override string ProviderName => "OpenRouter";
    protected override string Endpoint => "https://openrouter.ai/api/v1/chat/completions";

    protected override string Model => this["LessonCorrectionModel"] is { Length: > 0 } m ? m : base.Model;

    public OpenRouterProvider(HttpClient http, ILogger<OpenRouterProvider> logger, IConfiguration config)
        : base(http, logger, config, "OpenRouter") { }
}
