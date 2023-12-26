using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{
    public class BuyingResultModel
    {
        public string StockId { get; set; }
        public string StockName { get; set; }
        public decimal Price { get; set; }
        public string Type { get; set; }
        public decimal HighIn52 { get; set; }
        public decimal LowIn52 { get; set; }
        public string EpsInterval { get; set; }
        public decimal EPS { get; set; }
        public double PE { get; set; }
        public List<RevenueData> RevenueDatas { get; set; }
        public List<VolumeData> VolumeDatas { get;set; }
        public double VTI { get; set; }
        public int Amount { get; set; }
    }
}
