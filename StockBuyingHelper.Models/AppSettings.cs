using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Models
{


    public class AppSettings
    {
        public const string _ConnectionStrings = "ConnectionStrings";

        public class ConnectionStrings
        {
            public string SBHConnection { get; set; }
        }
    }
}
