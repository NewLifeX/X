using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Web
{
    /// <summary>网页工具类</summary>
    public static class WebHelper
    {
        #region 辅助
        /// <summary>输出脚本</summary>
        /// <param name="script"></param>
        public static void WriteScript(String script)
        {
            Js.WriteScript(script, true);
        }
        #endregion

        #region 弹出信息
        /// <summary>弹出页面提示</summary>
        /// <param name="msg"></param>
        public static void Alert(String msg)
        {
            Js.Alert(msg);
        }

        /// <summary>弹出页面提示并停止输出后退一步！</summary>
        /// <param name="msg"></param>
        public static void AlertAndEnd(String msg)
        {
            Js.Alert(msg).End();
        }

        /// <summary>弹出页面提示，并刷新该页面</summary>
        /// <param name="msg"></param>
        public static void AlertAndRefresh(String msg)
        {
            Js.Alert(msg).Refresh().End();
        }

        /// <summary>弹出页面提示并重定向到另外的页面</summary>
        /// <param name="msg"></param>
        /// <param name="url"></param>
        public static void AlertAndRedirect(String msg, String url)
        {
            Js.Alert(msg).Redirect(url).End();
        }

        /// <summary>弹出页面提示并关闭当前页面</summary>
        /// <param name="msg"></param>
        public static void AlertAndClose(String msg)
        {
            Js.Alert(msg).Close().End();
        }
        #endregion

        #region 输入检查
        /// <summary>检查控件值是否为空，若为空，显示错误信息，并聚焦到控件上</summary>
        /// <param name="control">要检查的控件</param>
        /// <param name="errmsg">错误信息。若为空，将使用ToolTip</param>
        /// <returns></returns>
        public static Boolean CheckEmptyAndFocus(Control control, String errmsg)
        {
            if (control == null) throw new ArgumentNullException("control");

            if (control is WebControl && String.IsNullOrEmpty(errmsg)) errmsg = (control as WebControl).ToolTip;

            if (control is TextBox)
            {
                TextBox box = control as TextBox;
                if (!String.IsNullOrEmpty(box.Text)) return true;
            }
            else if (control is ListControl)
            {
                ListControl box = control as ListControl;
                if (!String.IsNullOrEmpty(box.Text)) return true;
            }
            else
                throw new XException("暂时不支持{0}控件！", control.GetType());

            control.Focus();
            if (!String.IsNullOrEmpty(errmsg)) Alert(errmsg);
            return false;
        }
        #endregion

        #region 用户主机
        /// <summary>用户主机</summary>
        public static String UserHost
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    String str = (String)HttpContext.Current.Items["UserHostAddress"];
                    if (!String.IsNullOrEmpty(str)) return str;

                    if (Request != null)
                    {
                        str = Request.ServerVariables["REMOTE_ADDR"];
                        if (String.IsNullOrEmpty(str)) str = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                        if (String.IsNullOrEmpty(str)) str = Request.UserHostName;
                        if (String.IsNullOrEmpty(str)) str = Request.UserHostAddress;
                        HttpContext.Current.Items["UserHostAddress"] = str;
                        return str;
                    }
                }
                return null;
            }
        }

        /// <summary>页面文件名</summary>
        public static String PageName { get { return Path.GetFileName(Request.FilePath); } }
        #endregion

        #region 导出Excel
        /// <summary>导出Excel</summary>
        /// <param name="gv"></param>
        /// <param name="filename"></param>
        /// <param name="max"></param>
        public static void ExportExcel(GridView gv, String filename, Int32 max)
        {
            ExportExcel(gv, filename, max, Encoding.Default);
        }

        /// <summary>导出Excel</summary>
        /// <param name="gv"></param>
        /// <param name="filename"></param>
        /// <param name="max"></param>
        /// <param name="encoding"></param>
        public static void ExportExcel(GridView gv, String filename, Int32 max, Encoding encoding)
        {
            //var Request = HttpContext.Current.Request;
            //var Response = HttpContext.Current.Response;

            //去掉所有列的排序
            foreach (DataControlField item in gv.Columns)
            {
                if (item is DataControlField) (item as DataControlField).SortExpression = null;
            }
            if (max > 0) gv.PageSize = max;
            gv.DataBind();

            // 新建页面
            var page = new Page();
            var form = new HtmlForm();

            page.EnableEventValidation = false;
            page.Controls.Add(form);
            form.Controls.Add(gv);

            Response.Clear();
            Response.Buffer = true;
            Response.Charset = encoding.WebName;
            Response.ContentEncoding = encoding;
            /*
             * 按照RFC2231的定义， 多语言编码的Content-Disposition应该这么定义：
             * Content-Disposition: attachment; filename*="utf8''%e6%94%b6%e6%ac%be%e7%ae%a1%e7%90%86.xls"
             * filename后面的等号之前要加 *
             * filename的值用单引号分成三段，分别是字符集(utf8)、语言(空)和urlencode过的文件名。
             * 最好加上双引号，否则文件名中空格后面的部分在Firefox中显示不出来
             */
            var cd = String.Format("attachment;filename=\"{0}\"", filename);
            if (Request.UserAgent.Contains("MSIE"))
                cd = String.Format("attachment;filename=\"{0}\"", HttpUtility.UrlEncode(filename, encoding));
            else if (Request.UserAgent.Contains("Firefox"))
                cd = String.Format("attachment;filename*=\"{0}''{1}\"", encoding.WebName, HttpUtility.UrlEncode(filename, encoding));

            Response.AppendHeader("Content-Disposition", cd);
            Response.ContentType = "application/ms-excel";

            var sw = new StringWriter();
            var htw = new HtmlTextWriter(sw);
            page.RenderControl(htw);

            var html = sw.ToString();
            //if (html.StartsWith("<div>")) html = html.SubString("<div>".Length);
            //if (html.EndsWith("</div>")) html = html.SubString(0, html.Length - "</div>".Length);

            html = String.Format("<meta http-equiv=\"content-type\" content=\"application/ms-excel; charset={0}\"/>", encoding.WebName) + Environment.NewLine + html;

            Response.Output.Write(html);

            //var wd = new WebDownload(html, encoding);
            //wd.Mode = WebDownload.DispositionMode.Attachment;
            //wd.Render();

            Response.Flush();
            Response.End();
        }
        #endregion

        #region Http请求
        /// <summary>Http请求</summary>
        public static HttpRequest Request { get { return HttpContext.Current != null ? HttpContext.Current.Request : null; } }

        /// <summary>返回请求字符串和表单的名值字段，过滤空值和ViewState，同名时优先表单</summary>
        public static IDictionary<String, String> Params
        {
            get
            {
                var dic = HttpContext.Current.Items["Params"] as IDictionary<String, String>;
                if (dic != null) return dic;

                // 这里必须用可空字典，否则直接通过索引查不到数据时会抛出异常
                dic = new NullableDictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                var nvss = new NameValueCollection[] { Request.QueryString, Request.Form };
                foreach (var nvs in nvss)
                {
                    foreach (var item in nvs.AllKeys)
                    {
                        if (item.IsNullOrWhiteSpace()) continue;
                        if (item.StartsWithIgnoreCase("__VIEWSTATE")) continue;

                        // 空值不需要
                        var value = nvs[item];
                        if (value.IsNullOrWhiteSpace()) continue;

                        // 同名时有限表单
                        dic[item] = value.Trim();
                    }
                }
                HttpContext.Current.Items["Params"] = dic;

                return dic;
            }
        }

        /// <summary>把Http请求的数据反射填充到目标对象</summary>
        /// <param name="target">要反射搜索的目标对象，比如页面Page</param>
        /// <returns></returns>
        public static Int32 Fill(Object target)
        {
            if (target == null) return 0;

            var count = 0;
            var type = target.GetType();
            var nvs = new NameValueCollection[] { Request.QueryString, Request.Form };

            // 精确搜索属性、字段，模糊搜索属性、字段
            foreach (var nv in nvs)
            {
                foreach (var item in nv.AllKeys)
                {
                    var member = type.GetMemberEx(item, true);
                    if (member != null && (member is FieldInfo || member is PropertyInfo))
                    {
                        count++;
                        target.SetValue(member, nv[item]);
                    }
                }
            }

            return count;
        }

        //public static Int32 GetInt(String name, Int32 defaultValue = 0) { return Request[name].ToInt(defaultValue); }

        //public static Boolean GetBoolean(String name, Boolean defaultValue = false) { return Request[name].ToBoolean(defaultValue); }

        //public static DateTime GetDateTime(String name) { return Request[name].ToDateTime(); }

        /// <summary>获取整型参数</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Int32 RequestInt(String name) { return Request[name].ToInt(); }

        /// <summary>接收布尔值</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static bool RequestBool(String name) { return Request[name].ToBoolean(); }

        /// <summary>接收时间</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static DateTime RequestDateTime(String name) { return Request[name].ToDateTime(); }

        /// <summary>接收Double</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Double RequestDouble(String name) { return Request[name].ToDouble(); }

        ///// <summary>字符转换为数字</summary>
        ///// <param name="val"></param>
        ///// <returns></returns>
        //public static Int32 ConvertInt(String val)
        //{
        //    Int32 r = 0;
        //    if (String.IsNullOrEmpty(val)) return r;
        //    Int32.TryParse(val, out r);
        //    return r;
        //}

        ///// <summary>字符转换为布尔</summary>
        ///// <param name="val"></param>
        ///// <returns></returns>
        //public static bool ConvertBool(String val)
        //{
        //    bool r = false;
        //    if (String.IsNullOrEmpty(val)) return r;

        //    val = val.Trim();

        //    //if (val.EqualIC("True") || "1".Equals(val))
        //    //{
        //    //    return true;
        //    //}
        //    //else if (val.EqualIC("False") || "0".Equals(val))
        //    //{
        //    //    return false;
        //    //}
        //    if (val.EqualIgnoreCase("True", "1")) return true;
        //    if (val.EqualIgnoreCase("False", "0")) return false;

        //    return r;
        //}

        ///// <summary>字符转换为时间</summary>
        ///// <param name="val"></param>
        ///// <returns></returns>
        //public static DateTime ConvertDateTime(String val)
        //{
        //    DateTime r = DateTime.MinValue;
        //    if (String.IsNullOrEmpty(val)) return r;
        //    DateTime.TryParse(val, out r);
        //    return r;
        //}

        ///// <summary>字符转换</summary>
        ///// <param name="val"></param>
        ///// <returns></returns>
        //public static Double ConvertDouble(String val)
        //{
        //    Double r = 0;
        //    if (String.IsNullOrEmpty(val)) return r;
        //    Double.TryParse(val, out r);
        //    return r;
        //}
        #endregion

        #region Http响应
        /// <summary>Http响应</summary>
        public static HttpResponse Response { get { return HttpContext.Current != null ? HttpContext.Current.Response : null; } }
        #endregion

        #region Cookie
        /// <summary>写入Cookie</summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        public static void WriteCookie(String name, String value)
        {
            var cookie = Request.Cookies[name];
            if (cookie == null) cookie = new HttpCookie(name);

            cookie.Value = value;
            Response.AppendCookie(cookie);
        }

        /// <summary>写入Cookie</summary>
        /// <param name="name">名称</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        public static void WriteCookie(String name, String key, String value)
        {
            var cookie = Request.Cookies[name];
            if (cookie == null) cookie = new HttpCookie(name);

            cookie[key] = value;
            Response.AppendCookie(cookie);
        }

        /// <summary>写入Cookie</summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <param name="expires">过期时间，单位秒</param>
        public static void WriteCookie(String name, String value, int expires)
        {
            var cookie = Request.Cookies[name];
            if (cookie == null) cookie = new HttpCookie(name);

            cookie.Value = value;
            cookie.Expires = DateTime.Now.AddSeconds(expires);
            Response.AppendCookie(cookie);
        }

        /// <summary>读取Cookie</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static String ReadCookie(String name)
        {
            var cookies = Request.Cookies;
            if (cookies == null) return null;
            if (cookies[name] == null) return "";

            return cookies[name].Value + "";
        }

        /// <summary>读取Cookie</summary>
        /// <param name="name">名称</param>
        /// <param name="key">键</param>
        /// <returns></returns>
        public static String ReadCookie(String name, String key)
        {
            var cookies = Request.Cookies;
            if (cookies == null) return null;
            if (cookies[name] == null || cookies[name][key] == null) return "";

            return cookies[name][key] + "";
        }
        #endregion

        #region Url扩展
        /// <summary>追加Url参数，默认空时加问号，否则加与符号</summary>
        /// <param name="sb"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static StringBuilder UrlParam(this StringBuilder sb, String str)
        {
            if (str.IsNullOrWhiteSpace()) return sb;

            if (sb.Length == 0)
                sb.Append("?");
            else
                sb.Append("&");

            sb.Append(str);

            return sb;
        }

        /// <summary>追加Url参数，默认空时加问号，否则加与符号</summary>
        /// <param name="sb"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static StringBuilder UrlParam(this StringBuilder sb, String name, Object value)
        {
            if (name.IsNullOrWhiteSpace()) return sb;

            // 必须注意，value可能是时间类型
            return UrlParam(sb, "{0}={1}".F(name, value));
        }
        #endregion
    }
}