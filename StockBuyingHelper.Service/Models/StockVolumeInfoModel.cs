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
        //public string StockName { get; set; }
        public List<VolumeData> VolumeInfo { get; set; }
    }

    public class VolumeData
    {
        public DateOnly txDate { get; set; }//交易日
        public int foreignDiffVolK { get; set; }//外資(張)
        public int dealerDiffVolK { get; set; }//自營(張)
        public int investmentTrustDiffVolK { get; set; }//投信(張)
        public int volumeK { get; set; } //總成交量(張)
    }




}
