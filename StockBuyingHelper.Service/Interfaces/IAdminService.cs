﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Interfaces
{
    public interface IAdminService
    {
        public Task<(int cnt, string errorMsg)> DeleteVolumeDetail();
    }
}
