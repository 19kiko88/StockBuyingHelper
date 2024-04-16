using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using StockBuyingHelper.Service.Models;
using StockBuyingHelper.Models;
using StockBuyingHelper.Service.Interfaces;
using System.Security.Cryptography;
using SBH.Repositories;
using SBH.Repositories.Models;

namespace StockBuyingHelper.Service.Implements
{
    public class AuthService: IAuthService
    {
        private readonly IRsaService _rasService;
        private readonly SBHContext _context;

        public AuthService(
            IRsaService rasService,
            SBHContext context
        )
        {
            _rasService = rasService;
            _context = context;
        }


        /// <summary>
        /// 登入 (取得JWT Token)
        /// Ref：
        /// (YouTube)https://www.youtube.com/watch?v=rH_fZ4Zkxic&ab_channel=TechnoSaviour
        /// (GitHub)https://github.com/Technosaviour/RSA-net-core/tree/RSA-Angular-net-core
        /// https://pieterdlinde.medium.com/angular-rsa-encryption-netcore-decryption-public-private-key-78f2770f955f
        /// </summary>
        /// <param name="jwtSettings"></param>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<(string jwtToken, string errorMsg)> Login(AppSettings.JwtSettings jwtSettings, string account, string password)
        {
            var errorMsg = string.Empty;            
            var jwtToken = string.Empty;

            if (string.IsNullOrEmpty(account))
            {
                errorMsg = "尚未登入.";
                return (jwtToken: jwtToken, errorMsg: errorMsg);
            }

            //var user = _context.Users.Where(c => c.Account.ToLower() == account.ToLower()).FirstOrDefault();
            var user = 
                (from userInfo in _context.Users 
                join userRole in _context.User_Role on userInfo.Role equals userRole.Role_Id
                where userInfo.Account.ToLower() == account.ToLower() && userInfo.Status == true
                select new UserInfoModel { 
                    Account = userInfo.Account,
                    Password = userInfo.Password,
                    PasswordSalt = userInfo.Password_Salt,
                    Name = userInfo.User_Name,
                    Email = userInfo.Email,
                    Role = userRole.Role_Name
                }).FirstOrDefault();

            if (user != null)
            {
                //(私Key)解密   
                var decryptPsw = _rasService.Decrypt(password);

                //頭尾補上英文salt字串
                var pswWithSalt = $"{user.PasswordSalt.Substring(3, 3)}{decryptPsw}{user.PasswordSalt.Substring(0, 3)}";

                //密碼比對(RSA加密後的結果每次都不會一樣，所以要解密後再進行比對，不能直接用加密字串進行比對。)
                var pswCompare = _rasService.Decrypt(user.Password) == pswWithSalt ? true : false;

                if (pswCompare == false)
                {
                    return (jwtToken: jwtToken, errorMsg: "密碼錯誤.");
                }                
            }
            else
            {
                return (jwtToken: jwtToken, errorMsg: "帳號密碼錯誤或帳號停止使用.");
            }

            var claims = new List<Claim>
            {
                new Claim("Account", user.Account),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
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
