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
        public List<StockVolumeAPIModel.List> VolumeInfo { get; set; }
    }
}
