using Microsoft.AspNetCore.Mvc;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models.Models;
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
            var yahooApiRequestCount = 0;

            try
            {
                sw.Start();

                string validateMsg = string.Empty;
                if (reqData.priceHigh < reqData.priceLow)
                {
                    validateMsg += "價格區間錯誤.";
                }

                if (reqData.vtiIndex < 700)
                {
                    validateMsg += "vti至少大(等)於700.";
                }

                if (!(reqData.volumeTxDateInterval >= 3 && reqData.volumeTxDateInterval <= 10))
                {
                    validateMsg += "平均成交量交易日區間必須介於3~10.";
                }

                if (reqData.volume < 500)
                {
                    validateMsg += "平均成交量至少大(等)於500.";                    
                }

                if (!string.IsNullOrEmpty(validateMsg))
                {
                    res.Message = validateMsg;
                    return res;
                }

                var filterIds = new List<string>();
                if (!string.IsNullOrEmpty(reqData.specificStockId))
                {
                    if (reqData.specificStockId.IndexOf(',') > 0)
                    {
                        filterIds = reqData.specificStockId.Split(',').ToList();
                        _stockService.IgnoreFilter = true;
                    }
                    else
                    {
                        filterIds = new List<string> { reqData.specificStockId };
                        _stockService.IgnoreFilter = true;
                    }
                }

                //篩選條件1：股價0~200
                var listStockInfo = await _stockService.GteStockInfo(filterIds, reqData.queryEtfs, reqData.priceLow.Value, reqData.priceHigh.Value);

                var list52HighLow = await _stockService.GetHighLowIn52Weeks(listStockInfo);

                //篩選條件2：vti(reqData.vtiIndex) > 800
                /*
                 * 改用Result取代await，block執行緒。避免非同步先執行
                 * ref：
                 * https://www.huanlintalk.com/2016/01/async-and-await.html
                 * https://www.52x7.com/index.php/2022/08/19/csharp-async-await-exec-sequence/
                 */
                var listVti = _stockService.GetFilterVTI(list52HighLow, reqData.vtiIndex).Result;
                var vtiFilterIds = listVti.Select(c => c.StockId).ToList();

                #region Yahoo API(要減少Request次數，變免被block)

                #region get Volume
                //篩選條件3：查詢的交易日範圍內，平均成交量大於500
                var listVolume = _stockService.GetFilterVolume(vtiFilterIds, reqData.volume.Value, reqData.volumeTxDateInterval.Value).Result;
                var volumeIds = listVolume.Select(cc => cc.StockId);
                var volumeFilterData = listStockInfo.Where(c => volumeIds.Contains(c.StockId)).ToList();
                yahooApiRequestCount += vtiFilterIds.Count;
                #endregion

                #region get EPS & PE
                //篩選條件4：近四季eps>0, pe<=25。縮小資料範圍
                var listPe = _stockService.GetFilterPe(volumeFilterData, reqData.epsAcc4Q.Value, reqData.pe.Value).Result;
                var peIds = listPe.Select(cc => cc.StockId);
                var peFilterData = listStockInfo.Where(c => peIds.Contains(c.StockId)).ToList();
                yahooApiRequestCount += volumeFilterData.Count;
                #endregion

                #region get Revenue
                //篩選條件5：近6個月的月營收YoY只少要有3個月為正成長 && 最新的YoY必須要大於0
                var listRevenue = await _stockService.GetFilterRevenue(peFilterData, 6);
                yahooApiRequestCount += peFilterData.Count;
                #endregion

                #endregion

                res.Content =
                    (from revenue in listRevenue
                     join pe in listPe on revenue.StockId equals pe.StockId
                     join volume in listVolume on pe.StockId equals volume.StockId
                     join vti in listVti on volume.StockId equals vti.StockId
                     join highLowin52 in list52HighLow on vti.StockId equals highLowin52.StockId
                     join stockInfo in listStockInfo on highLowin52.StockId equals stockInfo.StockId
                     select new BuyingResultDto
                     {
                         stockId = stockInfo.StockId,
                         stockName = stockInfo.StockName,
                         price = stockInfo.Price,
                         highIn52 = highLowin52.HighPriceInCurrentYear,
                         lowIn52 = highLowin52.LowPriceInCurrentYear,
                         epsInterval = pe.EpsAcc4QInterval,
                         eps = pe.EpsAcc4Q,
                         pe = pe.PE,
                         revenueDatas = revenue.RevenueData,
                         volumeDatas = volume.VolumeInfo,
                         vti = vti.VTI,
                         amount = vti.Amount,
                         cfiCode = stockInfo.CFICode
                     }
                    )
                    .OrderByDescending(o => o.cfiCode).ThenByDescending(o => o.amount)
                    .ToList();

                var idx = 0;
                foreach (var item in res.Content)
                {
                    idx++;
                    item.sn = idx.ToString();
                }

                res.Success = true;
            }
            catch (Exception ex)
            {
                res.Message = ex.Message;
            }

            sw.Stop();
            res.Message += $"Run time：{Math.Round(Convert.ToDouble(sw.ElapsedMilliseconds / 1000), 2)}(s)。YahooApiReqestCount：{yahooApiRequestCount}。";
            res.Success = true;

            return res;
        }

        //getROE
        //filter0050
        //exceldownload
        //10年線
    }
}
