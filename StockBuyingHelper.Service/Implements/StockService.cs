using AngleSharp;
using AngleSharp.Dom;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Implements
{
    public class StockService: IStockService
    {
        private readonly object _lock = new object();

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

        public async Task<List<GetHighLowIn52WeeksInfoModel>> GetHighLowIn52Weeks()
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
                var document = await context.OpenAsync(res => res.Content(sr));


                var listTR = document.QuerySelectorAll("tr[id^=row]");
                var datas = new List<GetHighLowIn52WeeksInfoModel>();                   
                
                foreach (var tr in listTR)
                {
                    var Tds = tr.Children;
                    try
                    {
                        datas.Add(new GetHighLowIn52WeeksInfoModel()
                        {
                            StockId = Tds[0].TextContent.Trim(),
                            StockName = Tds[1].TextContent.Trim(),
                            Price = Convert.ToDecimal(Tds[2].TextContent),
                            High = Convert.ToDecimal(Tds[3].TextContent),
                            Low = Convert.ToDecimal(Tds[4].TextContent),
                            HighLowPriceGap = Convert.ToDecimal(Tds[5].TextContent),
                            HighLowPercentGap = Convert.ToDouble(Tds[6].TextContent),
                            UpdateDate = DateOnly.FromDateTime(DateTime.Now),
                            HighPriceInCurrentSeason = Convert.ToDecimal(Tds[8].TextContent),
                            HighPriceInCurrentSeasonPercentGap = Convert.ToDouble(Tds[9].TextContent),
                            LowPriceInCurrentSeason = Convert.ToDecimal(Tds[10].TextContent),
                            LowPriceInCurrentSeasonPercentGap = Convert.ToDouble(Tds[11].TextContent),
                            HighPriceInCurrentHalfYear = Convert.ToDecimal(Tds[12].TextContent),
                            HighPriceInCurrentHalfYearPercentGap = Convert.ToDouble(Tds[13].TextContent),
                            LowPriceInCurrentHalfYear = Convert.ToDecimal(Tds[14].TextContent),
                            LowPriceInCurrentHalfYearPercentGap = Convert.ToDouble(Tds[15].TextContent),
                            HighPriceInCurrentYear = Convert.ToDecimal(Tds[16].TextContent),
                            HighPriceInCurrentYearPercentGap = Convert.ToDouble(Tds[17].TextContent),
                            LowPriceInCurrentYear = Convert.ToDecimal(Tds[18].TextContent),
                            LowPriceInCurrentYearPercentGap = Convert.ToDouble(Tds[19].TextContent),
                        });
                    }
                    catch (Exception ex)
                    {
                        var err = ex.Message;                      
                    }

                }

                res = datas;
            }

            return res;
        }

        public async Task<List<StockPriceModel>> GetPrice()
        {
            var res = new List<StockPriceModel>();

            try
            {
                var httpClient = new HttpClient();
                //var listPrice = new List<StockPrice>();
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

                        res.Add(new StockPriceModel()
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

        public async Task<List<VtiInfoModel>> GetVTI(List<StockPriceModel> priceData, List<GetHighLowIn52WeeksInfoModel> highLowData, int amountLimit = 0)
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
        /// EPS
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<List<EpsInfoModel>> GetEPS(List<VtiInfoModel> data)
        {
            var res = new List<EpsInfoModel>();
            var httpClient = new HttpClient();
            var idx = 0;
            var taskCount = 20;
            var tasks = new Task[taskCount];
            var vtiGroup = new List<VtiInfoModel>[taskCount];

            foreach (var item in data)
            {
                idx++;

                var groupNo = idx % taskCount;
                if (vtiGroup[groupNo] == null)
                {
                    vtiGroup[groupNo] = new List<VtiInfoModel>();
                }
                vtiGroup[groupNo].Add(item);
            }

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
        /// 本益比(PE) & 近四季EPS
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<List<PeInfoModel>> GetPE(List<VtiInfoModel> data)
        {
            var res = new List<PeInfoModel>();
            var httpClient = new HttpClient();
            var idx = 0;
            var taskCount = 20;
            var tasks = new Task[taskCount];
            var vtiGroup = new List<VtiInfoModel>[taskCount];

            foreach (var item in data)
            {
                idx++;

                var groupNo = idx % taskCount;
                if (vtiGroup[groupNo] == null)
                {
                    vtiGroup[groupNo] = new List<VtiInfoModel>();
                }
                vtiGroup[groupNo].Add(item);
            }

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

                            var epsAcc4Q = deserializeData.data.data.result.revenues.Count > 0 ? Convert.ToDecimal(deserializeData.data.data.result.revenues[0].epsAcc4Q ?? "0.01") : 0.01M;
                            var interval = deserializeData.data.data.result.revenues.Count > 0 ? deserializeData.data.data.result.revenues[0].date.ToString("yyyy/MM") ?? "" : "";

                            var peInfo = new PeInfoModel() {
                                StockId = item.StockId, 
                                StockName = item.StockName, 
                                EpsAcc4QInterval = interval,
                                EpsAcc4Q = epsAcc4Q,
                                PE = Convert.ToDouble(Math.Round(item.Price / epsAcc4Q, 2))
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
        /// 每月營收
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<List<RevenueInfoModel>> GetRevenue(List<VtiInfoModel> data)
        {
            var res = new List<RevenueInfoModel>();
            var httpClient = new HttpClient();
            var idx = 0;
            var taskCount = 20;
            var tasks = new Task[taskCount];
            var vtiGroup = new List<VtiInfoModel>[taskCount];

            foreach (var item in data)
            {
                idx++;

                var groupNo = idx % taskCount;
                if (vtiGroup[groupNo] == null)
                {
                    vtiGroup[groupNo] = new List<VtiInfoModel>();
                }
                vtiGroup[groupNo].Add(item);
            }

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
                    && (c.MOM_1 > 0 || c.YOY_1 > 0)
                )
                .OrderByDescending(o => o.EPS);                

            return res.ToList();
        }
    }
}
