using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{
    public class RevenueInfo
    {
        public string StockId { get; set; }
        public string StockName { get; set; }
        public List<Revenue> RevenueData { get; set; }
    }

    public class Revenue
    {
        public string RevenueInterval { get; set; }
        public double MOM { get; set; }
        public double YOY { get; set; }
        //public double YoyGrandTotal { get; set; }
    }
}
