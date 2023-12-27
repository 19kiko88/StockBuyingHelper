using Microsoft.AspNetCore.Mvc;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Dtos;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
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
                var listStockInfo = await _stockService.GetStockList();
                var listPrice = await _stockService.GetPrice();                
                var listHighLow = await _stockService.GetHighLowIn52Weeks(listPrice);
                var listVti = await _stockService.GetVTI(listPrice, listHighLow, reqData.specificStockId, reqData.vtiIndex);
                var data =
                    (from a in listStockInfo
                     join b in listVti on a.StockId equals b.StockId
                     select new StockInfoDto
                     {
                         StockId = a.StockId,
                         StockName = a.StockName,
                         Price = b.Price,
                         Type = a.CFICode
                     })
                     .Where(c => ((reqData.queryEtfs && 1 == 1) || (!reqData.queryEtfs && c.Type == StockType.ESVUFR)))
                     .ToList();
                var listPe = await _stockService.GetPE(data);
                Thread.Sleep(6000);//間隔6秒，避免被誤認攻擊
                var listRevenue = await _stockService.GetRevenue(data, 3);
                Thread.Sleep(6000);//間隔6秒，避免被誤認攻擊
                var listVolume = await _stockService.GetVolume(data, 7);
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
                //res.Exception = ex;
                res.Message = ex.Message;
            }

            sw.Start();
            res.Message += $"Run time：{Math.Round(Convert.ToDouble(sw.ElapsedMilliseconds / 1000), 2)}(s)。";

            return res;
        }

        //getROE
        //filter0050
        //exceldownload
        //10年線
    }
}
