using System.Data;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using AngleSharp;
using SBH.Repositories.Models;
using StockBuyingHelper.Service.Dtos;
using StockBuyingHelper.Service.Enums;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using StockBuyingHelper.Service.Utility;


namespace StockBuyingHelper.Service.Implements
{
    public class StockService: IStockService
    {
        public bool IgnoreFilter { get; set; }
        private readonly object _lock = new object();
        private readonly int cacheExpireTime = 1440;//快取保留時間
        private readonly IVolumeService _volumeService;
        private readonly SBHContext _context;

        public StockService(
            SBHContext context,
            IVolumeService volumeService
            )
        {
            _context = context;
            _volumeService = volumeService;
        }

        /// <summary>
        /// 取得台股清單(上市.櫃)
        /// </summary>
        /// <returns></returns>
        public async Task<List<StockInfoModel>> GetStockInfo(MarketEnum market)
        {
            var res = new List<StockInfoModel>();


            if (AppCacheUtils.IsSet(CacheType.StockList) == false)
            {
                var httpClient = new HttpClient();
                var url = $"https://isin.twse.com.tw/isin/C_public.jsp?strMode={Convert.ToInt16(market)}";//本國上市證券國際證券辨識號碼一覽表
                var resMessage = await httpClient.GetAsync(url);

                //檢查回應的伺服器狀態StatusCode是否是200 OK
                if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//註冊BIG 5編碼
                    using (var sr = new StreamReader(await resMessage.Content.ReadAsStreamAsync(), Encoding.GetEncoding(950)))
                    {
                        var htmlString = sr.ReadToEnd();
                        var config = Configuration.Default;
                        var context = BrowsingContext.New(config);
                        var document = await context.OpenAsync(res => res.Content(htmlString));
                        var listTR = document.QuerySelectorAll("tr:not([colspan])");

                        //ESVUFR：一般股, ETFs：ETF
                        var filterListTr = listTR.Where(c =>
                            c.Children.Count() >= 7
                            && (c.Children[5].TextContent == StockType.ESVUFR || StockType.ETFs.Contains(c.Children[5].TextContent))
                            ).ToList();

                        foreach (var tr in filterListTr)
                        {
                            var arrayTd = tr.Children.ToArray();
                            res.Add(new StockInfoModel()
                            {
                                StockId = arrayTd[0].TextContent.Split('　')[0],
                                StockName = arrayTd[0].TextContent.Split('　').Length == 2 ? arrayTd[0].TextContent.Split('　')[1] : "",
                                ISINCode = arrayTd[1].TextContent,
                                Market = arrayTd[3].TextContent,
                                IndustryType = arrayTd[4].TextContent,
                                CFICode = arrayTd[5].TextContent,
                                Note = arrayTd[6].TextContent
                            });
                        }
                    }
                }

                AppCacheUtils.Set(CacheType.StockList, res, AppCacheUtils.Expiration.Absolute, cacheExpireTime);
            }

            res = (List<StockInfoModel>)AppCacheUtils.Get(CacheType.StockList);

            res = res.Where(c => c.CFICode == StockType.ESVUFR || StockType.ETFs.Contains(c.CFICode)).ToList();

            return res;
        }

        /// <summary>
        /// 篩選台股清單(上市.櫃)
        /// </summary>
        /// <param name="queryEtfs">是否顯示ETF個股</param>
        /// <param name="specificIds">指定股票代碼</param>
        /// <returns></returns>
        public async Task<List<StockInfoDto>> GetFilterStockInfo(bool queryEtfs, List<string>? specificIds = null)
        {
            var data = _context.Stock_Info;//await GetStockList();
            var data_ = data.AsEnumerable();

            if (IgnoreFilter == false && queryEtfs == false)
            {
                data_ = data_.Where(c => c.CFICode == StockType.ESVUFR).AsEnumerable();
            }

            if (specificIds != null && specificIds.Count > 0)
            {
                data_ = data_.Where(c => specificIds.Contains(c.Stock_Id)).AsEnumerable();
            }

            var res = data_.Select(c => new StockInfoDto()
            {
                StockId = c.Stock_Id,
                StockName = c.Stock_Name,
                CFICode = c.CFICode,
                IndustryType = c.Industry_Type,
            }).ToList();

            return res;
        }

