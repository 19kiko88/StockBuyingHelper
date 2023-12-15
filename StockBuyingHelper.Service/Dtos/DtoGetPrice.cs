using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Dtos
{
    public class DtoGetPrice
    {
        public string StockId { get; set; }
        public string StockName { get; set; }
        public decimal Price { get; set; }

    }
}
