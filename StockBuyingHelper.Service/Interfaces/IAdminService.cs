using StockBuyingHelper.Service.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Interfaces
{
    public interface IAdminService
    {
        public Task<(int cnt, string errorMsg)> TruncateTable(string tableName);
        public Task RefreshStockList();
        public Task RefreshRevenueInfo();
        public Task RefreshVolumeInfo();
        public Task RefreshEpsInfo(string Os = "Windows");
        public Task<List<HistoryInfoDto>> GetHistory();
    }
}
