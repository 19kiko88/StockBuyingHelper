using Microsoft.AspNetCore.Mvc;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service;
using StockBuyingHelper.Service.Dtos;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using StockBuyingHelper.Service.Utility;
using System.Diagnostics;

namespace StockBuingHelper.Web.Controllers
{

    [ApiController]
    [Route("api/[controller]/[action]")]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly int cacheExpireTime = 1440;//快取保留時間

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        [HttpPost]
        public async Task<Result<List<BuyingResultDto>>> GetVtiData([FromBody] ResGetVtiDataDto reqData)
        {
            var sw = new Stopwatch();
            var res = new Result<List<BuyingResultDto>>();
            var listStockInfo = new List<StockInfoModel>();
            try
            {
                sw.Start();

                if (AppCacheUtils.IsSet(CacheType.StockList) == false)
                {
                    listStockInfo = await _stockService.GetStockList();
                    AppCacheUtils.Set(CacheType.StockList, listStockInfo, AppCacheUtils.Expiration.Absolute, cacheExpireTime);
                }
                else
                {
                    listStockInfo = (List<StockInfoModel>)AppCacheUtils.Get(CacheType.StockList);                    
                }

                var ids = listStockInfo.Select(c => c.StockId).ToList();
                Thread.Sleep(1500);//ids為非同步取得，sleep 1.5s

                var listPrice = await _stockService.GetPrice(ids);

                var listHighLow = await _stockService.GetHighLowIn52Weeks(listPrice);

                var listVti = await _stockService.GetVTI(listPrice, listHighLow, reqData.specificStockId, reqData.vtiIndex);

                var data =
                    (from a in listStockInfo
                     join b in listVti on a.StockId equals b.StockId
                     select new StockInfoDto
                     {
                         StockId = a.StockId,
                         StockName = a.StockName,
                         Price = b.Price,
                         Type = a.CFICode
                     }).AsEnumerable();

                if (string.IsNullOrEmpty(reqData.specificStockId) && reqData.queryEtfs == false)
                {
                    data = data.Where(c => c.Type == StockType.ESVUFR).AsEnumerable();
                }
                var filterData = data.ToList();


                var listPe = await _stockService.GetPE(filterData);
                Thread.Sleep(5000);//間隔5秒，避免被誤認攻擊

                var listRevenue = await _stockService.GetRevenue(filterData, 3);
                Thread.Sleep(5000);//間隔5秒，避免被誤認攻擊

                var listVolume = await _stockService.GetVolume(filterData, 7);

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
