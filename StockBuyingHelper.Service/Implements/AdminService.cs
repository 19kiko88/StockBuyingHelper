using EFCore.BulkExtensions;
using SBH.Repositories.Models;
using StockBuyingHelper.Service.Dtos;
using StockBuyingHelper.Service.Enums;
using StockBuyingHelper.Service.Interfaces;
using StockBuyingHelper.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Implements
{
    public class AdminService: IAdminService
    {
        private readonly IAdoNetService _adoNetService;
        private readonly IStockService _stockService;
        private readonly SBHContext _context;

        public AdminService(
            IStockService stockService,
            IAdoNetService adoNetService,
            SBHContext context
            ) 
        { 
            _stockService = stockService;
            _adoNetService = adoNetService;
            _context = context;
        }

        public async Task<(int cnt, string errorMsg)> TruncateTable(string tableName)
        {
            var sqlCommand = @$"truncate table {tableName}";
            var res = await _adoNetService.ExecuteNonQuery(sqlCommand);
            return res;
        }        

        public async Task RefreshStockList()
        {            
            await TruncateTable("Stock_Info");
            var res = await _stockService.GetStockInfo(MarketEnum.ListedCompany);

            var data = res.Select(c => new Stock_Info
            {
                Stock_Id = c.StockId.Trim(),
                Stock_Name = c.StockName,
                Market = c.Market,
                Industry_Type = c.IndustryType,
                CFICode = c.CFICode
            });

            _context.BulkInsert(data);
        }

        public async Task RefreshRevenueInfo()
        {
            await TruncateTable("Revenue_Info");
            var ids = _context.Stock_Info.Select(c => c.Stock_Id).ToList();
            var revenueData = await _stockService.GetRevenue(ids);
            var data = new List<Revenue_Info>();

            foreach (var item in revenueData)
            {
                foreach (var revenueItem in item.RevenueData)
                {
                    data.Add(new Revenue_Info()
                    {
                        Stock_Id = item.StockId,
                        Interval_Year = revenueItem.revenueInterval.Split('/')[0],
                        Interval_Month = revenueItem.revenueInterval.Split('/')[1],
                        MoM = Convert.ToDecimal(revenueItem.mom),
                        YoYMonth = Convert.ToDecimal(revenueItem.monthYOY),
                        YoY = Convert.ToDecimal(revenueItem.yoy),
                    });
                }
            }

            _context.BulkInsert(data);
        }

        public async Task RefreshVolumeInfo()
        {
            await TruncateTable("Volume_Detail");
            var volumeData = await _stockService.GetVolume();
            var data = new List<Volume_Detail>();

            foreach (var item in volumeData)
            {
                if (item.VolumeInfo != null)
                {
                    foreach (var volumeItem in item.VolumeInfo)
                    {
                        try
                        {
                            data.Add(new Volume_Detail()
                            {
                                Stock_Id = item.StockId,
                                Tx_Date = Convert.ToDateTime($"{volumeItem.txDate.Year}/{volumeItem.txDate.Month}/{volumeItem.txDate.Day}") ,
                                Foreign_Diff_VolK = volumeItem.foreignDiffVolK,
                                Dealer_Diff_VolK = volumeItem.dealerDiffVolK,
                                InvestmentTrust_Diff_VolK = volumeItem.investmentTrustDiffVolK,
                                VolumeK = volumeItem.volumeK
                            });
                        }
                        catch (Exception ex)
                        {
                            var msg = ex.Message;
                        }

                    }
                }
            }

            _context.BulkInsert(data);
        }

        public async Task RefreshEpsInfo(string Os = "Windows")
        {
            await TruncateTable("Eps_Info");
            var epsData = await _stockService.GetEps(Os);
            var data = new List<Eps_Info>();

            foreach (var item in epsData)
            {
                try
                {
                    data.Add(new Eps_Info()
                    {
                        Stock_Id = item.StockId,
                        Eps_Acc_4Q = item.EpsAcc4Q,
                        Eps_Acc_4Q_Interval_Start = item.EpsAcc4QInterval.IndexOf("~") > 0 ? item.EpsAcc4QInterval.Split('~')[0] : "",
                        Eps_Acc_4Q_Interval_End = item.EpsAcc4QInterval.IndexOf("~") > 0 ? item.EpsAcc4QInterval.Split('~')[1] : "",
                    });
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                }
            }

            _context.BulkInsert(data);
        }

        public async Task<List<HistoryInfoDto>> GetHistory()
        {
            var data = _context.History.Select(c => new HistoryInfoDto()
            {
                HistoryId = c.History_Id,   
                Content = c.Content,
                CreateUser = c.Create_User,
                CreateDate = DateOnly.FromDateTime(c.Create_Date_Time)
            }).ToList();

            return data;
        }
    }
}
