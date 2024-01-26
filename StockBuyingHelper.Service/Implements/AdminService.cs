using StockBuyingHelper.Service.Interfaces;
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

        public AdminService(IAdoNetService adoNetService) 
        { 
            _adoNetService = adoNetService;
        }

        public async Task<(int cnt, string errorMsg)> DeleteVolumeDetail()
        {            
            var sqlCommand = @"truncate table Volume_Detail";
            var res = await _adoNetService.CreateCommand(sqlCommand);
            return res;
        }
    }
}
