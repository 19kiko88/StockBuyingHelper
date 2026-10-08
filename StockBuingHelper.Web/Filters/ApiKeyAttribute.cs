using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Cryptography;
using System.Text;

namespace StockBuingHelper.Web.Filters
{
    /// <summary>
    /// 驗證請求標頭 X-Api-Key 是否與伺服器端金鑰一致(給排程上傳資料的匿名 API 使用)。
    /// 金鑰優先讀環境變數 SBH_VPS_API_KEY,其次讀設定 VpsApiKey(放在不進版控的 appsettings.Local.json)。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ApiKeyAttribute : Attribute, IAuthorizationFilter
    {
        private const string HeaderName = "X-Api-Key";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var services = context.HttpContext.RequestServices;
            var logger = services.GetRequiredService<ILogger<ApiKeyAttribute>>();

            var expected = Environment.GetEnvironmentVariable("SBH_VPS_API_KEY")
                ?? services.GetRequiredService<IConfiguration>()["VpsApiKey"];

            // 伺服器沒設定金鑰時一律拒絕,避免忘了設定就變成沒有保護
            if (string.IsNullOrWhiteSpace(expected))
            {
                logger.LogError("伺服器未設定 VpsApiKey,拒絕 {Path} 的請求", context.HttpContext.Request.Path);
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                return;
            }

            var actual = context.HttpContext.Request.Headers[HeaderName].ToString();

            // 固定時間比較,避免從回應時間差推測金鑰
            var isValid = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(actual),
                Encoding.UTF8.GetBytes(expected));

            if (!isValid)
            {
                logger.LogWarning("{Path} 的 X-Api-Key 驗證失敗,來源 {RemoteIp}",
                    context.HttpContext.Request.Path, context.HttpContext.Connection.RemoteIpAddress);
                context.Result = new UnauthorizedResult();
            }
        }
    }
}
