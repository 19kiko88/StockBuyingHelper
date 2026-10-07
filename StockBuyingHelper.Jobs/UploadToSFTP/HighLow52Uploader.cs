using System.Net.Http.Json;
using Serilog;
using StockBuyingHelper.Jobs.Models;

namespace StockBuyingHelper.Jobs.Upload;

public sealed class HighLow52Uploader
{
    private readonly HttpClient _httpClient;
    private readonly string? _uploadUrl;

    public HighLow52Uploader(HttpClient httpClient, string? uploadUrl)
    {
        _httpClient = httpClient;
        _uploadUrl = uploadUrl;
    }

    public async Task UploadAsync(IReadOnlyList<HighLow52> data, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_uploadUrl))
        {
            Log.Warning("未設定 UploadUrl,略過上傳 52 週高低價");
            return;
        }

        // API 收到空陣列會回成功但不寫檔,所以沒有資料時直接不送
        if (data.Count == 0)
        {
            Log.Warning("沒有可上傳的 52 週高低價資料,略過上傳");
            return;
        }

        using var response = await _httpClient.PostAsJsonAsync(_uploadUrl, data, cancellationToken);
        response.EnsureSuccessStatusCode();

        Log.Information("已上傳 {Count} 筆 52 週高低價:{Response}", data.Count, await response.Content.ReadAsStringAsync(cancellationToken));
    }
}
