using Microsoft.Extensions.Options;
using StockBuyingHelper.Models;
using StockBuyingHelper.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StockBuyingHelper.Models.AppSettings;

namespace StockBuyingHelper.Service.Implements
{
    public class AdoNetService: IAdoNetService
    {
        private readonly AppSettings.ConnectionStrings _conn;

        public AdoNetService(IOptions<AppSettings.ConnectionStrings> conn) 
        {
            _conn = conn.Value;
        }

        public async Task<(int modifyRowCount, string errorMsg)> CreateCommand(string sqlCommand)
        {
            var res = 0;
            var errorMsg = string.Empty;

            using (SqlConnection connection = new SqlConnection(_conn.SBHConnection))
            {
                SqlCommand command = new SqlCommand(sqlCommand, connection);
                command.Connection.Open();

                try
                {
                    res = command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    errorMsg = $"CreateCommand Error. {ex.Message}";
                }
            }

            return (res, errorMsg);
        }
    }
}
