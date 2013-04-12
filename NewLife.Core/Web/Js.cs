using System;
using System.Web;
using System.Web.UI;
using NewLife.Model;

namespace NewLife.Web
{
    /// <summary>JavaScript脚本。提供Js的基本操作，同时也支持继承扩展</summary>
    /// <remarks>
    /// 提供静态成员<see cref="Current"/>，以及常用的<see cref="WriteScript"/>和<see cref="Alert"/>
    /// </remarks>
    public class Js : IJs
    {
        #region 通用静态
        //private static IJs Current;

        private static IJs _Current;
        /// <summary>当前实现</summary>
        public static IJs Current { get { return _Current; } set { _Current = value; } }

        static Js()
        {
            // 查找实现了IJs接口的外部实现
            //foreach (var item in AssemblyX.FindAllPlugins(typeof(IJs), true, true))
            //{
            //    if (item != typeof(Js))
            //    {
            //        _js = TypeX.CreateInstance(item) as IJs;
            //        if (_js != null) break;
            //    }
            //}

            Current = ObjectContainer.Current.AutoRegister<IJs, Js>().Resolve<IJs>();

            if (Current != null) Current = new Js();
        }

        /// <summary>Js脚本编码</summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String Encode(String str)
        {
            if (String.IsNullOrEmpty(str)) return null;

            str = str.Replace(@"\", @"\\");
            str = str.Replace("'", @"\'");
            str = str.Replace(Environment.NewLine, @"\n");
            str = str.Replace("\r", @"\n");
            str = str.Replace("\n", @"\n");

            return str;
        }

        /// <summary>输出脚本</summary>
        /// <param name="script"></param>
        /// <param name="addScriptTags"></param>
        /// <returns></returns>
        public static IJs WriteScript(String script, Boolean addScriptTags = true)
        {
            return Current.WriteScript(script, addScriptTags);
        }

        /// <summary>弹出页面提示</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        /// <returns></returns>
        public static IJs Alert(String format, params Object[] args)
        {
            if (String.IsNullOrEmpty(format)) return Current;

            return Current;
        }
        #endregion

        #region 成员
        /// <summary>输出脚本</summary>
        /// <param name="script"></param>
        /// <param name="addScriptTags"></param>
        /// <returns></returns>
        IJs IJs.WriteScript(String script, Boolean addScriptTags)
        {
            OnWriteScript(script, addScriptTags);
            return this;
        }

        /// <summary>输出脚本</summary>
        /// <param name="script"></param>
        /// <param name="addScriptTags"></param>
        /// <returns></returns>
        protected virtual void OnWriteScript(String script, Boolean addScriptTags)
        {
            var page = HttpContext.Current.Handler as Page;
            page.ClientScript.RegisterStartupScript(page.GetType(), script, script, addScriptTags);
        }

        /// <summary>弹出页面提示</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        /// <returns></returns>
        IJs IJs.Alert(String format, params Object[] args)
        {
            if (String.IsNullOrEmpty(format)) return this;

            var msg = String.Format(format, args);
            msg = Encode(msg);

            OnAlert(msg);

            return this;
        }

        /// <summary>弹出页面提示</summary>
        /// <param name="msg">字符串</param>
        /// <returns></returns>
        protected virtual void OnAlert(String msg)
        {
            WriteScript("alert('" + Encode(msg) + "');", true);
        }

        /// <summary>停止输出</summary>
        public virtual IJs End() { HttpContext.Current.Response.End(); return this; }

        /// <summary>后退一步</summary>
        public virtual IJs Back() { return WriteScript("history.go(-1);"); }

        /// <summary>刷新该页面</summary>
        public virtual IJs Refresh() { return WriteScript("location.href = location.href;"); }

        /// <summary>重定向到另外的页面</summary>
        /// <param name="url"></param>
        public virtual IJs Redirect(String url)
        {
            if (!url.Contains("?"))
                url += "?";
            else
                url += "&";

            url += "rnd=";
            url += DateTime.Now.Ticks.ToString();

            WriteScript("location.href = '" + url + "';");
            return End();
        }

        /// <summary>关闭当前页面</summary>
        public virtual IJs Close() { return WriteScript("window.close();"); }
        #endregion
    }

    /// <summary>JavaScript脚本接口</summary>
    public interface IJs
    {
        /// <summary>输出脚本</summary>
        /// <param name="script"></param>
        /// <param name="addScriptTags"></param>
        /// <returns></returns>
        IJs WriteScript(String script, Boolean addScriptTags);

        /// <summary>弹出页面提示</summary>
        /// <param name="format">格式化字符串</param>
        /// <param name="args">格式化参数</param>
        /// <returns></returns>
        IJs Alert(String format, params Object[] args);

        /// <summary>停止输出</summary>
        IJs End();

        /// <summary>后退一步</summary>
        IJs Back();

        /// <summary>刷新该页面</summary>
        IJs Refresh();

        /// <summary>重定向到另外的页面</summary>
        /// <param name="url"></param>
        IJs Redirect(String url);

        /// <summary>关闭当前页面</summary>
        IJs Close();
    }
}