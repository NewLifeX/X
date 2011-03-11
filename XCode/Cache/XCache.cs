using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading;
using System.Web;
using XCode.XLicense;
using NewLife.Configuration;

namespace XCode.Cache
{
    /// <summary>
    /// 数据缓存类
    /// </summary>
    internal static class XCache
    {
        #region 初始化
        private static SqlCache<CacheItem<DataSet>> _TableCache = new SqlCache<CacheItem<DataSet>>();
        private static SqlCache<CacheItem<Int32>> _IntCache = new SqlCache<CacheItem<Int32>>();

        /// <summary>
        /// 缓存相对有效期。
        /// -2	关闭缓存
        /// -1	非独占数据库，有外部系统操作数据库，使用请求级缓存；
        ///  0	永久静态缓存；
        /// >0	静态缓存时间，单位是秒；
        /// </summary>
        public static Int32 Expiration = -2;

        /// <summary>
        /// 数据缓存类型
        /// </summary>
        public static XCacheType CacheType
        {
            get
            {
                if (Expiration > 0) return XCacheType.Period;
                return (XCacheType)Expiration;
            }
        }

        /// <summary>
        /// 初始化设置。
        /// 读取配置；
        /// </summary>
        static XCache()
        {
            //读取配置
            //读取缓存有效期
            //String str = ConfigurationManager.AppSettings["XCacheExpiration"];
            Expiration = Config.GetConfig<Int32>("XCode.Cache.Expiration", Config.GetConfig<Int32>("XCacheExpiration", -2));
            //if (!String.IsNullOrEmpty(str))
            //{
            //    Int32 k = 0;
            //    if (Int32.TryParse(str, out k))
            //    {
            //        if (k >= -2) Expiration = k;
            //    }
            //}
            //读取检查周期
            //str = ConfigurationManager.AppSettings["XCacheCheckPeriod"];
            CheckPeriod = Config.GetConfig<Int32>("XCode.Cache.CheckPeriod", Config.GetConfig<Int32>("XCacheCheckPeriod"));
            //if (!String.IsNullOrEmpty(str))
            //{
            //    Int32 k = 0;
            //    if (Int32.TryParse(str, out k))
            //    {
            //        if (k > 0) CheckPeriod = k;
            //    }
            //}
        }
        #endregion

        #region 缓存维护
        /// <summary>
        /// 缓存维护定时器
        /// </summary>
        private static Timer AutoCheckCacheTimer;

        /// <summary>
        /// 维护定时器的检查周期，默认5秒
        /// </summary>
        public static Int32 CheckPeriod = 5;

        /// <summary>
        /// 维护
        /// </summary>
        /// <param name="obj"></param>
        private static void Check(Object obj)
        {
            //关闭缓存、永久静态缓存和请求级缓存时，不需要检查
            if (CacheType != XCacheType.Period) return;

            if (_TableCache.Count > 0)
            {
                lock (_TableCache)
                {
                    if (_TableCache.Count > 0)
                    {
                        List<String> toDel = null;
                        foreach (String sql in _TableCache.Keys)
                            if (_TableCache[sql].CacheTime.AddSeconds(Expiration) < DateTime.Now)
                            {
                                if (toDel == null) toDel = new List<String>();
                                toDel.Add(sql);
                            }
                        if (toDel != null && toDel.Count > 0)
                            foreach (String sql in toDel)
                                _TableCache.Remove(sql);
                    }
                }
            }
            if (_IntCache.Count > 0)
            {
                lock (_IntCache)
                {
                    if (_IntCache.Count > 0)
                    {
                        List<String> toDel = null;
                        foreach (String sql in _IntCache.Keys)
                            if (_IntCache[sql].CacheTime.AddSeconds(Expiration) < DateTime.Now)
                            {
                                if (toDel == null) toDel = new List<String>();
                                toDel.Add(sql);
                            }
                        if (toDel != null && toDel.Count > 0)
                            foreach (String sql in toDel)
                                _IntCache.Remove(sql);
                    }
                }
            }
        }

        /// <summary>
        /// 创建定时器。
        /// 因为定时器的原因，实际缓存时间可能要比Expiration要大
        /// </summary>
        private static void CreateTimer()
        {
            if (AutoCheckCacheTimer != null) return;

            // 声明定时器。无限延长时间，实际上不工作
            AutoCheckCacheTimer = new Timer(new TimerCallback(Check), null, Timeout.Infinite, Timeout.Infinite);
            // 改变定时器为5秒后触发一次。
            AutoCheckCacheTimer.Change(CheckPeriod * 1000, CheckPeriod * 1000);
        }
        #endregion

