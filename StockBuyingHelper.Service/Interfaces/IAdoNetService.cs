using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Interfaces
{
    public interface IAdoNetService
    {
        public Task<(int modifyRowCount, string errorMsg)> ExecuteNonQuery(string sqlCommand);
    }
}
