using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Project.BLL.AI.Providers;

public class HuggingFaceProvider : OpenAICompatibleProvider
{
    public override string ProviderName => "HuggingFace";
    protected override string Endpoint => $"{(BaseUrl.Length > 0 ? BaseUrl : "https://router.huggingface.co/v1").TrimEnd('/')}/chat/completions";

    public HuggingFaceProvider(HttpClient http, ILogger<HuggingFaceProvider> logger, IConfiguration config)
        : base(http, logger, config, "HuggingFace") { }
}
