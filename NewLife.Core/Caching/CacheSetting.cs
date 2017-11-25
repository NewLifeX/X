using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using NewLife.Xml;

namespace NewLife.Caching
{
    /// <summary>缓存配置</summary>
    [Description("缓存配置")]
    [XmlConfigFile("Config/Cache.config", 15000)]
    public class CacheConfig : XmlConfig<CacheConfig>
    {
        #region 属性
        /// <summary>调试开关。默认true</summary>
        [Description("调试开关。默认true")]
        public Boolean Debug { get; set; } = true;

        /// <summary>配置项</summary>
        [Description("配置项。名称、地址、提供者，Memory/Redis")]
        public CacheSetting[] Items { get; set; } = new CacheSetting[0];
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public CacheConfig()
        {

        }
        #endregion

        #region 方法
        /// <summary>加载后</summary>
        protected override void OnLoaded()
        {
            // 排除重复
            var list = Items;
            if (list != null && list.Length > 0)
            {
                var dic = new Dictionary<String, CacheSetting>();
                foreach (var item in list)
                {
                    if (item != null && !item.Name.IsNullOrEmpty()) dic[item.Name] = item;
                }
                Items = dic.Select(e => e.Value).ToArray();
            }

            base.OnLoaded();
        }

        /// <summary>获取 或 增加 配置项</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public CacheSetting GetOrAdd(String name)
        {
            var ms = Items ?? new CacheSetting[0];
            var item = ms.FirstOrDefault(e => e?.Name == name);
            if (item != null) return item;

            // 如果找不到，则增加
            item = new CacheSetting { Name = name };
            //Items.Add(item);
            lock (ms)
            {
                var list = new List<CacheSetting>(ms)
                {
                    item
                };

                Items = list.ToArray();
            }

            return item;
        }
        #endregion
    }

    /// <summary>配置项</summary>
    public class CacheSetting
    {
        /// <summary>名称</summary>
        [XmlAttribute]
        public String Name { get; set; }

        /// <summary>地址</summary>
        [XmlAttribute]
        public String Value { get; set; }

        /// <summary>提供者</summary>
        [XmlAttribute]
        public String Provider { get; set; }
    }
}