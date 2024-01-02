using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace StockBuyingHelper.Service.Utility
{
    /*
     * 使用記憶體快取 MemoryCache 增加回應速度
     * ref：https://blog.hungwin.com.tw/csharp-memorycache/
     */
    public static class AppCacheUtils
    {
        #region 屬性
        private static ObjectCache Cache
        {
            get
            {
                return MemoryCache.Default;
            }
        }

        // 因為與其他應用程式共用此記憶體快取，所以建議增加此應用程式的前置名稱
        private static string IdNameStart = "SBH_";

        public enum Expiration
        {
            Absolute,
            Sliding
        }
        #endregion

        #region 建構子
        //public AppCache()
        //{

        //}
        #endregion


        #region 方法
        /// <summary>
        /// 取得快取
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object Get(string key)
        {
            return Cache[IdNameStart + key];
        }

        /// <summary>
        /// 移除快取
        /// </summary>
        /// <param name="key"></param>
        public static void Remove(string key)
        {
            Cache.Remove(IdNameStart + key);
        }

        /// <summary>
        /// 是否存在快取
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool IsSet(string key)
        {
            return (Cache[IdNameStart + key] != null);
        }

        /// <summary>
        /// 設定快取
        /// </summary>
        /// <param name="key">KEY</param>
        /// <param name="data">資料</param>
        public static void Set(string key, object data)
        {
            Set(key, data, Expiration.Absolute, 1440);
        }

        /// <summary>
        /// 設定快取
        /// </summary>
        /// <param name="key">KEY</param>
        /// <param name="data">資料</param>
        /// <param name="Expiration">保留別</param>
        /// <param name="cacheTime">保存時間(分鐘)</param>
        public static void Set(string key, object data, Expiration expiration, int cacheTime)
        {
            CacheItemPolicy policy = new CacheItemPolicy();
            if (expiration == Expiration.Absolute)
            {
                policy.AbsoluteExpiration = DateTime.Now + TimeSpan.FromMinutes(cacheTime);
            }
            else if (expiration == Expiration.Sliding)
            {
                policy.SlidingExpiration = TimeSpan.FromMinutes(cacheTime);
            }
            Cache.Add(new CacheItem(IdNameStart + key, data), policy);
        }
        #endregion
    }
}
