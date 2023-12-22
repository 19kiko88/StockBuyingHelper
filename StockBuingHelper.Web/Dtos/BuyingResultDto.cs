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
        public string RevenueInterval_1 { get; set; }
        public double? MOM_1 { get; set; }
        public double? YOY_1 { get; set; }
        public string RevenueInterval_2 { get; set; }
        public double? MOM_2 { get; set; }
        public double? YOY_2 { get; set; }
        public string RevenueInterval_3 { get; set; }
        public double? MOM_3 { get; set; }
        public double? YOY_3 { get; set; }
        public List<VolumeData> VolumeDatas { get; set; }
        public double VTI { get; set; }
        public int Amount { get; set; }
    }
}
