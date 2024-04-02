using AngleSharp;
using Microsoft.IdentityModel.Tokens;
using StockBuyingHelper.Models;
using StockBuyingHelper.Service.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Interfaces
{
    public interface ILoginService
    {
        public Task<(string jwtToken, string errorMsg)> JwtLogin(AppSettings.JwtSettings jwtSettings, string account, string password);

        /// <summary>
        /// 驗證JWT內容是否被竄改過
        /// Ref：https://stackoverflow.com/questions/38725038/c-sharp-how-to-verify-signature-on-jwt-token
        /// </summary>
        /// <param name="jwt">JWT</param>
        /// <param name="secretKey">JWT Key</param>
        /// <returns></returns>
        public Task<bool> JwtSignatureVerify(string jwt, string secretKey);
    }
}
