using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Models.Interface
{
    public interface IResult<T>
    {
        T Content { get; set; }
    }
}
