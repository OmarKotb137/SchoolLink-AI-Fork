using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Project.BLL.AI.Providers;

public class CloudflareAIProvider : OpenAICompatibleProvider
{
    public override string ProviderName => "CloudflareAI";
    protected override string Endpoint
    {
        get
        {
            var baseUrl = BaseUrl.Length > 0 ? BaseUrl : "https://gateway.ai.cloudflare.com/v1";
            var accountId = this["AccountId"];
            var gateway = this["Gateway"] is { Length: > 0 } g ? g : "default";
            return $"{baseUrl.TrimEnd('/')}/{accountId}/{gateway}/compat/chat/completions";
        }
    }

    public CloudflareAIProvider(HttpClient http, ILogger<CloudflareAIProvider> logger, IConfiguration config)
        : base(http, logger, config, "CloudflareAI") { }
}
