using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using NewLife.Configuration;

namespace NewLife.Mvc
{
    /// <summary>一般控制器工厂接口</summary>
    public class GenericControllerFactory : IControllerFactory
    {
        #region IControllerFactory 成员
        private static string _TempleteDir;
        /// <summary>
        /// 一般控制器的模版根目录,默认为网站根目录 ~,以~或~/开始,始终不以/结尾
        /// </summary>
        public static string TempleteDir
        {
            get
            {
                if (_TempleteDir == null)
                {
                    string s = Config.GetConfig<string>("NewLife.Mvc.TempleteDir", "~");

                    if (s[0] == '/')
                    {
                        s = "~" + s;
                    }
                    else if (s[0] != '~' && s.Length > 1 && s[1] != '/')
                    {
                        s = "~/" + s;
                    }
                    _TempleteDir = s.TrimEnd('/');
                }
                return _TempleteDir;
            }
        }
        private static string[] _AcceptSuffixs;
        /// <summary>
        /// 一般控制器接受处理的请求后缀名,默认包含aspx,xt
        /// </summary>
        public static string[] AcceptSuffixs
        {
            get
            {
                if (_AcceptSuffixs == null)
                {
                    string[] s = Config.GetConfigSplit<string>("NewLife.Mvc.AcceptSuffixs", ",", null);
                    List<string> suffixs = new List<string>();

                    if (s == null || s.Length == 0 || s[0][0] != '*') // 没有标记为:* 覆盖,则添加默认的后缀
                    {
                        suffixs.AddRange(@"aspx,xt".Split(','));
                    }

                    if (s != null && s.Length > 0)
                    {
                        if (s[0][0] == '*') s[0] = s[0].Substring(1);
                        foreach (var item in s)
                        {
                            if (!string.IsNullOrEmpty(item)) suffixs.Add(item);
                        }
                    }
                    _AcceptSuffixs = suffixs.ToArray();
                }
                return _AcceptSuffixs;
            }
        }
        /// <summary>
        /// 将指定的路径解析为模板文件物理路径,不检查文件是否存在
        /// </summary>
        /// <param name="path">以/ ~/ 或普通字符开始都可以</param>
        /// <returns></returns>
        public static string ResolveTempletePath(string path)
        {
            if (string.IsNullOrEmpty(path)) path = TempleteDir;
            if (path[0] == '/') path = path.Substring(1);
            if (path[0] == '~' && path.Length > 1 && path[1] == '/') path = path.Substring(2);

            path = TempleteDir + "/" + path;
            return HttpContext.Current.Server.MapPath(path);
        }
        /// <summary>
        /// 当前控制器工厂产生的控制器是否支持指定路径的请求
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Support(string path)
        {
            string f = ResolveTempletePath(RouteContext.Current.RoutePath);
            string e = Path.GetExtension(f).TrimStart('.');
            if (Array.Exists<string>(AcceptSuffixs, a => string.Equals(a, e, StringComparison.OrdinalIgnoreCase)))
            {
                return File.Exists(f);
            }
            return false;
        }

        /// <summary>
        /// 创建控制器
        /// </summary>
        /// <returns></returns>
        public IController Create() { return new GenericController(); }
        #endregion

    }
}