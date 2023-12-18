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
        public double VTI { get; set; }
        public int Amount { get; set; }
    }
}
