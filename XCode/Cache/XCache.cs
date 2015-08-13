using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Web;
using NewLife.Configuration;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Threading;
using XCode.DataAccessLayer;

namespace XCode.Cache
{
    /// <summary>数据缓存类</summary>
    /// <remarks>
    /// 以SQL为键对查询进行缓存，同时关联表。执行SQL时，根据关联表删除缓存。
    /// </remarks>
    static class XCache
    {
        #region 初始化
        private static Dictionary<String, CacheItem<DataSet>> _TableCache = new Dictionary<String, CacheItem<DataSet>>();
        private static Dictionary<String, CacheItem<Int32>> _IntCache = new Dictionary<String, CacheItem<Int32>>();

        static readonly String _dst = "XCache_DataSet_";
        static readonly String _int = "XCache_Int32_";

        /// <summary>缓存相对有效期。
        /// -2	关闭缓存
        /// -1	非独占数据库，有外部系统操作数据库，使用请求级缓存；
        ///  0	永久静态缓存；
        /// >0	静态缓存时间，单位是秒；
        /// </summary>
        //public static Int32 Expiration = -1;
        static Int32 Expiration { get { return CacheSetting.CacheExpiration; } }

        /// <summary>数据缓存类型</summary>
        internal static CacheKinds Kind { get { return Expiration > 0 ? CacheKinds.有效期缓存 : (CacheKinds)Expiration; } }

        /// <summary>初始化设置。读取配置</summary>
        static XCache()
        {
            //读取缓存有效期
            //Expiration = Config.GetMutilConfig<Int32>(-2, "XCode.Cache.Expiration", "XCacheExpiration");
            //读取检查周期
            //CheckPeriod = Config.GetMutilConfig<Int32>(5, "XCode.Cache.CheckPeriod", "XCacheCheckPeriod");
            CheckPeriod = CacheSetting.CheckPeriod;

            //if (Expiration < -2) Expiration = -2;
            if (CheckPeriod <= 0) CheckPeriod = 5;

            if (DAL.Debug)
            {
                // 需要处理一下，而不是直接用Kind转换而来的字符串，否则可能因为枚举被混淆后而无法显示正确的名字
                String name = null;
                switch (Kind)
                {
                    case CacheKinds.关闭缓存:
                        name = "关闭缓存";
                        break;
                    case CacheKinds.请求级缓存:
                        name = "请求级缓存";
                        break;
                    case CacheKinds.永久静态缓存:
                        name = "永久静态缓存";
                        break;
                    case CacheKinds.有效期缓存:
                        name = "有效期缓存";
                        break;
                    default:
                        break;
                }
                if (Kind < CacheKinds.有效期缓存)
                    DAL.WriteLog("一级缓存：{0}", name);
                else
                    DAL.WriteLog("一级缓存：{0}秒{1}", Expiration, name);
            }
        }
        #endregion

        #region 缓存维护
        /// <summary>缓存维护定时器</summary>
        private static TimerX AutoCheckCacheTimer;

        /// <summary>维护定时器的检查周期，默认5秒</summary>
        public static Int32 CheckPeriod = 5;

