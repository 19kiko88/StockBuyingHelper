using System.Text;
using System.Text.Json;

using AngleSharp;
using AngleSharp.Dom;

using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using StockBuyingHelper.Service.Utility;


namespace StockBuyingHelper.Service.Implements
{
    public class StockService: IStockService
    {
        private readonly object _lock = new object();

        /// <summary>
        /// 取得台股清單(上市.櫃)
        /// </summary>
        /// <returns></returns>
        public async Task<List<StockInfoModel>> GetStockList()
        {
            var res = new List<StockInfoModel>();
            var httpClient = new HttpClient();
            var url = "https://isin.twse.com.tw/isin/C_public.jsp?strMode=2";
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
                    var listTR = document.QuerySelectorAll("tr");

                    foreach (var tr in listTR)
                    {
                        var arrayTd = tr.Children.ToArray();
                        if (tr.Children.Length >= 7)
                        {
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
            }

            res = res.Where(c => c.CFICode == "ESVUFR" || c.CFICode.StartsWith("CEO")).ToList();

            return res;
        }

        public async Task<List<GetHighLowIn52WeeksInfoModel>> GetHighLowIn52Weeks(List<StockPriceInfoModel> realTimeData)
        {
            var res = new List<GetHighLowIn52WeeksInfoModel>();
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
                
                var group = TaskUtils.GroupSplit(listTR.ToList());//分群組 for 多執行緒分批執行
                var tasks = new Task[group.Length];

                for (int i = 0; i < tasks.Length; i++)
                {
                    var goupData = group[i];
                    tasks[i] = Task.Run(async () =>
                    {
                        foreach (var itemTr in goupData)
                        {
                            var td = itemTr.Children;
                            var currentData = realTimeData.Where(c => c.StockId == td[0].TextContent.Trim()).FirstOrDefault();//?.Price ?? 0;
                            if (currentData != null)
                            {
                                var currentPrice = currentData?.Price ?? 0;
                                decimal.TryParse(td[16].TextContent, out var highPriceInCurrentYear);
                                decimal.TryParse(td[18].TextContent, out var lowPriceInCurrentYear);
                                double.TryParse(td[17].TextContent, out var highPriceInCurrentYearPercentGap);
                                double.TryParse(td[19].TextContent, out var lowPriceInCurrentYearPercentGap);

                                var data = new GetHighLowIn52WeeksInfoModel()
                                {
                                    StockId = td[0].TextContent.Trim(),
                                    StockName = td[1].TextContent.Trim(),
                                    Price = currentPrice,
                                    HighPriceInCurrentYear = currentPrice > highPriceInCurrentYear ? currentPrice : highPriceInCurrentYear,
                                    HighPriceInCurrentYearPercentGap = highPriceInCurrentYearPercentGap,
                                    LowPriceInCurrentYear = currentPrice < lowPriceInCurrentYear ? currentPrice : lowPriceInCurrentYear,
                                    LowPriceInCurrentYearPercentGap = highPriceInCurrentYearPercentGap
                                };

                                lock (_lock)
                                {
                                    res.Add(data);
                                }

                                await Task.Delay(100);//add await for async
                            }
                        }
                    });
                }
                Task.WaitAll(tasks);
            }

            return res;
        }

        /// <summary>
        /// 取得即時價格
        /// </summary>
        /// <returns></returns>
        public async Task<List<StockPriceInfoModel>> GetPrice()
        {
            var res = new List<StockPriceInfoModel>();

            try
            {
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

                    var listTR = document.QuerySelectorAll("#CPHB1_gv tr");
                    foreach (var tr in listTR)
                    {
                        if (tr.Index() == 0)
                        {
                            continue;
                        }

                        res.Add(new StockPriceInfoModel()
                        {
                            StockId = tr.Children[0].InnerHtml,
                            StockName = tr.Children[1].Children[0].InnerHtml,
                            Price = Convert.ToDecimal(tr.Children[2].Children[0].InnerHtml)
                        });
                    }
                }

            }
            catch (Exception ex)
            {
                var errMsg = ex.Message;
            }

            return res;
        }

