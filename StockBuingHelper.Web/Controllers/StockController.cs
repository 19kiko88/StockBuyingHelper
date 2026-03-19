using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using System.Text;

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
        public async Task<Result<List<BuyingResultDto>>> GetVtiData([FromBody] ReqGetVtiDataDto reqData)
        {
            var sw = new Stopwatch();
            var res = new Result<List<BuyingResultDto>>();
            var yahooApiRequestCount = 0;
            var role = User.Claims.Where(C => C.Type == ClaimTypes.Role).FirstOrDefault()?.Value;

            try
            {
                sw.Start();

                string validateMsg = string.Empty;

                var highLow52Path = _appCustSettings.PathSettings.HighLow52Data;                
                if (!Path.Exists(highLow52Path))
                {
                    Directory.CreateDirectory(highLow52Path);
                }

                if (Directory.GetFiles(highLow52Path, "*.csv").Any() == false)
                {
                    validateMsg += "查無52周區間內最高&最低價資料csv檔.";
                }

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
                    filterIds = _stockService.Get0050List().Result;
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
                _logger.LogInformation($"filterIds => {JsonConvert.SerializeObject(filterIds)}");

                /*
                 * 選股條件ref：
                 * https://www.ptt.cc/bbs/Stock/M.1680899841.A.5F6.html
                 * https://www.ptt.cc/bbs/Stock/M.1468072684.A.DD1.html
                 * https://www.finlab.tw/%E4%B8%89%E7%A8%AE%E6%9C%88%E7%87%9F%E6%94%B6%E9%80%B2%E9%9A%8E%E7%9C%8B%E6%B3%95/
                 * 
                 * 資料來源：
                 * GetPrice => histock
                 * 52周高低價 => csv檔案(source：goodinfo)
                 * GetRevenue() 營收資料 => Yahoo
                 * GetVolume() 每日成交量資料 => Yahoo
                 * GetEps() 近四季EPS => Yahoo
                 * 
                 * 排程執行：
                 * 每日 Coravel排程 自動更新下面3項資料：Volume(成交量). Revenue(營收). Eps(近四季EPS)。分開執行，避免單次請求過多被YAHOO block
                 * builder.Services.AddTransient<RefreshVolumeInfoTask>();
                 * builder.Services.AddTransient<RefreshRevenueInfoTask>();
                 * builder.Services.AddTransient<RefreshEpsInfoTask>();
                 */
                //篩選條件：UI篩選條件
                var listStockInfo = await _stockService.GetFilterStockInfo(reqData.queryEtfs, filterIds);
                _logger.LogInformation($"listStockInfo count => {listStockInfo.Count}");

                //篩選條件：股價區間，預設0~200
                var listPrice = await _stockService.GetFilterPrice(reqData.priceLow.Value, reqData.priceHigh.Value);
                _logger.LogInformation($"listPrice count => {listPrice.Count}");

                //篩選條件2：vti(reqData.vtiIndex)，預設80~100
                var listVti = await _stockService.GetFilterVTI(listPrice, reqData.vtiIndex);
                _logger.LogInformation($"listVti count => {listVti.Count}");

                //篩選條件3：營收篩選(近3個月的月營收YoY必須為正成長 && 最新的YoY必須要大於0)
                var listRevenu = await _stockService.GetFilterRevenue(3);
                _logger.LogInformation($"listRevenu count => {listRevenu.Count}");

                //篩選條件4：查詢的交易日範圍內，平均成交量大於(預設)500
                var listVolume = await _stockService.GetFilterVolume(reqData.volume.Value);
                _logger.LogInformation($"listVolume count => {listVolume.Count}");

                //篩選條件5：近四季eps > (預設)1
                var listEps = await _stockService.GetFilterEps(reqData.epsAcc4Q.Value);
                _logger.LogInformation($"listEps count => {listEps.Count}");

                //中繼篩選結果，減少查詢的股票數量，避免重複呼叫Yahoo API被block
                filterIds =
                    (
                    from stock in listStockInfo
                    join price in listPrice on stock.StockId equals price.StockId
                    join vti in listVti on price.StockId equals vti.StockId
                    join revenu in listRevenu on vti.StockId equals revenu.StockId
                    join volume in listVolume on revenu.StockId equals volume.StockId
                    join eps in listEps on volume.StockId equals eps.StockId
                    select stock.StockId).ToList();

                //篩選條件6：pe <= 20
                var listPe = await _stockService.GetFilterPe(filterIds, 6, reqData.pe.Value);                
                yahooApiRequestCount += filterIds.Count;
                _logger.LogInformation($"listPe count => {listPe.Count}");

                //篩選條件7：近四季roe > 15%
                var listRoeRoa = await _stockService.GetFilterRoeRoa(filterIds);
                _logger.LogInformation($"listRoeRoa count => {listRoeRoa.Count}");

                //篩選條件8：[近一季EPS增長率要大於0%] & [當前價格 > MA20](先取消)
                var listHiStockData = await _stockService.GetFilterHiStockData(filterIds);
                _logger.LogInformation($"listHiStockData count => {listHiStockData.Count}");

                res.Content =
                    (
                    from stock in listStockInfo
                    join price in listPrice on stock.StockId equals price.StockId
                    join vti in listVti on price.StockId equals vti.StockId
                    join revenu in listRevenu on vti.StockId equals revenu.StockId
                    join volume in listVolume on revenu.StockId equals volume.StockId
                    join eps in listEps on revenu.StockId equals eps.StockId
                    join pe in listPe on revenu.StockId equals pe.StockId
                    join roe in listRoeRoa on revenu.StockId equals roe.StockId
                    join hiStockData in listHiStockData on revenu.StockId equals hiStockData.StockId
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
                         roe = roe.SumROE, 
                         //ma20 = cmMoneyData.MA20,
                         epsGrowthQoQ = hiStockData.EpsGrowthQoQ,
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

        [HttpPost]
        [AllowAnonymous] // 允許匿名訪問，不用jwt
        public IActionResult SaveHighLow52ToCsv([FromBody] List<ReqHighLow52Dto> data)
        {
            var msg = string.Empty;
            var filePath = string.Empty;
            var fileName = string.Empty;

            if (data != null && data.Count > 0)
            {
                var dataModel =
                (
                    from item in data
                    select new StockHighLowIn52WeeksInfoModel
                    {
                        StockId = item.StockId,
                        HighPriceInCurrentYear = item.High52,
                        LowPriceInCurrentYear = item.Low52
                    }
                ).ToList();

                var csvBuilder = new StringBuilder();

                // add csv header.
                csvBuilder.AppendLine("StockId,HighPriceInCurrentYear,LowPriceInCurrentYear");
                // 寫入數據行 (Data Rows)
                foreach (var record in dataModel)
                {
                    // 確保數值 (double) 和時間戳 (string) 被正確格式化                    
                    string line = $"\"{record.StockId}\",\"{record.HighPriceInCurrentYear}\",\"{record.LowPriceInCurrentYear}\"";
                    csvBuilder.AppendLine(line);
                }
                // 獲取 CSV 內容字串
                string csvContent = csvBuilder.ToString();


                #region save file to server                
                string exportFolder = _appCustSettings.PathSettings!.HighLow52Data;

                // 檢查資料夾是否存在，不存在則建立
                if (!Directory.Exists(exportFolder))
                {
                    Directory.CreateDirectory(exportFolder);
                }

                // 生成檔案名稱，使用 GUID 確保唯一性
                //fileName = $"Export_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}_{Guid.NewGuid()}.csv";
                fileName = $"StockList_{DateTime.Now.ToString("yyyyMMdd")}.csv";
                filePath = Path.Combine(exportFolder, fileName);

                // 寫入檔案，使用 UTF-8 編碼
                System.IO.File.WriteAllText(filePath, csvContent, Encoding.UTF8);
                #endregion

                msg = $"成功處理 {dataModel.Count} 筆資料並儲存為 CSV。";

            }

            return Ok(new
            {
                message = msg,
                path = filePath,
                fileName = fileName
            });
        }

        [HttpPost]
        [AllowAnonymous] // 允許匿名訪問，不用jwt
        public IActionResult Save0050List([FromBody] List<string> data)
        {
            var msg = string.Empty;
            var filePath = string.Empty;
            var fileName = string.Empty;

            if (data != null && data.Count > 0)
            {
                try
                {
                    var csvBuilder = new StringBuilder();

                    // add csv header.
                    csvBuilder.AppendLine("StockId");

                    // 寫入數據行 (Data Rows)
                    foreach (var record in data)
                    {
                        csvBuilder.AppendLine($"\"{record}\"");
                    }
                    // 獲取 CSV 內容字串
                    string csvContent = csvBuilder.ToString();


                    #region save file to server                
                    string exportFolder = _appCustSettings.PathSettings!.List0050Data;

                    // 檢查資料夾是否存在，不存在則建立
                    if (!Directory.Exists(exportFolder))
                    {
                        Directory.CreateDirectory(exportFolder);
                    }

                    // 生成檔案名稱，使用 GUID 確保唯一性
                    //fileName = $"Export_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}_{Guid.NewGuid()}.csv";
                    fileName = $"0050_{DateTime.Now.ToString("yyyyMMdd")}.csv";
                    filePath = Path.Combine(exportFolder, fileName);

                    // 寫入檔案，使用 UTF-8 編碼
                    System.IO.File.WriteAllText(filePath, csvContent, Encoding.UTF8);
                    #endregion

                    msg = $"成功處理 {data.Count} 筆資料並儲存為 CSV。";
                }
                catch (Exception ex)
                {
                    return BadRequest(new
                    {
                        message = $"API [Save0050List] 儲存失敗：{ex.Message}",
                        path = "",
                        fileName = ""
                    });
                }


            }

            return Ok(new
            {
                message = msg,
                path = filePath,
                fileName = fileName
            });
        }

        //getROE
        //filter0050
        //exceldownload
        //10年線
    }
}
