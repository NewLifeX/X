using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml.Serialization;
using NewLife.Log;
using NewLife.Model;
using NewLife.Xml;

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
            var context = app.Context;

            String path = app.Request.AppRelativeCurrentExecutionFilePath.Substring(1);
            String query = app.Request.QueryString.ToString();

            var cfg = UrlConfig.Current;
            foreach (var item in cfg.Urls)
            {
                var url = item.GetRewriteUrl(path);
                if (url != null)
                {
                    context.RewritePath(url);

                    return;
                }
            }
        }
        #endregion
    }

    /// <summary>Url重写地址配置</summary>
    [XmlConfigFile("Config/UrlRewrite.config", 15000)]
    public class UrlConfig : XmlConfig<UrlConfig>
    {
        private List<Item> _Urls;
        /// <summary>匹配地址集合</summary>
        public List<Item> Urls { get { return _Urls; } set { _Urls = value; } }

        /// <summary>Url重写地址配置项</summary>
        public class Item
        {
            private String _Url;
            /// <summary>Url正则表达式</summary>
            [XmlAttribute]
            public String Url { get { return _Url; } set { _Url = value; } }

            private String _To;
            /// <summary>目标地址</summary>
            public String To { get { return _To; } set { _To = value; } }

            private Regex _reg;
            /// <summary>获取指定输入的重写Url</summary>
            /// <param name="input"></param>
            /// <returns></returns>
            public String GetRewriteUrl(String input)
            {
                if (_reg == null) _reg = new Regex(Url, RegexOptions.IgnoreCase);

                var m = _reg.Match(input);
                if (m == null) return null;

                return m.Result(To);
            }
        }
    }
}