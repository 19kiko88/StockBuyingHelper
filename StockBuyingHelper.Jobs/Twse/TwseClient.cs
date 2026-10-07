using System.Globalization;
using System.Text.Json;
using Serilog;
using StockBuyingHelper.Jobs.Models;

namespace StockBuyingHelper.Jobs.Twse;

public sealed record TwseFetchResult(bool IsTradingDay, IReadOnlyList<DailyQuote> Quotes);

public sealed class TwseClient
{
    private const string TableTitleMarker = "每日收盤行情";

    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
    ];

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public TwseClient(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    public async Task<TwseFetchResult> FetchAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var url = $"{_baseUrl}?date={date:yyyyMMdd}&type=ALLBUT0999&response=json";

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var json = await _httpClient.GetStringAsync(url, cancellationToken);
                return ParseResponse(json);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                if (attempt >= RetryDelays.Length)
                {
                    throw new InvalidOperationException($"抓取 {date:yyyy-MM-dd} 的 TWSE 資料失敗,已重試 {RetryDelays.Length} 次", ex);
                }

                var delay = RetryDelays[attempt];
                Log.Warning(ex, "抓取 {Date} 失敗,{DelaySeconds} 秒後重試(第 {Attempt} 次)", date, delay.TotalSeconds, attempt + 1);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static TwseFetchResult ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("stat", out var statProp) ||
            statProp.GetString() != "OK" ||
            !root.TryGetProperty("tables", out var tablesProp))
        {
            return new TwseFetchResult(false, Array.Empty<DailyQuote>());
        }

        JsonElement? targetTable = null;
        foreach (var table in tablesProp.EnumerateArray())
        {
            if (table.TryGetProperty("title", out var titleProp) &&
                titleProp.GetString() is { } title &&
                title.Contains(TableTitleMarker, StringComparison.Ordinal))
            {
                targetTable = table;
                break;
            }
        }

        if (targetTable is null)
        {
            throw new InvalidOperationException(
                $"TWSE 回應中找不到包含「{TableTitleMarker}」的資料表,API 回應格式可能已變更");
        }

        var fieldIndex = new Dictionary<string, int>();
        var i = 0;
        foreach (var field in targetTable.Value.GetProperty("fields").EnumerateArray())
        {
            fieldIndex[field.GetString()!] = i++;
        }

        var codeIdx = fieldIndex["證券代號"];
        var nameIdx = fieldIndex["證券名稱"];
        var highIdx = fieldIndex["最高價"];
        var lowIdx = fieldIndex["最低價"];

        var quotes = new List<DailyQuote>();
        foreach (var row in targetTable.Value.GetProperty("data").EnumerateArray())
        {
            var cells = row.EnumerateArray().Select(c => c.GetString()).ToArray();
            var code = cells[codeIdx]!;
            var name = cells[nameIdx]!;
            var high = ParseDecimalOrNull(cells[highIdx]);
            var low = ParseDecimalOrNull(cells[lowIdx]);
            quotes.Add(new DailyQuote(code, name, high, low));
        }

        return new TwseFetchResult(true, quotes);
    }

    private static decimal? ParseDecimalOrNull(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var cleaned = raw.Replace(",", "").Trim();
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }
}
