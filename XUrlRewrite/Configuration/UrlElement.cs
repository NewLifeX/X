using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using NewLife.Log;
using XUrlRewrite.Helper;

namespace XUrlRewrite.Configuration
{
    /// <summary>
    /// Url映射配置元素
    ///
    /// 需要注意的是在配置文件中,xml标签的属性写&amp;符号时需要写为&amp;amp;
    /// <see cref="System.Text.RegularExpressions.Regex"/>
    /// </summary>
    public class UrlElement : ConfigurationElement
    {
        /// <summary>
        /// 映射类型,主要有normal和regexp,默认regexp
        /// </summary>
        [ConfigurationProperty("type", DefaultValue = "regexp")]
        public String Type
        {
            get
            {
                String ret = (String)this["type"];
                if (ret == "regex") ret = "regexp";
                return ret;
            }
            set
            {
                String val = value.Trim().ToLower();
                if (val == "regex") val = "regexp";
                if (val != "normal" && val != "regexp")
                {
                    throw new Exception(String.Format("{0} is invalid url type", val));
                }

                this["type"] = val;
            }
        }

        /// <summary>
        /// 需要重写的Url,如果是regexp类型的话这里是正则表达式
        /// </summary>
        [ConfigurationProperty("url", IsRequired = true, IsKey = true)]
        public String Url
        {
            get
            {
                return (String)this["url"];
            }
            set
            {
                this["url"] = value;
            }
        }

        /// <summary>
        /// Url重写的目标,如果是regexp类型的话这里可以使用$1...9表示正则的子匹配
        /// </summary>
        [ConfigurationProperty("to", IsRequired = true)]
        public String To
        {
            get
            {
                return (String)this["to"];
            }
            set
            {
                this["to"] = value;
            }
        }

        /// <summary>
        /// 是否启用,默认是
        /// </summary>
        [ConfigurationProperty("enabled", DefaultValue = true)]
        public Boolean Enabled
        {
            get
            {
                return (Boolean)this["enabled"];
            }
            set
            {
                this["enabled"] = value;
            }
        }

        /// <summary>
        /// 正则额外标记,主要有i忽略大小 c忽略语言中的区域性差异 m多行模式 s单行模式
        /// </summary>
        [ConfigurationProperty("regexFlag", DefaultValue = "")]
        public String RegexFlag
        {
            get
            {
                return (String)this["regexFlag"];
            }
            set
            {
                this["regexFlag"] = value;
            }
        }

        /// <summary>
        /// 忽略大小写,仅在普通模式生效
        /// </summary>
        [ConfigurationProperty("ignoreCase", DefaultValue = true)]
        public Boolean IgnoreCase
        {
            get
            {
                return (Boolean)this["ignoreCase"];
            }
            set
            {
                this["ignoreCase"] = value;
            }
        }

        private Regex _regex;
        /// <summary>
        /// Url对应的正则对象,如果Type是regexp
        /// </summary>
        internal Regex Regex
        {
            get
            {
                if (_regex == null)
                {
                    _regex = new Regex(Url, this.RegexFlags);
                }
                return _regex;
            }
        }

        private RegexOptions _regexOptions = RegexOptions.None;
        /// <summary>
        /// RegexFlag对应的正则选项
        /// </summary>
        private RegexOptions RegexFlags
        {
            get
            {
                if (_regexOptions == RegexOptions.None)
                {
                    _regexOptions = RegexOptions.Compiled;
                    foreach (char o in RegexFlag.ToLower())
                    {
                        if (o.Equals('i'))
                        {
                            _regexOptions = _regexOptions | RegexOptions.IgnoreCase;
                        }
                        else if (o.Equals('c'))
                        {
                            _regexOptions = _regexOptions | RegexOptions.CultureInvariant;
                        }
                        else if (o.Equals('m'))
                        {
                            _regexOptions = _regexOptions | RegexOptions.Multiline;
                        }
                        else if (o.Equals('s'))
                        {
                            _regexOptions = _regexOptions | RegexOptions.Singleline;
                        }
                    }
                }
                return _regexOptions;
            }
        }

        /// <summary>
        /// 分离指定url中path和query部分
        /// </summary>
        /// <param name="url"></param>
        /// <param name="path">输出path部分</param>
        /// <param name="query">输出query部分,不包含?,如果url不存在query部分则返回空白字符串</param>
        private static void SeparateUrl(String url, out String path, out String query)
        {
            Int32 indexOf = url.IndexOf("?");
            if (indexOf != -1)
            {
                path = url.Substring(0, indexOf);
                query = url.Substring(indexOf + 1);
            }
            else
            {
                path = url;
                query = "";
            }
        }

