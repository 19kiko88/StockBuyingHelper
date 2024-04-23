using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service
{
    public static class CacheType
    {
        public static string StockList { get; set; } = "StockList";
        public static string PriceHighLowIn52WeeksList { get; set; } = "PriceHighLowIn52WeeksList";        
    }
}
