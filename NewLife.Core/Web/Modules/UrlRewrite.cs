using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Serialization;
using NewLife.Xml;
using System.ComponentModel;

namespace NewLife.Web
{
    /// <summary>Url重写模块</summary>
    public class UrlRewrite : IHttpModule
    {
        #region IHttpModule Members
        void IHttpModule.Dispose() { }

        /// <summary>初始化模块，准备拦截请求。</summary>
        /// <param name="context"></param>
        void IHttpModule.Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;
        }
        #endregion

        #region 业务处理
        /// <summary>处理方法</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnBeginRequest(object sender, EventArgs e)
        {
            var app = sender as HttpApplication;
            var path = app.Request.AppRelativeCurrentExecutionFilePath.TrimStart('~');

            var cfg = UrlConfig.Current;
            CheckConfig(cfg);
            foreach (var item in cfg.Urls)
            {
                var url = item.GetRewriteUrl(path);
                if (url != null)
                {
                    if (RewritePath(url)) return;
                }
            }
        }

        /// <summary>检查配置。如果配置项为空，则增加示例配置</summary>
        /// <param name="config"></param>
        protected virtual void CheckConfig(UrlConfig config)
        {
            if (config.Urls == null) config.Urls = new List<UrlConfig.Item>();
            if (config.Urls.Count < 1)
            {
                config.Urls.Add(new UrlConfig.Item { Url = "Test_(\\d+)", Target = "Test.aspx?ID=$1" });
                config.Save();
            }
        }

        /// <summary>重定向到目标地址</summary>
        /// <param name="url"></param>
        /// <returns>是否成功重定向</returns>
        protected virtual Boolean RewritePath(String url)
        {
            var query = "";
            var p = url.IndexOf('?');
            if (p >= 0)
            {
                query = url.Substring(p + 1);
                url = url.Substring(0, p);
            }
            HttpContext.Current.RewritePath(url, null, query);

            return true;
        }
        #endregion
    }

    /// <summary>Url重写地址配置</summary>
    [Description("Url重写地址配置")]
    [XmlConfigFile("Config/UrlRewrite.config", 15000)]
    public class UrlConfig : XmlConfig<UrlConfig>
    {
        private List<Item> _Urls;
        /// <summary>匹配地址集合</summary>
        [Description("Url为匹配请求地址的正则表达式，Target为要重写的目标地址，可以使用$1等匹配项")]
        public List<Item> Urls { get { return _Urls; } set { _Urls = value; } }

        /// <summary>Url重写地址配置项</summary>
        public class Item
        {
            private String _Url;
            /// <summary>Url正则表达式</summary>
            [XmlAttribute]
            public String Url { get { return _Url; } set { _Url = value; } }

            private String _Target;
            /// <summary>目标地址</summary>
            [XmlAttribute]
            public String Target { get { return _Target; } set { _Target = value; } }

            private Regex _reg;
            /// <summary>获取指定输入的重写Url</summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public String GetRewriteUrl(String input)
            {
                if (String.IsNullOrEmpty(input)) return input;
                if (String.IsNullOrEmpty(Url)) return null;
                if (String.IsNullOrEmpty(Target)) return null;

                if (_reg == null) _reg = new Regex(Url, RegexOptions.IgnoreCase);

                var m = _reg.Match(input);
                if (m == null || !m.Success) return null;

                return m.Result(Target);
            }
        }
    }
}