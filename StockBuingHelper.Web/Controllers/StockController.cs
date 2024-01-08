﻿using Microsoft.AspNetCore.Mvc;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Dtos;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using System.Diagnostics;

namespace StockBuingHelper.Web.Controllers
{

    [ApiController]
    [Route("api/[controller]/[action]")]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private static List<StockVolumeInfoModel> lockVolumeObj = new List<StockVolumeInfoModel>();

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        [HttpPost]
        public async Task<Result<List<BuyingResultDto>>> GetVtiData([FromBody] ResGetVtiDataDto reqData)
        {
            var sw = new Stopwatch();
            var res = new Result<List<BuyingResultDto>>();

            try
            {
                sw.Start();

                var filterIds = new List<string>();
                if (!string.IsNullOrEmpty(reqData.specificStockId))
                {
                    if (reqData.specificStockId.IndexOf(',') > 0)
                    {
                        filterIds = reqData.specificStockId.Split(',').ToList();
                        _stockService.SpecificStockId = true;
                    }
                    else
                    {
                        filterIds = new List<string> { reqData.specificStockId };
                        _stockService.SpecificStockId = true;
                    }
                }

                var listStockInfo = await _stockService.GetFilterStockList(reqData.queryEtfs, filterIds);

                //篩選條件1.：股價0~200。縮小資料範圍
                var ids = listStockInfo.Select(c => c.StockId).ToList();
                Thread.Sleep(1500);
                var listPrice = await _stockService.GetFilterPriceList(ids, 0, 200);

                var listHighLow = await _stockService.GetHighLowIn52Weeks(listPrice);

                //篩選條件2.：vti(reqData.vtiIndex) > 800。縮小資料範圍
                var listVti = await _stockService.GetFilterVTI(listPrice, listHighLow, filterIds.Count > 0 ? 0 : reqData.vtiIndex);

                #region Yahoo API(要減少Request次數，變免被block)

                #region get Volume
                //篩選條件3.：近3個交易日，必須有一天成交量大於500。縮小資料範圍
                var listVolume = await _stockService.GetFilterVolumeList(listVti, 500, 7);
                //dto for parameter
                var volumeDto =
                    (from a in listVolume
                     join b in listPrice on a.StockId equals b.StockId
                     join c in listStockInfo on b.StockId equals c.StockId
                     select new StockInfoDto
                     {
                         StockId = c.StockId,
                         StockName = c.StockName,
                         Price = b.Price,
                         Type = c.CFICode
                     }).ToList();
                #endregion



                #region get EPS & PE
                //篩選條件4.：近四季eps>0, pe<=25。縮小資料範圍
                var listPe = await _stockService.GetFilterPeList(volumeDto, 0, 25);
                #endregion



                #region get Revenue
                var listRevenue = await _stockService.GetRevenue(listPe, 3);
                #endregion


                #endregion


                var buyingList = await _stockService.GetBuyingResult(listStockInfo, listVti, listPe, listRevenue, listVolume, reqData.specificStockId);
                
                res.Content = buyingList.Select((c, idx) => new BuyingResultDto
                {
                    sn = (idx + 1).ToString(),
                    stockId = c.StockId,
                    stockName = c.StockName,
                    price = c.Price,
                    highIn52 = c.HighIn52,
                    lowIn52 = c.LowIn52,
                    epsInterval = c.EpsInterval,
                    eps = c.EPS,
                    pe = c.PE,
                    revenueDatas = c.RevenueDatas,
                    volumeDatas = c.VolumeDatas,
                    vti = c.VTI,
                    amount = c.Amount
                }).ToList();

                res.Success = true;
            }
            catch (Exception ex)
            {
                res.Message = ex.Message;
            }

            sw.Stop();
            res.Message += $"Run time：{Math.Round(Convert.ToDouble(sw.ElapsedMilliseconds / 1000), 2)}(s)。";

            return res;
        }

        //getROE
        //filter0050
        //exceldownload
        //10年線
    }
}
