using StockBuyingHelper.Service.Implements;
using StockBuyingHelper.Service.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<IStockService, StockService>();

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

app.UseHttpsRedirection();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    //®M¥ÎCORS
    app.UseCors("allowCors");
}

app.MapControllers();

app.Run();
