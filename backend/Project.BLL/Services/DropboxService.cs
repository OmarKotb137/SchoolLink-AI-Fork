using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Common.Results;
using Microsoft.Extensions.Configuration;
using Project.BLL.Interfaces;

namespace Project.BLL.Services;

public class DropboxService : IDropboxService
{
    private readonly HttpClient _httpClient;
    private readonly string _appKey;
    private readonly string _appSecret;
    private readonly string _refreshToken;
    private string? _currentAccessToken;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public DropboxService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _appKey = configuration["Dropbox:AppKey"] ?? throw new InvalidOperationException("Dropbox:AppKey is not configured");
        _appSecret = configuration["Dropbox:AppSecret"] ?? throw new InvalidOperationException("Dropbox:AppSecret is not configured");
        _refreshToken = configuration["Dropbox:RefreshToken"] ?? throw new InvalidOperationException("Dropbox:RefreshToken is not configured");
        _currentAccessToken = configuration["Dropbox:AccessToken"];
    }

    public async Task<OperationResult<string>> UploadFileAsync(Stream fileStream, string fileName, string? folder = null)
    {
        var path = $"/{(folder ?? "SchoolLink")}/{fileName}";

        var apiArg = JsonSerializer.Serialize(new
        {
            path,
            mode = "add",
            autorename = true
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload")
        {
            Headers = { { "Dropbox-API-Arg", apiArg } },
            Content = new StreamContent(fileStream)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await SendWithRetryAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return OperationResult<string>.Failure($"Dropbox upload failed: {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var filePath = result.GetProperty("path_display").GetString()!;

        var linkResult = await GetSharedLinkAsync(filePath);
        if (!linkResult.IsSuccess)
            return linkResult;

        return OperationResult<string>.Success(linkResult.Data!, "File uploaded successfully");
    }

    public async Task<OperationResult<string>> GetSharedLinkAsync(string path)
    {
        var body = new { path };

        var response = await SendWithRetryAsync(
            new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/sharing/create_shared_link_with_settings")
            {
                Content = JsonContent.Create(body)
            });

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var url = result.GetProperty("url").GetString()!;
            var directUrl = url.Replace("?dl=0", "?dl=1").Replace("www.dropbox.com", "dl.dropboxusercontent.com");
            return OperationResult<string>.Success(directUrl, "Shared link created");
        }

        var errorJson = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var getExisting = await SendWithRetryAsync(
                new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/sharing/list_shared_links")
                {
                    Content = JsonContent.Create(new { path })
                });

            if (getExisting.IsSuccessStatusCode)
            {
                var existingResult = await getExisting.Content.ReadFromJsonAsync<JsonElement>();
                var links = existingResult.GetProperty("links");
                if (links.GetArrayLength() > 0)
                {
                    var url = links[0].GetProperty("url").GetString()!;
                    var directUrl = url.Replace("?dl=0", "?dl=1").Replace("www.dropbox.com", "dl.dropboxusercontent.com");
                    return OperationResult<string>.Success(directUrl, "Shared link retrieved");
                }
            }
        }

        return OperationResult<string>.Failure($"Failed to create shared link: {errorJson}");
    }

    public async Task<OperationResult> DeleteFileAsync(string path)
    {
        var body = new { path };

        var response = await SendWithRetryAsync(
            new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/files/delete")
            {
                Content = JsonContent.Create(body)
            });

        if (response.IsSuccessStatusCode)
            return OperationResult.Success("File deleted from Dropbox");

        var error = await response.Content.ReadAsStringAsync();
        return OperationResult.Failure($"Dropbox delete failed: {error}");
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, int retryCount = 1)
    {
        await EnsureTokenAsync();
        ApplyAuthorization();

        var cloned = await CloneRequestAsync(request);
        var response = await _httpClient.SendAsync(cloned);

        if (!response.IsSuccessStatusCode && retryCount > 0)
        {
            var error = await response.Content.ReadAsStringAsync();
            if (error.Contains("expired_access_token"))
            {
                await RefreshAccessTokenAsync();
                ApplyAuthorization();

                var retryRequest = await CloneRequestAsync(request);
                return await _httpClient.SendAsync(retryRequest);
            }
        }

        return response;
    }

    private async Task EnsureTokenAsync()
    {
        if (_currentAccessToken != null)
            return;

        await RefreshAccessTokenAsync();
    }

    private async Task RefreshAccessTokenAsync()
    {
        await _tokenLock.WaitAsync();
        try
        {
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/oauth2/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = _refreshToken,
                    ["client_id"] = _appKey,
                    ["client_secret"] = _appSecret
                })
            };

            var response = await _httpClient.SendAsync(tokenRequest);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            _currentAccessToken = result.GetProperty("access_token").GetString()!;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private void ApplyAuthorization()
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _currentAccessToken);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);
            if (request.Content.Headers.ContentType != null)
                clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
        }

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        return clone;
    }
}