        public async Task<List<VtiInfoModel>> GetVTI(List<StockPriceInfoModel> priceData, List<GetHighLowIn52WeeksInfoModel> highLowData, int amountLimit = 0)
        {
            var listVTI =
                (from a in priceData
                join b in highLowData on a.StockId equals b.StockId
                select new VtiInfoModel {
                    StockId = a.StockId,
                    StockName = a.StockName,
                    Price = a.Price,
                    HighIn52 = b.HighPriceInCurrentYear,
                    LowIn52 = b.LowPriceInCurrentYear
                }).ToList();

            foreach (var item in listVTI)
            {
                var diffHigh = (item.HighIn52 - item.LowIn52) == 0M ? 0.01M : (item.HighIn52 - item.LowIn52);

                /*
                 *Ref：https://www.facebook.com/1045010642367425/posts/1076024329266056/
                 *在近52周最高最低價格區間內，目前價格離最高價還有多少百分比(vti越高，表示離52周區間內最高點越近)
                 */
                item.VTI = Convert.ToDouble(Math.Round( 1 - (item.Price - item.LowIn52) / diffHigh, 2));
                item.Amount = Convert.ToInt32(Math.Round(item.VTI, 2) * 1000);
            }

            if (amountLimit > 0)
            {
                listVTI = listVTI.Where(c => c.Amount > amountLimit).ToList();
            }

            return listVTI;
        }

