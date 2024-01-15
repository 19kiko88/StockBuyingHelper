using StockBuyingHelper.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Interfaces
{
    public interface IVolumeService
    {
        public void InsertVolumeDetail(List<StockVolumeInfoModel> data);

        public Task<List<StockVolumeInfoModel>> GetDbVolumeDetail();
    }
}
