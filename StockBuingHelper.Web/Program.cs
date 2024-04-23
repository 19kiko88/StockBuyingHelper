using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Serilog;
using StockBuyingHelper.Models;
using StockBuyingHelper.Service.Implements;
using StockBuyingHelper.Service.Interfaces;
using System.Text;
using SBH.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using Coravel;
using StockBuingHelper.Web.Tasks;

var builder = WebApplication.CreateBuilder(args);
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
var _configuration = builder.Configuration;
var secret = _configuration.GetValue<string>("JwtSettings:Secret");
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddDbContext<SBHContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SBHConnection")));

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

// Add services to the container.

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // nameof(ApiResponseHandler);//
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;// nameof(ApiResponseHandler); 
})
.AddJwtBearer(options =>
{
    // 當驗證失敗時，回應標頭會包含 WWW-Authenticate 標頭，這裡會顯示失敗的詳細錯誤原因
    options.IncludeErrorDetails = true; // 預設值為 true，有時會特別關閉

    options.TokenValidationParameters = new TokenValidationParameters
    {
        // 透過這項宣告，就可以從 "NAME" 取值
        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        // 透過這項宣告，就可以從 "Role" 取值，並可讓 [Authorize] 判斷角色
        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",

        // 驗證 Issuer (一般都會)
        ValidateIssuer = true,
        ValidIssuer = _configuration.GetValue<string>("JwtSettings:ValidIssuer"),

        // 驗證 Audience (通常不太需要)
        ValidateAudience = false,
        //ValidAudience = = _configuration.GetValue<string>("JwtSettings:ValidAudience"),

        // 驗證 Token 的有效期間 (一般都會)
        ValidateLifetime = true,

        // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
        ValidateIssuerSigningKey = false,

        // 應該從 IConfiguration 取得
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("JwtSettings:Key")))
    };

    /*
     *回應Json格式錯誤訊息
     *Ref：https://stackoverflow.com/questions/70884906/net-how-to-set-the-the-response-body-when-the-authorization-failed
     *
     *自訂HttpErrorResponse的回應格式
     *从壹开始【NetCore3.0】 46 ║ 授权认证：自定义返回格式
     *Ref：https://www.cnblogs.com/laozhang-is-phi/p/11833800.html
     *
     */
    //options.Events = new JwtBearerEvents
    //{
    //    OnAuthenticationFailed = context =>
    //    {
    //        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    //        context.Response.ContentType = "application/json";
    //        var result = JsonConvert.SerializeObject(new StockBuyingHelper.Models.Models.Result<int>()
    //        {
    //            Success = false,
    //            Message = "未取得JWT授權.",
    //            Content = 401,
    //        }); ;

    //        return context.Response.WriteAsync(result);
    //    },
    //    OnForbidden = context =>
    //    {
    //        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    //        context.Response.ContentType = "application/json; charset=utf-8";
    //        var result = JsonConvert.SerializeObject(new StockBuyingHelper.Models.Models.Result<int>()
    //        {
    //            Success = false,
    //            Message = "權限不足.",
    //            Content = 403,
    //        });

    //        return context.Response.WriteAsync(result);
    //    }
    //};
});

builder.Services.AddControllers();
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "wwwroot";
});

//註冊Coravel
/*
 *Ref：
 *Coravel官網： https://docs.coravel.net/Scheduler/
 *https://blog.yowko.com/coravel/
 *Crontab格式： https://hyak4j.github.io/2021_11_26_linuxcrontab/
 */
builder.Services.AddScheduler();
builder.Services.AddTransient<RefreshVolumeInfoTask>();
builder.Services.AddTransient<RefreshRevenueInfoTask>();
builder.Services.AddTransient<RefreshEpsInfoTask>();

builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IVolumeService, VolumeService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdoNetService, AdoNetService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRsaService, RsaService>();


//builder.Services.AddOptions();
builder.Services.Configure<AppSettings.ConnectionStrings>(builder.Configuration.GetSection(AppSettings._ConnectionStrings));
builder.Services.Configure<AppSettings.JwtSettings>(builder.Configuration.GetSection(AppSettings._JwtSettings));
builder.Services.Configure<AppSettings.CustomizeSettings>(builder.Configuration.GetSection("CustomizeSettings"));


/*CORS*/
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
        name:MyAllowSpecificOrigins,
        builder =>
        {
            builder.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
        });
    });
}


var app = builder.Build();

var provider = app.Services;
provider.UseScheduler(scheduler =>
{
    //Crontab格式： https://hyak4j.github.io/2021_11_26_linuxcrontab/
    scheduler.Schedule<RefreshVolumeInfoTask>().Cron("0 10 * * *");//UTC時間要減8小時(10:00(UTC) = 18:00(UTC+8))
    scheduler.Schedule<RefreshRevenueInfoTask>().Cron("30 18 * * *");//UTC時間要減8小時(18:30(UTC) = 02:30(UTC+8))
    scheduler.Schedule<RefreshEpsInfoTask>().Cron("30 19 * * *");//UTC時間要減8小時(19:30(UTC) = 03:30(UTC+8))
});

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseRouting();
if (app.Environment.IsDevelopment())
{
    //套用CORS
    app.UseCors(MyAllowSpecificOrigins);
}
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.MapControllers();
app.UseSpaStaticFiles();


/*
 *UseSpa() returns index.html from API instead of 404
 *ref：https://stackoverflow.com/questions/67625133/usespa-returns-index-html-from-api-instead-of-404
 *
 *ref：https://www.cnblogs.com/dudu/p/16686077.html
 */
app.MapWhen(x => !x.Request.Path.Value.StartsWith("/api"), builder =>
{
    builder.UseSpa(spa =>
    {
        spa.Options.SourcePath = $"wwwroot";
        if (app.Environment.IsDevelopment())
        {
            spa.UseProxyToSpaDevelopmentServer("http://localhost:4200");
        }
    });
});

app.Run();
