namespace StockBuingHelper.Web.Dtos.Request
{
    public class ResGetVtiDataDto
    {
        public string? specificStockId { get; set; }
        public int vtiIndex { get; set; }
        public bool queryEtfs { get; set; } = false;
    }
}
