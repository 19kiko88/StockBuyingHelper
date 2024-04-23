﻿using SBH.Repositories.Models;
using StockBuyingHelper.Service.Dtos;
using StockBuyingHelper.Service.Enums;
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
        public bool IgnoreFilter { get; set; }

        /// <summary>
        /// 取得台股清單(上市.櫃)
        /// </summary>
        /// <returns></returns>
        public Task<List<StockInfoModel>> GetStockInfo(MarketEnum market);

        /// <summary>
        /// 篩選台股清單(上市.櫃)
        /// </summary>
        /// <param name="queryEtfs">是否顯示ETF個股</param>
        /// <param name="specificIds">指定股票代碼</param>
        /// <returns></returns>
        public Task<List<StockInfoDto>> GetFilterStockInfo(bool queryEtfs, List<string>? specificIds = null);

        ///// <summary>
        ///// 取得個股基本資料 & 價格
        ///// </summary>
        ///// <param name="filterIds"></param>
        ///// <param name="queryEtfs"></param>
        ///// <param name="priceLow"></param>
        ///// <param name="priceHigh"></param>
        ///// <returns></returns>
        //public Task<List<StockInfoModel>> GetStockInfo(List<string>? filterIds, bool queryEtfs, decimal priceLow = 0, decimal priceHigh = 99999);

        /// <summary>
        /// 取得即時價格
        /// </summary>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public Task<List<StockPriceInfoModel>> GetPrice(/*List<string>? specificIds = null, */int taskCount = 25);

        /// <summary>
        /// 篩選即時價格
        /// </summary>
        /// <param name="priceLow">價格區間下限</param>
        /// <param name="priceHigh">價格區間上限</param>
        /// <returns></returns>
        public Task<List<PriceInfoDto>> GetFilterPrice(decimal priceLow = 0, decimal priceHigh = 200);

        /// <summary>
        /// 取得52周間最高 & 最低價(非最終成交價)
        /// </summary>
        /// <param name="realTimeData">即時成交價</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public Task<List<StockHighLowIn52WeeksInfoModel>> GetHighLowIn52Weeks(/*List<StockInfoModel> realTimeData, */int taskCount = 25);

        /// <summary>
        /// 取得近52周最高最低價格區間內，目前價格離最高價還有多少百分比，並換算成vti係數(vti越高，表示離52周區間內最高點越近)
        /// </summary>
        /// <param name="highLowData">取得52周間最高 & 最低價資料(非最終成交價)</param>
        /// <returns></returns>
        public Task<List<StockVtiInfoModel>> GetVTI(List<PriceInfoDto> highLowData);

        /// <summary>
        /// 篩選vti係數
        /// </summary>
        /// <param name="priceData">取得52周間最高 & 最低價資料(非最終成交價)</param>
        /// <param name="amountLimit">vti轉換後的購買股數</param>
        /// <returns></returns>
        public Task<List<VtiInfoDto>> GetFilterVTI(List<PriceInfoDto> priceData, int[] vtiRange);

        /// <summary>
        /// 每日成交量資訊
        /// </summary>
        /// <param name="vtiDataIds">資料來源</param>
        /// <param name="txDateCount">交易日</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public Task<List<StockVolumeInfoModel>> GetVolume(List<string> ids = null, int txDateCount = 10, int taskCount = 25);

        /// <summary>
        /// 篩選每日成交量資訊
        /// </summary>
        /// <param name="vtiDataIds">資料來源</param>
        /// <param name="volumeKLimit">成交量</param>
        /// <param name="txDateCount">顯示交易日</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public Task<List<StockVolumeInfoModel>> GetFilterVolume(int volumeKLimit = 500);

        /// <summary>
        /// 取得本益比(PE) & 近四季EPS
        /// 本益比驗證：https://www.cmoney.tw/forum/stock/1256
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public Task<List<EpsInfoDto>> GetEps(string Os = "Windows", int taskCount = 25);

        /// <summary>
        /// 篩選本益比(PE) & 近四季EPS
        /// </summary>
        /// <param name="eps">近四季EPS篩選條件</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public Task<List<EpsInfoDto>> GetFilterEps(decimal eps = 0, string Os = "Windows", int taskCount = 25);

        public Task<List<RevenueInfoModel>> GetRevenue(List<string>? ids = null, int revenueMonthCount = 3, int taskCount = 25);

        public Task<List<RevenueInfoModel>> GetFilterRevenue(int revenueTakeCount = 3);

        /// <summary>        
        /// 取得每月MoM. YoY增減趴數. PE (同一個頁面可以同時取得每月MoM. YoY增減趴數. PE，減少Request次數)
        /// </summary>
        /// <param name="ids">資料來源</param>
        /// <param name="revenueMonthCount">顯示營收資料筆數(by 月)</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public Task<List<PeInfoModel>> GetPe(List<string> ids, int revenueMonthCount = 3, int taskCount = 25);

        /// <summary>
        /// 篩選取得每月MoM. YoY增減趴數. PE
        /// </summary>
        /// <param name="ids">資料來源</param>
        /// <param name="revenueMonthCount">顯示營收資料筆數(by 月)</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public Task<List<PeInfoModel>> GetFilterPe(List<string> ids, int revenueMonthCount = 3, double pe = 20, int taskCount = 25);
    }
}
