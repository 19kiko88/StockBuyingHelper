using AngleSharp;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Implements
{
    public class StockService: IStockService
    {
        public async Task<string> GetStockList()
        {
            var res = "";
            var httpClient = new HttpClient();
            var url = "https://isin.twse.com.tw/isin/C_public.jsp?strMode=2";
            var resMessage = await httpClient.GetAsync(url);

            //檢查回應的伺服器狀態StatusCode是否是200 OK
            if (resMessage.StatusCode == System.Net.HttpStatusCode.OK)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//註冊BIG 5編碼
                using (var sr = new StreamReader(await resMessage.Content.ReadAsStreamAsync(), Encoding.GetEncoding(950)))
                {
                    res = sr.ReadToEnd();
                }                
            }

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
    }
}
