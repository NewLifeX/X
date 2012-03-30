using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Web;
using System.Web.UI;

namespace XControl
{
    /// <summary>脚本助手</summary>>
    public class ScriptHelper
    {
        #region 当前页面
        private Page _page = null;

        /// <summary>当前页面</summary>>        
        public Page page
        {
            get
            {
                if (_page == null)
                {
                    _page = HttpContext.Current.Handler as Page;
                } return _page;
            }
        }
        #endregion

        /// <summary>向当前页面注册客户端脚本</summary>>
        /// <param name="script"></param>
        public void RegisterScript(string script)
        {
            RegisterScript(Guid.NewGuid().ToString(), script);
        }

        /// <summary>向当前页面注册客户端脚本</summary>>
        /// <param name="key"></param>
        /// <param name="script"></param>
        public void RegisterScript(string key, string script)
        {
            page.ClientScript.RegisterStartupScript(page.GetType(), key, script, true);
        }

        /// <summary>
        /// 向页面注册HiddenField控件
        /// add by Vincent.Q 11.01.27
        /// </summary>
        /// <param name="as_hfieldid"></param>
        /// <param name="as_value"></param>
        public void RegisterHiddenField(string as_hfieldid, string as_value)
        {
            page.ClientScript.RegisterHiddenField(as_hfieldid, as_value);
        }

        /// <summary>使用键和 URL 向 System.Web.UI.Page 对象注册客户端脚本。</summary>>
        /// <param name="key"></param>
        /// <param name="url"></param>
        /// <param name="ieVer"></param>
        /// <param name="defer"></param>
        public void RegisterInclude(string key, string url, int ieVer, bool defer)
        {
            if (!page.ClientScript.IsClientScriptBlockRegistered(key.ToUpper()) && !string.IsNullOrEmpty(url))
            {
                url = page.ResolveUrl(url) + "?ver=" + new FileInfo(page.Server.MapPath(url)).LastWriteTime.Ticks.ToString();
                string js = "<script type=\"text/javascript\" src=\"" + url + "\" " + (defer ? "defer" : "") + "></script>";
                if (ieVer > 0)
                {
                    js = "<!–[if IE " + ieVer + "]>" + js + "<![endif]–>";
                }
                page.ClientScript.RegisterClientScriptBlock(page.GetType(), key.ToUpper(), js, false);
            }
        }

        /// <summary>去除换行和多余空白字符</summary>>
        /// <param name="str"></param>
        /// <returns></returns>
        public string Compressed(string str)
        {
            str = System.Text.RegularExpressions.Regex.Replace(str, "\r\n|[ ]{3,}", "");
            return str;
        }
    }
}
