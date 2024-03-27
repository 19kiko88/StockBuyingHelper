using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Interfaces;
using System.Security.Claims;

namespace StockBuingHelper.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly AppSettings.JwtSettings _jwt;
        private readonly ILoginService _loginService;

        public LoginController(
            ILoginService loginService,
            IOptions<AppSettings.JwtSettings> jwt
            )
        {
            _loginService = loginService;
            _jwt = jwt.Value;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<Result<string>> Login([FromBody] LoginDto data)
        {
            var res = new Result<string>();
            var jwt = await _loginService.JwtLogin(_jwt, data.Account, data.Password);
            
            if (!string.IsNullOrEmpty(jwt.errorMsg))
            {
                res.Message = jwt.errorMsg;                
            }
            else
            {
                res.Content = jwt.jwtToken;
                res.Success = true;
            }


            return res;
        }

        [Authorize]
        public string TestAPI() 
        {
            var userInfo = $"name：{HttpContext.User.FindFirstValue("Name")}, email：{HttpContext.User.FindFirstValue("Email")}, Role：{HttpContext.User.FindFirstValue("Role")}";
            return userInfo;
        }
    }
}
