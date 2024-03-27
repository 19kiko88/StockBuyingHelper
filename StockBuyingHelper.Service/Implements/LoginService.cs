using AngleSharp;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using StockBuyingHelper.Service.Models;
using StockBuyingHelper.Models;
using StockBuyingHelper.Service.Interfaces;

namespace StockBuyingHelper.Service.Implements
{
    public class LoginService: ILoginService
    {
        private readonly IConfiguration _config;

        public LoginService(/*IConfiguration config*/)
        {
            //_config = config;
        }


        public async Task<(string jwtToken, string errorMsg)> JwtLogin(AppSettings.JwtSettings jwtSettings, string account, string password)
        {
            var errorMsg = string.Empty;
            var user = new UserInfoModel();
            var jwtToken = string.Empty;

            if (string.IsNullOrEmpty(account))
            {
                errorMsg = "尚未登入.";
                return (jwtToken: jwtToken, errorMsg: errorMsg);
            }

            if (account == "homer_chen" && password == "Ab12345678")
            {
                user = new UserInfoModel()
                {
                    Account = account,
                    EmployeeId = "AA12345678",
                    Name = "Test",
                    Email = "Test@test.com",
                    Role = "1"
                };
            }
            else
            {
                return (jwtToken: jwtToken, errorMsg: "帳號密碼錯誤.");
            }

            var claims = new List<Claim>
            {
                new Claim("Name", user.Name),
                new Claim("Email", user.Email),
                new Claim("Role", user.Role.ToString()),
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));

            var jwt = new JwtSecurityToken(
                issuer: jwtSettings.ValidIssuer,
                audience: jwtSettings.ValidAudience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
            );
            
            jwtToken = new JwtSecurityTokenHandler().WriteToken(jwt);

            return (jwtToken: jwtToken, errorMsg: errorMsg);
        }
    }
}