        /// <summary>维护</summary>
        /// <param name="obj"></param>
        private static void Check(Object obj)
        {
            //关闭缓存、永久静态缓存和请求级缓存时，不需要检查
            if (Kind != CacheKinds.有效期缓存) return;

            if (_TableCache.Count > 0)
            {
                lock (_TableCache)
                {
                    if (_TableCache.Count > 0)
                    {
                        var list = new List<String>();
                        foreach (var sql in _TableCache.Keys)
                        {
                            if (_TableCache[sql].ExpireTime < DateTime.Now)
                            {
                                list.Add(sql);
                            }
                        }
                        if (list != null && list.Count > 0)
                        {
                            foreach (var sql in list)
                                _TableCache.Remove(sql);
                        }
                    }
                }
            }
            if (_IntCache.Count > 0)
            {
                lock (_IntCache)
                {
                    if (_IntCache.Count > 0)
                    {
                        var list = new List<String>();
                        foreach (var sql in _IntCache.Keys)
                        {
                            if (_IntCache[sql].ExpireTime < DateTime.Now)
                            {
                                list.Add(sql);
                            }
                        }
                        if (list != null && list.Count > 0)
                        {
                            foreach (var sql in list)
                                _IntCache.Remove(sql);
                        }
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
            //关闭缓存、永久静态缓存和请求级缓存时，不需要检查
            if (Kind != CacheKinds.有效期缓存) return;

            if (AutoCheckCacheTimer != null) return;

            AutoCheckCacheTimer = new TimerX(Check, null, CheckPeriod * 1000, CheckPeriod * 1000);
            //// 声明定时器。无限延长时间，实际上不工作
            //AutoCheckCacheTimer = new Timer(new TimerCallback(Check), null, Timeout.Infinite, Timeout.Infinite);
            //// 改变定时器为5秒后触发一次。
            //AutoCheckCacheTimer.Change(CheckPeriod * 1000, CheckPeriod * 1000);
        }
        #endregion

        #region 添加缓存
        /// <summary>添加数据表缓存。</summary>
        /// <param name="cache">缓存对象</param>
        /// <param name="prefix">前缀</param>
        /// <param name="sql">SQL语句</param>
        /// <param name="value">待缓存记录集</param>
        /// <param name="tableNames">表名数组</param>
        static void Add<T>(Dictionary<String, CacheItem<T>> cache, String prefix, String sql, T value, String[] tableNames)
        {
            //关闭缓存
            if (Kind == CacheKinds.关闭缓存) return;

            //请求级缓存
            if (Kind == CacheKinds.请求级缓存)
            {
                if (Items == null) return;

                Items.Add(prefix + sql, new CacheItem<T>(tableNames, value));
                return;
            }

            //静态缓存
            if (cache.ContainsKey(sql)) return;
            lock (cache)
            {
                if (cache.ContainsKey(sql)) return;

                cache.Add(sql, new CacheItem<T>(tableNames, value, Expiration));
            }

            //带有效期
            if (Kind == CacheKinds.有效期缓存) CreateTimer();
        }

        /// <summary>添加数据表缓存。</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="value">待缓存记录集</param>
        /// <param name="tableNames">表名数组</param>
        public static void Add(String sql, DataSet value, String[] tableNames) { Add(_TableCache, _dst, sql, value, tableNames); }

        /// <summary>添加Int32缓存。</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="value">待缓存整数</param>
        /// <param name="tableNames">表名数组</param>
        public static void Add(String sql, Int32 value, String[] tableNames) { Add(_IntCache, _int, sql, value, tableNames); }
        #endregion

        #region 删除缓存
        /// <summary>移除依赖于某个数据表的缓存</summary>
        /// <param name="tableName">数据表</param>
        public static void Remove(String tableName)
        {
            //请求级缓存
            if (Kind == CacheKinds.请求级缓存)
            {
                var cs = Items;
                if (cs == null) return;

                var toDel = new List<Object>();
                foreach (var obj in cs.Keys)
                {
                    var str = obj as String;
                    if (!String.IsNullOrEmpty(str) && (str.StartsWith(_dst) || str.StartsWith(_int)))
                    {
                        var ci = cs[obj] as CacheItem;
                        if (ci != null && ci.IsDependOn(tableName)) toDel.Add(obj);
                    }
                }
                foreach (var obj in toDel)
                    cs.Remove(obj);
                return;
            }

            //静态缓存
            lock (_TableCache)
            {
                // 2011-03-11 大石头 这里已经成为性能瓶颈，将来需要优化，瓶颈在于_TableCache[sql]
                // 2011-11-22 大石头 改为遍历集合，而不是键值，避免每次取值的时候都要重新查找
                var list = new List<String>();
                foreach (var item in _TableCache)
                    if (item.Value.IsDependOn(tableName)) list.Add(item.Key);

                foreach (var sql in list)
                    _TableCache.Remove(sql);
            }
            lock (_IntCache)
            {
                var list = new List<String>();
                foreach (var item in _IntCache)
                    if (item.Value.IsDependOn(tableName)) list.Add(item.Key);

                foreach (var sql in list)
                    _IntCache.Remove(sql);
            }
        }

        /// <summary>移除依赖于一组数据表的缓存</summary>
        /// <param name="tableNames"></param>
        public static void Remove(String[] tableNames)
        {
            foreach (var tn in tableNames)
                Remove(tn);
        }

        /// <summary>清空缓存</summary>
        public static void RemoveAll()
        {
            //请求级缓存
            if (Kind == CacheKinds.请求级缓存)
            {
                var cs = Items;
                if (cs == null) return;

                var toDel = new List<Object>();
                foreach (var obj in cs.Keys)
                {
                    var str = obj as String;
                    if (!String.IsNullOrEmpty(str) && (str.StartsWith(_dst) || str.StartsWith(_int))) toDel.Add(obj);
                }
                foreach (var obj in toDel)
                    cs.Remove(obj);
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
        /// <summary>获取DataSet缓存</summary>
        /// <param name="cache">缓存对象</param>
        /// <param name="sql">SQL语句</param>
        /// <param name="value">结果</param>
        /// <returns></returns>
        static Boolean TryGetItem<T>(Dictionary<String, CacheItem<T>> cache, String sql, out T value)
        {
            value = default(T);

            //关闭缓存
            if (Kind == CacheKinds.关闭缓存) return false;

            CheckShowStatics(ref NextShow, ref Total, ShowStatics);

            //请求级缓存
            if (Kind == CacheKinds.请求级缓存)
            {
                if (Items == null) return false;

                var prefix = String.Format("XCache_{0}_", typeof(T).Name);
                var ci = Items[prefix + sql] as CacheItem<T>;
                if (ci == null) return false;

                value = ci.Value;
            }
            else
            {
                CacheItem<T> ci = null;
                if (!cache.TryGetValue(sql, out ci) || ci == null) return false;
                value = ci.Value;
            }

            Interlocked.Increment(ref Shoot);

            return true;
        }

        /// <summary>获取DataSet缓存</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ds">结果</param>
        /// <returns></returns>
        public static Boolean TryGetItem(String sql, out DataSet ds) { return TryGetItem(_TableCache, sql, out ds); }

        /// <summary>获取Int32缓存</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="count">结果</param>
        /// <returns></returns>
        public static Boolean TryGetItem(String sql, out Int32 count) { return TryGetItem(_IntCache, sql, out count); }
        #endregion

        #region 属性
        /// <summary>缓存个数</summary>
        internal static Int32 Count
        {
            get
            {
                //关闭缓存
                if (Kind == CacheKinds.关闭缓存) return 0;
                //请求级缓存
                if (Kind == CacheKinds.请求级缓存)
                {
                    if (Items == null) return 0;
                    var k = 0;
                    foreach (var obj in Items.Keys)
                    {
                        var str = obj as String;
                        if (!String.IsNullOrEmpty(str) && (str.StartsWith(_dst) || str.StartsWith(_int))) k++;
                    }
                    return k;
                }
                return _TableCache.Count + _IntCache.Count;
            }
        }

        /// <summary>请求级缓存项</summary>
        static IDictionary Items { get { return HttpContext.Current != null ? HttpContext.Current.Items : null; } }
        #endregion

        #region 统计
        /// <summary>总次数</summary>
        public static Int32 Total;

        /// <summary>命中</summary>
        public static Int32 Shoot;

        /// <summary>下一次显示时间</summary>
        public static DateTime NextShow;

        /// <summary>检查并显示统计信息</summary>
        /// <param name="next"></param>
        /// <param name="total"></param>
        /// <param name="show"></param>
        public static void CheckShowStatics(ref DateTime next, ref Int32 total, Func show)
        {
            if (next < DateTime.Now)
            {
                var isfirst = next == DateTime.MinValue;
                next = DAL.Debug ? DateTime.Now.AddMinutes(10) : DateTime.Now.AddHours(24);

                if (!isfirst) show();
            }

            Interlocked.Increment(ref total);
        }

        /// <summary>显示统计信息</summary>
        public static void ShowStatics()
        {
            if (Total > 0)
            {
                var sb = new StringBuilder();
                // 排版需要，一个中文占两个字符位置
                var str = Kind.ToString();
                sb.AppendFormat("一级缓存<{0,-" + (20 - str.Length) + "}>", str);
                sb.AppendFormat("总次数{0,7:n0}", Total);
                if (Shoot > 0) sb.AppendFormat("，命中{0,7:n0}（{1,6:P02}）", Shoot, (Double)Shoot / Total);

                XTrace.WriteLine(sb.ToString());
            }
        }
        #endregion

        #region 缓存类型
        /// <summary>数据缓存类型</summary>
        internal enum CacheKinds
        {
            /// <summary>关闭缓存</summary>
            关闭缓存 = -2,

            /// <summary>请求级缓存</summary>
            请求级缓存 = -1,

            /// <summary>永久静态缓存</summary>
            永久静态缓存 = 0,

            /// <summary>带有效期缓存</summary>
            有效期缓存 = 1
        }
        #endregion
    }
}