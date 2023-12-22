using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{

    public class StockVolumeAPIModel
    {
        public Data data { get; set; }
        public Meta meta { get; set; }



        // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
        public class Data
        {
            public List<List> list { get; set; }
            public DateTime refreshedTs { get; set; }
        }

        public class List
        {
            public double? changePercent { get; set; } = 0;
            public DateTime date { get; set; }
            public int dealerBuyVolK { get; set; }
            public int dealerDiffVolK { get; set; }
            public int dealerSellVolK { get; set; }
            public object endDate { get; set; }
            public int foreignBuyVolK { get; set; }
            public int foreignDiffVolK { get; set; }
            public string foreignHoldPercent { get; set; }
            public int foreignSellVolK { get; set; }
            public int investmentTrustBuyVolK { get; set; }
            public int investmentTrustDiffVolK { get; set; }
            public int investmentTrustSellVolK { get; set; }
            public string period { get; set; }
            public string price { get; set; }
            public int totalBuyVolK { get; set; }
            public int totalDiffVolK { get; set; }
            public int totalSellVolK { get; set; }
            public string volumeK { get; set; }
            public string formattedDate { get; set; }
        }

        public class Meta
        {
        }
    }






}
