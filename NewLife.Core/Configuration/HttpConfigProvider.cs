using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NewLife.Configuration
{
    /// <summary>分布式配置中心文件提供者</summary>
    public class HttpConfigProvider : ConfigProvider
    {
        /// <summary>服务器</summary>
        public String Server { get; set; }

        /// <summary>应用Key</summary>
        public String AppKey { get; set; }

        /// <summary>应用密钥</summary>
        public String Secret { get; set; }

        /// <summary>本地缓存配置数据，即使网络断开，仍然能够加载使用本地数据</summary>
        public Boolean LocalCache { get; set; }
    }
}