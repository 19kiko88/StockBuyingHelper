using Microsoft.AspNetCore.HttpOverrides;
using StockBuyingHelper.Models;
using StockBuyingHelper.Service.Implements;
using StockBuyingHelper.Service.Interfaces;
using ElmahCore.Mvc;
using ElmahCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "wwwroot";
});
builder.Services.Configure<AppSettings.ConnectionStrings>(builder.Configuration.GetSection(AppSettings._ConnectionStrings));
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IVolumeService, VolumeService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdoNetService, AdoNetService>();

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

//Ref¡Ghttps://github.com/ElmahCore/ElmahCore
builder.Services.AddElmah();


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    //®M¥ÎCORS
    app.UseCors("allowCors");
}

app.MapControllers();
app.UseSpaStaticFiles();
app.UseElmah();

/*
 *UseSpa() returns index.html from API instead of 404
 *ref¡Ghttps://stackoverflow.com/questions/67625133/usespa-returns-index-html-from-api-instead-of-404
 *
 *ref¡Ghttps://www.cnblogs.com/dudu/p/16686077.html
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
