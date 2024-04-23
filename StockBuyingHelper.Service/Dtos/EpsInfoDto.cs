using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Dtos
{
    public class EpsInfoDto
    {
        public string StockId { get; set; }
        public string EpsAcc4QInterval { get; set; }
        public decimal EpsAcc4Q { get; set; }
    }
}
