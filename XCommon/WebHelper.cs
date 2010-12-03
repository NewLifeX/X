using System;
using System.Web;
using System.Web.UI;
using System.Text;
using System.IO;
using System.Web.UI.WebControls;

namespace XCommon
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
        /// 弹出页面提示
        /// </summary>
        /// <param name="msg"></param>
        public static void Alert(String msg)
        {
            Page.ClientScript.RegisterStartupScript(Page.GetType(), "alert", "alert('" + msg + "');", true);
        }

        /// <summary>
        /// 弹出页面提示并停止输出后退一步！
        /// </summary>
        /// <param name="msg"></param>
        public static void AlertAndEnd(String msg)
        {
            WriteScript("alert('" + msg + "');history.go(-1);");
            HttpContext.Current.Response.End();
        }

        /// <summary>
        /// 弹出页面提示，并刷新该页面
        /// </summary>
        /// <param name="msg"></param>
        public static void AlertAndRefresh(String msg)
        {
            //Page.ClientScript.RegisterStartupScript(Page.GetType(), "alert", "alert('" + msg + "');location.href = location.href;", true);

            WriteScript("alert('" + msg + "');location.href = location.href;");
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

            WriteScript("alert('" + msg + "');location.href = '" + url + "';");
            HttpContext.Current.Response.End();
        }

        /// <summary>
        /// 弹出页面提示并关闭当前页面
        /// </summary>
        /// <param name="msg"></param>
        public static void AlertAndClose(String msg)
        {
            WriteScript("alert('" + msg + "');window.close();");
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
                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                {
                    String str = HttpContext.Current.Request.UserHostName;
                    if (String.IsNullOrEmpty(str)) str = HttpContext.Current.Request.UserHostAddress;
                    return str;
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
            HttpResponse Response = HttpContext.Current.Response;

            //去掉所有列的排序
            foreach (DataControlField item in gv.Columns)
            {
                if (item is DataControlField) (item as DataControlField).SortExpression = null;
            }
            if (max > 0) gv.PageSize = max;
            gv.DataBind();

            Response.Clear();
            Response.Buffer = true;
            Response.Charset = "GB2312";
            // 如果设置为 GetEncoding("GB2312");导出的文件将会出现乱码！！！
            Response.ContentEncoding = Encoding.Default;
            Response.AppendHeader("Content-Disposition", "attachment;filename=" + filename);
            Response.ContentType = "application/ms-excel";//设置输出文件类型为excel文件。 
            StringWriter oStringWriter = new StringWriter();
            HtmlTextWriter oHtmlTextWriter = new HtmlTextWriter(oStringWriter);
            gv.RenderControl(oHtmlTextWriter);
            String html = oStringWriter.ToString();
            //if (html.StartsWith("<div>")) html = html.SubString("<div>".Length);
            //if (html.EndsWith("</div>")) html = html.SubString(0, html.Length - "</div>".Length);
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