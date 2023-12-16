using AngleSharp;
using AngleSharp.Dom;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using System.Text;

namespace StockBuyingHelper.Service.Implements
{
    public class StockService: IStockService
    {
        public async Task<List<StockInfo>> GetStockList()
        {
            var res = new List<StockInfo>();
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
                            res.Add(new StockInfo()
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

        public async Task<List<GetHighLowIn52WeeksInfo>> GetHighLowIn52Weeks()
        {
            var res = new List<GetHighLowIn52WeeksInfo>();
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
                var datas = new List<GetHighLowIn52WeeksInfo>();                   
                
                foreach (var tr in listTR)
                {
                    var Tds = tr.Children;
                    try
                    {
                        datas.Add(new GetHighLowIn52WeeksInfo()
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

        public async Task<List<StockPrice>> GetPrice()
        {
            var res = new List<StockPrice>();

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

                        res.Add(new StockPrice()
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

        public async Task<List<VtiInfo>> GetVTI(List<StockPrice> priceData, List<GetHighLowIn52WeeksInfo> highLowData, int amountLimit = 0)
        {
            var listVTI =
                (from a in priceData
                join b in highLowData on a.StockId equals b.StockId
                select new VtiInfo {
                    StockId = a.StockId,
                    StockName = a.StockName,
                    Price = a.Price,
                    HighIn52 = b.HighPriceInCurrentYear,
                    LowIn52 = b.LowPriceInCurrentYear
                }).ToList();

            foreach (var item in listVTI)
            {
                try
                {
                    var diffHigh = (item.HighIn52 - item.LowIn52) == 0M ? 0.01M : (item.HighIn52 - item.LowIn52);

                    /*
                     *Ref：https://www.facebook.com/1045010642367425/posts/1076024329266056/
                     *在近52周最高最低價格區間內，目前價格離最高價還有多少百分比(vti越高，表示離52周區間內最高點越近)
                     */
                    item.VTI = Math.Round( 1 - (item.Price - item.LowIn52) / diffHigh, 2);
                    item.Amount = Convert.ToInt32(Math.Round(item.VTI, 2) * 1000);
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                }
            }

            if (amountLimit > 0)
            {
                listVTI = listVTI.Where(c => c.Amount > amountLimit).ToList();
            }

            return listVTI;
        }   
        
        public async Task<List<EpsInfo>> GetEPS(List<VtiInfo> data)
        {
            var res = new List<EpsInfo>();

            try
            {
                var httpClient = new HttpClient();

                foreach (var item in data) 
                {
                    var url = $"https://tw.stock.yahoo.com/quote/{item.StockId}.TW/eps";//eps
                    //var url_revenue = https://tw.stock.yahoo.com/quote/2317.TW/revenue; //營收
                    var resMessage = await httpClient.GetAsync(url);

                    //檢查回應的伺服器狀態StatusCode是否是200 OK
                    if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var sr = await resMessage.Content.ReadAsStringAsync();
                        var config = Configuration.Default;
                        var context = BrowsingContext.New(config);
                        var document = await context.OpenAsync(res => res.Content(sr));

                        var listTR = document.QuerySelectorAll("#main-3-QuoteFinanceEps-Proxy .table-body-wrapper li").Take(4);//只取最近4季的EPS
                        var epsInfo = new EpsInfo() { StockId = item.StockId, StockName = item.StockName };
                        foreach (var tr in listTR)
                        {
                            epsInfo.EPS += Convert.ToDecimal(tr.QuerySelector("span").InnerHtml);
                        }

                        var reEPS = epsInfo.EPS == 0.0M ? 0.01M : epsInfo.EPS;
                        epsInfo.PE = Convert.ToDouble( Math.Round(item.Price / reEPS, 2) );
                        res.Add(epsInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                var errMsg = ex.Message;
            }

            return res;
        }

        /// <summary>
        /// 每月營收
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<List<RevenueInfo>> GetRevenue(List<VtiInfo> data)
        {
            var res = new List<RevenueInfo>();

            try
            {
                var httpClient = new HttpClient();

                foreach (var item in data)
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
                        var revenueInfo = new RevenueInfo() { StockId = item.StockId, StockName = item.StockName, RevenueData = new List<Revenue>() };
                        foreach (var tr in listTR)
                        {
                            revenueInfo.RevenueData.Add(new Revenue()
                            {
                                YearMonth = tr.QuerySelector("div").Children[0].TextContent,//YYYY/MM
                                MOM = Convert.ToDouble(tr.QuerySelectorAll("span")[1].TextContent.Replace("%", "")),//MOM 
                                YOY = Convert.ToDouble(tr.QuerySelectorAll("span")[3].TextContent.Replace("%", "")),//YOY
                                YoyGrandTotal = Convert.ToDouble(tr.QuerySelectorAll("span")[6].TextContent.Replace("%", ""))
                            });
                        }

                        res.Add(revenueInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                var errMsg = ex.Message;
            }

            return res;
        }
    }
}
