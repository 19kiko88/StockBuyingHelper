using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{
    public class EpsInfo
    {
        public string StockId { get; set; }
        public string StockName { get; set; }
        public decimal EPS { get; set; }
        public double PE { get; set; }
    }
}
