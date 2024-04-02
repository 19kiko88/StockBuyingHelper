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
        private readonly IRsaService _rasService;

        public LoginService(IRsaService rasService)
        {
            _rasService = rasService;
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
        public async Task<(string jwtToken, string errorMsg)> JwtLogin(AppSettings.JwtSettings jwtSettings, string account, string password)
        {
            var errorMsg = string.Empty;
            var user = new UserInfoModel();
            var jwtToken = string.Empty;
            //模擬DB資料
            var mockDbUser = new List<UserInfoModel>()
            {
                new UserInfoModel(){ 
                    Account = "Admin",
                    //Password為加上PasswordSalt後的完整密碼的雜湊值
                    Password = "a4cmsDIckfdXpRvqlrrjfL+qQi3huPGjXT40+ftLAJO685B42T45bN22EEKTdoKW17Hd6+edxpm3z3nno9QIZG0p4hDszQIfxYYpKYbrMgNABfDanymuqRFv12nZCCt0eRMF7qrWX5TejKaHc6RyE1J/bnyu5PQL/inAkMnw0UITgyQxPWadNszO304oHSP197oUTlNCJHSPnfzmQXvEF8Px/w9id/o5W1o7UzmguIlACCiZuryzNfeo7lpUjvcWjNVyUiyoFGXWuKxdfq4OBolfUYAmhnrTY+nA1S0w9H8UEaLv0vAtgrDZYivNXg7DH/2YQtRV4alXzsWyLygHwA==",
                    PasswordSalt = "onLrFc",
                    Name = "管理員",
                    Email = "Admin@test.com",
                    Role = "1"
                },
                new UserInfoModel(){
                    Account = "Test_Account",
                    //Password為加上PasswordSalt後的完整密碼的雜湊值
                    Password = "chBVb0lkT4SLeOLKjPdHU+kTCyut1HbWAk8NBqC/LXW9jm9EUfsByLbf5NdHtLa7/wTtZY4kJUvHRTY7BpDwmm2Vd1DyUNETCXPBPuLx54XBKRkV6J0shUzzVFF3haYE3x2OJL48t/hy7yGiGw8FBUvEiFILzjII0i55uggfWEQyXb71nBBMQLJbgUVsJUOCodD36nEu4QgYg4a9PRp3zAcTmg1NUD+GZdCk2fBMOGXLKFRAvSw96TY8QASnx8lOsTV5k8GlfJC1Zu4T7OJXi2rRJFbRJWJQp0B+2n7DwfH+IxP4wh1+/PvU/wweNCkhyUiNsV3yc9XytSo7p8WGRw==",
                    PasswordSalt = "eeoBIB",
                    Name = "測試帳號",
                    Email = "Test@test.com",
                    Role = "999"
                },
            };

            if (string.IsNullOrEmpty(account))
            {
                errorMsg = "尚未登入.";
                return (jwtToken: jwtToken, errorMsg: errorMsg);
            }

            user = mockDbUser.Where(c => c.Account.ToLower() == account.ToLower()).FirstOrDefault();
            if (user != null)
            {
                //(私Key)解密   
                var decryptPsw = _rasService.Decrypt(password);

                //頭尾補上英文salt字串
                var pswWithSalt = $"{user.PasswordSalt.Substring(3, 3)}{decryptPsw}{user.PasswordSalt.Substring(0, 3)}";

                //密碼比對(RSA加密後的結果每次都不會一樣，所以要解密後再進行比對。)
                var pswCompare = _rasService.Decrypt(user.Password) == pswWithSalt ? true : false;

                if (pswCompare == false)
                {
                    return (jwtToken: jwtToken, errorMsg: "密碼錯誤.");
                }                
            }
            else
            {
                return (jwtToken: jwtToken, errorMsg: "帳號錯誤.");
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
