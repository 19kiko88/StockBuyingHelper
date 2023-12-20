using Microsoft.AspNetCore.Mvc;
using StockBuyingHelper.Service.Dtos;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;

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
        public async Task<List<BuyingResultDto>> GetVtiData([FromQuery] bool queryEtfs = false)
        {
            var res = new List<BuyingResultDto>();
            try
            {
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
                var listPe = await _stockService.GetPE(data, 25);
                var listRevenue = await _stockService.GetRevenue(data, 25);
                var buyingList = await _stockService.GetBuyingResult(listStockInfo, listVti, listPe, listRevenue);
                
                res = buyingList.Select((c, idx) => new BuyingResultDto
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
                    RevenueInterval_1 = c.RevenueInterval_1,
                    MOM_1 = c.MOM_1,
                    YOY_1 = c.YOY_1,
                    RevenueInterval_2 = c.RevenueInterval_2,
                    MOM_2 = c.MOM_2,
                    YOY_2 = c.YOY_2,
                    RevenueInterval_3 = c.RevenueInterval_3,
                    MOM_3 = c.MOM_3,
                    YOY_3 = c.YOY_3,
                    VTI = c.VTI,
                    Amount = c.Amount
                }).ToList();

            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }   

            return res;
        }

        //getROE
        //filter0050
        //exceldownload
        //10年線
    }
}
