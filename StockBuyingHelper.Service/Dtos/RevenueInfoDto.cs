using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Dtos
{
    public class RevenueInfoDto
    {
        public string StockId { get; set; }
        public string RevenueInterval { get; set; }
        public double MoM { get; set; }
        public double YoYMonth { get; set; }
        public double YoY { get; set; }
    }
}
