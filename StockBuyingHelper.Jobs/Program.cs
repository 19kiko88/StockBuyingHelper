using Microsoft.Extensions.Configuration;
using Serilog;
using StockBuyingHelper.Jobs.Data;
using StockBuyingHelper.Jobs.Runners;
using StockBuyingHelper.Jobs.Twse;
using StockBuyingHelper.Jobs.Upload;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var dbPath = configuration["DbPath"] ?? "stock.db";
var baseUrl = configuration["BaseUrl"] ?? throw new InvalidOperationException("缺少 BaseUrl 設定");
var requestDelaySeconds = configuration.GetValue<int?>("RequestDelaySeconds") ?? 5;
var backfillDays = configuration.GetValue<int?>("BackfillDays") ?? 380;
var uploadUrl = configuration["UploadUrl"];
var retainTradingDays = configuration.GetValue<int?>("RetainTradingDays") ?? PriceDatabase.MinRetainTradingDays;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

var mode = args.Length >= 2 && args[0] == "--mode" ? args[1] : "daily";

try
{
    if (retainTradingDays < PriceDatabase.MinRetainTradingDays)
    {
        throw new InvalidOperationException(
            $"RetainTradingDays={retainTradingDays} 小於下限 {PriceDatabase.MinRetainTradingDays},52 週高低價會被低估,拒絕執行");
    }

    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("StockBuyingHelper/1.0");

    var client = new TwseClient(httpClient, baseUrl);
    var database = new PriceDatabase(Path.Combine(AppContext.BaseDirectory, dbPath));
    database.EnsureSchema();

    switch (mode)
    {
        case "daily":
            await new DailyRunner(client, database, new HighLow52Uploader(httpClient, uploadUrl), retainTradingDays).RunAsync();
            break;
        case "backfill":
            await new BackfillRunner(client, database, backfillDays, TimeSpan.FromSeconds(requestDelaySeconds), retainTradingDays).RunAsync();
            break;
        default:
            Log.Error("未知的 --mode 參數: {Mode},可用值為 daily 或 backfill", mode);
            return 1;
    }

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "執行失敗");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
