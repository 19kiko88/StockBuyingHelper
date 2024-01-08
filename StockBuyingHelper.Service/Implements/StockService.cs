﻿using System.Text;
using System.Text.Json;
using AngleSharp;
using StockBuyingHelper.Service.Dtos;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using StockBuyingHelper.Service.Utility;


namespace StockBuyingHelper.Service.Implements
{
    public class StockService: IStockService
    {
        public bool SpecificStockId { get; set; }
        private readonly object _lock = new object();
        private readonly int cacheExpireTime = 1440;//快取保留時間
        //private static List<StockVolumeInfoModel> lockVolumeObj = new List<StockVolumeInfoModel>();

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

        public async Task<List<StockInfoModel>> GetFilterStockList(bool queryEtfs, List<string> specificIds = null)
        {
            var data = await GetStockList();
            var data_ = data.AsEnumerable();

            if (SpecificStockId == false && queryEtfs == false)
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
        /// <returns></returns>
        public async Task<List<StockPriceInfoModel>> GetPrice(List<string> specificIds = null, int taskCount = 25)
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

        public async Task<List<StockPriceInfoModel>> GetFilterPriceList(List<string> specificIds = null, decimal? priceLow = 0, decimal? priceHigh = 200)
        {
            var res = await GetPrice(specificIds);

            if (SpecificStockId == false && (priceLow.HasValue || priceHigh.HasValue))
            {
                priceLow = priceLow.HasValue ? priceLow.Value : 0;
                priceHigh = priceHigh.HasValue ? priceHigh.Value : 99999;
                res = res.Where(c => c.Price >= priceLow && c.Price <= priceHigh).ToList();
            }

            return res;
        }

        /// <summary>
        /// 取得52周間最高 & 最低價(非最後收盤成交價)
        /// </summary>
        /// <param name="realTimeData">即時成交價</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<StockHighLowIn52WeeksInfoModel>> GetHighLowIn52Weeks(List<StockPriceInfoModel> realTimeData, int taskCount = 25)
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
        /// 每日成交量資訊
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="txDateCount">交易日(3~10)</param>
        /// <param name="taskCount">多執行緒數量</param>
        /// <returns></returns>
        public async Task<List<StockVolumeInfoModel>> GetVolume(List<VtiInfoModel> data, int txDateCount = 10, int taskCount = 25)
        {
            var res = new List<StockVolumeInfoModel>();
            var httpClient = new HttpClient();

            if (txDateCount < 3)
            {
                txDateCount = 3;
            }
            if (txDateCount > 10)
            {
                txDateCount = 10;
            }

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
                        var url = $"https://tw.stock.yahoo.com/_td-stock/api/resource/StockServices.tradesWithQuoteStats;limit=210;period=day;symbol={item.StockId}.TW?bkt=&device=desktop&ecma=modern&feature=enableGAMAds%2CenableGAMEdgeToEdge%2CenableEvPlayer&intl=tw&lang=zh-Hant-TW&partner=none&prid=5m2req5ioan5s&region=TW&site=finance&tz=Asia%2FTaipei&ver=1.2.2112&returnMeta=true";
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
                                StockId = item.StockId,
                                StockName = item.StockName,
                                VolumeInfo = deserializeData.data.list.Take(txDateCount).Select(c => new VolumeData 
                                {
                                    txDate = DateOnly.FromDateTime(Convert.ToDateTime(c.formattedDate)),
                                    dealerDiffVolK = c.dealerDiffVolK,
                                    investmentTrustDiffVolK = c.investmentTrustDiffVolK,
                                    foreignSellVolK = c.foreignDiffVolK,
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

        public async Task<List<StockVolumeInfoModel>> GetFilterVolumeList(List<VtiInfoModel> vtiData, int volumeKLimit = 500, int? txDateCount = 10, int? taskCount = 25)
        {
            //var res = new List<StockVolumeInfoModel>();

            //var noVolumeInfoData = vtiData.GroupJoin(lockVolumeObj, a => a.StockId, b => b.StockId, (a, b) => new
            //{
            //    a.StockId,
            //    a.StockName,
            //    b
            //}).SelectMany(c => c.b.DefaultIfEmpty(), (c, b) => new
            //{
            //    c.StockId,
            //    c.StockName,
            //    hasVolumeData = (b == null ? false : true)
            //});

            //var newData = new List<StockInfoDto>();
            //foreach (var item in noVolumeInfoData)
            //{
            //    if (item.hasVolumeData)
            //    {
            //        var oldData = lockVolumeObj.Where(c => c.StockId == item.StockId).FirstOrDefault();
            //        if (oldData != null)
            //        {
            //            res.Add(oldData);//記憶體已經有volume資料，直接add到model
            //        }
            //    }
            //    else
            //    {
            //        newData.Add(new StockInfoDto() { StockId = item.StockId, StockName = item.StockName });//記憶體還沒有volume資料的newData，要透過yahoo api取得volume相關資料
            //    }
            //}

            //var newDataVolumeInfo = await GetVolume(newData, txDateCount.Value, taskCount.Value);
            //res.AddRange(newDataVolumeInfo);//api取得的結果加入到model

            //lock (lockVolumeObj)
            //{
            //    //var diff = res.Except(lockVolumeObj);
            //    foreach (var item in newDataVolumeInfo)
            //    {//移除舊資料，記憶體只存放最新資料
            //        //lockVolumeObj.RemoveAll(c => c.StockId == item.StockId);
            //        lockVolumeObj.Add(item);
            //    }
            //}
            var res = await GetVolume(vtiData, txDateCount.Value, taskCount.Value);
            if (SpecificStockId == false && volumeKLimit > 0)
            {
                //近3個交易日，必須有一天成交量大於500
                res = res.Where(c => c.VolumeInfo.Take(3).Where(c => c.volumeK > 500).Any()).ToList();
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
        public async Task<List<VtiInfoModel>> GetVTI(List<StockPriceInfoModel> priceData, List<StockHighLowIn52WeeksInfoModel> highLowData)
        {
            var vtiData =
                (from a in priceData
                 join b in highLowData on a.StockId equals b.StockId
                 select new VtiInfoModel
                 {
                     StockId = a.StockId,
                     StockName = a.StockName,
                     Price = a.Price,
                     HighIn52 = b.HighPriceInCurrentYear,
                     LowIn52 = b.LowPriceInCurrentYear
                 }).ToList();


            foreach (var item in vtiData)
            {
                var diffHigh = (item.HighIn52 - item.LowIn52) == 0M ? 0.01M : (item.HighIn52 - item.LowIn52);

                /*
                 *Ref：https://www.facebook.com/1045010642367425/posts/1076024329266056/
                 *在近52周最高最低價格區間內，目前價格離最高價還有多少百分比(vti越高，表示離52周區間內最高點越近)
                 */
                item.VTI = Convert.ToDouble(Math.Round( 1 - (item.Price - item.LowIn52) / diffHigh, 2));
                item.Amount = Convert.ToInt32(Math.Round(item.VTI, 2) * 1000);
            }

            return vtiData;
        }

        public async Task<List<VtiInfoModel>> GetFilterVTI(List<StockPriceInfoModel> priceData, List<StockHighLowIn52WeeksInfoModel> highLowData, int? amountLimit = 0)
        {
            var res = await GetVTI(priceData, highLowData);
            if (SpecificStockId == false && amountLimit.HasValue)
            {
                res = res.Where(c => c.Amount >= amountLimit).ToList();
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
        public async Task<List<PeInfoModel>> GetPE(List<StockInfoDto> data, int taskCount = 25)
        {
            var res = new List<PeInfoModel>();
            var httpClient = new HttpClient();

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

        public async Task<List<PeInfoModel>> GetFilterPeList(List<StockInfoDto> data, decimal? eps = 0, double? pe = 25, int taskCount = 25)
        {
            var res = await GetPE(data, taskCount);

            res =
                (from a in res
                 join b in data on a.StockId equals b.StockId
                 where StockType.ETFs.Contains(b.Type) || (b.Type == StockType.ESVUFR && (a.EpsAcc4Q > eps && a.PE <= pe))
                 select a).ToList();            

            return res;
        }

        /// <summary>
        /// 取得每月MoM. YoY增減趴數
        /// </summary>
        /// <param name="data">資料來源</param>
        /// <param name="taskCount">多執行緒的Task數量</param>
        /// <returns></returns>
        public async Task<List<RevenueInfoModel>> GetRevenue(List<PeInfoModel> data, int revenueMonthCount = 3, int taskCount = 25)
        {
            var res = new List<RevenueInfoModel>();
            var httpClient = new HttpClient();

            if (revenueMonthCount < 3)
            {
                revenueMonthCount = 3;
            }
            if (revenueMonthCount > 6)
            {
                revenueMonthCount = 6;
            }

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
                        var url = $"https://tw.stock.yahoo.com/quote/{item.StockId}.TW/revenue"; //營收
                        var resMessage = await httpClient.GetAsync(url);

                        //檢查回應的伺服器狀態StatusCode是否是200 OK
                        if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var sr = await resMessage.Content.ReadAsStringAsync();
                            var config = Configuration.Default;
                            var context = BrowsingContext.New(config);
                            var document = await context.OpenAsync(res => res.Content(sr));

                            var listTR = document.QuerySelectorAll("#qsp-revenue-table .table-body-wrapper ul li[class*='List']").Take(revenueMonthCount);
                            var revenueInfo = new RevenueInfoModel() { StockId = item.StockId, StockName = item.StockName, RevenueData = new List<RevenueData>() };
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
        public async Task<List<BuyingResultModel>> GetBuyingResult(List<StockInfoModel> stockData, List<VtiInfoModel> vtiData, List<PeInfoModel> peData, List<RevenueInfoModel> revenueData, List<StockVolumeInfoModel> volumeData, string specificStockId = "")
        {
            var data =
                (from a in stockData
                 join b in vtiData on a.StockId equals b.StockId
                 join c in peData on a.StockId equals c.StockId
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
                     PE = c.PE,
                     RevenueDatas = d.RevenueData,
                     VolumeDatas = e.VolumeInfo,
                     VTI = b.VTI,
                     Amount = b.Amount
                 }).AsEnumerable();

            if (string.IsNullOrEmpty(specificStockId))
            {
                data = data.Where(c =>
                    //c.VolumeDatas.Take(3).Where(c => c.volumeK > 500).Any() &&
                     (c.Type == StockType.ESVUFR
                        && (
                                    //c.EPS > 0 && c.PE < 25
                                    //&& (
                                    //(c.RevenueDatas[0].MOM > 0 || c.RevenueDatas[1].MOM > 0 || c.RevenueDatas[2].MOM > 0) 
                                    c.RevenueDatas.Where(c => c.monthYOY > 0).Count() >= 3
                                    &&
                                    (
                                        c.RevenueDatas[0].yoy > 0 //|| 
                                                                  //(c.RevenueDatas[0].YOY > 0 && (c.RevenueDatas[0].YOY > c.RevenueDatas[1].YOY && c.RevenueDatas[1].YOY > c.RevenueDatas[2].YOY))
                                    )
                                //)
                            )
                        )
                        || StockType.ETFs.Contains(c.Type)//ETF不管營收
                 ).AsEnumerable();
            }

            var res = data
                 .OrderByDescending(o => o.Type).ThenByDescending(o => o.EPS)
                 .ToList();

            /*
             * 選股條件：
             * https://www.ptt.cc/bbs/Stock/M.1680899841.A.5F6.html
             * https://www.ptt.cc/bbs/Stock/M.1468072684.A.DD1.html
             * 1.長期(1年)：VTI大於800 
             * 2.長期(近四季)：EPS > 0 && PE < 25
             * 3.中長期(近一季)：MoM至少有兩個月為正 || (最新(YoY)當月累計營收要比去年累計營收高 || YoY逐步轉正)
             * 4.短期：近3個交易日，有成交量超過500的紀錄
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
