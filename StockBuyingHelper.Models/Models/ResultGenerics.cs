using StockBuyingHelper.Models.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Models.Models
{
    public class Result<T>: Result, IResult<T>, IResult
    {
        public T Content { get; set; }

        public Result() : this(false)//this關鍵字會呼叫Result(bool success)建構子
        {
            
        }

        public Result(bool success) : base(success) 
        {
            Type typeFromHandle = typeof(T);
            if (typeFromHandle.IsValueType || typeFromHandle.GetConstructor(Type.EmptyTypes) != null)
            {
                Content = (T)Activator.CreateInstance(typeFromHandle);
            }
            else
            {
                Content = default(T);
            }
        }
    }
}
