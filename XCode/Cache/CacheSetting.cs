using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using NewLife.Configuration;
using NewLife.Log;
using XCode.DataAccessLayer;

namespace XCode.Cache
{
    /// <summary>全局缓存设置</summary>
    public class CacheSetting
    {
        #region 属性
        private Boolean _Debug;
        /// <summary>是否调试缓存模块</summary>
        [Description("是否调试缓存模块")]
        public Boolean Debug { get { return _Debug; } set { _Debug = value; } }

        private Boolean _Alone = true;
        /// <summary>是否独占数据库，独占时将大大加大缓存权重，默认true（Debug时为false）</summary>
        [Description("是否独占数据库，独占时将大大加大缓存权重，默认true（Debug时为false）")]
        public Boolean Alone { get { return _Alone; } set { _Alone = value; } }

        private Int32 _Expiration = -2;
        /// <summary>缓存相对有效期。
        /// -2	关闭缓存
        /// -1	非独占数据库，有外部系统操作数据库，使用请求级缓存；
        ///  0	永久静态缓存；
        /// >0	静态缓存时间，单位是秒；
        /// </summary>
        [Description("缓存有效期。-2	关闭缓存；-1	非独占数据库，请求级缓存；0	永久静态缓存；>0	静态缓存时间，单位秒；默认-2")]
        public Int32 Expiration { get { return _Expiration; } set { _Expiration = value; } }

        private Int32 _CheckPeriod = 5;
        /// <summary>缓存维护定时器的检查周期，默认5秒</summary>
        [Description("缓存维护定时器的检查周期，默认5秒")]
        public Int32 CheckPeriod { get { return _CheckPeriod; } set { _CheckPeriod = value; } }

        private Int32 _EntityCacheExpire = 60;
        /// <summary>实体缓存过期时间，默认60秒</summary>
        [Description("实体缓存过期时间，默认60秒")]
        public Int32 EntityCacheExpire { get { return _EntityCacheExpire; } set { _EntityCacheExpire = value; } }

        private Int32 _SingleCacheExpire = 60;
        /// <summary>单对象缓存过期时间，默认60秒</summary>
        [Description("单对象缓存过期时间，默认60秒")]
        public Int32 SingleCacheExpire { get { return _SingleCacheExpire; } set { _SingleCacheExpire = value; } }
        #endregion

        #region 方法
        public void Init()
        {
            Debug = Config.GetConfig<Boolean>("XCode.Cache.Debug", false);
            Alone = Config.GetConfig<Boolean>("XCode.Cache.Alone", !Debug);

            Expiration = Config.GetMutilConfig<Int32>(Alone ? 60 : -2, "XCode.Cache.Expiration", "XCacheExpiration");
            CheckPeriod = Config.GetMutilConfig<Int32>(5, "XCode.Cache.CheckPeriod", "XCacheCheckPeriod");

            EntityCacheExpire = Config.GetConfig<Int32>("XCode.Cache.EntityCacheExpire", 60);
            SingleCacheExpire = Config.GetConfig<Int32>("XCode.Cache.SingleCacheExpire", 60);
        }
        #endregion
    }
}