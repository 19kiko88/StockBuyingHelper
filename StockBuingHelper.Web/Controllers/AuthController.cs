using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using StockBuingHelper.Web.Dtos.Request;
using StockBuingHelper.Web.Dtos.Response;
using StockBuyingHelper.Models;
using StockBuyingHelper.Models.Models;
using StockBuyingHelper.Service.Interfaces;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StockBuingHelper.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppSettings.JwtSettings _jwt;
        private readonly IAuthService _loginService;

        public AuthController(
            IAuthService loginService,
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
            var jwt = await _loginService.Login(_jwt, data.Account, data.Password);
            
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

        /// <summary>
        /// 驗證JWT內容是否被竄改過
        /// Ref：https://stackoverflow.com/questions/38725038/c-sharp-how-to-verify-signature-on-jwt-token
        /// </summary>
        /// <param name="jwt">JWT</param>
        /// <param name="secretKey">JWT Key</param>
        /// <returns></returns>
        public async Task<Result<bool>> JwtSignatureVerify(string jwt) 
        {
            var res = new Result<bool>();
            try
            {
                res.Content = await _loginService.JwtSignatureVerify(jwt, _jwt.Key);
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
