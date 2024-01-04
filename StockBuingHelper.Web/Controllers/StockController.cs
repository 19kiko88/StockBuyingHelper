using Microsoft.AspNetCore.Mvc;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Dtos;
using StockBuyingHelper.Service.Interfaces;
using System.Diagnostics;

namespace StockBuingHelper.Web.Controllers
{

    [ApiController]
    [Route("api/[controller]/[action]")]
    public class StockController : ControllerBase
    {
        private readonly IStockService _stockService;

        public StockController(IStockService stockService)
        {
            _stockService = stockService;
        }

        [HttpPost]
        public async Task<Result<List<BuyingResultDto>>> GetVtiData([FromBody] ResGetVtiDataDto reqData)
        {
            var sw = new Stopwatch();
            var res = new Result<List<BuyingResultDto>>();

            try
            {
                sw.Start();

                var filterIds = new List<string>();
                if (!string.IsNullOrEmpty(reqData.specificStockId))
                {
                    if (reqData.specificStockId.IndexOf(',') > 0)
                    {
                        filterIds = reqData.specificStockId.Split(',').ToList();
                    }
                    else
                    {
                        filterIds = new List<string> { reqData.specificStockId };
                    }
                }

                var listStockInfo = await _stockService.GetStockList(reqData.queryEtfs, filterIds);
                var ids = listStockInfo.Select(c => c.StockId).ToList();
                var listPrice = await _stockService.GetPrice(ids);
                var listHighLow = await _stockService.GetHighLowIn52Weeks(listPrice);
                var listVti = await _stockService.GetVTI(listPrice, listHighLow, filterIds.Count > 0 ? true : false, reqData.vtiIndex);

                //vti篩選，縮小資料範圍
                var vtiFilterData = listVti.Select(c => new StockInfoDto { StockId = c.StockId, StockName = c.StockName }).ToList();
                var listVolume = await _stockService.GetVolume(vtiFilterData, 7);

                //volume篩選，縮小資料範圍
                var volumFilterData = (from a in listPrice 
                                   join b in listVolume on a.StockId equals b.StockId
                                   select new StockInfoDto
                                   {
                                       StockId = a.StockId, StockName = a.StockName, Price = a.Price
                                   }).ToList();
                var listPe = await _stockService.GetPE(volumFilterData);
                var listRevenue = await _stockService.GetRevenue(volumFilterData, 3);

                var buyingList = await _stockService.GetBuyingResult(listStockInfo, listVti, listPe, listRevenue, listVolume, reqData.specificStockId);
                
                res.Content = buyingList.Select((c, idx) => new BuyingResultDto
                {
                    sn = (idx + 1).ToString(),
                    stockId = c.StockId,
                    stockName = c.StockName,
                    price = c.Price,
                    highIn52 = c.HighIn52,
                    lowIn52 = c.LowIn52,
                    epsInterval = c.EpsInterval,
                    eps = c.EPS,
                    pe = c.PE,
                    revenueDatas = c.RevenueDatas,
                    volumeDatas = c.VolumeDatas,
                    vti = c.VTI,
                    amount = c.Amount
                }).ToList();

                res.Success = true;
            }
            catch (Exception ex)
            {
                res.Message = ex.Message;
            }

            sw.Stop();
            res.Message += $"Run time：{Math.Round(Convert.ToDouble(sw.ElapsedMilliseconds / 1000), 2)}(s)。";

            return res;
        }

        //getROE
        //filter0050
        //exceldownload
        //10年線
    }
}
