using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Utility
{
    public static class TaskUtils
    {
        /// <summary>
        /// 切割List資料 for 多執行緒分批執行
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="splitCount"></param>
        /// <returns></returns>
        public static List<List<T>> GroupSplit<T>(List<T> data, int? splitCount = 20)
        {
            var cnt = splitCount.HasValue ? splitCount.Value : 1;
            var group = new List<List<T>>();

            for (int i = 0; i < data.Count; i++)
            {                    
                var groupNo = i % cnt;
                if (group.Count < (groupNo + 1))
                {
                    group.Add(new List<T>());
                }
                group[groupNo].Add(data[i]);
            }

            return group;
        }
    }
}
