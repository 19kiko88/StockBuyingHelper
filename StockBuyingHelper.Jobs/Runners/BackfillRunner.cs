using Serilog;
using StockBuyingHelper.Jobs.Data;
using StockBuyingHelper.Jobs.Twse;

namespace StockBuyingHelper.Jobs.Runners;

public sealed class BackfillRunner
{
    private readonly TwseClient _client;
    private readonly PriceDatabase _database;
    private readonly int _backfillDays;
    private readonly TimeSpan _requestDelay;
    private readonly int _retainTradingDays;

    public BackfillRunner(TwseClient client, PriceDatabase database, int backfillDays, TimeSpan requestDelay, int retainTradingDays)
    {
        _client = client;
        _database = database;
        _backfillDays = backfillDays;
        _requestDelay = requestDelay;
        _retainTradingDays = retainTradingDays;
    }

    public async Task RunAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var dates = Enumerable.Range(1, _backfillDays)
            .Select(offset => today.AddDays(-offset))
            .Where(d => d.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            .OrderBy(d => d)
            .ToList();

        Log.Information(
            "回補範圍:{Start} ~ {End},共 {Count} 個候選交易日(已排除六日)",
            dates[0], dates[^1], dates.Count);

        for (var i = 0; i < dates.Count; i++)
        {
            var date = dates[i];
            var progress = $"{i + 1}/{dates.Count}";

            if (_database.HasDataForDate(date))
            {
                Log.Information("{Date} 已有資料,略過 ({Progress})", date, progress);
                continue;
            }

            var result = await _client.FetchAsync(date);

            if (result.IsTradingDay)
            {
                _database.UpsertDay(date, result.Quotes);
                Log.Information("{Date} 完成寫入,共 {Count} 檔股票 ({Progress})", date, result.Quotes.Count, progress);
            }
            else
            {
                Log.Information("{Date} 非交易日(假日),略過 ({Progress})", date, progress);
            }

            await Task.Delay(_requestDelay);
        }

        var deleted = _database.TrimOldData(_retainTradingDays);
        Log.Information("回補完成,已刪除超過 {Retain} 個交易日的舊資料共 {Deleted} 筆", _retainTradingDays, deleted);
    }
}
