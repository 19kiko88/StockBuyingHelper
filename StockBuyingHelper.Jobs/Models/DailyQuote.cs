namespace StockBuyingHelper.Jobs.Models;

public sealed record DailyQuote(string Code, string Name, decimal? HighPrice, decimal? LowPrice);
