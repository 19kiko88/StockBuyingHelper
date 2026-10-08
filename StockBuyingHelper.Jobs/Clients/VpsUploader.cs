using System.Net.Http.Json;
using Serilog;

namespace StockBuyingHelper.Jobs.Clients;

public sealed class VpsUploader
{
    private readonly HttpClient _httpClient;
    private readonly string? _baseUrl;
    private readonly string? _apiKey;

    public VpsUploader(HttpClient httpClient, string? baseUrl, string? apiKey)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
        _apiKey = apiKey;
    }

    public async Task UploadAsync<T>(string endpoint, IReadOnlyList<T> data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_baseUrl))
        {
            Log.Warning("未設定 VpsBaseUrl,略過上傳 {Endpoint}", endpoint);
            return;
        }

        // API 收到空陣列會回成功但不處理,所以沒有資料時直接不送
        if (data.Count == 0)
        {
            Log.Warning("沒有可上傳的資料,略過上傳 {Endpoint}", endpoint);
            return;
        }

        var url = $"{_baseUrl.TrimEnd('/')}/{endpoint}";

        // 金鑰只加在這個請求上,不能放 DefaultRequestHeaders,否則同一個 HttpClient 打證交所時也會一併送出
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(data) };
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            Log.Warning("未設定 VpsApiKey,以未驗證方式上傳 {Endpoint}", endpoint);
        }
        else
        {
            request.Headers.Add("X-Api-Key", _apiKey);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"上傳 {endpoint} 失敗:HTTP {(int)response.StatusCode},回應:{body}");
        }

        Log.Information("已上傳 {Count} 筆到 {Endpoint}:{Response}", data.Count, endpoint, body);
    }
}
