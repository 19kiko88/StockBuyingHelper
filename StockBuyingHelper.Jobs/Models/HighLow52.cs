namespace StockBuyingHelper.Jobs.Models;

public sealed record HighLow52(string StockId, string StockName, decimal High52, decimal Low52);
