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
        public Task<List<StockInfo>> GetStockList();
        public Task<List<GetHighLowIn52WeeksInfo>> GetHighLowIn52Weeks();
        public Task<List<StockPrice>> GetPrice();
        public Task<List<VtiInfo>> GetVTI(List<StockPrice> priceData, List<GetHighLowIn52WeeksInfo> highLowData, int amountLimit = 0);
        public Task<List<EpsInfo>> GetEPS(List<VtiInfo> data);
    }
}
