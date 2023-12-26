using StockBuyingHelper.Service.Models;

namespace StockBuingHelper.Web.Dtos
{
    public class BuyingResultDto
    {
        public string Sn { get; set; }
        public string StockId { get; set; }
        public string StockName { get; set; }
        public decimal Price { get; set; }
        public decimal HighIn52 { get; set; }
        public decimal LowIn52 { get; set; }
        public string EpsInterval { get; set; }
        public decimal EPS { get; set; }
        public double PE { get; set; }
        public List<RevenueData> RevenueDatas { get; set; }
        public List<VolumeData> VolumeDatas { get; set; }
        public double VTI { get; set; }
        public int Amount { get; set; }
    }
}
