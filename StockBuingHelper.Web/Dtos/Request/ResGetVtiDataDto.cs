namespace StockBuingHelper.Web.Dtos.Request
{
    public class ResGetVtiDataDto
    {
        public string? specificStockId { get; set; }
        public decimal? priceLow { get; set; } = 0;
        public decimal? priceHigh { get; set; } = 200;
        public int vtiIndex { get; set; }
        public int? volumeTxDateInterval { get; set; } = 7;
        public int? volume { get; set; } = 500;
        public decimal? epsAcc4Q { get; set; } = 0;
        public double? pe { get; set; } = 25;
        public bool queryEtfs { get; set; } = false;
    }
}
