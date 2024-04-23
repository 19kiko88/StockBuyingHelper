using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Dtos
{
    public class PriceInfoDto
    {
        public string StockId { get; set; }

        public decimal Price { get; set; }
        public decimal HighPriceInCurrentYear { get; set; }
        /// <summary>
        /// 現距1年高點跌幅
        /// </summary>
        public double HighPriceInCurrentYearPercentGap { get; set; }
        /// <summary>
        /// 1年最低股價(元)
        /// </summary>
        public decimal LowPriceInCurrentYear { get; set; }
        /// <summary>
        /// 現距1年低點漲幅
        /// </summary>
        public double LowPriceInCurrentYearPercentGap { get; set; }
    }
}
