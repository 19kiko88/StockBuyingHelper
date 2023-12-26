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
        public List<RevenueData> RevenueData { get; set; }
    }

    public class RevenueData
    {
        public string RevenueInterval { get; set; }
        public double MOM { get; set; }
        public double MonthYOY { get; set; }
        public double YOY { get; set; }        
    }
}
