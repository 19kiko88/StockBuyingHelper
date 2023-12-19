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
        /// <summary>
        /// 取得台股清單(上市.櫃)
        /// </summary>
        /// <returns></returns>
        public Task<List<StockInfoModel>> GetStockList();


        public Task<List<GetHighLowIn52WeeksInfoModel>> GetHighLowIn52Weeks(List<StockPriceInfoModel> realTimeData);

        /// <summary>
        /// 取得即時價格
        /// </summary>
        /// <returns></returns>
        public Task<List<StockPriceInfoModel>> GetPrice();


        public Task<List<VtiInfoModel>> GetVTI(List<StockPriceInfoModel> priceData, List<GetHighLowIn52WeeksInfoModel> highLowData, int amountLimit = 0);


        [Obsolete("近四季EPS取得，改由GetPE從Yahoo Stock取得")]
        public Task<List<EpsInfoModel>> GetEPS(List<VtiInfoModel> data);

        /// <summary>
        /// 取得本益比(PE) & 近四季EPS
        /// 本益比驗證：https://www.cmoney.tw/forum/stock/1256
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public Task<List<PeInfoModel>> GetPE(List<StockPriceInfoModel> data, int taskCount = 20);

        /// <summary>
        /// 取得每月MoM. YoY增減趴數
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public Task<List<RevenueInfoModel>> GetRevenue(List<StockPriceInfoModel> data, int taskCount = 20);


        public Task<List<BuyingResultModel>> GetBuyingResult(List<VtiInfoModel> vtiData, List<PeInfoModel> peData, List<RevenueInfoModel> revenueData);
    }
}
