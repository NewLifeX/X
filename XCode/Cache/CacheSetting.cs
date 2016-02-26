using System;
using System.ComponentModel;
using NewLife.Configuration;

namespace XCode.Cache
{
    /// <summary>全局缓存设置</summary>
    public class CacheSetting
    {
        #region 属性
        /// <summary>是否调试缓存模块</summary>
        [Description("是否调试缓存模块")]
        public Boolean Debug { get; set; }

        /// <summary>是否独占数据库，独占时将大大加大缓存权重，默认true（Debug时为false）</summary>
        [Description("是否独占数据库，独占时将大大加大缓存权重，默认true（Debug时为false）")]
        public Boolean Alone { get; set; }

        /// <summary>一级缓存相对有效期。
        /// -2	关闭缓存
        /// -1	非独占数据库，有外部系统操作数据库，使用请求级缓存；
        ///  0	永久静态缓存；
        /// >0	静态缓存时间，单位是秒；
        /// </summary>
        [Description("一级缓存有效期。-2 关闭缓存；-1 非独占数据库，请求级缓存；0 永久静态缓存；>0 静态缓存时间，单位秒；默认-2")]
        public Int32 Expiration { get; set; }

        ///// <summary>一级缓存维护定时器的检查周期，默认5秒</summary>
        //[Description("一级缓存维护定时器的检查周期，默认5秒")]
        //public Int32 CheckPeriod { get; set; }

        /// <summary>实体缓存过期时间，默认60秒</summary>
        [Description("实体缓存过期时间，默认60秒")]
        public Int32 EntityCacheExpire { get; set; }

        /// <summary>单对象缓存过期时间，默认60秒</summary>
        [Description("单对象缓存过期时间，默认60秒")]
        public Int32 SingleCacheExpire { get; set; }
        #endregion

        #region 方法
        /// <summary>实例化缓存设置</summary>
        public CacheSetting()
        {
            Alone = true;
            Expiration = -1;
            //CheckPeriod = 5;
            EntityCacheExpire = 60;
            SingleCacheExpire = 60;
        }

        /// <summary>初始化</summary>
        public void Init()
        {
            Debug = Config.GetConfig<Boolean>("XCode.Cache.Debug", false);
            Alone = Config.GetConfig<Boolean>("XCode.Cache.Alone", !Debug);

            Expiration = Config.GetMutilConfig<Int32>(Alone ? 60 : -1, "XCode.Cache.Expiration", "XCacheExpiration");
            //CheckPeriod = Config.GetMutilConfig<Int32>(5, "XCode.Cache.CheckPeriod", "XCacheCheckPeriod");

            EntityCacheExpire = Config.GetConfig<Int32>("XCode.Cache.EntityCacheExpire", 60);
            SingleCacheExpire = Config.GetConfig<Int32>("XCode.Cache.SingleCacheExpire", 60);
        }
        #endregion
    }
}