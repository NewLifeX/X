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

        /// <summary>
        /// 缓存相对有效期。
        /// -2	关闭缓存
        /// -1	非独占数据库，有外部系统操作数据库，使用请求级缓存；
        ///  0	永久静态缓存；
        /// >0	静态缓存时间，单位是秒；
        /// </summary>
        public static Int32 Expiration = -1;

        /// <summary>数据缓存类型</summary>
        static CacheKinds Kind { get { return Expiration > 0 ? CacheKinds.有效期缓存 : (CacheKinds)Expiration; } }

        /// <summary>
        /// 初始化设置。
        /// 读取配置；
        /// </summary>
        static XCache()
        {
            //读取缓存有效期
            Expiration = Config.GetMutilConfig<Int32>(-2, "XCode.Cache.Expiration", "XCacheExpiration");
            //读取检查周期
            CheckPeriod = Config.GetMutilConfig<Int32>(5, "XCode.Cache.CheckPeriod", "XCacheCheckPeriod");

            if (Expiration < -2) Expiration = -2;
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
        private static Timer AutoCheckCacheTimer;

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
                        List<String> toDel = null;
                        foreach (String sql in _TableCache.Keys)
                            if (_TableCache[sql].ExpireTime < DateTime.Now)
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
                            if (_IntCache[sql].ExpireTime < DateTime.Now)
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
            //关闭缓存、永久静态缓存和请求级缓存时，不需要检查
            if (Kind != CacheKinds.有效期缓存) return;

            if (AutoCheckCacheTimer != null) return;

            // 声明定时器。无限延长时间，实际上不工作
            AutoCheckCacheTimer = new Timer(new TimerCallback(Check), null, Timeout.Infinite, Timeout.Infinite);
            // 改变定时器为5秒后触发一次。
            AutoCheckCacheTimer.Change(CheckPeriod * 1000, CheckPeriod * 1000);
        }
        #endregion

        #region 添加缓存
        /// <summary>添加数据表缓存。</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ds">待缓存记录集</param>
        /// <param name="tableNames">表名数组</param>
        public static void Add(String sql, DataSet ds, String[] tableNames)
        {
            //关闭缓存
            if (Kind == CacheKinds.关闭缓存) return;

            //请求级缓存
            if (Kind == CacheKinds.请求级缓存)
            {
                if (Items == null) return;
                Items.Add(_dst + sql, new CacheItem<DataSet>(tableNames, ds));
                return;
            }

            //静态缓存
            if (_TableCache.ContainsKey(sql)) return;
            lock (_TableCache)
            {
                if (_TableCache.ContainsKey(sql)) return;

                _TableCache.Add(sql, new CacheItem<DataSet>(tableNames, ds, Expiration));
            }

            //带有效期
            if (Kind == CacheKinds.有效期缓存) CreateTimer();
        }

        /// <summary>添加Int32缓存。</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="n">待缓存整数</param>
        /// <param name="tableNames">表名数组</param>
        public static void Add(String sql, Int32 n, String[] tableNames)
        {
            //关闭缓存
            if (Kind == CacheKinds.关闭缓存) return;

            //请求级缓存
            if (Kind == CacheKinds.请求级缓存)
            {
                if (Items == null) return;
                Items.Add(_int + sql, new CacheItem<Int32>(tableNames, n));
                return;
            }

            //静态缓存
            if (_IntCache.ContainsKey(sql)) return;
            lock (_IntCache)
            {
                if (_IntCache.ContainsKey(sql)) return;

                _IntCache.Add(sql, new CacheItem<Int32>(tableNames, n, Expiration));
            }

            //带有效期
            if (Kind == CacheKinds.有效期缓存) CreateTimer();
        }
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

                List<Object> toDel = new List<Object>();
                foreach (Object obj in cs.Keys)
                {
                    String str = obj as String;
                    if (!String.IsNullOrEmpty(str) && (str.StartsWith(_dst) || str.StartsWith(_int)))
                    {
                        CacheItem ci = cs[obj] as CacheItem;
                        if (ci != null && ci.IsDependOn(tableName)) toDel.Add(obj);
                    }
                }
                foreach (Object obj in toDel)
                    cs.Remove(obj);
                return;
            }

            //静态缓存
            lock (_TableCache)
            {
                // 2011-03-11 大石头 这里已经成为性能瓶颈，将来需要优化，瓶颈在于_TableCache[sql]
                // 2011-11-22 大石头 改为遍历集合，而不是键值，避免每次取值的时候都要重新查找
                List<String> toDel = new List<String>();
                foreach (var item in _TableCache)
                    if (item.Value.IsDependOn(tableName)) toDel.Add(item.Key);

                foreach (String sql in toDel)
                    _TableCache.Remove(sql);
            }
            lock (_IntCache)
            {
                List<String> toDel = new List<String>();
                foreach (var item in _IntCache)
                    if (item.Value.IsDependOn(tableName)) toDel.Add(item.Key);

                foreach (String sql in toDel)
                    _IntCache.Remove(sql);
            }
        }

        /// <summary>移除依赖于一组数据表的缓存</summary>
        /// <param name="tableNames"></param>
        public static void Remove(String[] tableNames)
        {
            foreach (String tn in tableNames) Remove(tn);
        }

        /// <summary>清空缓存</summary>
        public static void RemoveAll()
        {
            //请求级缓存
            if (Kind == CacheKinds.请求级缓存)
            {
                var cs = Items;
                if (cs == null) return;

                List<Object> toDel = new List<Object>();
                foreach (Object obj in cs.Keys)
                {
                    String str = obj as String;
                    if (!String.IsNullOrEmpty(str) && (str.StartsWith(_dst) || str.StartsWith(_int))) toDel.Add(obj);
                }
                foreach (Object obj in toDel)
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
        /// <param name="sql">SQL语句</param>
        /// <param name="ds">结果</param>
        /// <returns></returns>
        public static Boolean TryGetItem(String sql, out DataSet ds)
        {
            ds = null;

            //关闭缓存
            if (Kind == CacheKinds.关闭缓存) return false;

            CheckShowStatics(ref NextShow, ref Total, ShowStatics);

            //请求级缓存
            if (Kind == CacheKinds.请求级缓存)
            {
                if (Items == null) return false;

                CacheItem<DataSet> ci = Items[_dst + sql] as CacheItem<DataSet>;
                if (ci == null) return false;

                ds = ci.Value;
            }
            else
            {
                CacheItem<DataSet> ci = null;
                if (!_TableCache.TryGetValue(sql, out ci) || ci == null) return false;
                ds = ci.Value;
            }

            Interlocked.Increment(ref Shoot);

            return true;
        }

        /// <summary>获取Int32缓存</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="count">结果</param>
        /// <returns></returns>
        public static Boolean TryGetItem(String sql, out Int32 count)
        {
            count = -1;

            //关闭缓存
            if (Kind == CacheKinds.关闭缓存) return false;

            CheckShowStatics(ref NextShow, ref Total, ShowStatics);

            //请求级缓存
            if (Kind == CacheKinds.请求级缓存)
            {
                if (Items == null) return false;

                CacheItem<Int32> ci = Items[_int + sql] as CacheItem<Int32>;
                if (ci == null) return false;

                count = ci.Value;
            }
            else
            {
                CacheItem<Int32> ci = null;
                if (!_IntCache.TryGetValue(sql, out ci) || ci == null) return false;
                count = ci.Value;
            }
            count = _IntCache[sql].Value;

            Interlocked.Increment(ref Shoot);

            return true;
        }
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
                    Int32 k = 0;
                    foreach (Object obj in Items.Keys)
                    {
                        String str = obj as String;
                        if (!String.IsNullOrEmpty(str) && (str.StartsWith(_dst) || str.StartsWith(_int))) k++;
                    }
                    return k;
                }
                return _TableCache.Count + _IntCache.Count;
            }
        }

        /// <summary>请求级缓存项</summary>
        static IDictionary Items { get { return HttpContext.Current != null ? HttpContext.Current.Items : null; } }

        //private static Boolean? _Debug;
        ///// <summary>是否调试</summary>
        //public static Boolean Debug
        //{
        //    get
        //    {
        //        if (_Debug == null) _Debug = Config.GetConfig<Boolean>("XCode.Cache.Debug", false);
        //        return _Debug.Value;
        //    }
        //    set { _Debug = value; }
        //}
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
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("一级缓存<{0}>", Kind);
                sb.AppendFormat("总次数{0}", Total);
                if (Shoot > 0) sb.AppendFormat("，命中{0}（{1:P02}）", Shoot, (Double)Shoot / Total);

                XTrace.WriteLine(sb.ToString());
            }
        }
        #endregion

        #region 缓存类型
        /// <summary>数据缓存类型</summary>
        enum CacheKinds
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