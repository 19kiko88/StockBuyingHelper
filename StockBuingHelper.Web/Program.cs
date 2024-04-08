using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StockBuyingHelper.Models;
using StockBuyingHelper.Service.Implements;
using StockBuyingHelper.Service.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
var _configuration = builder.Configuration;
var secret = _configuration.GetValue<string>("JwtSettings:Secret");

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);

// Add services to the container.

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // �����ҥ��ѮɡA�^�����Y�|�]�t WWW-Authenticate ���Y�A�o�̷|��ܥ��Ѫ��Բӿ��~��]
    options.IncludeErrorDetails = true; // �w�]�Ȭ� true�A���ɷ|�S�O����

    options.TokenValidationParameters = new TokenValidationParameters
    {
        // �z�L�o���ŧi�A�N�i�H�q "NAME" ����
        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        // �z�L�o���ŧi�A�N�i�H�q "Role" ���ȡA�åi�� [Authorize] �P�_����
        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",         

        // ���� Issuer (�@�볣�|)
        ValidateIssuer = true,
        ValidIssuer = _configuration.GetValue<string>("JwtSettings:ValidIssuer"),

        // ���� Audience (�q�`���ӻݭn)
        ValidateAudience = false,
        //ValidAudience = = _configuration.GetValue<string>("JwtSettings:ValidAudience"),

        // ���� Token �����Ĵ��� (�@�볣�|)
        ValidateLifetime = true,

        // �p�G Token ���]�t key �~�ݭn���ҡA�@�볣�u��ñ���Ӥw
        ValidateIssuerSigningKey = false,

        // ���ӱq IConfiguration ���o
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("JwtSettings:Key")))
    };
});

builder.Services.AddControllers();
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "wwwroot";
});

builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IVolumeService, VolumeService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdoNetService, AdoNetService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<ILoginService, LoginService>();
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
        options.AddPolicy("allowCors",
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

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    //�M��CORS
    app.UseCors("allowCors");
}

app.MapControllers();
app.UseSpaStaticFiles();

/*
 *UseSpa() returns index.html from API instead of 404
 *ref�Ghttps://stackoverflow.com/questions/67625133/usespa-returns-index-html-from-api-instead-of-404
 *
 *ref�Ghttps://www.cnblogs.com/dudu/p/16686077.html
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
