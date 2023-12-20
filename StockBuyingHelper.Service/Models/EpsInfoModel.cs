using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Models
{
    public class EpsInfoModel
    {
        public Data data { get; set; }
        public Meta meta { get; set; }        
    }


    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Data
    {
        public Data data { get; set; }
        public Result result { get; set; }
    }

    public class LastYear
    {
        public DateTime date { get; set; }
        public object revenue { get; set; }
        public object revenueAcc { get; set; }
        public object revenueMoM { get; set; }
        public string revenueQoQ { get; set; }
        public string revenueYoY { get; set; }
        public object revenueYoYAcc { get; set; }
    }

    public class Meta
    {
    }

    public class PriceAssessment
    {
        public string avgPrice { get; set; }
    }

    public class Result
    {
        public List<Revenue2> revenues { get; set; }
    }

    public class Revenue2
    {
        public string symbol { get; set; }
        public DateTime date { get; set; }
        public object revenue { get; set; }
        public object revenueAcc { get; set; }
        public object revenueMoM { get; set; }
        public string revenueQoQ { get; set; }
        public string revenueYoY { get; set; }
        public object revenueYoYAcc { get; set; }
        public object eps { get; set; }
        public string epsQoQ { get; set; }
        public string epsYoY { get; set; }
        public string epsAcc4Q { get; set; }
        public LastYear lastYear { get; set; }
        public PriceAssessment priceAssessment { get; set; }
    }
}