        private ProcessRewriteURL RewriteUrlFunc = null;
        /// <summary>
        /// 处理Url重写的委托
        /// </summary>
        /// <param name="form">原始输入的url,应该近包含path部分,不能包括query部分</param>
        /// <param name="query">重写目标的query部分,如果不存在则为""</param>
        /// <returns>重写目标的path部分,如果不符合当前重写规则应该返回null</returns>
        delegate String ProcessRewriteURL(String form, out String query);

        bool HavQuerySymbol = true;

        /// <summary>
        /// 用于处理正则表达式类型Url重写的方法
        /// </summary>
        /// <param name="form"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        internal String ProcessRegexpRewriteURL(String form, out String query)
        {
            var match = false;
            var ret = Regex.Replace(form, delegate(Match m)
            {
                match = true;
                return m.Result(To);
            });
            query = "";
            if (match)
            {
                if (HavQuerySymbol)
                {
                    SeparateUrl(ret, out ret, out query);
                    if (string.IsNullOrEmpty(query)) HavQuerySymbol = false;
                }
                return ret;
            }
            return null;
        }

        private String ProcessNormalRewriteURLResult, ProcessNormalRewriteURLQuery;
        /// <summary>
        /// 用于处理普通Url重写的方法
        /// </summary>
        /// <param name="form"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        internal String ProcessNormalRewriteURL(String form, out String query)
        {
            String a = form, b = this.Url;
            if (this.IgnoreCase)
            {
                a = a.ToLower();
                b = b.ToLower();
            }
            if (a == b)
            {
                if (ProcessNormalRewriteURLResult == null)
                {
                    SeparateUrl(this.To, out ProcessNormalRewriteURLResult, out ProcessNormalRewriteURLQuery);
                }
                query = ProcessNormalRewriteURLQuery;
                return ProcessNormalRewriteURLResult;
            }
            query = "";
            return null;
        }
        /// <summary>
        /// 尝试重写指定路径,如果重写成功返回true,并会调用app.Context.RewritePath实现重写
        /// </summary>
        /// <param name="path">尝试重写的路径</param>
        /// <param name="query">原始的query字符串,如果重写成功会附加到重写目标的query字符串中</param>
        /// <param name="app"></param>
        /// <param name="cfg"></param>
        /// <returns></returns>
        internal Boolean RewriteUrl(String path, String query, HttpApplication app, UrlRewriteConfig cfg)
        {
            if (RewriteUrlFunc == null)
            {
                switch (this.Type)
                {
                    case "normal":
                        RewriteUrlFunc = ProcessNormalRewriteURL;
                        break;
                    case "regex":
                    case "regexp":
                        RewriteUrlFunc = ProcessRegexpRewriteURL;
                        break;
                    default:
                        break;
                }
            }
            String queryString = "";
            String filePath = RewriteUrlFunc != null ? RewriteUrlFunc(path, out queryString) : null;
            if (filePath != null)
            {
                RewriteHelper.Create(path, query, ToString(), app)
                    .RewriteToInfo(cfg.Directory, filePath, queryString)
                    .TraceLog();
                if (File.Exists(app.Server.MapPath(cfg.Directory + filePath)))
                {
                    if (!string.IsNullOrEmpty(query))
                    {
                        queryString = (queryString + "&" + query).TrimStart('&');
                    }
                    app.Context.RewritePath(cfg.Directory + filePath, app.Request.PathInfo, queryString, true);
                }
#if DEBUG
                else
                {
                    if (Manager.Debug) XTrace.WriteLine("重写目标{0}文件不存在", cfg.Directory + filePath);
                }
#endif
                return true;
            }
            return false;
        }

        /// <summary>
        /// 更新时使用,用于保存旧Url
        /// </summary>
        public String UpdateKey { get; set; }

        /// <summary>
        /// 重写
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Type != "regexp") sb.AppendFormat(" type=\"{0}\"", Type);
            if (!string.IsNullOrEmpty(RegexFlag)) sb.AppendFormat(" regexFlag=\"{0}\"", RegexFlag);
            if (!IgnoreCase) sb.Append(" ignoreCase=\"false\"");
            if (!Enabled) sb.Append(" enabled=\"false\"");
            return string.Format("<add url=\"{0}\" to=\"{1}\"{2}/>", Url, To, sb);
        }
    }
}