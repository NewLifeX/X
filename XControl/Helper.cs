using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Reflection;

namespace XControl
{
    internal static class Helper
    {
        /// <summary>
        /// 逐层向上找控件
        /// </summary>
        /// <typeparam name="TControl"></typeparam>
        /// <param name="control"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TControl FindControlUp<TControl>(Control control, String id) where TControl : Control
        {
            if (control == null || String.IsNullOrEmpty(id)) return null;
            if (control.ID == id && control is TControl) return control as TControl;

            if (control.Parent == null) return null;

            Control parent = control.Parent;
            if (parent.ID == id && parent is TControl) return parent as TControl;

            // 在兄弟节点向下找
            if (parent.Controls != null && parent.Controls.Count > 0)
            {
                foreach (Control item in parent.Controls)
                {
                    // 向上搜索的关键是要避开自己
                    if (item == control) continue;

                    TControl elm = FindControl<TControl>(item, id);
                    if (elm != null) return elm;
                }
            }

            // 向上递归
            return FindControlUp<TControl>(parent, id);
        }

        /// <summary>
        /// 逐层向下找控件
        /// </summary>
        /// <typeparam name="TControl"></typeparam>
        /// <param name="control"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TControl FindControl<TControl>(Control control, String id) where TControl : Control
        {
            if (control == null || String.IsNullOrEmpty(id)) return null;
            if (control.ID == id && control is TControl) return control as TControl;

            if (control.Controls == null || control.Controls.Count <= 0) return null;

            //// 深度搜索算法
            //foreach (Control item in control.Controls)
            //{
            //    Control elm = FindControl<TControl>(item, id);
            //    if (elm != null) return elm;
            //}

            // 广度搜索算法
            Queue<Control> queue = new Queue<Control>();
            queue.Enqueue(control);
            while (queue.Count > 0)
            {
                control = queue.Dequeue();
                if (control.ID == id && control is TControl) return control as TControl;
                if (control.Controls == null || control.Controls.Count <= 0) continue;

                // 子控件进入队列
                foreach (Control item in control.Controls)
                {
                    queue.Enqueue(item);
                }
            }

            return null;
        }

        #region Javascript和控件HTML属性工具方法

        /// <summary>
        /// 将指定字符串作为html标签属性中可使用的字符串返回
        /// </summary>
        /// <param name="s"></param>
        public static string HTMLPropertyEscape(string s)
        {
            return (s + "").Replace("\"", "&quot;")
                //.Replace("&", "&amp;") 现代浏览器基本都会对此容错,不合理的命名实体会忽略
                .Replace("\r", "&#13;").Replace("\n", "&#10;");
        }

        /// <summary>
        /// 将指定字符串作为html标签属性中可使用的字符串返回
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string HTMLPropertyEscape(string fmt, params object[] args)
        {
            return HTMLPropertyEscape(string.Format(fmt, args));
        }

        /// <summary>
        /// 将指定字符串作为html标签属性中可使用的字符串返回
        /// </summary>
        /// <param name="ctl">需要添加属性的控件</param>
        /// <param name="attname">需要添加的html属性名,以on开头的名称将会使用JsMinSimple处理换行</param>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string HTMLPropertyEscape(WebControl ctl, string attname, string fmt, params object[] args)
        {
            string s = null;
            if (attname.StartsWith("on", StringComparison.OrdinalIgnoreCase))
            {
                s = JsMinSimple(s);
            }
            s = HTMLPropertyEscape(fmt, args);
            ctl.Attributes[attname] = s;
            return s;
        }

        /// <summary>
        /// 使用指定的名值对创建对应的Javascript Object的声明字符串
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string JsObjectString(params object[] args)
        {
            return JsObjectString((s, o) => true, args);
        }

        /// <summary>
        /// 使用指定的名值对创建对应的Javascript Object的声明字符串
        /// 返回的结果类似 {'a':0.1, 'b':'2', 'c':true},如果args长度为0则返回{}
        ///
        /// 如果不是成对出现的 则会忽略掉最后一个
        /// 如果需要去除开始和结尾的{},可以使用String.Trim('{','}')
        /// </summary>
        /// <param name="filter">过滤器,遍历名值,只对于返回true的名值进行处理</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string JsObjectString(Func<string, object, bool> filter, params object[] args)
        {
            return JsObjectString(true, filter, args);
        }

        static Regex JsIdentityWord = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// 使用指定的名值对创建对应的Javascript Object的声明字符串
        /// </summary>
        /// <param name="escapeValue"></param>
        /// <param name="filter"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string JsObjectString(bool escapeValue, Func<string, object, bool> filter, params object[] args)
        {
            if (args.Length == 0) return "{}";
            int l = args.Length / 2;
            List<string> r = new List<string>(l);
            for (int i = 0; i < l; i++)
            {
                string k = JsStringEscape(args[i * 2].ToString());
                if (!JsIdentityWord.IsMatch(k)) k = "'" + k + "'";
                object v = args[i * 2 + 1];
                if (filter(k, v))
                {
                    if (escapeValue)
                    {
                        // 所有js中不能直接输出的字面值都编码为字符串
                        if (!IsJsLiteralValue(v))
                        {
                            v = "'" + JsStringEscape(v.ToString()) + "'";
                        }
                    }
                    r.Add(string.Format("{0}:{1}", k, v));
                }
            }
            return "{" + string.Join(",", r.ToArray()) + "}";
        }

        /// <summary>
        /// 指定变量是否属于js的字面值类型,字面值类型可以直接ToString输出
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool IsJsLiteralValue(object v)
        {
            return v is bool || v is byte || v is double || v is Int16 || v is Int32 || v is Int64 || v is sbyte || v is float || v is UInt16 || v is UInt32 || v is UInt64;
        }

        /// <summary>
        /// 将指定的字符串作为javascript中使用的字符串内容返回,没有js字符串声明两边的双引号
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string JsStringEscape(string i)
        {
            return (i + "").Replace(@"\", @"\\").Replace("'", @"\'").Replace("\"", @"\""").Replace("\r", @"\r").Replace("\n", @"\n");
        }

        /// <summary>
        /// 将指定的字符串作为javascript中使用的字符串内容返回,没有js字符串声明两边的双引号
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string JsStringEscape(string fmt, params object[] args)
        {
            return JsStringEscape(string.Format(fmt, args));
        }

        static Regex reMinJs = new Regex(@"\s*(?:\r\n|\r|\n)+\s*", RegexOptions.Compiled);

        /// <summary>
        /// 将指定的javascript代码做简单压缩,去除换行和缩进
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string JsMinSimple(string i)
        {
            return reMinJs.Replace(i + "", "");
        }

        /// <summary>
        /// 将指定的javascript代码做简单压缩,去除换行和缩进
        /// </summary>
        /// <param name="fmt"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string JsMinSimple(string fmt, params object[] args)
        {
            return JsMinSimple(string.Format(fmt, args));
        }

        #endregion Javascript和控件HTML属性工具方法
    }
}