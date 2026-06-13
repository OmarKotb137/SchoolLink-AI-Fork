using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Project.BLL.Interfaces;

namespace Project.BLL.Services;

public class BrevoEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public BrevoEmailService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task SendEmailVerificationOtpAsync(string toEmail, string toName, string code, CancellationToken ct = default)
    {
        var apiKey = _config["Brevo:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Brevo:ApiKey is not configured");

        var senderEmail = _config["Brevo:SenderEmail"];
        if (string.IsNullOrWhiteSpace(senderEmail))
            throw new InvalidOperationException("Brevo:SenderEmail is not configured");

        var senderName = _config["Brevo:SenderName"] ?? "SchoolLink";
        var subject = "كود تفعيل البريد الإلكتروني - SchoolLink";

        var payload = new
        {
            sender = new { name = senderName, email = senderEmail },
            to = new[] { new { email = toEmail, name = toName } },
            subject,
            htmlContent = $"""
                <div style="font-family:Arial,sans-serif;line-height:1.7;color:#111827">
                  <h2>تفعيل البريد الإلكتروني</h2>
                  <p>كود التحقق الخاص بك في SchoolLink هو:</p>
                  <p style="font-size:28px;font-weight:700;letter-spacing:4px">{code}</p>
                  <p>ينتهي هذا الكود خلال 10 دقائق. إذا لم تطلبه، يمكنك تجاهل هذه الرسالة.</p>
                </div>
                """
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("api-key", apiKey);

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
    }
}
