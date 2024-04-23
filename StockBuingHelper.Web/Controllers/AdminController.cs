using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Interfaces;
using System.Data;

namespace StockBuingHelper.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _admin;

        public AdminController(
            IAdminService admin
            ) 
        { 
            _admin = admin;
        }

        /// <summary>
        /// 清除db.[Volume_Detail]。重新抓取從Yahoo API抓最新成交資料
        /// </summary>
        /// <returns></returns>
        [HttpDelete]
        public async Task<Result<int>> DeleteVolumeDetail()
        {
            var res = new Result<int>();
            try
            {
                var data = await _admin.TruncateTable("Volume_Detail");
                if (string.IsNullOrEmpty(data.errorMsg))
                {
                    res.Success = true;
                }
                else
                {
                    res.Message = data.errorMsg;
                }
            }
            catch (Exception ex)
            {
                res.Message = ex.Message;
            }

            return res;
        }

        [HttpPost]
        public async Task RefreshStockList()
        {
            await _admin.RefreshStockList();
        }

        [HttpPost]
        public async Task RefreshRevenueInfo()
        {
            await _admin.RefreshRevenueInfo();
        }

        [HttpPost]
        public async Task RefreshVolumeInfo()
        {
           await _admin.RefreshVolumeInfo();
        }

        [HttpPost]
        public async Task RefreshEpsInfo()
        {
            await _admin.RefreshEpsInfo();
        }
    }
}
