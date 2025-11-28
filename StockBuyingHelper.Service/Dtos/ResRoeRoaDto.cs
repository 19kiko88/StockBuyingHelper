using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Dtos
{
    public class ResRoeRoaDto
    {
        public string StockId { get; set; }
        public decimal SumROE { get; set; }
        public decimal SumROA { get; set; }
    }
}
