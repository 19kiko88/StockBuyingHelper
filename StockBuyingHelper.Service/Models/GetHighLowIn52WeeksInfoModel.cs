using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{
    public class GetHighLowIn52WeeksInfoModel
    {
        /// <summary>
        /// 代號
        /// </summary>
        public string StockId { get; set; }
        /// <summary>
        /// 名稱
        /// </summary>
        public string StockName { get; set; }
        /// <summary>
        /// 成交
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// 最高
        /// </summary>
        public decimal High { get; set; }
        /// <summary>
        /// 最低
        /// </summary>
        public decimal Low { get; set; }
        /// <summary>
        /// 漲跌價
        /// </summary>
        public decimal HighLowPriceGap { get; set; }
        /// <summary>
        /// 漲跌幅度
        /// </summary>
        public double HighLowPercentGap { get; set; }
        /// <summary>
        /// 更新日期
        /// </summary>
        public DateOnly UpdateDate { get; set; }
        /// <summary>
        /// 3個月最高股價(元)
        /// </summary>
        public decimal HighPriceInCurrentSeason { get; set; }
        /// <summary>
        /// 現距3個月高點跌幅
        /// </summary>
        public double HighPriceInCurrentSeasonPercentGap { get; set; }
        /// <summary>
        /// 3個月最低股價(元)
        /// </summary>
        public decimal LowPriceInCurrentSeason { get; set; }
        /// <summary>
        /// 現距3個月低點漲幅
        /// </summary>
        public double LowPriceInCurrentSeasonPercentGap { get; set; }
        /// <summary>
        /// 半年最高股價(元)
        /// </summary>
        public decimal HighPriceInCurrentHalfYear { get; set; }
        /// <summary>
        /// 現距半年高點跌幅
        /// </summary>
        public double HighPriceInCurrentHalfYearPercentGap { get; set; }
        /// <summary>
        /// 半年最低股價(元)
        /// </summary>
        public decimal LowPriceInCurrentHalfYear { get; set; }
        /// <summary>
        /// 現距半年低點漲幅
        /// </summary>
        public double LowPriceInCurrentHalfYearPercentGap { get; set; }
        /// <summary>
        /// 1年最高股價(元)
        /// </summary>
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
