using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{

    public class StockVolumeInfoModel
    {
        public string StockId { get; set; }
        public string StockName { get; set; }
        public List<VolumeData> VolumeInfo { get; set; }
    }

    public class VolumeData
    {
        public DateOnly TxDate { get; set; }//交易日
        public int ForeignSellVolK { get; set; }//外資(張)
        public int DealerDiffVolK { get; set; }//自營(張)
        public int InvestmentTrustDiffVolK { get; set; }//投信(張)
        public int VolumeK { get; set; } //總成交量(張)
    }




}