        /// <summary>
        /// 取得即時價格
        /// </summary>
        /// <param name="specificIds">指定特定股票代碼</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<StockPriceInfoModel>> GetPrice(/*List<string>? specificIds = null, */int taskCount = 25)
        {
            var res = new List<StockPriceInfoModel>();

            var httpClient = new HttpClient();
            //var url = $"https://mis.twse.com.tw/stock/api/getStockInfo.jsp?ex_ch=tse_{id}.tw&json=1&delay=0";
            //var url = "https://histock.tw/stock/rank.aspx?&p=1&d=1";
            var url = "https://histock.tw/stock/rank.aspx?p=all";

            var resMessage = await httpClient.GetAsync(url);

            //檢查回應的伺服器狀態StatusCode是否是200 OK
            if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var sr = await resMessage.Content.ReadAsStringAsync();
                var config = Configuration.Default;
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(res => res.Content(sr));

                var listTR = document.QuerySelectorAll("#CPHB1_gv tr").Skip(1);//.Where((c,idx) => idx > 0);
                //if (specificIds != null && specificIds.Count > 0)
                //{
                //    listTR = listTR.Where(c => specificIds.Contains(c.Children[0].InnerHtml));
                //}
                var group = TaskUtils.GroupSplit(listTR.ToList(), taskCount);//分群組 for 多執行緒分批執行
                var tasks = new Task[group.Count];

                for (int i = 0; i < tasks.Length; i++)
                {
                    var groupData = group[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        foreach (var tr in groupData)
                        {
                            lock (_lock)
                            {
                                res.Add(new StockPriceInfoModel()
                                {
                                    StockId = tr.Children[0].InnerHtml,
                                    Price = Convert.ToDecimal(tr.Children[2].Children[0].InnerHtml)
                                });
                            }
                        }
                    });
                }
                Task.WaitAll(tasks);
            }         

            return res;
        }

        /// <summary>
        /// 篩選即時價格
        /// </summary>
        /// <param name="priceLow">價格區間下限</param>
        /// <param name="priceHigh">價格區間上限</param>
        /// <returns></returns>
        public async Task<List<PriceInfoDto>> GetFilterPrice(decimal priceLow = 0, decimal priceHigh = 200)
        {
            var currentPriceData = await GetPrice();
            var highLowPriceIn52WeeksData = await GetHighLowIn52Weeks();

            var data =
                (from a in currentPriceData
                 join b in highLowPriceIn52WeeksData on a.StockId equals b.StockId
                 select new PriceInfoDto
                 {
                     StockId = a.StockId,
                     Price = a.Price,
                     HighPriceInCurrentYear = b.HighPriceInCurrentYear,
                     HighPriceInCurrentYearPercentGap = b.HighPriceInCurrentYear > 0 ? Convert.ToDouble(Math.Round(((a.Price - b.HighPriceInCurrentYear) / b.HighPriceInCurrentYear) * 100, 2)) : 0,//現距1年高點跌幅(%)
                     LowPriceInCurrentYear = b.LowPriceInCurrentYear,//1年最低股價(元)
                     LowPriceInCurrentYearPercentGap = b.LowPriceInCurrentYear > 0 ? Convert.ToDouble(Math.Round(((a.Price - b.LowPriceInCurrentYear) / b.LowPriceInCurrentYear) * 100, 2)) : 0//現距1年低點漲幅(%)
                 }).ToList();

            if (IgnoreFilter == false)
            {
                data = data.Where(c => c.Price >= priceLow && c.Price <= priceHigh).ToList();
            }

            return data;
        }

        /// <summary>
        /// 取得52周間最高 & 最低價(非最後收盤成交價)
        /// </summary>
        /// <param name="realTimeData">即時成交價</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<StockHighLowIn52WeeksInfoModel>> GetHighLowIn52Weeks(/*List<StockInfoModel> realTimeData, */int taskCount = 25)
        {
            var res = new List<StockHighLowIn52WeeksInfoModel>();

            if (AppCacheUtils.IsSet(CacheType.PriceHighLowIn52WeeksList) == false)
            {
                var httpClient = new HttpClient();
                //add header [User-Agent]，避免被檢查出爬蟲
                httpClient.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

                //最高/最低股價統計(三個月/半個月/一年)
                var url = "https://goodinfo.tw/tw2/StockList.asp?MARKET_CAT=%E4%B8%8A%E5%B8%82&INDUSTRY_CAT=%E4%B8%8A%E5%B8%82%E5%85%A8%E9%83%A8&SHEET=%E6%BC%B2%E8%B7%8C%E5%8F%8A%E6%88%90%E4%BA%A4%E7%B5%B1%E8%A8%88&SHEET2=%E6%9C%80%E9%AB%98%2F%E6%9C%80%E4%BD%8E%E8%82%A1%E5%83%B9%E7%B5%B1%E8%A8%88(%E4%B8%89%E5%80%8B%E6%9C%88%2F%E5%8D%8A%E5%B9%B4%2F%E4%B8%80%E5%B9%B4)";
                var resMessage = await httpClient.GetAsync(url);            

                //檢查回應的伺服器狀態StatusCode是否是200 OK
                if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var sr = await resMessage.Content.ReadAsStringAsync();
                    var config = Configuration.Default;
                    var context = BrowsingContext.New(config);
                    var document = await context.OpenAsync(c => c.Content(sr));
                    var listTR = document.QuerySelectorAll("tr[id^=row]");     
                
                    var group = TaskUtils.GroupSplit(listTR.ToList(), taskCount);//分群組 for 多執行緒分批執行
                    var tasks = new Task[taskCount];

                    for (int i = 0; i < tasks.Length; i++)
                    {
                        var groupData = group[i];
                        tasks[i] = Task.Run(async () =>
                        {
                            foreach (var itemTr in groupData)
                            {
                                var td = itemTr.Children;
                                //var currentData = realTimeData.Where(c => c.StockId == td[0].TextContent.Trim()).FirstOrDefault();
                                //if (currentData != null)
                                //{
                                    //網站資料來源非即時，必須把成交價更新為即時價格
                                    //var currentPrice = currentData?.Price ?? 0;
                                    decimal.TryParse(td[16].TextContent, out var highPriceInCurrentYear);
                                    decimal.TryParse(td[18].TextContent, out var lowPriceInCurrentYear);
                                    //double.TryParse(td[17].TextContent, out var highPriceInCurrentYearPercentGap);
                                    //double.TryParse(td[19].TextContent, out var lowPriceInCurrentYearPercentGap);

                                    var data = new StockHighLowIn52WeeksInfoModel()
                                    {
                                        StockId = td[0].TextContent.Trim(),
                                        //Price = currentPrice,
                                        HighPriceInCurrentYear = /*currentPrice > highPriceInCurrentYear ? currentPrice : */highPriceInCurrentYear,//1年最高股價(元)
                                        //HighPriceInCurrentYearPercentGap = highPriceInCurrentYear > 0 ? Convert.ToDouble(Math.Round(((currentPrice - highPriceInCurrentYear) / highPriceInCurrentYear) * 100, 2)) : 0,//現距1年高點跌幅(%)
                                        LowPriceInCurrentYear = /*currentPrice < lowPriceInCurrentYear ? currentPrice : */lowPriceInCurrentYear,//1年最低股價(元)
                                        //LowPriceInCurrentYearPercentGap = lowPriceInCurrentYear > 0 ? Convert.ToDouble(Math.Round(((currentPrice - lowPriceInCurrentYear) / lowPriceInCurrentYear) * 100, 2)) : 0//現距1年低點漲幅(%)
                                    };

                                    lock (_lock)
                                    {
                                        res.Add(data);
                                    }

                                    //await Task.Delay(10);//add await for async
                                //}
                            }
                        });
                    }
                    Task.WaitAll(tasks);
                }


                AppCacheUtils.Set(CacheType.PriceHighLowIn52WeeksList, res, AppCacheUtils.Expiration.Absolute, cacheExpireTime);
            }

            res = (List<StockHighLowIn52WeeksInfoModel>)AppCacheUtils.Get(CacheType.PriceHighLowIn52WeeksList);

            return res;
        }

        /// <summary>
        /// 取得近52周最高最低價格區間內，目前價格離最高價還有多少百分比，並換算成vti係數(vti越高，表示離52周區間內最高點越近)
        /// </summary>
        /// <param name="highLowData">取得52周間最高 & 最低價資料(非最終成交價)</param>
        /// <returns></returns>
        public async Task<List<StockVtiInfoModel>> GetVTI(List<PriceInfoDto> highLowData)
        {
            var res = new List<StockVtiInfoModel>();

            foreach (var item in highLowData)
            {
                /*
                 *Ref：https://www.facebook.com/1045010642367425/posts/1076024329266056/
                 *在近52周最高最低價格區間內，目前價格離最高價還有多少百分比(vti越高，表示離52周區間內最高點越近)
                 */
                var diffHigh = (item.HighPriceInCurrentYear - item.LowPriceInCurrentYear) == 0M ? 0.01M : (item.HighPriceInCurrentYear - item.LowPriceInCurrentYear);
                var vti = Convert.ToDouble(Math.Round(1 - (item.Price - item.LowPriceInCurrentYear) / diffHigh, 2));
                var amount = Convert.ToInt32(Math.Round(vti, 2) * 1000);

                var vtiData = new StockVtiInfoModel
                {
                    StockId = item.StockId,
                    VTI = vti,
                    Amount = amount
                };

                res.Add(vtiData);
            }

            return res;
        }

        /// <summary>
        /// 篩選vti係數
        /// </summary>
        /// <param name="priceData">取得52周間最高 & 最低價資料(非最終成交價)</param>
        /// <param name="amountLimit">vti轉換後的購買股數</param>
        /// <returns></returns>
        public async Task<List<VtiInfoDto>> GetFilterVTI(List<PriceInfoDto> priceData, int[] vtiRange)
        {
            var res = new List<VtiInfoDto>();
            var data = await GetVTI(priceData);
            var sRange = vtiRange[0] * 0.01;
            var eRange = vtiRange[1] *0.01;

            if (IgnoreFilter == false)
            {
                data = data.Where(c => c.VTI >= sRange && c.VTI <= eRange).ToList();
            }

            res =
                data.Select(c => new VtiInfoDto()
                {
                    StockId = c.StockId,
                    Vti = c.VTI,
                }).ToList();

            return res;
        }

        /// <summary>
        /// 每日成交量資訊
        /// </summary>
        /// <param name="vtiDataIds">資料來源</param>
        /// <param name="txDateCount">交易日</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public async Task<List<StockVolumeInfoModel>> GetVolume(List<string> ids = null, int txDateCount = 10, int taskCount = 25)
        {
            var res = new List<StockVolumeInfoModel>();
            //分群組 for 多執行緒分批執行

            if (ids == null)
            {
                ids = _context.Stock_Info.Select(c => c.Stock_Id).Distinct().ToList();
            }

            var groups = TaskUtils.GroupSplit(ids, taskCount);
            var tasks = new Task[groups.Count];

            for (int i = 0; i < groups.Count; i++)
            {
                var idGroups = groups[i];
                tasks[i] = Task.Run(async () =>
                {
                    foreach (var strockId in idGroups)
                    {
                        var httpClient = new HttpClient();
                        var url = $"https://tw.stock.yahoo.com/_td-stock/api/resource/StockServices.tradesWithQuoteStats;limit=210;period=day;symbol={strockId}.TW?bkt=&device=desktop&ecma=modern&feature=enableGAMAds%2CenableGAMEdgeToEdge%2CenableEvPlayer&intl=tw&lang=zh-Hant-TW&partner=none&prid=5m2req5ioan5s&region=TW&site=finance&tz=Asia%2FTaipei&ver=1.2.2112&returnMeta=true";
                        var resMessage = await httpClient.GetAsync(url);

                        //檢查回應的伺服器狀態StatusCode是否是200 OK
                        if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
                        {

                            var sr = await resMessage.Content.ReadAsStringAsync();

                            /*
                             *json => model web tool
                             *ref：https://json2csharp.com/
                             */
                            var deserializeData = JsonSerializer.Deserialize<StockVolumeAPIModel>(sr);
                            var volumeInfo = new StockVolumeInfoModel()
                            {
                                StockId = strockId,
                                //StockName = item.StockName,
                                VolumeInfo = deserializeData.data.list.Take(txDateCount).Select(c => new VolumeData 
                                {
                                    txDate = DateOnly.FromDateTime(Convert.ToDateTime(c.formattedDate)),
                                    dealerDiffVolK = c.dealerDiffVolK,
                                    investmentTrustDiffVolK = c.investmentTrustDiffVolK,
                                    foreignDiffVolK = c.foreignDiffVolK,
                                    volumeK = Convert.ToInt32(c.volumeK)
                                }).ToList()
                            };

                            lock (_lock)
                            {
                                res.Add(volumeInfo);
                            }
                        }
                    }
                });
            }
            Task.WaitAll(tasks);


            return res;
        }

        /// <summary>
        /// 篩選每日成交量資訊
        /// </summary>
        /// <param name="vtiDataIds">資料來源</param>
        /// <param name="volumeKLimit">成交量</param>
        /// <param name="txDateCount">顯示交易日</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public async Task<List<StockVolumeInfoModel>> GetFilterVolume(int volumeKLimit = 500)
        {
            //var res = new List<StockVolumeInfoModel>();

            var res =
                _context.Volume_Detail
                .GroupBy(c => c.Stock_Id)
                .Select(c => new StockVolumeInfoModel
                {
                    StockId = c.Key,
                    VolumeInfo = c.Select(cc => new VolumeData()
                    {
                        txDate = DateOnly.FromDateTime(cc.Tx_Date),
                        foreignDiffVolK = cc.Foreign_Diff_VolK,
                        dealerDiffVolK = cc.Dealer_Diff_VolK,
                        investmentTrustDiffVolK = cc.InvestmentTrust_Diff_VolK,
                        volumeK = cc.VolumeK
                    }).ToList()
                })
                .ToList();
            //var dbVolume = _volumeService.GetDbVolumeDetail().Result;//取得已經儲存db的volume資料。.Result取代await，避免非同步先執行後面的程式

            //var volumeInfoData = (vtiDataIds.GroupJoin(dbVolume, a => a, b => b.StockId, (a, b) => new
            //{
            //    a,
            //    b
            //}).SelectMany(c => c.b.DefaultIfEmpty(), (c, b) => new
            //{
            //    StockId = c.a,
            //    hasDbVolumeData = (b == null ? false : true)
            //}))
            //.ToList();

            //var newData = new List<string>();
            //foreach (var item in volumeInfoData)
            //{
            //    if (item.hasDbVolumeData)
            //    {
            //        var oldData = dbVolume.Where(c => c.StockId == item.StockId).FirstOrDefault();
            //        if (oldData != null)
            //        {
            //            res.Add(oldData);//DB已經有volume資料，直接add到model
            //        }
            //    }
            //    else
            //    {
            //        newData.Add(item.StockId);//DB還沒有volume資料的newData，要透過yahoo api取得volume相關資料
            //    }
            //}

            //var newDataVolumeInfo = await GetVolume(newData, txDateCount, taskCount);
            //res.AddRange(newDataVolumeInfo);//api取得的結果加入到model

            if (IgnoreFilter == false)
            {
                //平均成交量大(等)於500
                res = res.Where(c => c.VolumeInfo.Average(c => c.volumeK) >= volumeKLimit).ToList();
            }

            return res;
        }

        /// <summary>
        /// 取得本益比(PE) & 近四季EPS
        /// 本益比驗證：https://www.cmoney.tw/forum/stock/1256
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<EpsInfoDto>> GetEps(string Os = "Windows", int taskCount = 25)
        {
            var res = new List<EpsInfoDto>();

            //分群組 for 多執行緒分批執行
            var ids = _context.Stock_Info.Select(c => c.Stock_Id).Distinct().ToList();
            var groups = TaskUtils.GroupSplit(ids, taskCount);
            var tasks = new Task[groups.Count];

            for (int i = 0; i < groups.Count; i++)
            {
                var vtiData = groups[i];
                tasks[i] = Task.Run(async () =>
                {
                    foreach (var item in vtiData)
                    {
                        var httpClient = new HttpClient();
                        var url = @$"https://tw.stock.yahoo.com/_td-stock/api/resource/StockServices.revenues;includedFields=priceAssessment;period=quarterSum4;priceAssessmentPeriod=quarter;symbol={item}.TW?bkt=&device=desktop&ecma=modern&feature=enableGAMAds%2CenableGAMEdgeToEdge%2CenableEvPlayer&intl=tw&lang=zh-Hant-TW&partner=none&prid=7ojmd05invvv9&region=TW&site=finance&tz=Asia%2FTaipei&ver=1.2.2103&returnMeta=true";
                        var resMessage = await httpClient.GetAsync(url);

                        //檢查回應的伺服器狀態StatusCode是否是200 OK
                        if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var sr = await resMessage.Content.ReadAsStringAsync();

                            /*
                             *json => model web tool 
                             *ref：https://json2csharp.com/
                             */
                            var deserializeData = JsonSerializer.Deserialize<EpsInfoModel>(sr);

                            var startQuater = string.Empty;
                            var endQuater = string.Empty;
                            var epsAcc4Q = deserializeData.data.data.result.revenues.Count > 0 ? Convert.ToDecimal(deserializeData.data.data.result.revenues[0].epsAcc4Q) : 0M;
                            if (epsAcc4Q > 0)
                            {
                                var d = DateOnly.FromDateTime(deserializeData.data.data.result.revenues[0].date);
                                startQuater = $"{d.AddMonths(-9).Year}Q{Convert.ToInt32(d.AddMonths(-9).Month) / 3}";
                                endQuater = $"{d.Year}Q{Convert.ToInt32(d.Month) / 3}";

                                if (Os == "Linux")
                                {
                                    /*
                                     * Linux的日期會自動減一天，所以要AddDays(1)回去
                                     * ex：
                                     * api return date：2023-12-01T00:00:00+08:00
                                        deserializeData.data.data.result.revenues[0].date.AddMonths(-9); => 02/28/2023 16:00:00
                                        deserializeData.data.data.result.revenues[0].date; => 11/30/2023 16:00:00
                                     */
                                    startQuater = $"{d.AddMonths(-9).AddDays(1).Year}Q{Convert.ToInt32(d.AddMonths(-9).AddDays(1).Month) / 3}";
                                    endQuater = $"{d.AddDays(1).Year}Q{Convert.ToInt32(d.AddDays(1).Month) / 3}";
                                }
                            }

                            var interval = deserializeData.data.data.result.revenues.Count > 0 ? $"{startQuater}~{endQuater}" : "";

                            var peInfo = new EpsInfoDto()
                            {
                                StockId = item,
                                EpsAcc4QInterval = interval,
                                EpsAcc4Q = epsAcc4Q,                                
                            };

                            lock (_lock)
                            {
                                res.Add(peInfo);
                            }
                        }
                    }
                });
            }
            Task.WaitAll(tasks);

            return res;
        }

        /// <summary>
        /// 篩選本益比(PE) & 近四季EPS
        /// </summary>
        /// <param name="eps">近四季EPS篩選條件</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<EpsInfoDto>> GetFilterEps(decimal eps = 0, string Os = "Windows", int taskCount = 25)
        {
            //var res = new List<EpsInfoDto>();
            //var epsData = _context.Eps_Info.ToList();//await GetEps(taskCount, Os);
            var data =
                (
                from a in _context.Stock_Info.ToList()
                join b in _context.Eps_Info.ToList() on a.Stock_Id equals b.Stock_Id
                select new
                {
                    StockId = a.Stock_Id,
                    a.CFICode,
                    EpsAcc4Q = b.Eps_Acc_4Q ?? 0,
                    EpsAcc4QInterval = $"{b.Eps_Acc_4Q_Interval_Start}~{b.Eps_Acc_4Q_Interval_End}"
                }).AsEnumerable();

            if (!IgnoreFilter)
            {
                data = data.Where(c => 
                    StockType.ETFs.Contains(c.CFICode) || 
                    (c.CFICode == StockType.ESVUFR && (c.EpsAcc4Q > eps))
                    );   
            }

            var res = data.Select(c => new EpsInfoDto()
            {
                StockId = c.StockId,
                EpsAcc4Q = c.EpsAcc4Q,
                EpsAcc4QInterval = c.EpsAcc4QInterval
            }).ToList();

            return res;
        }

        /// <summary>
        /// 取得每月MoM. YoY增減趴數. PE
        /// </summary>
        /// <param name="ids">資料來源</param>
        /// <param name="revenueMonthCount">顯示營收資料筆數(by 月)</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public async Task<List<PeInfoModel>> GetPe(List<string> ids, int revenueMonthCount = 3, int taskCount = 25)
        {
            var res = new List<PeInfoModel>();
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var httpClient = new HttpClient();

            //分群組 for 多執行緒分批執行
            var groups = TaskUtils.GroupSplit(ids, taskCount);
            var tasks = new Task[groups.Count];

            for (int i = 0; i < groups.Count; i++)
            {
                var vtiData = groups[i];
                tasks[i] = Task.Run(async () =>
                {
                    foreach (var id in vtiData)
                    {
                        var sr = string.Empty;

                        using (HttpRequestMessage reqest = new HttpRequestMessage(HttpMethod.Get, $"https://tw.stock.yahoo.com/quote/{id}.TW/revenue"))
                        {

                            lock (_lock)
                            {
                                sr = httpClient.Send(reqest).Content.ReadAsStringAsync().Result;
                            }

                            var document = context.OpenAsync(res => res.Content(sr)).Result;
                            var peInfo = new PeInfoModel()
                            {
                                StockId = id,
                                Pe = double.TryParse(document.QuerySelector("#main-0-QuoteHeader-Proxy div").ChildNodes[1].ChildNodes[1].ChildNodes[1].TextContent.Split('(')[0].Trim(), out var pe) ? pe : 99999d,//取得本益比(pe)
                            };

                            lock (_lock)
                            {
                                res.Add(peInfo);
                            }
                        }











                        //var url = $"https://tw.stock.yahoo.com/quote/{id}.TW/revenue";
                        //var resMessage = await httpClient.GetAsync(url);

                        ////檢查回應的伺服器狀態StatusCode是否是200 OK
                        //if (resMessage.StatusCode == HttpStatusCode.OK)
                        //{
                        //    var sr = await resMessage.Content.ReadAsStringAsync();
                        //    var document = await context.OpenAsync(res => res.Content(sr));
                        //    var peInfo = new PeInfoModel()
                        //    {
                        //        StockId = id,
                        //        Pe = double.TryParse(document.QuerySelector("#main-0-QuoteHeader-Proxy div").ChildNodes[1].ChildNodes[1].ChildNodes[1].TextContent.Split('(')[0].Trim(), out var pe) ? pe : 99999d,//取得本益比(pe)
                        //    };

                        //    lock (_lock)
                        //    {
                        //        res.Add(peInfo);
                        //    }
                        //}
                    }
                });
            }
            Task.WaitAll(tasks);

            return res;
        }

        /// <summary>
        /// 篩選取得每月MoM. YoY增減趴數. PE
        /// </summary>
        /// <param name="ids">資料來源</param>
        /// <param name="revenueMonthCount">顯示營收資料筆數(by 月)</param>
        /// <param name="pe">本益比(PE)</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public async Task<List<PeInfoModel>> GetFilterPe(List<string> ids, int revenueMonthCount = 3, double pe = 20, int taskCount = 25)
        {
            var peData = await GetPe(ids, revenueMonthCount, taskCount);

            if (!IgnoreFilter)
            {
                peData = (
                    from a in _context.Stock_Info.ToList()
                    join b in peData on a.Stock_Id equals b.StockId
                    where
                    a.CFICode == StockType.ESVUFR && (b.Pe <= pe)
                    ||
                    StockType.ETFs.Contains(a.CFICode)
                    orderby a.CFICode descending
                    select b
                    ).ToList();
            }

            return peData;
        }

        public async Task<List<RevenueInfoModel>> GetRevenue(List<string>? ids = null, int revenueMonthCount = 3, int taskCount = 25)
        {
            var res = new List<RevenueInfoModel>();

            if (ids == null)
            {
                ids = _context.Stock_Info.Select(c => c.Stock_Id).Distinct().ToList();
            }

            //分群組 for 多執行緒分批執行
            var groups = TaskUtils.GroupSplit(ids, taskCount);
            var tasks = new Task[groups.Count];

            var httpClientHandler = new HttpClientHandler
            {
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            };
            var httpClient = new HttpClient(httpClientHandler);// { SslProtocols = System.Security.Authentication.SslProtocols.Tls };
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);

            for (int i = 0; i < groups.Count; i++)
            {
                var vtiData = groups[i];
                tasks[i] = Task.Run(async () =>
                {
                    foreach (var id in vtiData)
                    {
                        var sr = string.Empty;

                        using (HttpRequestMessage reqest = new HttpRequestMessage(HttpMethod.Get, $"https://tw.stock.yahoo.com/quote/{id}.TW/revenue"))
                        {

                            lock (_lock)
                            {
                                sr = httpClient.Send(reqest).Content.ReadAsStringAsync().Result;
                            }
                                
                            var document = context.OpenAsync(res => res.Content(sr)).Result;
                            var listTR = document.QuerySelectorAll("#qsp-revenue-table .table-body-wrapper ul li[class*='List']").Take(revenueMonthCount);
                            var revenueInfo = new RevenueInfoModel() { StockId = id,  RevenueData = new List<RevenueData>() };
                            foreach (var tr in listTR)
                            {
                                revenueInfo.RevenueData.Add(new RevenueData()
                                {
                                    revenueInterval = tr.QuerySelector("div").Children[0].TextContent,//YYYY/MM
                                    mom = Convert.ToDouble(tr.QuerySelectorAll("span")[1].TextContent.Replace("%", "")),//MOM 
                                    monthYOY = Convert.ToDouble(tr.QuerySelectorAll("span")[3].TextContent.Replace("%", "")),//月營收年增率
                                    yoy = Convert.ToDouble(tr.QuerySelectorAll("span")[6].TextContent.Replace("%", ""))//YOY
                                });
                            }

                            lock (_lock)
                            {
                                res.Add(revenueInfo);
                            }

                        }






                        //var url = $"https://tw.stock.yahoo.com/quote/{id}.TW/revenue"; //營收
                        //var httpClient = new HttpClient();
                        //var resMessage = await httpClient.GetAsync(url);

                        ////檢查回應的伺服器狀態StatusCode是否是200 OK
                        //if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
                        //{
                        //    var sr = await resMessage.Content.ReadAsStringAsync();
                        //    var config = Configuration.Default;
                        //    var context = BrowsingContext.New(config);
                        //    var document = await context.OpenAsync(res => res.Content(sr));

                        //    var listTR = document.QuerySelectorAll("#qsp-revenue-table .table-body-wrapper ul li[class*='List']").Take(revenueMonthCount);
                        //    var revenueInfo = new RevenueInfoModel() { StockId = id, RevenueData = new List<RevenueData>() };
                        //    foreach (var tr in listTR)
                        //    {
                        //        revenueInfo.RevenueData.Add(new RevenueData()
                        //        {
                        //            revenueInterval = tr.QuerySelector("div").Children[0].TextContent,//YYYY/MM
                        //            mom = Convert.ToDouble(tr.QuerySelectorAll("span")[1].TextContent.Replace("%", "")),//MOM 
                        //            monthYOY = Convert.ToDouble(tr.QuerySelectorAll("span")[3].TextContent.Replace("%", "")),//月營收年增率
                        //            yoy = Convert.ToDouble(tr.QuerySelectorAll("span")[6].TextContent.Replace("%", ""))//YOY
                        //        });
                        //    }

                        //    lock (_lock)
                        //    {
                        //        res.Add(revenueInfo);
                        //    }
                        //}
                    }
                });
            }
            Task.WaitAll(tasks);

            return res;
        }

        public async Task<List<RevenueInfoModel>> GetFilterRevenue(int revenueTakeCount = 3)
        {
            var list = new List<RevenueInfoModel> ();

            list = _context.Revenue_Info.GroupBy(g => g.Stock_Id).Select(s => new RevenueInfoModel()
            {
                StockId = s.Key,
                RevenueData = s.Select(ss => new RevenueData()
                {
                    revenueInterval = ss.Interval_Year + "/" + ss.Interval_Month,
                    mom = Convert.ToDouble(ss.MoM),
                    monthYOY = Convert.ToDouble(ss.YoYMonth),
                    yoy = Convert.ToDouble(ss.YoY)
                }).OrderByDescending(c => c.revenueInterval).ToList()
            }).ToList();

            if (!IgnoreFilter)
            {
                list =
                    (from a in _context.Stock_Info.ToList()
                    join b in list on a.Stock_Id equals b.StockId
                    where
                    a.CFICode == StockType.ESVUFR && (!b.RevenueData.Take(revenueTakeCount).Where(c => c.monthYOY < 0).Any() && (b.RevenueData.Count > 0 && b.RevenueData[0].yoy > 0))
                    ||
                    StockType.ETFs.Contains(a.CFICode)
                    select b).ToList() ;

            }
              
            return list;
        }
    }
}
