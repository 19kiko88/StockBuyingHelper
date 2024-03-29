using AngleSharp;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using StockBuyingHelper.Service.Models;
using StockBuyingHelper.Models;
using StockBuyingHelper.Service.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace StockBuyingHelper.Service.Implements
{
    public class LoginService: ILoginService
    {
        private readonly IConfiguration _config;

        public LoginService()
        {
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


        /// <summary>
        /// 驗證JWT內容是否被竄改過
        /// Ref：https://stackoverflow.com/questions/38725038/c-sharp-how-to-verify-signature-on-jwt-token
        /// </summary>
        /// <param name="jwt">JWT</param>
        /// <param name="secretKey">JWT Key</param>
        /// <returns></returns>
        public async Task<bool> JwtSignatureVerify(string jwt, string secretKey)
        {
            var res = false;
            string[] parts = jwt.Split(".".ToCharArray());
            var header = parts[0];
            var payload = parts[1];
            var signature = parts[2];//Base64UrlEncoded signature from the token

            byte[] bytesToSign = Encoding.UTF8.GetBytes(string.Join(".", header, payload));

            byte[] secret = Encoding.UTF8.GetBytes(secretKey);

            var alg = new HMACSHA256(secret);
            var hash = alg.ComputeHash(bytesToSign);

            var output = Convert.ToBase64String(hash);
            output = output.Split('=')[0]; // Remove any trailing '='s
            output = output.Replace('+', '-'); // 62nd char of encoding
            output = output.Replace('/', '_'); // 63rd char of encoding
                                               //var computedSignature = Base64UrlEncode(hash);
            
            if (signature == output)
            {
                res = true;
            }

            return res;


            //return userInfo;
        }
    }
}
