using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{
    public class StockPriceModel
    {
        public string StockId { get; set; }
        public string StockName { get; set; }
        public decimal Price { get; set; }
    }
}
