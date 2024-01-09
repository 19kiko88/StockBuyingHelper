using StockBuyingHelper.Service.Models;

namespace StockBuingHelper.Web.Dtos.Response
{
    public class BuyingResultDto
    {
        public string sn { get; set; }
        public string stockId { get; set; }
        public string stockName { get; set; }
        public decimal price { get; set; }
        public decimal highIn52 { get; set; }
        public decimal lowIn52 { get; set; }
        public string epsInterval { get; set; }
        public decimal eps { get; set; }
        public double pe { get; set; }
        public List<RevenueData> revenueDatas { get; set; }
        public List<VolumeData> volumeDatas { get; set; }
        public double vti { get; set; }
        public int amount { get; set; }
        public string cfiCode { get; set; }
    }
}
