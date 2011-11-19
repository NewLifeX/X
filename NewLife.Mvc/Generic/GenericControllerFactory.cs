using System;
using System.Collections.Generic;
using System.IO;
using NewLife.Configuration;

namespace NewLife.Mvc
{
    /// <summary>一般控制器工厂接口</summary>
    public class GenericControllerFactory : IControllerFactory
    {
        #region 方法

        private static string _TempleteDir;

        /// <summary>
        /// 一般控制器的模版根目录,默认为网站根目录,即空白字符串,始终以普通字符开始,并且始终不以/符号结尾
        /// </summary>
        public static string TempleteDir
        {
            get
            {
                if (_TempleteDir == null)
                {
                    string s = Config.GetConfig<string>("NewLife.Mvc.TempleteDir", "");

                    if (s.StartsWith("/")) s = s.Substring(1);
                    if (s.StartsWith("~/")) s = s.Substring(2);

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
                    List<string> suffixs = new List<string>(@"aspx,xt".Split(','));

                    if (s != null && s.Length > 0)
                    {
                        string first = s[0];
                        if (first.StartsWith("*")) //第一个为*表示覆盖掉默认控制器处理的后缀
                        {
                            suffixs.Clear();
                            s[0] = first.Substring(0);
                        }
                        suffixs.AddRange(Array.FindAll<string>(s, a => !string.IsNullOrEmpty(a.Trim())));
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
            if (path == null) path = "";
            if (path.StartsWith("/")) path = path.Substring(1);
            if (path.StartsWith("~/")) path = path.Substring(2);

            return Path.GetFullPath(Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, TempleteDir), path.TrimStart('/')));
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

        ///// <summary>
        ///// 创建控制器
        ///// </summary>
        ///// <returns></returns>
        //public IController Create() { return new GenericController(); }

        #endregion

        #region IControllerFactory 成员
        /// <summary>返回实现 <see cref="T:NewLife.Mvc.IController" /> 接口的类的实例。</summary>
        /// <returns>处理请求的新的 <see cref="T:NewLife.Mvc.IController" /> 对象。</returns>
        /// <param name="context"><see cref="T:NewLife.Mvc.IRouteContext" /> 类的实例，它提供对用于为 HTTP 请求提供服务的内部服务器对象的引用。</param>
        public IController GetController(IRouteContext context)
        {
            return Support(context.Path) ? new GenericController() : null;
        }

        /// <summary>使工厂可以重用现有的处理程序实例。</summary>
        /// <param name="handler">要重用的 <see cref="T:NewLife.Mvc.IController" /> 对象。</param>
        public void ReleaseController(IController handler) { }
        #endregion
    }
}