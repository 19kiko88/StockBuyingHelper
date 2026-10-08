using System.Net.Http.Json;
using Serilog;

namespace StockBuyingHelper.Jobs.Clients;

public sealed class VpsUploader
{
    private readonly HttpClient _httpClient;
    private readonly string? _baseUrl;

    public VpsUploader(HttpClient httpClient, string? baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
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
        using var response = await _httpClient.PostAsJsonAsync(url, data, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"上傳 {endpoint} 失敗:HTTP {(int)response.StatusCode},回應:{body}");
        }

        Log.Information("已上傳 {Count} 筆到 {Endpoint}:{Response}", data.Count, endpoint, body);
    }
}
