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
            try
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
                         EpsInterval = listEps.Where(c => c.StockId == a.StockId).FirstOrDefault()?.EpsInterval ?? "",
                         EPS = listEps.Where(c => c.StockId == a.StockId).FirstOrDefault()?.EpsData?.Sum(s => s.EPS) ?? 0,
                         PE = listEps.Where(c => c.StockId == a.StockId).FirstOrDefault()?.PE ?? 0,
                         RevenueInterval_1 = listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[0]?.RevenueInterval : "",
                         MOM_1 = listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[0].MOM : 0,
                         YOY_1 = listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[0].YOY : 0,
                         RevenueInterval_2 = listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[1]?.RevenueInterval : "",
                         MOM_2 = listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[1].MOM : 0,
                         YOY_2 = listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[1].YOY : 0,
                         RevenueInterval_3 = listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[2]?.RevenueInterval : "",
                         MOM_3 = listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[2].MOM : 0,
                         YOY_3 = listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData.Count > 0 ? listRevenue.Where(c => c.StockId == a.StockId).FirstOrDefault()?.RevenueData[2].YOY : 0,
                         a.VTI,
                         a.Amount
                     })
                .ToList()
                .Where(c =>
                    c.EPS > 0
                    && c.PE < 30
                    && (c.MOM_1 > 0 || c.YOY_1 > 0)
                )
                .OrderByDescending(o => o.EPS);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }   

            return "";
        }
    }
}
