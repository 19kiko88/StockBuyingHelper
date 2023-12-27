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
        public static List<T>[] GroupSplit<T>(List<T> data, int? splitCount = 20)
        {
            var cnt = 0;

            if (splitCount.HasValue)
            {
                cnt = splitCount.Value;
            }

            var group = new List<T>[cnt];

            for (int i = 0; i < data.Count; i++)
            {
                try
                {
                    var groupNo = i % cnt;
                    if (group[groupNo] == null)
                    {
                        group[groupNo] = new List<T>();
                    }

                    group[groupNo].Add(data[i]);
                }
                catch (Exception ex)
                {
                    var qq = ex.Message;
                }
            }
            group = group.Where(c => c != null && c.Count > 0).ToArray();

            return group;
        }
    }
}
