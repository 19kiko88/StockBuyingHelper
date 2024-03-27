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
    }
}
