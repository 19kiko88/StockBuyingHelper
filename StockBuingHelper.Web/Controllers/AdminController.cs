using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Interfaces;
using System.Data;

namespace StockBuingHelper.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _admin;

        public AdminController(IAdminService admin) 
        { 
            _admin = admin;
        }

        /// <summary>
        /// 清除db.[Volume_Detail]。重新抓取從Yahoo API抓最新成交資料
        /// </summary>
        /// <returns></returns>
        [Authorize(Roles = "Admin")]
        [HttpDelete]
        public async Task<Result<int>> DeleteVolumeDetail()
        {
            var res = new Result<int>();
            try
            {
                var data = await _admin.DeleteVolumeDetail();
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
    }
}