        [Obsolete("近四季EPS取得，改由GetPE從Yahoo Stock取得")]
        /// <summary>
        /// EPS
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<List<EpsInfoModel>> GetEPS(List<VtiInfoModel> data)
        {
            var res = new List<EpsInfoModel>();
            var httpClient = new HttpClient();
            var taskCount = 20;
            var tasks = new Task[taskCount];

            //分群組 for 多執行緒分批執行
            var vtiGroup = TaskUtils.GroupSplit(data, taskCount);

            for (int i = 0; i < tasks.Length; i++)
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
                            var epsInfo = new EpsInfoModel() { StockId = item.StockId, StockName = item.StockName, EpsData = new List<Eps>() };
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

        /// <summary>
        /// 取得本益比(PE) & 近四季EPS
        /// 本益比驗證：https://www.cmoney.tw/forum/stock/1256
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<PeInfoModel>> GetPE(List<StockPriceInfoModel> data, int taskCount = 20)
        {
            var res = new List<PeInfoModel>();
            var httpClient = new HttpClient();
            var tasks = new Task[taskCount];

            //分群組 for 多執行緒分批執行
            var vtiGroup = TaskUtils.GroupSplit(data, taskCount);

            for (int i = 0; i < tasks.Length; i++)
            {
                var vtiData = vtiGroup[i];
                tasks[i] = Task.Run(async () =>
                {
                    foreach (var item in vtiData)
                    {
                        var url = @$"https://tw.stock.yahoo.com/_td-stock/api/resource/StockServices.revenues;includedFields=priceAssessment;period=quarterSum4;priceAssessmentPeriod=quarter;symbol={item.StockId}.TW?bkt=&device=desktop&ecma=modern&feature=enableGAMAds%2CenableGAMEdgeToEdge%2CenableEvPlayer&intl=tw&lang=zh-Hant-TW&partner=none&prid=7ojmd05invvv9&region=TW&site=finance&tz=Asia%2FTaipei&ver=1.2.2103&returnMeta=true";
                        var resMessage = await httpClient.GetAsync(url);

                        //檢查回應的伺服器狀態StatusCode是否是200 OK
                        if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var sr = await resMessage.Content.ReadAsStringAsync();
                            var deserializeData = JsonSerializer.Deserialize<EpsInfo2Model>(sr);

                            var epsAcc4Q = deserializeData.data.data.result.revenues.Count > 0 ? Convert.ToDecimal(deserializeData.data.data.result.revenues[0].epsAcc4Q) : 0M;
                            var interval = deserializeData.data.data.result.revenues.Count > 0 ? $"{deserializeData.data.data.result.revenues[0].date.AddMonths(-11).ToString("yyyy/MM")} ~ {deserializeData.data.data.result.revenues[0].date.ToString("yyyy/MM")}" : "";

                            var peInfo = new PeInfoModel() {
                                StockId = item.StockId, 
                                StockName = item.StockName, 
                                EpsAcc4QInterval = interval,
                                EpsAcc4Q = epsAcc4Q,
                                PE = epsAcc4Q > 0 ? Convert.ToDouble(Math.Round(item.Price / epsAcc4Q, 2)) : 0
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
        /// 取得每月MoM. YoY增減趴數
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<RevenueInfoModel>> GetRevenue(List<StockPriceInfoModel> data, int taskCount = 20)
        {
            var res = new List<RevenueInfoModel>();
            var httpClient = new HttpClient();
            var tasks = new Task[taskCount];

            //分群組 for 多執行緒分批執行
            var vtiGroup = TaskUtils.GroupSplit(data, taskCount);

            for (int i = 0; i < tasks.Length; i++)
            {
                var vtiData = vtiGroup[i];
                tasks[i] = Task.Run(async () =>
                {
                    foreach (var item in vtiData)
                    {
                        var url = $"https://tw.stock.yahoo.com/quote/{item.StockId}.TW/revenue"; //營收
                        var resMessage = await httpClient.GetAsync(url);

                        //檢查回應的伺服器狀態StatusCode是否是200 OK
                        if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var sr = await resMessage.Content.ReadAsStringAsync();
                            var config = Configuration.Default;
                            var context = BrowsingContext.New(config);
                            var document = await context.OpenAsync(res => res.Content(sr));

                            var listTR = document.QuerySelectorAll("#qsp-revenue-table .table-body-wrapper ul li[class*='List']").Take(4);
                            var revenueInfo = new RevenueInfoModel() { StockId = item.StockId, StockName = item.StockName, RevenueData = new List<Revenue>() };
                            foreach (var tr in listTR)
                            {
                                revenueInfo.RevenueData.Add(new Revenue()
                                {
                                    RevenueInterval = tr.QuerySelector("div").Children[0].TextContent,//YYYY/MM
                                    MOM = Convert.ToDouble(tr.QuerySelectorAll("span")[1].TextContent.Replace("%", "")),//MOM 
                                    YOY = Convert.ToDouble(tr.QuerySelectorAll("span")[6].TextContent.Replace("%", ""))//YOY
                                });
                            }

                            lock (_lock) 
                            {
                                res.Add(revenueInfo);
                            }                            
                        }
                    }
                });
            }
            Task.WaitAll(tasks);

            return res;
        }


        public async Task<List<BuyingResultModel>> GetBuyingResult(List<VtiInfoModel> vtiData, List<PeInfoModel> peData, List<RevenueInfoModel> revenueData)
        {
            var res =
                (from a in vtiData
                select new BuyingResultModel()
                {
                    StockId = a.StockId,
                    StockName = a.StockName,
                    Price = a.Price,
                    HighIn52 = a.HighIn52,
                    LowIn52 = a.LowIn52,
                    EpsInterval = peData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.EpsAcc4QInterval,
                    EPS = peData.Where(c => c.StockId == a.StockId).FirstOrDefault().EpsAcc4Q,
                    PE = peData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.PE ?? 0,
                    RevenueInterval_1 = revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[0]?.RevenueInterval : "",
                    MOM_1 = revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[0].MOM : 0,
                    YOY_1 = revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[0].YOY : 0,
                    RevenueInterval_2 = revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[1]?.RevenueInterval : "",
                    MOM_2 = revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[1].MOM : 0,
                    YOY_2 = revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[1].YOY : 0,
                    RevenueInterval_3 = revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[2]?.RevenueInterval : "",
                    MOM_3 = revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[2].MOM : 0,
                    YOY_3 = revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? revenueData.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[2].YOY : 0,
                    VTI = a.VTI,
                    Amount = a.Amount
                })
                .Where(c =>
                    c.EPS > 0
                    && c.PE < 30
                    //&& (c.MOM_1 > 0 || c.YOY_1 > 0)
                    && (c.MOM_1 > 0 || c.MOM_2 > 0 && c.MOM_3 > 0 )
                )
                .OrderByDescending(o => o.EPS);                

            return res.ToList();
        }
    }
}