        #region 添加缓存
        /// <summary>
        /// 添加数据表缓存。
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ds">待缓存记录集</param>
        /// <param name="tableNames">表名数组</param>
        public static void Add(String sql, DataSet ds, String[] tableNames)
        {
            //关闭缓存
            if (CacheType == XCacheType.Close) return;
            //请求级缓存
            if (CacheType == XCacheType.RequestCache)
            {
                if (HttpContext.Current == null) return;
                HttpContext.Current.Items.Add("XCache_DataSet_" + sql, new CacheItem<DataSet>(tableNames, ds));
                return;
            }
            //静态缓存
            if (_TableCache.ContainsKey(sql)) return;
            lock (_TableCache)
            {
                if (_TableCache.ContainsKey(sql)) return;

                _TableCache.Add(sql, new CacheItem<DataSet>(tableNames, ds));
            }
            //带有效期
            if (CacheType == XCacheType.Period) CreateTimer();

            ////检查缓存授权
            //if (License.CacheCount != Count)
            //    License.CacheCount = Count;
        }

        /// <summary>
        /// 添加Int32缓存。
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="n">待缓存整数</param>
        /// <param name="tableNames">表名数组</param>
        public static void Add(String sql, Int32 n, String[] tableNames)
        {
            //关闭缓存
            if (CacheType == XCacheType.Close) return;
            //请求级缓存
            if (CacheType == XCacheType.RequestCache)
            {
                if (HttpContext.Current == null) return;
                HttpContext.Current.Items.Add("XCache_Int32_" + sql, new CacheItem<Int32>(tableNames, n));
                return;
            }
            //静态缓存
            if (_IntCache.ContainsKey(sql)) return;
            lock (_IntCache)
            {
                if (_IntCache.ContainsKey(sql)) return;

                _IntCache.Add(sql, new CacheItem<Int32>(tableNames, n));
            }
            //带有效期
            if (CacheType == XCacheType.Period) CreateTimer();

            ////检查缓存授权
            //if (License.CacheCount != Count)
            //    License.CacheCount = Count;
        }
        #endregion

        #region 删除缓存
        /// <summary>
        /// 移除依赖于某个数据表的缓存
        /// </summary>
        /// <param name="tableName">数据表</param>
        public static void Remove(String tableName)
        {
            //请求级缓存
            if (CacheType == XCacheType.RequestCache)
            {
                if (HttpContext.Current == null) return;
                List<Object> toDel = new List<Object>();
                foreach (Object obj in HttpContext.Current.Items.Keys)
                {
                    String str = obj as String;
                    if (!String.IsNullOrEmpty(str))
                    {
                        if (str.StartsWith("XCache_DataSet_"))
                        {
                            CacheItem<DataSet> ci = HttpContext.Current.Items[obj] as CacheItem<DataSet>;
                            if (ci != null && ci.IsDependOn(tableName)) toDel.Add(obj);
                        }
                        if (str.StartsWith("XCache_Int32_"))
                        {
                            CacheItem<Int32> ci = HttpContext.Current.Items[obj] as CacheItem<Int32>;
                            if (ci != null && ci.IsDependOn(tableName)) toDel.Add(obj);
                        }
                    }
                }
                foreach (Object obj in toDel)
                    HttpContext.Current.Items.Remove(obj);
                return;
            }
            //静态缓存
            lock (_TableCache)
            {
                //TODO 2011-03-11 大石头 这里已经成为性能瓶颈，将来需要优化，瓶颈在于_TableCache[sql]
                List<String> toDel = new List<String>();
                foreach (String sql in _TableCache.Keys)
                    if (_TableCache[sql].IsDependOn(tableName)) toDel.Add(sql);
                foreach (String sql in toDel)
                    _TableCache.Remove(sql);
            }
            lock (_IntCache)
            {
                List<String> toDel = new List<String>();
                foreach (String sql in _IntCache.Keys)
                    if (_IntCache[sql].IsDependOn(tableName)) toDel.Add(sql);
                foreach (String sql in toDel)
                    _IntCache.Remove(sql);
            }
        }

