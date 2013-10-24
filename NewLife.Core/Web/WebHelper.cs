using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using NewLife.Exceptions;

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
            var Request = HttpContext.Current.Request;
            var Response = HttpContext.Current.Response;

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

        #region 请求相关
        /// <summary>获取整型参数</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Int32 RequestInt(String name)
        {
            String str = HttpContext.Current.Request[name];
            if (String.IsNullOrEmpty(str)) return 0;

            Int32 n = 0;
            if (!Int32.TryParse(str, out n)) n = 0;

            return n;
        }

        /// <summary>接收布尔值</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static bool RequestBool(String name)
        {
            return ConvertBool(HttpContext.Current.Request[name]);
        }

        /// <summary>接收时间</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static DateTime RequestDateTime(String name)
        {
            return ConvertDateTime(HttpContext.Current.Request[name]);
        }

        /// <summary>接收Double</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Double RequestDouble(String name)
        {
            return ConvertDouble(HttpContext.Current.Request[name]);
        }

        /// <summary>字符转换为数字</summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static Int32 ConvertInt(String val)
        {
            Int32 r = 0;
            if (String.IsNullOrEmpty(val)) return r;
            Int32.TryParse(val, out r);
            return r;
        }

        /// <summary>字符转换为布尔</summary>
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

        /// <summary>字符转换为时间</summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static DateTime ConvertDateTime(String val)
        {
            DateTime r = DateTime.MinValue;
            if (String.IsNullOrEmpty(val)) return r;
            DateTime.TryParse(val, out r);
            return r;
        }

        /// <summary>字符转换</summary>
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