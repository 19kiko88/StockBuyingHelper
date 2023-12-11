using StockBuyingHelper.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Interfaces
{
    public interface IStockService
    {
        public Task<string> GetStockList();
        public Task<List<GetHighLowIn52WeeksInfo>> GetHighLowIn52Weeks();
    }
}