        /// <summary>
        /// 移除依赖于一组数据表的缓存
        /// </summary>
        /// <param name="tableNames"></param>
        public static void Remove(String[] tableNames)
        {
            foreach (String tn in tableNames) Remove(tn);
        }

        /// <summary>
        /// 清空缓存
        /// </summary>
        public static void RemoveAll()
        {
            //请求级缓存
            if (CacheType == XCacheType.RequestCache)
            {
                if (HttpContext.Current == null) return;
                List<Object> toDel = new List<Object>();
                foreach (Object obj in HttpContext.Current.Items.Keys)
                {
                    String str = obj as String;
                    if (!String.IsNullOrEmpty(str))
                    {
                        if (str.StartsWith("XCache_DataSet_") || str.StartsWith("XCache_Int32_")) toDel.Add(obj);
                    }
                }
                foreach (Object obj in toDel)
                    HttpContext.Current.Items.Remove(obj);
                return;
            }
            //静态缓存
            lock (_TableCache)
            {
                _TableCache.Clear();
            }
            lock (_IntCache)
            {
                _IntCache.Clear();
            }
        }
        #endregion

        #region 查找缓存
        /// <summary>
        /// 查找缓存中是否包含某一项
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public static Boolean Contain(String sql)
        {
            //关闭缓存
            if (CacheType == XCacheType.Close) return false;
            //请求级缓存
            if (CacheType == XCacheType.RequestCache)
            {
                if (HttpContext.Current == null) return false;
                return HttpContext.Current.Items.Contains("XCache_DataSet_" + sql);
            }
            return _TableCache.ContainsKey(sql);
        }

        /// <summary>
        /// 获取DataSet缓存
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public static DataSet Item(String sql)
        {
            //关闭缓存
            if (CacheType == XCacheType.Close) return null;
            //请求级缓存
            if (CacheType == XCacheType.RequestCache)
            {
                if (HttpContext.Current == null) return null;
                CacheItem<DataSet> ci = HttpContext.Current.Items["XCache_DataSet_" + sql] as CacheItem<DataSet>;
                if (ci == null) return null;
                return ci.TValue;
            }
            return _TableCache[sql].TValue;
        }

        /// <summary>
        /// 查找Int32缓存中是否包含某一项
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public static Boolean IntContain(String sql)
        {
            //关闭缓存
            if (CacheType == XCacheType.Close) return false;
            //请求级缓存
            if (CacheType == XCacheType.RequestCache)
            {
                if (HttpContext.Current == null) return false;
                return HttpContext.Current.Items.Contains("XCache_Int32_" + sql);
            }
            return _IntCache.ContainsKey(sql);
        }

        /// <summary>
        /// 获取Int32缓存
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public static Int32 IntItem(String sql)
        {
            //关闭缓存
            if (CacheType == XCacheType.Close) return -1;
            //请求级缓存
            if (CacheType == XCacheType.RequestCache)
            {
                if (HttpContext.Current == null) return -1;
                CacheItem<Int32> ci = HttpContext.Current.Items["XCache_Int32_" + sql] as CacheItem<Int32>;
                if (ci == null) return -1;
                return ci.TValue;
            }
            return _IntCache[sql].TValue;
        }
        #endregion

        /// <summary>
        /// 缓存个数
        /// </summary>
        internal static Int32 Count
        {
            get
            {
                //关闭缓存
                if (CacheType == XCacheType.Close) return 0;
                //请求级缓存
                if (CacheType == XCacheType.RequestCache)
                {
                    if (HttpContext.Current == null) return 0;
                    Int32 k = 0;
                    foreach (Object obj in HttpContext.Current.Items.Keys)
                    {
                        String str = obj as String;
                        if (!String.IsNullOrEmpty(str))
                        {
                            if (str.StartsWith("XCache_DataSet_") || str.StartsWith("XCache_Int32_")) k++;
                        }
                    }
                    return k;
                }
                return _TableCache.Count + _IntCache.Count;
            }
        }
    }

    /// <summary>
    /// 数据缓存类型
    /// </summary>
    internal enum XCacheType
    {
        /// <summary>
        /// 关闭缓存
        /// </summary>
        Close = -2,
        /// <summary>
        /// 请求级缓存
        /// </summary>
        RequestCache = -1,
        /// <summary>
        /// 永久静态缓存
        /// </summary>
        Infinite = 0,
        /// <summary>
        /// 带有效期缓存
        /// </summary>
        Period = 1
    }
}