using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Interfaces;
using System.Data;
using System.Diagnostics;
using System.Security.Claims;

namespace StockBuingHelper.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;
        private readonly AppSettings.CustomizeSettings _appCustSettings;
        private readonly ILogger<StockController> _logger;

        public StockController(
            IStockService stockService, 
            IOptions<AppSettings.CustomizeSettings> appCustSettings,
            ILogger<StockController> logger
            )
        {
            _stockService = stockService;
            _appCustSettings = appCustSettings.Value;
            _logger = logger;
        }

        [HttpPost]
        public async Task<Result<List<BuyingResultDto>>> GetVtiData([FromBody] ResGetVtiDataDto reqData)
        {          
            var sw = new Stopwatch();
            var res = new Result<List<BuyingResultDto>>();
            var yahooApiRequestCount = 0;
            var role = User.Claims.Where(C => C.Type == ClaimTypes.Role).FirstOrDefault()?.Value;

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

                //"manual"
                if (reqData.queryType == "manual" && string.IsNullOrEmpty(reqData.specificStockId))
                {
                    validateMsg += "請輸入股票代碼";
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
                    if (role == "Admin" && reqData.specificStockId.IndexOf(',') > 0)
                    {//Admin才可以手動查詢多筆資料
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

                //篩選條件：UI篩選條件
                var listStockInfo = await _stockService.GetFilterStockInfo(reqData.queryEtfs, filterIds);

                //篩選條件：股價區間，預設0~200
                var listPrice = await _stockService.GetFilterPrice(reqData.priceLow.Value, reqData.priceHigh.Value);

                //篩選條件2：vti(reqData.vtiIndex)，預設80~100
                var listVti = await _stockService.GetFilterVTI(listPrice, reqData.vtiIndex);

                //篩選條件3：營收篩選(近3個月的月營收YoY必須為正成長 && 最新的YoY必須要大於0)
                var listRevenu = await _stockService.GetFilterRevenue(3);

                //篩選條件4：查詢的交易日範圍內，平均成交量大於(預設)500
                var listVolume = await _stockService.GetFilterVolume(reqData.volume.Value);

                //篩選條件5：近四季eps > (預設)1
                var listEps = await _stockService.GetFilterEps(reqData.epsAcc4Q.Value, _appCustSettings.OperationSystem);

                filterIds =
                    (
                    from stock in listStockInfo
                    join price in listPrice on stock.StockId equals price.StockId
                    join vti in listVti on price.StockId equals vti.StockId
                    join revenu in listRevenu on vti.StockId equals revenu.StockId
                    join volume in listVolume on revenu.StockId equals volume.StockId
                    join eps in listEps on volume.StockId equals eps.StockId
                    select stock.StockId).ToList();

                //篩選條件5：pe <= 20
                var listPe = await _stockService.GetFilterPe(filterIds, 6, reqData.pe.Value);
                yahooApiRequestCount += filterIds.Count;

                res.Content =
                    (
                    from stock in listStockInfo
                    join price in listPrice on stock.StockId equals price.StockId
                    join vti in listVti on price.StockId equals vti.StockId
                    join revenu in listRevenu on vti.StockId equals revenu.StockId
                    join volume in listVolume on revenu.StockId equals volume.StockId
                    join eps in listEps on revenu.StockId equals eps.StockId
                    join pe in listPe on revenu.StockId equals pe.StockId
                    select new BuyingResultDto
                     {
                         stockId = stock.StockId,
                         stockName = stock.StockName,
                         price = price.Price,
                         highIn52 = price.HighPriceInCurrentYear,
                         lowIn52 = price.LowPriceInCurrentYear,
                         epsInterval = eps.EpsAcc4QInterval,
                         eps = eps.EpsAcc4Q,
                         pe = pe.Pe,
                         revenueDatas = revenu.RevenueData,
                         volumeDatas = volume.VolumeInfo.OrderByDescending(o => o.txDate).ToList(),
                         vti = Math.Round(vti.Vti * 100, 2),
                         //amount = vti.Amount,
                         cfiCode = stock.CFICode
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

        //getROE
        //filter0050
        //exceldownload
        //10年線
    }
}
