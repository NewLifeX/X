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
        public Boolean CacheDebug { get; set; }

        /// <summary>是否独占数据库，独占时将大大加大缓存权重，默认true</summary>
        [Description("是否独占数据库，独占时将大大加大缓存权重，默认true")]
        public Boolean Alone { get; set; } = true;

        /// <summary>实体缓存过期时间，默认60秒</summary>
        [Description("实体缓存过期时间，默认60秒")]
        public Int32 EntityCacheExpire { get; set; } = 60;

        /// <summary>单对象缓存过期时间，默认60秒</summary>
        [Description("单对象缓存过期时间，默认60秒")]
        public Int32 SingleCacheExpire { get; set; } = 60;
        #endregion

        #region 方法
        /// <summary>实例化缓存设置</summary>
        public CacheSetting()
        {
            Alone = true;
            EntityCacheExpire = 60;
            SingleCacheExpire = 60;
        }
        #endregion
    }
}