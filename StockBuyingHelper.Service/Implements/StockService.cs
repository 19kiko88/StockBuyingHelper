using System.Data;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using AngleSharp;
using AngleSharp.Io;
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
        //private static List<StockVolumeInfoModel> lockVolumeObj = new List<StockVolumeInfoModel>();

        public StockService(IVolumeService volumeService)
        {
            _volumeService = volumeService;
        }

        /// <summary>
        /// 取得台股清單(上市.櫃)
        /// </summary>
        /// <returns></returns>
        public async Task<List<StockInfoModel>> GetStockList()
        {
            var res = new List<StockInfoModel>();


            if (AppCacheUtils.IsSet(CacheType.StockList) == false)
            {
                var httpClient = new HttpClient();
                var url = "https://isin.twse.com.tw/isin/C_public.jsp?strMode=2";//本國上市證券國際證券辨識號碼一覽表
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
        public async Task<List<StockInfoModel>> GetFilterStockList(bool queryEtfs, List<string>? specificIds = null)
        {
            var data = await GetStockList();
            var data_ = data.AsEnumerable();

            if (IgnoreFilter == false && queryEtfs == false)
            {
                data_ = data_.Where(c => c.CFICode == StockType.ESVUFR).AsEnumerable();
            }

            if (specificIds != null && specificIds.Count > 0)
            {
                data_ = data_.Where(c => specificIds.Contains(c.StockId)).AsEnumerable();
            }

            var res = data_.ToList();

            return res;
        }

        /// <summary>
        /// 取得即時價格
        /// </summary>
        /// <param name="specificIds">指定特定股票代碼</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<StockPriceInfoModel>> GetPrice(List<string>? specificIds = null, int taskCount = 25)
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
                if (specificIds != null && specificIds.Count > 0)
                {
                    listTR = listTR.Where(c => specificIds.Contains(c.Children[0].InnerHtml));
                }
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
                                    StockName = tr.Children[1].Children[0].InnerHtml,
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
        /// <param name="specificIds">指定特定股票代碼</param>
        /// <param name="priceLow">價格區間下限</param>
        /// <param name="priceHigh">價格區間上限</param>
        /// <returns></returns>
        public async Task<List<StockPriceInfoModel>> GetFilterPrice(List<string>? specificIds = null, decimal priceLow = 0, decimal priceHigh = 200)
        {
            var res = await GetPrice(specificIds);

            if (IgnoreFilter == false)
            {
                res = res.Where(c => c.Price >= priceLow && c.Price <= priceHigh).ToList();
            }

            return res;
        }

        /// <summary>
        /// 取得個股基本資料 & 價格
        /// </summary>
        /// <param name="filterIds"></param>
        /// <param name="queryEtfs"></param>
        /// <param name="priceLow"></param>
        /// <param name="priceHigh"></param>
        /// <returns></returns>
        public async Task<List<StockInfoModel>> GteStockInfo(List<string>? filterIds, bool queryEtfs, decimal priceLow = 0, decimal priceHigh = 99999)
        {
            var stockInfo = await GetFilterStockList(queryEtfs, filterIds);

            var ids = stockInfo.Select(c => c.StockId).ToList();

            var priceInfo = await GetFilterPrice(ids, priceLow, priceHigh);

            var res = (from a in stockInfo
                      join b in priceInfo on a.StockId equals b.StockId
                      select new StockInfoModel
                      {
                          StockId = a.StockId,
                          StockName = a.StockName,
                          ISINCode = a.ISINCode,
                          Market = a.Market,
                          IndustryType = a.IndustryType,
                          CFICode = a.CFICode,
                          Note = a.Note,
                          Price = b.Price
                      }).ToList();

            return res;
        }

        /// <summary>
        /// 取得52周間最高 & 最低價(非最後收盤成交價)
        /// </summary>
        /// <param name="realTimeData">即時成交價</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<StockHighLowIn52WeeksInfoModel>> GetHighLowIn52Weeks(List<StockInfoModel> realTimeData, int taskCount = 25)
        {
            var res = new List<StockHighLowIn52WeeksInfoModel>();
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
                            var currentData = realTimeData.Where(c => c.StockId == td[0].TextContent.Trim()).FirstOrDefault();
                            if (currentData != null)
                            {
                                //網站資料來源非即時，必須把成交價更新為即時價格
                                var currentPrice = currentData?.Price ?? 0;
                                decimal.TryParse(td[16].TextContent, out var highPriceInCurrentYear);
                                decimal.TryParse(td[18].TextContent, out var lowPriceInCurrentYear);
                                //double.TryParse(td[17].TextContent, out var highPriceInCurrentYearPercentGap);
                                //double.TryParse(td[19].TextContent, out var lowPriceInCurrentYearPercentGap);

                                var data = new StockHighLowIn52WeeksInfoModel()
                                {
                                    StockId = td[0].TextContent.Trim(),
                                    StockName = td[1].TextContent.Trim(),
                                    Price = currentPrice,
                                    HighPriceInCurrentYear = currentPrice > highPriceInCurrentYear ? currentPrice : highPriceInCurrentYear,//1年最高股價(元)
                                    HighPriceInCurrentYearPercentGap = highPriceInCurrentYear > 0 ? Convert.ToDouble(Math.Round(((currentPrice - highPriceInCurrentYear) / highPriceInCurrentYear) * 100, 2)) : 0,//現距1年高點跌幅(%)
                                    LowPriceInCurrentYear = currentPrice < lowPriceInCurrentYear ? currentPrice : lowPriceInCurrentYear,//1年最低股價(元)
                                    LowPriceInCurrentYearPercentGap = lowPriceInCurrentYear > 0 ? Convert.ToDouble(Math.Round(((currentPrice - lowPriceInCurrentYear) / lowPriceInCurrentYear) * 100, 2)) : 0//現距1年低點漲幅(%)
                                };

                                lock (_lock)
                                {
                                    res.Add(data);
                                }

                                //await Task.Delay(10);//add await for async
                            }
                        }
                    });
                }
                Task.WaitAll(tasks);
            }

            return res;
        }

        /// <summary>
        /// 取得近52周最高最低價格區間內，目前價格離最高價還有多少百分比，並換算成vti係數(vti越高，表示離52周區間內最高點越近)
        /// </summary>
        /// <param name="highLowData">取得52周間最高 & 最低價資料(非最終成交價)</param>
        /// <returns></returns>
        public async Task<List<StockVtiInfoModel>> GetVTI(List<StockHighLowIn52WeeksInfoModel> highLowData)
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
        /// <param name="highLowData">取得52周間最高 & 最低價資料(非最終成交價)</param>
        /// <param name="amountLimit">vti轉換後的購買股數</param>
        /// <returns></returns>
        public async Task<List<StockVtiInfoModel>> GetFilterVTI(List<StockHighLowIn52WeeksInfoModel> highLowData, int[] vtiRange)
        {
            var res = await GetVTI(highLowData);

            var sRange = vtiRange[0] * 0.01;
            var eRange = vtiRange[1] *0.01;

            if (IgnoreFilter == false)
            {
                res = res.Where(c => c.VTI >= sRange && c.VTI <= eRange).ToList();
            }

            return res;
        }

        /// <summary>
        /// 每日成交量資訊
        /// </summary>
        /// <param name="vtiDataIds">資料來源</param>
        /// <param name="txDateCount">交易日</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public async Task<List<StockVolumeInfoModel>> GetVolume(List<string> vtiDataIds, int txDateCount = 10, int taskCount = 25)
        {
            var res = new List<StockVolumeInfoModel>();
            //分群組 for 多執行緒分批執行
            var groups = TaskUtils.GroupSplit(vtiDataIds, taskCount);
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
        public async Task<Tuple<List<StockVolumeInfoModel>, List<StockVolumeInfoModel>>> GetFilterVolume(List<string> vtiDataIds, int volumeKLimit = 500, int txDateCount = 10, int taskCount = 25)
        {
            var res = new List<StockVolumeInfoModel>();

            var dbVolume = _volumeService.GetDbVolumeDetail().Result;//取得已經儲存db的volume資料。.Result取代await，避免非同步先執行後面的程式

            var volumeInfoData = (vtiDataIds.GroupJoin(dbVolume, a => a, b => b.StockId, (a, b) => new
            {
                a,
                b
            }).SelectMany(c => c.b.DefaultIfEmpty(), (c, b) => new
            {
                StockId = c.a,
                hasDbVolumeData = (b == null ? false : true)
            }))
            .ToList();

            var newData = new List<string>();
            foreach (var item in volumeInfoData)
            {
                if (item.hasDbVolumeData)
                {
                    var oldData = dbVolume.Where(c => c.StockId == item.StockId).FirstOrDefault();
                    if (oldData != null)
                    {
                        res.Add(oldData);//DB已經有volume資料，直接add到model
                    }
                }
                else
                {
                    newData.Add(item.StockId);//DB還沒有volume資料的newData，要透過yahoo api取得volume相關資料
                }
            }

            var newDataVolumeInfo = await GetVolume(newData, txDateCount, taskCount);
            res.AddRange(newDataVolumeInfo);//api取得的結果加入到model

            if (IgnoreFilter == false)
            {
                //平均成交量大(等)於500
                res = res.Where(c => c.VolumeInfo.Average(c => c.volumeK) >= volumeKLimit).ToList();
            }

            return Tuple.Create(res, newDataVolumeInfo);
        }

        /// <summary>
        /// 取得本益比(PE) & 近四季EPS
        /// 本益比驗證：https://www.cmoney.tw/forum/stock/1256
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<EqsInfoModel>> GetEps(List<StockInfoModel> data, int taskCount = 25, string Os = "Windows")
        {
            var res = new List<EqsInfoModel>();

            //分群組 for 多執行緒分批執行
            var groups = TaskUtils.GroupSplit(data, taskCount);
            var tasks = new Task[groups.Count];

            for (int i = 0; i < groups.Count; i++)
            {
                var vtiData = groups[i];
                tasks[i] = Task.Run(async () =>
                {
                    foreach (var item in vtiData)
                    {
                        var httpClient = new HttpClient();
                        var url = @$"https://tw.stock.yahoo.com/_td-stock/api/resource/StockServices.revenues;includedFields=priceAssessment;period=quarterSum4;priceAssessmentPeriod=quarter;symbol={item.StockId}.TW?bkt=&device=desktop&ecma=modern&feature=enableGAMAds%2CenableGAMEdgeToEdge%2CenableEvPlayer&intl=tw&lang=zh-Hant-TW&partner=none&prid=7ojmd05invvv9&region=TW&site=finance&tz=Asia%2FTaipei&ver=1.2.2103&returnMeta=true";
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

                            var peInfo = new EqsInfoModel()
                            {
                                StockId = item.StockId,
                                StockName = item.StockName,
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
        /// <param name="data">資料來源</param>
        /// <param name="eps">近四季EPS篩選條件</param>
        /// <param name="pe">近四季PE篩選條件</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<EqsInfoModel>> GetFilterEps(List<StockInfoModel> data, decimal eps = 0, string Os = "Windows", int taskCount = 25)
        {
            var res = await GetEps(data, taskCount, Os);

            if (!IgnoreFilter)
            {
                res =
                (from a in res
                 join b in data on a.StockId equals b.StockId
                 where StockType.ETFs.Contains(b.CFICode) || (b.CFICode == StockType.ESVUFR && (a.EpsAcc4Q > eps))
                 select a).ToList();     
            }       

            return res;
        }

        /// <summary>
        /// 取得每月MoM. YoY增減趴數. PE
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="revenueMonthCount">顯示營收資料筆數(by 月)</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public async Task<List<RevenueInfoModel>> GetRevenueAndPe(List<StockInfoModel> data, int revenueMonthCount = 3, int taskCount = 25)
        {
            var res = new List<RevenueInfoModel>();
            var config = Configuration.Default;
            var context = BrowsingContext.New(config);
            var httpClient = new HttpClient();

            foreach (var item in data)
            {
                var url = $"https://tw.stock.yahoo.com/quote/{item.StockId}.TW/revenue";
                var resMessage = await httpClient.GetAsync(url);

                //檢查回應的伺服器狀態StatusCode是否是200 OK
                if (resMessage.StatusCode == HttpStatusCode.OK)
                {
                    var sr = await resMessage.Content.ReadAsStringAsync();
                    var document = await context.OpenAsync(res => res.Content(sr));

                    var listTR = document.QuerySelectorAll("#qsp-revenue-table .table-body-wrapper ul li[class*='List']").Take(revenueMonthCount);
                    var revenueInfo = new RevenueInfoModel() { StockId = item.StockId, StockName = item.StockName, RevenueData = new List<RevenueData>() };
                    foreach (var tr in listTR)
                    {
                        //取得本益比(pe)
                        revenueInfo.pe = double.TryParse(document.QuerySelector("#main-0-QuoteHeader-Proxy div").ChildNodes[1].ChildNodes[1].ChildNodes[1].TextContent.Split('(')[0].Trim(), out var pe) ? pe : 99999d;

                        //取得營收資料
                        revenueInfo.RevenueData.Add(new RevenueData()
                        {
                            revenueInterval = tr.QuerySelector("div").Children[0].TextContent,//YYYY/MM
                            mom = Convert.ToDouble(tr.QuerySelectorAll("span")[1].TextContent.Replace("%", "")),//MOM 
                            monthYOY = Convert.ToDouble(tr.QuerySelectorAll("span")[3].TextContent.Replace("%", "")),//月營收年增率
                            yoy = Convert.ToDouble(tr.QuerySelectorAll("span")[6].TextContent.Replace("%", "")),//YOY
                        });
                    }
                    res.Add(revenueInfo);
                }
            }

            return res;
        }

        /// <summary>
        /// 篩選取得每月MoM. YoY增減趴數. PE
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="revenueMonthCount">顯示營收資料筆數(by 月)</param>
        /// <param name="pe">本益比(PE)</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public async Task<List<RevenueInfoModel>> GetFilterRevenueAndPe(List<StockInfoModel> data, int revenueMonthCount = 3, double pe = 20, int taskCount = 25)
        {
            var res = await GetRevenueAndPe(data, revenueMonthCount, taskCount);

            if (!IgnoreFilter)
            {
                res = (from a in res
                       join b in data on a.StockId equals b.StockId
                       where
                         (b.CFICode == StockType.ESVUFR && (!a.RevenueData.Take(3).Where(c => c.monthYOY < 0).Any() && a.RevenueData[0].yoy > 0) && (a.pe <= pe))
                         ||
                         StockType.ETFs.Contains(b.CFICode)
                       orderby b.CFICode descending
                       select a).ToList();
            }


            return res;
        }


        [Obsolete("減少yahoo api發送request次數，改成逐步篩選")]
        /// <summary>
        /// 取得總表
        /// </summary>
        /// <param name="stockData"></param>
        /// <param name="vtiData">VTI資料</param>
        /// <param name="peData">近四季EPS&PE資料</param>
        /// <param name="revenueData">近三個月營收MoM. YoY資料</param>
        /// <returns></returns>
        public async Task<List<BuyingResultModel>> GetBuyingResult(List<StockInfoModel> stockData, List<StockVtiInfoModel> vtiData, List<EqsInfoModel> eqsData, List<RevenueInfoModel> revenueData, List<StockVolumeInfoModel> volumeData, string specificStockId = "")
        {
            var data =
                (from a in stockData
                 join b in vtiData on a.StockId equals b.StockId
                 join c in eqsData on a.StockId equals c.StockId
                 join d in revenueData on a.StockId equals d.StockId
                 join e in volumeData on a.StockId equals e.StockId
                 select new BuyingResultModel
                 {
                     StockId = a.StockId,
                     StockName = a.StockName,
                     Price = b.Price,
                     Type = a.CFICode,
                     HighIn52 = b.HighIn52,
                     LowIn52 = b.LowIn52,
                     EpsInterval = c.EpsAcc4QInterval,
                     EPS = c.EpsAcc4Q,
                     //PE = c.PE,
                     RevenueDatas = d.RevenueData,
                     VolumeDatas = e.VolumeInfo,
                     VTI = b.VTI,
                     Amount = b.Amount
                 }).AsEnumerable();

            var res = data
                 .OrderByDescending(o => o.Type).ThenByDescending(o => o.EPS)
                 .ToList();

            return res.ToList();
        }

        [Obsolete("近四季EPS取得，改由GetPE從Yahoo Stock取得")]
        /// <summary>
        /// EPS
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<List<ObsoleteEpsInfoModel>> GetEPS(List<StockVtiInfoModel> data)
        {
            var res = new List<ObsoleteEpsInfoModel>();
            var httpClient = new HttpClient();
            var taskCount = 20;
            var tasks = new Task[taskCount];

            //分群組 for 多執行緒分批執行
            var vtiGroup = TaskUtils.GroupSplit(data, taskCount);

            for (int i = 0; i < vtiGroup.Count; i++)
            {
                var vtiData = vtiGroup[i];
                tasks[i] = Task.Run(async () =>
                {
                    foreach (var item in vtiData)
                    {
                        var url = $"https://tw.stock.yahoo.com/quote/{item.StockId}.TW/eps";//eps
                        var resMessage = await httpClient.GetAsync(url);

                        //檢查回應的伺服器狀態StatusCode是否是200 OK
                        if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var sr = await resMessage.Content.ReadAsStringAsync();
                            var config = Configuration.Default;
                            var context = BrowsingContext.New(config);
                            var document = await context.OpenAsync(res => res.Content(sr));

                            var listTR = document.QuerySelectorAll("#qsp-eps-table .table-body-wrapper  ul li[class*='List']").Take(4);//只取最近4季的EPS
                            var epsInfo = new ObsoleteEpsInfoModel() { StockId = item.StockId, /*StockName = item.StockName,*/ EpsData = new List<Eps>() };
                            foreach (var tr in listTR)
                            {
                                var eps = Convert.ToDecimal(tr.QuerySelector("span").InnerHtml);

                                epsInfo.EpsData.Add(new Eps
                                {
                                    Quarter = tr.QuerySelector("div").Children[0].TextContent,
                                    EPS = eps
                                });
                            }
                            var epsSum = epsInfo.EpsData.Select(c => c.EPS).Sum();
                            epsInfo.EpsInterval = $"{epsInfo.EpsData.LastOrDefault()?.Quarter} ~ {epsInfo.EpsData.FirstOrDefault()?.Quarter}";

                            try
                            {
                                epsInfo.PE = Convert.ToDouble(Math.Round(item.Price / epsSum, 2));
                            }
                            catch (Exception ex)
                            {
                                var msg = ex.Message;
                            }

                            res.Add(epsInfo);
                        }
                    }
                });
            }
            Task.WaitAll(tasks);

            return res;
        }
    }
}
