using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{
    public static class StockType
    {
        public static string ESVUFR { get; set; } = "ESVUFR";
        public static List<string> ETFs { get; set; } = new List<string>() { "CEOGEU", "CEOIEU", "" };
    }
}
