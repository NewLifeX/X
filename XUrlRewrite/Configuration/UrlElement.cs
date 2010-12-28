using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Web;
using System.Text.RegularExpressions;
using System.IO;
using XUrlRewrite.Helper;
using NewLife.Log;

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
        [ConfigurationProperty("type",DefaultValue="regexp")]
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
                String val=value.Trim().ToLower();
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
        [ConfigurationProperty("url",IsRequired=true,IsKey=true)]
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
        [ConfigurationProperty("to",IsRequired=true)]
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
        [ConfigurationProperty("regexFlag",DefaultValue="")]
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
        private RegexOptions _regexOptions=RegexOptions.None;
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
        static void SeparateUrl(String url, out String path, out String query)
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

        private ProcessRewriteURL _ProcessRewriteUrl = null;
        /// <summary>
        /// 处理Url重写的委托
        /// </summary>
        /// <param name="form">原始输入的url,应该近包含path部分,不能包括query部分</param>
        /// <param name="query">重写目标的query部分,如果不存在则为""</param>
        /// <returns>重写目标的path部分,如果不符合当前重写规则应该返回null</returns>
        delegate String ProcessRewriteURL(String form, out String query);

        Boolean HavQuerySymbol = true;
        /// <summary>
        /// 用于处理正则表达式类型Url重写的方法
        /// </summary>
        /// <param name="form"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        internal String ProcessRegexpRewriteURL(String form, out String query)
        {
            String ret=this.Regex.Replace(form, "token"+this.To);
            if (ret != form)
            {
                ret = ret.Substring(5);//对应上面的token,避免替换结果和源完全一样,无法区分是否已替换的情况
                query = "";
                if (HavQuerySymbol)
                {
                    SeparateUrl(ret, out ret, out query);
                    if (query.Length == 0)
                    {
                        HavQuerySymbol = false;
                    }
                }
                return ret;
            }
            query = "";
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
            String a=form,b=this.Url;
            if(this.IgnoreCase){
                a = a.ToLower();
                b = b.ToLower();
            }
            if (a==b)
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
        internal Boolean RewriteUrl(String path, String query, HttpApplication app, UrlRewriteConfig cfg)
        {
            if (_ProcessRewriteUrl == null)
            {
                switch (this.Type)
                {
                    case "normal":
                        _ProcessRewriteUrl = ProcessNormalRewriteURL;
                        break;
                    case "regex":
                    case "regexp":
                        _ProcessRewriteUrl = ProcessRegexpRewriteURL;
                        break;
                    default:
                        break;
                }
            }
            String queryString="";
            String filePath = _ProcessRewriteUrl != null ? _ProcessRewriteUrl(path, out queryString) : null;
            if (filePath != null)
            {
                if (File.Exists(app.Server.MapPath(cfg.Directory + filePath)))
                {
                    RewriteHelper.Create(path, query, app).RewriteToInfo(cfg.Directory, filePath, queryString);
                    if (query.Length > 0)
                    {
                        queryString = queryString +  (queryString.Length > 0 ? "&" : "") + query;
                    }
                    app.Context.RewritePath(cfg.Directory + filePath, String.Empty, queryString, false);
#if DEBUG
                    if (XTrace.Debug) XTrace.WriteLine("[XTemplate] Rewrite url:{0} to {1}.", path, cfg.Directory + filePath);
#endif
                }
#if DEBUG
                else
                {
                    if (XTrace.Debug) XTrace.WriteLine("[XTemplate] Fail rewrite url:{0} to {1}, {1} not found.", path, cfg.Directory + filePath);
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
    }
}
