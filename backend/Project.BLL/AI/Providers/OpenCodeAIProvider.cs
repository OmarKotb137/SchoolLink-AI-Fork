using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Project.BLL.AI.Providers;

public class OpenCodeAIProvider : OpenAICompatibleProvider
{
    public override string ProviderName => "OpenCodeAI";
    protected override string Endpoint => $"{(BaseUrl.Length > 0 ? BaseUrl : "https://opencode.ai/zen/v1").TrimEnd('/')}/chat/completions";

    public OpenCodeAIProvider(HttpClient http, ILogger<OpenCodeAIProvider> logger, IConfiguration config)
        : base(http, logger, config, "OpenCodeAI") { }
}
