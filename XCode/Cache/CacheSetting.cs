using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Configuration;
using NewLife.Log;
using XCode.DataAccessLayer;

namespace XCode.Cache
{
    /// <summary>全局缓存设置</summary>
    public class CacheSetting
    {
        private static Boolean? _Debug;
        /// <summary>是否调试缓存模块</summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;

                _Debug = Config.GetConfig<Boolean>("XCode.Cache.Debug", false);

                return _Debug.Value;
            }
            set { _Debug = value; }
        }

        private static Int32? _CacheExpiration;
        /// <summary>缓存相对有效期。
        /// -2	关闭缓存
        /// -1	非独占数据库，有外部系统操作数据库，使用请求级缓存；
        ///  0	永久静态缓存；
        /// >0	静态缓存时间，单位是秒；
        /// </summary>
        public static Int32 CacheExpiration
        {
            get
            {
                if (_CacheExpiration.HasValue) return _CacheExpiration.Value;

                var n = Alone ? 60 : -2;
                _CacheExpiration = Config.GetMutilConfig<Int32>(n, "XCode.Cache.Expiration", "XCacheExpiration");

                return _CacheExpiration.Value;
            }
            set { _CacheExpiration = value; }
        }

        private static Int32? _CheckPeriod;
        /// <summary>缓存维护定时器的检查周期，默认5秒</summary>
        public static Int32 CheckPeriod
        {
            get
            {
                if (_CheckPeriod.HasValue) return _CheckPeriod.Value;

                _CheckPeriod = Config.GetMutilConfig<Int32>(5, "XCode.Cache.CheckPeriod", "XCacheCheckPeriod");

                return _CheckPeriod.Value;
            }
            set { _CheckPeriod = value; }
        }

        private static Boolean? _Alone;
        /// <summary>是否独占数据库，独占时将大大加大缓存权重，默认true（Debug时为false）</summary>
        public static Boolean Alone
        {
            get
            {
                if (_Alone.HasValue) return _Alone.Value;

                _Alone = Config.GetConfig<Boolean>("XCode.Cache.Alone", !Debug);
                if (Debug) DAL.WriteLog("使用数据库方式：{0}", _Alone.Value ? "独占，加大缓存权重" : "非独占");

                return _Alone.Value;
            }
            set { _Alone = value; }
        }

        private static Int32? _EntityCacheExpire;
        /// <summary>实体缓存过期时间，独占数据库默认600，非独占默认60</summary>
        public static Int32 EntityCacheExpire
        {
            get
            {
                if (_EntityCacheExpire.HasValue) return _EntityCacheExpire.Value;

                var n = Alone ? 600 : 60;
                _EntityCacheExpire = Config.GetConfig<Int32>("XCode.Cache.EntityCacheExpire", n);

                return _EntityCacheExpire.Value;
            }
            set { _EntityCacheExpire = value; }
        }

        private static Int32? _SingleCacheExpire;
        /// <summary>单对象缓存过期时间，独占数据库默认600，非独占默认60</summary>
        public static Int32 SingleCacheExpire
        {
            get
            {
                if (_SingleCacheExpire.HasValue) return _SingleCacheExpire.Value;

                var n = Alone ? 600 : 60;
                _SingleCacheExpire = Config.GetConfig<Int32>("XCode.Cache.SingleCacheExpire", n);

                return _SingleCacheExpire.Value;
            }
            set { _SingleCacheExpire = value; }
        }
    }
}