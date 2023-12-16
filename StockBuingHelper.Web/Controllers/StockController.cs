using Microsoft.AspNetCore.Mvc;

using StockBuyingHelper.Service;
using StockBuyingHelper.Service.Interfaces;

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
        public async Task<string> GetVtiData()
        {
            //var listStockInfo = await _stockService.GetStockList();
            var listPrice = await _stockService.GetPrice();
            var listHighLow = await _stockService.GetHighLowIn52Weeks();
            var listVti = await _stockService.GetVTI(listPrice, listHighLow, 800);
            var listEps = await _stockService.GetEPS(listVti);
            var listRevenue = await _stockService.GetRevenue(listVti);

            var QQQ =
                (from a in listVti
                 select new
                 {
                     a.StockId,
                     a.StockName,
                     a.Price,
                     a.HighIn52,
                     a.LowIn52,
                     listEps.Where(c => c.StockId == a.StockId).FirstOrDefault()?.EPS,
                     listEps.Where(c => c.StockId == a.StockId).FirstOrDefault()?.PE,
                     listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.FirstOrDefault()?.MOM,
                     listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.FirstOrDefault()?.YOY,
                     listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.FirstOrDefault()?.YoyGrandTotal,
                     a.VTI,
                     a.Amount
                 })
                 .ToList()
                .Where(c => c.EPS > 0 && c.PE < 30 && (c.MOM > 0 || c.YOY > 0 || c.YoyGrandTotal > 0))
                .OrderByDescending(o => o.EPS);

            //var qq =
            //    (
            //    from a in listVti
            //    join b in listEps on a.StockId equals b.StockId
            //    select new { 
            //        a.StockId, 
            //        a.StockName, 
            //        a.Price, 
            //        a.HighIn52, 
            //        a.LowIn52, 
            //        b.EPS, 
            //        b.PE,
            //        a.VTI, a.Amount 
            //    } 
            //    into res_1
            //    join c in listRevenue on res_1.StockId equals c.StockId
            //    select new
            //    {
            //        res_1.StockId,
            //        res_1.StockName,
            //        res_1.Price,
            //        res_1.HighIn52,
            //        res_1.LowIn52,
            //        res_1.EPS,
            //        res_1.PE,
            //        MOM = listRevenue.Where()
            //        c.RevenueData[0].YOY,
            //        c.RevenueData[0].YoyGrandTotal,
            //        res_1.VTI,
            //        res_1.Amount
            //    }).ToList()
            //    .Where(c => c.EPS > 0 && c.PE < 30)
            //    .OrderByDescending(o => o.EPS);
            //    //.OrderBy(o => o.PE);
            //    //.OrderByDescending(o => o.VTI);             


            return "";
        }
    }
}
