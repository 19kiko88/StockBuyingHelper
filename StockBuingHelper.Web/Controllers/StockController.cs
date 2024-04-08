using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models;
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
        private readonly AppSettings.CustomizeSettings _appCustSettings;
        private readonly ILogger<StockController> _logger;

        public StockController(
            IStockService stockService, 
            IVolumeService volumeService, 
            IAdminService adminService,
            IOptions<AppSettings.CustomizeSettings> appCustSettings,
            ILogger<StockController> logger
            )
        {
            _stockService = stockService;
            _volumeService = volumeService;
            _adminService = adminService;
            _appCustSettings = appCustSettings.Value;
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
                if (reqData.queryType == "0050")
                {
                    _stockService.IgnoreFilter = true;
                    filterIds = _appCustSettings.List0050;
                }
                else if (!string.IsNullOrEmpty(reqData.specificStockId))
                {
                    _stockService.IgnoreFilter = true;
                    if (reqData.specificStockId.IndexOf(',') > 0)
                    {
                        filterIds = reqData.specificStockId.Split(',').ToList();
                    }
                    else
                    {
                        filterIds = new List<string> { reqData.specificStockId };
                    }
                }
                    



                /*
                 * 選股條件ref：
                 * https://www.ptt.cc/bbs/Stock/M.1680899841.A.5F6.html
                 * https://www.ptt.cc/bbs/Stock/M.1468072684.A.DD1.html
                 * https://www.finlab.tw/%E4%B8%89%E7%A8%AE%E6%9C%88%E7%87%9F%E6%94%B6%E9%80%B2%E9%9A%8E%E7%9C%8B%E6%B3%95/
                 */
                //篩選條件1：股價0~200
                var listStockInfo = await _stockService.GteStockInfo(filterIds, reqData.queryEtfs, reqData.priceLow.Value, reqData.priceHigh.Value);
                var list52HighLow = await _stockService.GetHighLowIn52Weeks(listStockInfo);
                _logger.LogInformation("GetHighLowIn52Weeks OK.");                

                //篩選條件2：vti(reqData.vtiIndex) > 800
                var listVti = await _stockService.GetFilterVTI(list52HighLow, reqData.vtiIndex);
                var vtiFilterIds = listVti.Select(c => c.StockId).ToList();
                _logger.LogInformation("GetFilterVTI OK.");

                #region Yahoo API(要減少Request次數，變免被block)

                #region get Volume
                //篩選條件3：查詢的交易日範圍內，平均成交量大於500
                var volumeData = await _stockService.GetFilterVolume(vtiFilterIds, reqData.volume.Value, reqData.volumeTxDateInterval.Value);
                var listVolume = volumeData.Item1;
                var volumeNotInDb = volumeData.Item2;
                _volumeService.InsertVolumeDetail(volumeNotInDb);
                var volumeIds = listVolume.Select(cc => cc.StockId);
                var volumeFilterData = listStockInfo.Where(c => volumeIds.Contains(c.StockId)).ToList();
                yahooApiRequestCount += volumeNotInDb.Count;
                _logger.LogInformation("GetFilterVolume OK.");
                #endregion

                #region get EPS & PE
                //篩選條件4：近四季eps>1。縮小資料範圍
                var listEps = await _stockService.GetFilterEps(volumeFilterData, reqData.epsAcc4Q.Value, _appCustSettings.OperationSystem);
                var epsIds = listEps.Select(cc => cc.StockId);
                var epsFilterData = listStockInfo.Where(c => epsIds.Contains(c.StockId)).ToList();
                yahooApiRequestCount += volumeFilterData.Count;
                _logger.LogInformation("GetFilterPe OK.");
                #endregion

                #region get Revenue
                /*篩選條件5：
                 * 5-1：近3個月的月營收YoY必須為正成長 && 最新的YoY必須要大於0
                 * 5-2：pe <= 20
                 */
                var listRevenue = await _stockService.GetFilterRevenueAndPe(epsFilterData, 6, reqData.pe.Value);
                yahooApiRequestCount += epsFilterData.Count;
                _logger.LogInformation("GetFilterRevenue OK.");
                #endregion

                #endregion

                res.Content =
                    (from revenue in listRevenue
                     join eps in listEps on revenue.StockId equals eps.StockId
                     join volume in listVolume on eps.StockId equals volume.StockId
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
                         epsInterval = eps.EpsAcc4QInterval,
                         eps = eps.EpsAcc4Q,
                         pe = revenue.pe,
                         revenueDatas = revenue.RevenueData,
                         volumeDatas = volume.VolumeInfo.OrderByDescending(o => o.txDate).ToList(),
                         vti = Math.Round(vti.VTI * 100, 2),
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
