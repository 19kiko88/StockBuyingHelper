using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{
    public class RevenueInfoModel
    {
        public string StockId { get; set; }
        public string StockName { get; set; }
        public double pe { get; set; }
        public List<RevenueData> RevenueData { get; set; }
    }

    public class RevenueData
    {
        public string revenueInterval { get; set; }
        public double mom { get; set; }
        public double monthYOY { get; set; }
        public double yoy { get; set; }              
    }
}
