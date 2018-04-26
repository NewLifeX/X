using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using NewLife.Common;
using NewLife.Xml;

namespace NewLife.Web
{
    /// <summary>配置</summary>
    [XmlConfigFile("Config/OAuth.config", 15000)]
    public class OAuthConfig : XmlConfig<OAuthConfig>
    {
        #region 属性
        /// <summary>调试开关。默认true</summary>
        [Description("调试开关。默认true")]
        public Boolean Debug { get; set; } = true;

        /// <summary>应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址</summary>
        [Description("应用地址。域名和端口，应用系统经过反向代理重定向时指定外部地址")]
        public String AppUrl { get; set; }

        /// <summary>配置项</summary>
        [Description("配置项")]
        public OAuthItem[] Items { get; set; }
        #endregion

        #region 方法
        /// <summary>已加载</summary>
        protected override void OnLoaded()
        {
            var ms = Items;
            if (ms == null || ms.Length == 0)
            {
                var list = new List<OAuthItem>
                {
                    new OAuthItem { Name = "QQ" },
                    new OAuthItem { Name = "Weixin" },
                    new OAuthItem { Name = "Baidu" },
                    new OAuthItem { Name = "Weibo" },
                    new OAuthItem { Name = "Taobao" },
                    new OAuthItem { Name = "Alipay" },
                    new OAuthItem { Name = "Github" }
                };

                var sc = SysConfig.Current;
                var mi = new OAuthItem
                {
                    Name = "NewLife",
                    Server = "https://sso.newlifex.com/sso",
                    AppID = sc.Name,
                    Secret = sc.Name.GetBytes().RC4("NewLife".GetBytes()).ToBase64(),
                };
                list.Add(mi);
                Items = list.ToArray();
            }

            base.OnLoaded();
        }

        /// <summary>获取</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public OAuthItem Get(String name)
        {
            return Items.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
        }

        /// <summary>获取或添加</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public OAuthItem GetOrAdd(String name)
        {
            if (name.IsNullOrEmpty()) return null;

            var mi = Items.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
            if (mi != null) return mi;

            lock (this)
            {
                var list = new List<OAuthItem>(Items);
                mi = list.FirstOrDefault(e => e.Name.EqualIgnoreCase(name));
                if (mi != null) return mi;

                mi = new OAuthItem { Name = name };
                list.Add(mi);

                Items = list.ToArray();

                return mi;
            }
        }
        #endregion
    }

    /// <summary>开放验证服务器配置项</summary>
    public class OAuthItem
    {
        /// <summary>服务地址</summary>
        [XmlAttribute]
        public String Name { get; set; }

        /// <summary>验证服务地址</summary>
        [XmlAttribute]
        public String Server { get; set; }

        /// <summary>令牌服务地址。可以不同于验证地址的内网直达地址</summary>
        [XmlAttribute]
        public String AccessServer { get; set; }

        /// <summary>应用标识</summary>
        [XmlAttribute]
        public String AppID { get; set; }

        /// <summary>密钥</summary>
        [XmlAttribute]
        public String Secret { get; set; }

        /// <summary>授权范围</summary>
        [XmlAttribute]
        public String Scope { get; set; } = "";
    }
}