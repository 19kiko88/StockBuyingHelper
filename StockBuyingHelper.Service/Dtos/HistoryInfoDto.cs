using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Dtos
{
    public class HistoryInfoDto
    {
        public int HistoryId { get; set; }
        public string Content { get; set; }
        public string CreateUser {get;set;}
        public DateOnly CreateDate { get; set; }
    }
}
