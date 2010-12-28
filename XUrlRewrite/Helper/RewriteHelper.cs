using System;
using System.Collections.Generic;
using System.Text;
using System.Web;


namespace XUrlRewrite.Helper
{
    /// <summary>
    /// Url重写辅助工具类
    /// </summary>
    public class RewriteHelper
    {
        private String _rTemplateDir;
        private String _rPath;
        private String _rQuery;
        private String uPath;
        private String uQuery;
        
        /// <summary>
        /// 构造方法
        /// </summary>
        protected RewriteHelper()
        {
        }

        /// <summary>
        /// 创建Url重写辅助工具,主要用于在模板页解析相对于模板的路径
        /// </summary>
        /// <param name="uPath"></param>
        /// <param name="uQuery"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        internal static RewriteHelper Create(String uPath, String uQuery, HttpApplication app)
        {
            RewriteHelper ret = new RewriteHelper();
            ret.uPath = uPath;
            ret.uQuery = uQuery;
            ret.Path = app.Request.ApplicationPath.TrimEnd('/') + uPath;
            ret.AppRelativeCurrentExecutionFilePath = "~" + uPath;
            
            ret.QueryString = HttpUtility.ParseQueryString("");
            ret.QueryString.Add(app.Request.QueryString);

            HttpContext.Current.Items[typeof(RewriteHelper).FullName] = ret;
            return ret;
        }
        /// <summary>
        /// 获取当前请求中的RewriteHelper类,如果不存在会返回空白类,即所有涉及路径的操作和普通Page相关的路径操作一致
        /// </summary>
        public static RewriteHelper Current
        {
            get
            {
                RewriteHelper ret = HttpContext.Current.Items[typeof(RewriteHelper).FullName] as RewriteHelper;
                if (null == ret)
                {
                    ret = RewriteHelperEmpty;
                }
                return ret;
            }
        }

        /// <summary>
        /// 设置Url重写的目标地址信息
        /// </summary>
        /// <param name="rTemplateDir"></param>
        /// <param name="rPath"></param>
        /// <param name="rQuery"></param>
        /// <returns></returns>
        internal RewriteHelper RewriteToInfo(String rTemplateDir, String rPath, String rQuery)
        {
            _rTemplateDir = rTemplateDir;
            _rPath = rPath;
            _rQuery = rQuery;
            return this;
        }

        /// <summary>
        /// <see cref="System.Web.HttpRequest.Path"/>
        /// </summary>
        public String Path { get; set; }

        /// <summary>
        /// <see cref="System.Web.HttpRequest.FilePath"/>
        /// </summary>
        public String FilePath
        {
            get
            {
                return Path;
            }
        }

        /// <summary>
        /// <see cref="System.Web.HttpRequest.CurrentExecutionFilePath"/>
        /// </summary>
        public String CurrentExecutionFilePath
        {
            get
            {
                return Path;
            }
        }

        /// <summary>
        /// <see cref="System.Web.HttpRequest.AppRelativeCurrentExecutionFilePath"/>
        /// </summary>
        public String AppRelativeCurrentExecutionFilePath { get; set; }

        /// <summary>
        /// <see cref="System.Web.HttpRequest.QueryString"/>
        /// </summary>
        public System.Collections.Specialized.NameValueCollection QueryString { get; set; }

        private String _FormAction;
        /// <summary>
        /// 当前模板页form标签合理的action地址
        /// </summary>
        public String FormAction
        {
            get
            {
                if (null == _FormAction)
                {
                    _FormAction = VirtualPathUtility.GetFileName(uPath);
                    _FormAction += uQuery.Length > 0 ? "?" + uQuery : "";
                }
                return _FormAction;
            }
        }

        /// <summary>
        /// 解析指定的Url,返回相对于域名根路径的Url,即以/开头的路径
        /// </summary>
        /// <param name="url">如果以~/开头表示从模板根路径开始, 如果以/开头将原样返回, 其它路径按照相对于当前模板文件的路径决定</param>
        /// <returns></returns>
        public virtual String ResolveUrl(String url)
        {
            return VirtualPathUtility.ToAbsolute(_ResolveUrl(url));
        }

        /// <summary>
        /// 解析指定的Url,返回相对于当前应用的Url,即以~/开头的路径
        /// </summary>
        /// <param name="url">参见ResolveUrl方法的url参数</param>
        /// <returns></returns>
        public virtual String ResolveUrlAppRelative(String url)
        {
            return VirtualPathUtility.ToAppRelative(_ResolveUrl(url));
        }

        private String _ResolveUrl(String url)
        {
            if (url[0] == '~' && url[1] == '/')
            {
                return VirtualPathUtility.RemoveTrailingSlash(_rTemplateDir) + url.Substring(1);
            }
            else if (url[0] == '/')
            {
                return url;
            }
            else
            {
                return VirtualPathUtility.RemoveTrailingSlash(_rTemplateDir) + VirtualPathUtility.GetDirectory(_rPath) + url;
            }
        }

        static RewriteHelper RewriteHelperEmpty = new RewriteHelperEmptyWrap();
        /// <summary>
        /// 空重写辅助工具
        /// </summary>
        class RewriteHelperEmptyWrap : RewriteHelper
        {
            public RewriteHelperEmptyWrap()
            {
                _FormAction = "";
            }

            public override String ResolveUrl(String url)
            {
                if (url[0] == '~' && url[1] == '/')
                {
                    return VirtualPathUtility.ToAbsolute(url);
                }
                return url;
            }

            public override String ResolveUrlAppRelative(String url)
            {
                if (url[0] == '~' && url[1] == '/')
                {
                    return VirtualPathUtility.ToAppRelative(url);
                }
                return url;
            }
        }
    }
}
