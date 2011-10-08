using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace NewLife.Web
{
    /// <summary>
    /// 网页工具类
    /// </summary>
    public static class WebHelper
    {
        static Page Page { get { return HttpContext.Current.Handler as Page; } }

        #region 辅助
        /// <summary>
        /// 输出脚本
        /// </summary>
        /// <param name="script"></param>
        public static void WriteScript(String script)
        {
            HttpContext.Current.Response.Write(String.Format("<script type=\"text/javascript\">\n{0}\n</script>", script));
        }

        /// <summary>
        /// 按字节截取
        /// </summary>
        /// <param name="Str">字符串</param>
        /// <param name="StartIndex">开始位置</param>
        /// <param name="Len">长度</param>
        /// <returns></returns>
        public static String GetSubString(String Str, Int32 StartIndex, Int32 Len)
        {
            int j = 0;
            Int32 RLength = 0;
            Int32 SIndex = 0;
            char[] arr = Str.ToCharArray();
            for (int i = 0; i < arr.Length; i++)
            {
                j += (arr[i] > 0 && arr[i] < 255) ? 1 : 2;
                if (j <= StartIndex)
                    SIndex++;
                else
                {
                    if (j > Len + StartIndex) break;
                    RLength++;
                }

            }

            return RLength >= Str.Length ? Str : Str.Substring(StartIndex, RLength);
        }
        #endregion

        #region 弹出信息
        /// <summary>
        /// Js脚本编码
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String JsEncode(String str)
        {
            if (String.IsNullOrEmpty(str)) return null;

            str = str.Replace(@"\", @"\\");
            str = str.Replace("'", @"\'");
            str = str.Replace(Environment.NewLine, @"\n");
            str = str.Replace("\r", @"\n");
            str = str.Replace("\n", @"\n");

            return str;
        }

        /// <summary>
        /// 弹出页面提示
        /// </summary>
        /// <param name="msg"></param>
        public static void Alert(String msg)
        {
            Page.ClientScript.RegisterStartupScript(Page.GetType(), "alert", "alert('" + JsEncode(msg) + "');", true);
        }

        /// <summary>
        /// 弹出页面提示并停止输出后退一步！
        /// </summary>
        /// <param name="msg"></param>
        public static void AlertAndEnd(String msg)
        {
            WriteScript("alert('" + JsEncode(msg) + "');history.go(-1);");
            HttpContext.Current.Response.End();
        }

        /// <summary>
        /// 弹出页面提示，并刷新该页面
        /// </summary>
        /// <param name="msg"></param>
        public static void AlertAndRefresh(String msg)
        {
            //Page.ClientScript.RegisterStartupScript(Page.GetType(), "alert", "alert('" + msg + "');location.href = location.href;", true);

            WriteScript("alert('" + JsEncode(msg) + "');location.href = location.href;");
            HttpContext.Current.Response.End();
        }

        /// <summary>
        /// 弹出页面提示并重定向到另外的页面
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="url"></param>
        public static void AlertAndRedirect(String msg, String url)
        {
            if (!url.Contains("?"))
                url += "?";
            else
                url += "&";

            url += "rnd=";
            url += DateTime.Now.Ticks.ToString();

            //Page.ClientScript.RegisterStartupScript(Page.GetType(), "alert", "alert('" + msg + "');location.href = '" + url + "';", true);

            WriteScript("alert('" + JsEncode(msg) + "');location.href = '" + url + "';");
            HttpContext.Current.Response.End();
        }

        /// <summary>
        /// 弹出页面提示并关闭当前页面
        /// </summary>
        /// <param name="msg"></param>
        public static void AlertAndClose(String msg)
        {
            WriteScript("alert('" + JsEncode(msg) + "');window.close();");
            HttpContext.Current.Response.End();
        }
        #endregion

        #region 输入检查
        /// <summary>
        /// 检查控件值是否为空，若为空，显示错误信息，并聚焦到控件上
        /// </summary>
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
                throw new Exception(String.Format("暂时不支持{0}控件！", control.GetType()));

            control.Focus();
            if (!String.IsNullOrEmpty(errmsg)) Alert(errmsg);
            return false;
        }
        #endregion

        #region 用户主机
        /// <summary>
        /// 用户主机
        /// </summary>
        public static String UserHost
        {
            get
            {
                if (HttpContext.Current != null)
                {
                    String str = (String)HttpContext.Current.Items["UserHostAddress"];
                    if (!String.IsNullOrEmpty(str)) return str;

                    if (HttpContext.Current.Request != null)
                    {
                        str = HttpContext.Current.Request.UserHostName;
                        if (String.IsNullOrEmpty(str)) str = HttpContext.Current.Request.UserHostAddress;
                        HttpContext.Current.Items["UserHostAddress"] = str;
                        return str;
                    }
                }
                return null;
            }
        }
        #endregion

        #region 导出Excel
        /// <summary>
        /// 导出Excel
        /// </summary>
        /// <param name="gv"></param>
        /// <param name="filename"></param>
        /// <param name="max"></param>
        public static void ExportExcel(GridView gv, String filename, Int32 max)
        {
            ExportExcel(gv, filename, max, Encoding.Default);
        }

        /// <summary>
        /// 导出Excel
        /// </summary>
        /// <param name="gv"></param>
        /// <param name="filename"></param>
        /// <param name="max"></param>
        /// <param name="encoding"></param>
        public static void ExportExcel(GridView gv, String filename, Int32 max, Encoding encoding)
        {
            HttpResponse Response = HttpContext.Current.Response;

            //去掉所有列的排序
            foreach (DataControlField item in gv.Columns)
            {
                if (item is DataControlField) (item as DataControlField).SortExpression = null;
            }
            if (max > 0) gv.PageSize = max;
            gv.DataBind();

            // 新建页面
            Page page = new Page();
            HtmlForm form = new HtmlForm();

            page.EnableEventValidation = false;
            page.Controls.Add(form);
            form.Controls.Add(gv);

            Response.Clear();
            Response.Buffer = true;
            Response.Charset = encoding.WebName;
            Response.ContentEncoding = encoding;
            Response.AppendHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(filename, encoding));
            Response.ContentType = "application/ms-excel";

            StringWriter sw = new StringWriter();
            HtmlTextWriter htw = new HtmlTextWriter(sw);
            page.RenderControl(htw);

            String html = sw.ToString();
            //if (html.StartsWith("<div>")) html = html.SubString("<div>".Length);
            //if (html.EndsWith("</div>")) html = html.SubString(0, html.Length - "</div>".Length);

            html = String.Format("<meta http-equiv=\"content-type\" content=\"application/ms-excel; charset={0}\"/>", encoding.WebName) + Environment.NewLine + html;

            Response.Output.Write(html);
            Response.Flush();
            Response.End();
        }
        #endregion

        #region 请求相关
        /// <summary>
        /// 获取整型参数
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Int32 RequestInt(String name)
        {
            String str = HttpContext.Current.Request[name];
            if (String.IsNullOrEmpty(str)) return 0;

            Int32 n = 0;
            if (!Int32.TryParse(str, out n)) n = 0;

            return n;
        }

        /// <summary>
        /// 接收布尔值
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool RequestBool(String name)
        {
            return ConvertBool(HttpContext.Current.Request[name]);
        }

        /// <summary>
        /// 接收时间
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DateTime RequestDateTime(String name)
        {
            return ConvertDateTime(HttpContext.Current.Request[name]);
        }

        /// <summary>
        /// 接收Double
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Double RequestDouble(String name)
        {
            return ConvertDouble(HttpContext.Current.Request[name]);
        }

        /// <summary>
        /// 字符转换为数字
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static Int32 ConvertInt(String val)
        {
            Int32 r = 0;
            if (String.IsNullOrEmpty(val)) return r;
            Int32.TryParse(val, out r);
            return r;
        }

        /// <summary>
        /// 字符转换为布尔
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool ConvertBool(String val)
        {
            bool r = false;
            if (String.IsNullOrEmpty(val)) return r;

            String trimVal = val.Trim();

            if ("True".Equals(trimVal, StringComparison.OrdinalIgnoreCase) || "1".Equals(trimVal))
            {
                return true;
            }
            else if ("False".Equals(trimVal, StringComparison.OrdinalIgnoreCase) || "0".Equals(trimVal))
            {
                return false;
            }

            return r;
        }

        /// <summary>
        /// 字符转换为时间
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static DateTime ConvertDateTime(String val)
        {
            DateTime r = DateTime.MinValue;
            if (String.IsNullOrEmpty(val)) return r;
            DateTime.TryParse(val, out r);
            return r;
        }

        /// <summary>
        /// 字符转换
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static Double ConvertDouble(String val)
        {
            Double r = 0;
            if (String.IsNullOrEmpty(val)) return r;
            Double.TryParse(val, out r);
            return r;
        }
        #endregion
    }
}