using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Project.BLL.AI.Providers;

public class DeepSeekProvider : OpenAICompatibleProvider
{
    public override string ProviderName => "DeepSeek";
    protected override string Endpoint => "https://api.deepseek.com/v1/chat/completions";

    public DeepSeekProvider(HttpClient http, ILogger<DeepSeekProvider> logger, IConfiguration config)
        : base(http, logger, config, "DeepSeek") { }
}
