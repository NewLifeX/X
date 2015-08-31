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
    /// <summary>鏁版嵁缂撳瓨绫?/summary>
    /// <remarks>
    /// 浠QL涓洪敭瀵规煡璇㈣繘琛岀紦瀛橈紝鍚屾椂鍏宠仈琛ㄣ€傛墽琛孲QL鏃讹紝鏍规嵁鍏宠仈琛ㄥ垹闄ょ紦瀛樸€?
    /// </remarks>
    public static class XCache
    {
        #region 鍒濆鍖?
        private static Dictionary<String, CacheItem<DataSet>> _TableCache = new Dictionary<String, CacheItem<DataSet>>();
        private static Dictionary<String, CacheItem<Int32>> _IntCache = new Dictionary<String, CacheItem<Int32>>();

        static readonly String _dst = "XCache_DataSet_";
        static readonly String _int = "XCache_Int32_";

        /// <summary>缂撳瓨鐩稿鏈夋晥鏈熴€?
        /// -2	鍏抽棴缂撳瓨
        /// -1	闈炵嫭鍗犳暟鎹簱锛屾湁澶栭儴绯荤粺鎿嶄綔鏁版嵁搴擄紝浣跨敤璇锋眰绾х紦瀛橈紱
        ///  0	姘镐箙闈欐€佺紦瀛橈紱
        /// >0	闈欐€佺紦瀛樻椂闂达紝鍗曚綅鏄锛?
        /// </summary>
        //public static Int32 Expiration = -1;
        static Int32 Expiration { get { return Setting.Current.Cache.Expiration; } }

        /// <summary>鏁版嵁缂撳瓨绫诲瀷</summary>
        internal static CacheKinds Kind { get { return Expiration > 0 ? CacheKinds.鏈夋晥鏈熺紦瀛?: (CacheKinds)Expiration; } }

        /// <summary>鍒濆鍖栬缃€傝鍙栭厤缃?/summary>
        static XCache()
        {
            //璇诲彇缂撳瓨鏈夋晥鏈?
            //Expiration = Config.GetMutilConfig<Int32>(-2, "XCode.Cache.Expiration", "XCacheExpiration");
            //璇诲彇妫€鏌ュ懆鏈?
            //CheckPeriod = Config.GetMutilConfig<Int32>(5, "XCode.Cache.CheckPeriod", "XCacheCheckPeriod");
            CheckPeriod = Setting.Current.Cache.CheckPeriod;

            //if (Expiration < -2) Expiration = -2;
            if (CheckPeriod <= 0) CheckPeriod = 5;

            if (DAL.Debug)
            {
                // 闇€瑕佸鐞嗕竴涓嬶紝鑰屼笉鏄洿鎺ョ敤Kind杞崲鑰屾潵鐨勫瓧绗︿覆锛屽惁鍒欏彲鑳藉洜涓烘灇涓捐娣锋穯鍚庤€屾棤娉曟樉绀烘纭殑鍚嶅瓧
                String name = null;
                switch (Kind)
                {
                    case CacheKinds.鍏抽棴缂撳瓨:
                        name = "鍏抽棴缂撳瓨";
                        break;
                    case CacheKinds.璇锋眰绾х紦瀛?
                        name = "璇锋眰绾х紦瀛?;
                        break;
                    case CacheKinds.姘镐箙闈欐€佺紦瀛?
                        name = "姘镐箙闈欐€佺紦瀛?;
                        break;
                    case CacheKinds.鏈夋晥鏈熺紦瀛?
                        name = "鏈夋晥鏈熺紦瀛?;
                        break;
                    default:
                        break;
                }
                if (Kind < CacheKinds.鏈夋晥鏈熺紦瀛?
                    DAL.WriteLog("涓€绾х紦瀛橈細{0}", name);
                else
                    DAL.WriteLog("涓€绾х紦瀛橈細{0}绉抺1}", Expiration, name);
            }
        }
        #endregion

        #region 缂撳瓨缁存姢
        /// <summary>缂撳瓨缁存姢瀹氭椂鍣?/summary>
        private static TimerX AutoCheckCacheTimer;

        /// <summary>缁存姢瀹氭椂鍣ㄧ殑妫€鏌ュ懆鏈燂紝榛樿5绉?/summary>
        public static Int32 CheckPeriod = 5;

        /// <summary>缁存姢</summary>
        /// <param name="obj"></param>
        private static void Check(Object obj)
        {
            //鍏抽棴缂撳瓨銆佹案涔呴潤鎬佺紦瀛樺拰璇锋眰绾х紦瀛樻椂锛屼笉闇€瑕佹鏌?
            if (Kind != CacheKinds.鏈夋晥鏈熺紦瀛? return;

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
        /// 鍒涘缓瀹氭椂鍣ㄣ€?
        /// 鍥犱负瀹氭椂鍣ㄧ殑鍘熷洜锛屽疄闄呯紦瀛樻椂闂村彲鑳借姣擡xpiration瑕佸ぇ
        /// </summary>
        private static void CreateTimer()
        {
            //鍏抽棴缂撳瓨銆佹案涔呴潤鎬佺紦瀛樺拰璇锋眰绾х紦瀛樻椂锛屼笉闇€瑕佹鏌?
            if (Kind != CacheKinds.鏈夋晥鏈熺紦瀛? return;

            if (AutoCheckCacheTimer != null) return;

            AutoCheckCacheTimer = new TimerX(Check, null, CheckPeriod * 1000, CheckPeriod * 1000);
            //// 澹版槑瀹氭椂鍣ㄣ€傛棤闄愬欢闀挎椂闂达紝瀹為檯涓婁笉宸ヤ綔
            //AutoCheckCacheTimer = new Timer(new TimerCallback(Check), null, Timeout.Infinite, Timeout.Infinite);
            //// 鏀瑰彉瀹氭椂鍣ㄤ负5绉掑悗瑙﹀彂涓€娆°€?
            //AutoCheckCacheTimer.Change(CheckPeriod * 1000, CheckPeriod * 1000);
        }
        #endregion

        #region 娣诲姞缂撳瓨
        /// <summary>娣诲姞鏁版嵁琛ㄧ紦瀛樸€?/summary>
        /// <param name="cache">缂撳瓨瀵硅薄</param>
        /// <param name="prefix">鍓嶇紑</param>
        /// <param name="sql">SQL璇彞</param>
        /// <param name="value">寰呯紦瀛樿褰曢泦</param>
        /// <param name="tableNames">琛ㄥ悕鏁扮粍</param>
        static void Add<T>(Dictionary<String, CacheItem<T>> cache, String prefix, String sql, T value, String[] tableNames)
        {
            //鍏抽棴缂撳瓨
            if (Kind == CacheKinds.鍏抽棴缂撳瓨) return;

            //璇锋眰绾х紦瀛?
            if (Kind == CacheKinds.璇锋眰绾х紦瀛?
            {
                if (Items == null) return;

                Items.Add(prefix + sql, new CacheItem<T>(tableNames, value));
                return;
            }

            //闈欐€佺紦瀛?
            if (cache.ContainsKey(sql)) return;
            lock (cache)
            {
                if (cache.ContainsKey(sql)) return;

                cache.Add(sql, new CacheItem<T>(tableNames, value, Expiration));
            }

            //甯︽湁鏁堟湡
            if (Kind == CacheKinds.鏈夋晥鏈熺紦瀛? CreateTimer();
        }

        /// <summary>娣诲姞鏁版嵁琛ㄧ紦瀛樸€?/summary>
        /// <param name="sql">SQL璇彞</param>
        /// <param name="value">寰呯紦瀛樿褰曢泦</param>
        /// <param name="tableNames">琛ㄥ悕鏁扮粍</param>
        public static void Add(String sql, DataSet value, String[] tableNames) { Add(_TableCache, _dst, sql, value, tableNames); }

        /// <summary>娣诲姞Int32缂撳瓨銆?/summary>
        /// <param name="sql">SQL璇彞</param>
        /// <param name="value">寰呯紦瀛樻暣鏁?/param>
        /// <param name="tableNames">琛ㄥ悕鏁扮粍</param>
        public static void Add(String sql, Int32 value, String[] tableNames) { Add(_IntCache, _int, sql, value, tableNames); }
        #endregion

        #region 鍒犻櫎缂撳瓨
        /// <summary>绉婚櫎渚濊禆浜庢煇涓暟鎹〃鐨勭紦瀛?/summary>
        /// <param name="tableName">鏁版嵁琛?/param>
        public static void Remove(String tableName)
        {
            //璇锋眰绾х紦瀛?
            if (Kind == CacheKinds.璇锋眰绾х紦瀛?
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

            //闈欐€佺紦瀛?
            lock (_TableCache)
            {
                // 2011-03-11 澶х煶澶?杩欓噷宸茬粡鎴愪负鎬ц兘鐡堕锛屽皢鏉ラ渶瑕佷紭鍖栵紝鐡堕鍦ㄤ簬_TableCache[sql]
                // 2011-11-22 澶х煶澶?鏀逛负閬嶅巻闆嗗悎锛岃€屼笉鏄敭鍊硷紝閬垮厤姣忔鍙栧€肩殑鏃跺€欓兘瑕侀噸鏂版煡鎵?
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

        /// <summary>绉婚櫎渚濊禆浜庝竴缁勬暟鎹〃鐨勭紦瀛?/summary>
        /// <param name="tableNames"></param>
        public static void Remove(String[] tableNames)
        {
            foreach (var tn in tableNames)
                Remove(tn);
        }

        /// <summary>娓呯┖缂撳瓨</summary>
        public static void RemoveAll()
        {
            //璇锋眰绾х紦瀛?
            if (Kind == CacheKinds.璇锋眰绾х紦瀛?
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
            //闈欐€佺紦瀛?
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

        #region 鏌ユ壘缂撳瓨
        /// <summary>鑾峰彇DataSet缂撳瓨</summary>
        /// <param name="cache">缂撳瓨瀵硅薄</param>
        /// <param name="sql">SQL璇彞</param>
        /// <param name="value">缁撴灉</param>
        /// <returns></returns>
        static Boolean TryGetItem<T>(Dictionary<String, CacheItem<T>> cache, String sql, out T value)
        {
            value = default(T);

            //鍏抽棴缂撳瓨
            if (Kind == CacheKinds.鍏抽棴缂撳瓨) return false;

            CheckShowStatics(ref NextShow, ref Total, ShowStatics);

            //璇锋眰绾х紦瀛?
            if (Kind == CacheKinds.璇锋眰绾х紦瀛?
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

        /// <summary>鑾峰彇DataSet缂撳瓨</summary>
        /// <param name="sql">SQL璇彞</param>
        /// <param name="ds">缁撴灉</param>
        /// <returns></returns>
        public static Boolean TryGetItem(String sql, out DataSet ds) { return TryGetItem(_TableCache, sql, out ds); }

        /// <summary>鑾峰彇Int32缂撳瓨</summary>
        /// <param name="sql">SQL璇彞</param>
        /// <param name="count">缁撴灉</param>
        /// <returns></returns>
        public static Boolean TryGetItem(String sql, out Int32 count) { return TryGetItem(_IntCache, sql, out count); }
        #endregion

        #region 灞炴€?
        /// <summary>缂撳瓨涓暟</summary>
        internal static Int32 Count
        {
            get
            {
                //鍏抽棴缂撳瓨
                if (Kind == CacheKinds.鍏抽棴缂撳瓨) return 0;
                //璇锋眰绾х紦瀛?
                if (Kind == CacheKinds.璇锋眰绾х紦瀛?
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

        /// <summary>璇锋眰绾х紦瀛橀」</summary>
        static IDictionary Items { get { return HttpContext.Current != null ? HttpContext.Current.Items : null; } }
        #endregion

        #region 缁熻
        /// <summary>鎬绘鏁?/summary>
        public static Int32 Total;

        /// <summary>鍛戒腑</summary>
        public static Int32 Shoot;

        /// <summary>涓嬩竴娆℃樉绀烘椂闂?/summary>
        public static DateTime NextShow;

        /// <summary>妫€鏌ュ苟鏄剧ず缁熻淇℃伅</summary>
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

        /// <summary>鏄剧ず缁熻淇℃伅</summary>
        public static void ShowStatics()
        {
            if (Total > 0)
            {
                var sb = new StringBuilder();
                // 鎺掔増闇€瑕侊紝涓€涓腑鏂囧崰涓や釜瀛楃浣嶇疆
                var str = Kind.ToString();
                sb.AppendFormat("涓€绾х紦瀛?{0,-" + (20 - str.Length) + "}>", str);
                sb.AppendFormat("鎬绘鏁皗0,7:n0}", Total);
                if (Shoot > 0) sb.AppendFormat("锛屽懡涓瓄0,7:n0}锛坽1,6:P02}锛?, Shoot, (Double)Shoot / Total);

                XTrace.WriteLine(sb.ToString());
            }
        }
        #endregion

        #region 缂撳瓨绫诲瀷
        /// <summary>鏁版嵁缂撳瓨绫诲瀷</summary>
        internal enum CacheKinds
        {
            /// <summary>鍏抽棴缂撳瓨</summary>
            鍏抽棴缂撳瓨 = -2,

            /// <summary>璇锋眰绾х紦瀛?/summary>
            璇锋眰绾х紦瀛?= -1,

            /// <summary>姘镐箙闈欐€佺紦瀛?/summary>
            姘镐箙闈欐€佺紦瀛?= 0,

            /// <summary>甯︽湁鏁堟湡缂撳瓨</summary>
            鏈夋晥鏈熺紦瀛?= 1
        }
        #endregion
    }
}