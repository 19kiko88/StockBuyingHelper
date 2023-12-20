using System.Text;
using System.Text.Json;

using AngleSharp;
using AngleSharp.Dom;
using StockBuyingHelper.Service.Dtos;
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

            res = res.Where(c => c.CFICode == StockType.ESVUFR || StockType.ETFs.Contains(c.CFICode)).ToList();

            return res;
        }

        /// <summary>
        /// 取得即時價格
        /// </summary>
        /// <returns></returns>
        public async Task<List<StockInfoDto>> GetPrice()
        {
            var res = new List<StockInfoDto>();

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

                    res.Add(new StockInfoDto()
                    {
                        StockId = tr.Children[0].InnerHtml,
                        StockName = tr.Children[1].Children[0].InnerHtml,
                        Price = Convert.ToDecimal(tr.Children[2].Children[0].InnerHtml)
                    });
                }
            }

            return res;
        }

        /// <summary>
        /// 取得52周間最高 & 最低價(非最後收盤成交價)
        /// </summary>
        /// <param name="realTimeData">即時成交價</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<GetHighLowIn52WeeksInfoModel>> GetHighLowIn52Weeks(List<StockInfoDto> realTimeData, int taskCount = 20)
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

                                var data = new GetHighLowIn52WeeksInfoModel()
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

                                await Task.Delay(10);//add await for async
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
        /// <param name="priceData">即時價格資料</param>
        /// <param name="highLowData">取得52周間最高 & 最低價資料(非最終成交價)</param>
        /// <param name="amountLimit">vti篩選，vti值必須在多少以上</param>
        /// <returns></returns>
        public async Task<List<VtiInfoModel>> GetVTI(List<StockInfoDto> priceData, List<GetHighLowIn52WeeksInfoModel> highLowData, int amountLimit = 0)
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

        /// <summary>
        /// 取得本益比(PE) & 近四季EPS
        /// 本益比驗證：https://www.cmoney.tw/forum/stock/1256
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<PeInfoModel>> GetPE(List<StockInfoDto> data, int taskCount = 20)
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

                            /*
                             *json => model web tool 
                             *ref：https://json2csharp.com/
                             */
                            var deserializeData = JsonSerializer.Deserialize<EpsInfoModel>(sr);

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
        public async Task<List<RevenueInfoModel>> GetRevenue(List<StockInfoDto> data, int taskCount = 20)
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

        /// <summary>
        /// 取得總表
        /// </summary>
        /// <param name="stockData"></param>
        /// <param name="vtiData">VTI資料</param>
        /// <param name="peData">近四季EPS&PE資料</param>
        /// <param name="revenueData">近三個月營收MoM. YoY資料</param>
        /// <returns></returns>
        public async Task<List<BuyingResultModel>> GetBuyingResult(List<StockInfoModel> stockData, List<VtiInfoModel> vtiData, List<PeInfoModel> peData, List<RevenueInfoModel> revenueData)
        {
            var res =
                (from a in stockData
                 join b in vtiData on a.StockId equals b.StockId
                 join c in peData on a.StockId equals c.StockId
                 join d in revenueData on a.StockId equals d.StockId
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
                     PE = c.PE,
                     RevenueInterval_1 = d.RevenueData.Count > 0 ? d.RevenueData[0].RevenueInterval : "",
                     MOM_1 = d.RevenueData.Count > 0 ? d.RevenueData[0].MOM : 0,
                     YOY_1 = d.RevenueData.Count > 0 ? d.RevenueData[0].YOY : 0,
                     RevenueInterval_2 = d.RevenueData.Count > 0 ? d.RevenueData[1].RevenueInterval : "",
                     MOM_2 = d.RevenueData.Count > 0 ? d.RevenueData[1].MOM : 0,
                     YOY_2 = d.RevenueData.Count > 0 ? d.RevenueData[1].YOY : 0,
                     RevenueInterval_3 = d.RevenueData.Count > 0 ? d.RevenueData[2].RevenueInterval : "",
                     MOM_3 = d.RevenueData.Count > 0 ? d.RevenueData[2].MOM : 0,
                     YOY_3 = d.RevenueData.Count > 0 ? d.RevenueData[2].YOY : 0,
                     VTI = b.VTI,
                     Amount = b.Amount
                 })
                 .Where(c => 
                    (c.Type == StockType.ESVUFR && 
                    (
                        c.EPS > 0
                        && c.PE < 25
                        && ((c.MOM_1 > 0 || c.MOM_2 > 0 || c.MOM_3 > 0) || (c.YOY_1 > 0 || (c.YOY_1 > 0 &&  (c.YOY_1 > c.YOY_2  && c.YOY_2 > c.YOY_3))))
                    )) 
                    || StockType.ETFs.Contains(c.Type)//ETF不管營收
                 )
                 .OrderByDescending(o => o.Type).ThenByDescending(o => o.EPS)
                 .ToList();

            /*
             * 選股條件：
             * 1.長期(1年)：VTI大於800 
             * 2.長期(近四季)：EPS > 0 && PE < 25
             * 3.中長期(近一季)：MoM不能都為負成長 || (最新(YoY)當月累計營收要比去年累計營收高 || YoY逐步轉正)
             * 4.短期：交易量 > 300
             */

            return res.ToList();
        }

        [Obsolete("近四季EPS取得，改由GetPE從Yahoo Stock取得")]
        /// <summary>
        /// EPS
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<List<ObsoleteEpsInfoModel>> GetEPS(List<VtiInfoModel> data)
        {
            var res = new List<ObsoleteEpsInfoModel>();
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
                            var epsInfo = new ObsoleteEpsInfoModel() { StockId = item.StockId, StockName = item.StockName, EpsData = new List<Eps>() };
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
