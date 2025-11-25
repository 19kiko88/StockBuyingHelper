namespace StockBuingHelper.Web.Dtos.Request
{
    public class ReqHighLow52Dto
    {
        public string StockId { get; set; }
        public string StockName { get; set; }
        public decimal High52 { get; set; }
        public decimal Low52 { get; set; }
    }
}

