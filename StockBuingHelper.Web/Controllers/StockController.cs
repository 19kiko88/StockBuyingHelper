using Microsoft.AspNetCore.Mvc;
using StockBuingHelper.Web.Dtos;
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

        [HttpGet]
        public async Task<Result<List<BuyingResultDto>>> GetVtiData([FromQuery] bool queryEtfs = false)
        {
            var sw = new Stopwatch();
            var res = new Result<List<BuyingResultDto>>();
            try
            {
                sw.Start();
                var listStockInfo = await _stockService.GetStockList();
                var listPrice = await _stockService.GetPrice();                
                var listHighLow = await _stockService.GetHighLowIn52Weeks(listPrice);
                var listVti = await _stockService.GetVTI(listPrice, listHighLow, 800);
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
                     .Where(c => (queryEtfs && 1 == 1) || (!queryEtfs && c.Type == StockType.ESVUFR))
                     .ToList();
                var listPe = await _stockService.GetPE(data);
                var listRevenue = await _stockService.GetRevenue(data, 3);
                var listVolume = await _stockService.GetVolume(data, 7);
                var buyingList = await _stockService.GetBuyingResult(listStockInfo, listVti, listPe, listRevenue, listVolume);
                
                res.Content = buyingList.Select((c, idx) => new BuyingResultDto
                {
                    Sn = (idx + 1).ToString(),
                    StockId = c.StockId,
                    StockName = c.StockName,
                    Price = c.Price,
                    HighIn52 = c.HighIn52,
                    LowIn52 = c.LowIn52,
                    EpsInterval = c.EpsInterval,
                    EPS = c.EPS,
                    PE = c.PE,
                    RevenueDatas = c.RevenueDatas,
                    VolumeDatas = c.VolumeDatas,
                    VTI = c.VTI,
                    Amount = c.Amount
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
