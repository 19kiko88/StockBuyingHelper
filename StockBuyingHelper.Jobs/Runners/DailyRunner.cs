using Serilog;
using StockBuyingHelper.Jobs.Data;
using StockBuyingHelper.Jobs.Clients;

namespace StockBuyingHelper.Jobs.Runners;

public sealed class DailyRunner : IJob
{
    private const string HighLow52Endpoint = "SaveHighLow52ToCsv";

    private readonly TwseClient _client;
    private readonly PriceDatabase _database;
    private readonly VpsUploader _uploader;
    private readonly int _retainTradingDays;

    public DailyRunner(TwseClient client, PriceDatabase database, VpsUploader uploader, int retainTradingDays)
    {
        _client = client;
        _database = database;
        _uploader = uploader;
        _retainTradingDays = retainTradingDays;
    }

    public async Task RunAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (today.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            Log.Information("{Date} 是週末,不打 API", today);
            return;
        }

        var result = await _client.FetchAsync(today);

        if (!result.IsTradingDay)
        {
            Log.Information("{Date} 非交易日(假日),略過", today);
            return;
        }

        _database.UpsertDay(today, result.Quotes);
        Log.Information("{Date} 完成寫入,共 {Count} 檔股票", today, result.Quotes.Count);

        var deleted = _database.TrimOldData(_retainTradingDays);
        if (deleted > 0)
        {
            Log.Information("已刪除超過 {Retain} 個交易日的舊資料,共 {Deleted} 筆", _retainTradingDays, deleted);
        }

        await _uploader.UploadAsync(HighLow52Endpoint, _database.GetHighLow52(today));
    }
}
