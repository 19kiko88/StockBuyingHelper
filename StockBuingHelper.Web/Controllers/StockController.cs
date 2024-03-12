using Microsoft.AspNetCore.Mvc;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using System.Diagnostics;
using System.Text;

namespace StockBuingHelper.Web.Controllers
{

    [ApiController]
    [Route("api/[controller]/[action]")]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly IVolumeService _volumeService;
        private readonly IAdminService _adminService;
        private readonly IConfiguration _config;
        private readonly ILogger<StockController> _logger;

        public StockController(
            IStockService stockService, 
            IVolumeService volumeService, 
            IAdminService adminService,
            IConfiguration config,
            ILogger<StockController> logger
            )
        {
            _stockService = stockService;
            _volumeService = volumeService;
            _adminService = adminService;
            _config = config;
            _logger = logger;
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
                _logger.LogInformation("validateMsg OK.");

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

                /*
                 * 選股條件ref：
                 * https://www.ptt.cc/bbs/Stock/M.1680899841.A.5F6.html
                 * https://www.ptt.cc/bbs/Stock/M.1468072684.A.DD1.html
                 * https://www.finlab.tw/%E4%B8%89%E7%A8%AE%E6%9C%88%E7%87%9F%E6%94%B6%E9%80%B2%E9%9A%8E%E7%9C%8B%E6%B3%95/
                 */
                //篩選條件1：股價0~200
                var listStockInfo = _stockService.GteStockInfo(filterIds, reqData.queryEtfs, reqData.priceLow.Value, reqData.priceHigh.Value).Result;
                var list52HighLow = _stockService.GetHighLowIn52Weeks(listStockInfo).Result;
                _logger.LogInformation("GetHighLowIn52Weeks OK.");                

                //篩選條件2：vti(reqData.vtiIndex) > 800
                /*
                 * 改用Result取代await，block執行緒。避免非同步先執行
                 * ref：
                 * https://www.huanlintalk.com/2016/01/async-and-await.html
                 * https://www.52x7.com/index.php/2022/08/19/csharp-async-await-exec-sequence/
                 */
                var listVti = _stockService.GetFilterVTI(list52HighLow, reqData.vtiIndex).Result;
                var vtiFilterIds = listVti.Select(c => c.StockId).ToList();
                _logger.LogInformation("GetFilterVTI OK.");

                #region Yahoo API(要減少Request次數，變免被block)

                #region get Volume
                //篩選條件3：查詢的交易日範圍內，平均成交量大於500
                var volumeData = _stockService.GetFilterVolume(vtiFilterIds, reqData.volume.Value, reqData.volumeTxDateInterval.Value).Result;
                var listVolume = volumeData.Item1;
                var volumeNotInDb = volumeData.Item2;
                _volumeService.InsertVolumeDetail(volumeNotInDb);
                var volumeIds = listVolume.Select(cc => cc.StockId);
                var volumeFilterData = listStockInfo.Where(c => volumeIds.Contains(c.StockId)).ToList();
                yahooApiRequestCount += volumeNotInDb.Count;
                _logger.LogInformation("GetFilterVolume OK.");
                #endregion

                #region get EPS & PE
                //篩選條件4：近四季eps>0, pe<=25。縮小資料範圍
                var listPe = _stockService.GetFilterPe(volumeFilterData, reqData.epsAcc4Q.Value, reqData.pe.Value, _config.GetValue<string>("OperationSystem")).Result;
                var peIds = listPe.Select(cc => cc.StockId);
                var peFilterData = listStockInfo.Where(c => peIds.Contains(c.StockId)).ToList();
                yahooApiRequestCount += volumeFilterData.Count;
                _logger.LogInformation("GetFilterPe OK.");
                #endregion

                #region get Revenue
                //篩選條件5：近3個月的月營收YoY必須為正成長 && 最新的YoY必須要大於0
                var listRevenue = await _stockService.GetFilterRevenue(peFilterData, 6);
                yahooApiRequestCount += peFilterData.Count;
                _logger.LogInformation("GetFilterRevenue OK.");
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
                         volumeDatas = volume.VolumeInfo.OrderByDescending(o => o.txDate).ToList(),
                         vti = vti.VTI,
                         amount = vti.Amount,
                         cfiCode = stockInfo.CFICode
                     }
                    )
                    .OrderByDescending(o => o.cfiCode).ThenByDescending(o => o.eps).ThenByDescending(o => o.amount)
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
                _logger.LogDebug(ex.Message);
                res.Message = ex.ToString();
                sw.Stop();
                return res;
            }

            sw.Stop();
            res.Message += $"Run time：{Math.Round(Convert.ToDouble(sw.ElapsedMilliseconds / 1000), 2)}(s)。YahooApiReqestCount：{yahooApiRequestCount}。";
            res.Success = true;

            return res;
        }

        /// <summary>
        /// 清除db.[Volume_Detail]。重新抓取從Yahoo API抓最新成交資料
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public async Task<Result<int>> DeleteVolumeDetail()
        {
            var res = new Result<int>();
            try
            {
                var data = await _adminService.DeleteVolumeDetail();
                if (string.IsNullOrEmpty(data.errorMsg))
                {
                    res.Success = true;
                }                
                else 
                { 
                    res.Message = data.errorMsg;
                }
            }
            catch (Exception ex)
            {
                res.Message = ex.Message;
            }

            return res;
        }

        //getROE
        //filter0050
        //exceldownload
        //10年線
    }
}
