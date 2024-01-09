using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{
    public class StockInfoModel
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
        /// ISIN Code
        /// </summary>
        public string ISINCode{ get; set; }
        /// <summary>
        /// 市場別
        /// </summary>
        public string Market { get; set; }
        /// <summary>
        /// 產業別
        /// </summary>
        public string IndustryType { get; set; }
        /// <summary>
        /// CFICode
        /// </summary>
        public string CFICode { get; set; }
        /// <summary>
        /// 備註
        /// </summary>
        public string Note { get; set; }
        /// <summary>
        /// 成交價格
        /// </summary>
        public decimal Price { get; set; }
    }
}
