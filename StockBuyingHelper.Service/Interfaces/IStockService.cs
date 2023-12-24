using StockBuyingHelper.Service.Dtos;
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

        /// <summary>
        /// 取得52周間最高 & 最低價(非最終成交價)
        /// </summary>
        /// <param name="realTimeData">即時成交價</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public Task<List<StockHighLowIn52WeeksInfoModel>> GetHighLowIn52Weeks(List<StockInfoDto> realTimeData, int taskCount = 25);

        /// <summary>
        /// 每日成交量資訊
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="txDateCount">交易日(3~10)</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public Task<List<StockVolumeInfoModel>> GetVolume(List<StockInfoDto> data, int txDateCount = 10, int taskCount = 25);

        /// <summary>
        /// 取得即時價格
        /// </summary>
        /// <returns></returns>
        public Task<List<StockInfoDto>> GetPrice();

        /// <summary>
        /// 取得近52周最高最低價格區間內，目前價格離最高價還有多少百分比，並換算成vti係數(vti越高，表示離52周區間內最高點越近)
        /// </summary>
        /// <param name="priceData">即時價格資料</param>
        /// <param name="highLowData">取得52周間最高 & 最低價資料(非最終成交價)</param>
        /// <param name="amountLimit">vti篩選，vti值必須在多少以上</param>
        /// <returns></returns>
        public Task<List<VtiInfoModel>> GetVTI(List<StockInfoDto> priceData, List<StockHighLowIn52WeeksInfoModel> highLowData, int amountLimit = 0);

        /// <summary>
        /// 取得本益比(PE) & 近四季EPS
        /// 本益比驗證：https://www.cmoney.tw/forum/stock/1256
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public Task<List<PeInfoModel>> GetPE(List<StockInfoDto> data, int taskCount = 25);

        /// <summary>
        /// 取得每月MoM. YoY增減趴數
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public Task<List<RevenueInfoModel>> GetRevenue(List<StockInfoDto> data, int revenueMonthCount = 3, int taskCount = 25);


        /// <summary>
        /// 取得總表
        /// </summary>
        /// <param name="stockData">個股基本資料</param>
        /// <param name="vtiData">VTI資料</param>
        /// <param name="peData">近四季EPS&PE資料</param>
        /// <param name="revenueData">近三個月營收MoM. YoY資料</param>
        /// <returns></returns>
        public Task<List<BuyingResultModel>> GetBuyingResult(List<StockInfoModel> stockData, List<VtiInfoModel> vtiData, List<PeInfoModel> peData, List<RevenueInfoModel> revenueData, List<StockVolumeInfoModel> volumeData);

        [Obsolete("近四季EPS取得，改由GetPE從Yahoo Stock取得")]
        public Task<List<ObsoleteEpsInfoModel>> GetEPS(List<VtiInfoModel> data);
    }
}
