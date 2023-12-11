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
        public async Task<string> GetStockList()
        {
            //var res = await _stockService.GetStockList();
            var ress = await _stockService.GetHighLowIn52Weeks();
            return "";
        }
    }
}
