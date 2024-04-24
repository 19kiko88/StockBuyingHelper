using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Dtos;
using StockBuyingHelper.Service.Interfaces;
using System.Data;

namespace StockBuingHelper.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task RefreshStockList()
        {
            await _admin.RefreshStockList();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task RefreshRevenueInfo()
        {
            await _admin.RefreshRevenueInfo();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task RefreshVolumeInfo()
        {
           await _admin.RefreshVolumeInfo();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task RefreshEpsInfo()
        {
            await _admin.RefreshEpsInfo();
        }

        [HttpGet]
        [Authorize]
        public async Task<Result<List<HistoryInfoDto>>> GetHistory()
        {
            var res = new Result<List<HistoryInfoDto>>();

            try
            {
                var data = await _admin.GetHistory();
                data = data.OrderByDescending(c => c.HistoryId).ToList();
                
                res.Content = data;
                res.Success = true;
            }
            catch (Exception ex)
            {
                res.Message = ex.Message;                
            }

            return res;
        }
    }
}
