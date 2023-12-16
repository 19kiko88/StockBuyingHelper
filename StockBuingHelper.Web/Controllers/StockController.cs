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

            var qq =
                (
                from a in listVti
                join b in listEps on a.StockId equals b.StockId
                select new { a.StockId, a.StockName, a.Price, a.HighIn52, a.LowIn52, b.EPS, b.PE, a.VTI, a.Amount }
                ).ToList()
                .Where(c => c.EPS > 0 && c.PE < 30)
                .OrderByDescending(o => o.EPS);
                //.OrderBy(o => o.PE);
                //.OrderByDescending(o => o.VTI);             
                

            return "";
        }
    }
}
