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
        public Task<List<StockInfoModel>> GetStockList();
        public Task<List<GetHighLowIn52WeeksInfoModel>> GetHighLowIn52Weeks();
        public Task<List<StockPriceModel>> GetPrice();
        public Task<List<VtiInfoModel>> GetVTI(List<StockPriceModel> priceData, List<GetHighLowIn52WeeksInfoModel> highLowData, int amountLimit = 0);
        public Task<List<EpsInfoModel>> GetEPS(List<VtiInfoModel> data);
        public Task<List<PeInfoModel>> GetPE(List<VtiInfoModel> data);
        public Task<List<RevenueInfoModel>> GetRevenue(List<VtiInfoModel> data);
        public Task<List<BuyingResultModel>> GetBuyingResult(List<VtiInfoModel> vtiData, List<PeInfoModel> peData, List<RevenueInfoModel> revenueData);
    }
}
