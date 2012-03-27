using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using System.ComponentModel;
using System.Web.UI;
using System.Drawing;

namespace XControl
{
    /// <summary>
    /// 弹出窗口
    /// </summary>
    [Description("弹出窗口控件")]
    [ToolboxData("<{0}:LinkBox runat=server></{0}:LinkBox>")]
    [ToolboxBitmap(typeof(LinkButton))]
    public class LinkBox : LinkButton
    {
        #region 属性
        /// <summary>标题</summary>
        [DefaultValue(""), Themeable(false), Category(" "), Description("标题")]
        public String Title
        {
            get
            {
                String str = (String)ViewState["Title"];
                if (str == null) return Text;

                return str;
            }
            set
            {
                ViewState["Title"] = value;
            }
        }

        /// <summary>字符串</summary>
        public String Url
        {
            get
            {
                return (String)ViewState["Url"];
            }
            set
            {
                ViewState["Url"] = value;
            }
        }

        /// <summary>宽度</summary>
        [DefaultValue(null), Themeable(false), Category(" "), Description("宽度")]
        public Unit BoxWidth
        {
            get
            {
                Object obj = ViewState["BoxWidth"];
                return obj == null ? Unit.Empty : (Unit)obj;
            }
            set
            {
                ViewState["BoxWidth"] = value;
            }
        }

        /// <summary>高度</summary>
        [DefaultValue(null), Themeable(false), Category(" "), Description("高度")]
        public Unit BoxHeight
        {
            get
            {
                Object obj = ViewState["BoxHeight"];
                return obj == null ? Unit.Empty : (Unit)obj;
            }
            set
            {
                ViewState["BoxHeight"] = value;
            }
        }

        /// <summary>是否显示信息行</summary>
        [DefaultValue(false), Themeable(false), Category(" "), Description("是否显示信息行")]
        public Boolean ShowMessageRow
        {
            get
            {
                Object obj = ViewState["ShowMessageRow"];
                return obj == null ? false : (Boolean)obj;
            }
            set
            {
                ViewState["ShowMessageRow"] = value;
            }
        }

        /// <summary>消息标题</summary>
        [DefaultValue(""), Themeable(false), Category(" "), Description("消息标题")]
        public String MessageTitle
        {
            get
            {
                return (String)ViewState["MessageTitle"];
            }
            set
            {
                ViewState["MessageTitle"] = value;
            }
        }

        /// <summary>消息</summary>
        [DefaultValue(""), Themeable(false), Category(" "), Description("消息")]
        public String Message
        {
            get
            {
                String str = (String)ViewState["Message"];
                if (str == null) return String.Empty;

                return str;
            }
            set
            {
                ViewState["Message"] = value;
            }
        }

        /// <summary>是否显示按钮行</summary>
        [DefaultValue(false), Themeable(false), Category(" "), Description("是否显示按钮行")]
        public Boolean ShowButtonRow
        {
            get
            {
                Object obj = ViewState["ShowButtonRow"];
                return obj == null ? false : (Boolean)obj;
            }
            set
            {
                ViewState["ShowButtonRow"] = value;
            }
        }

        /// <summary>左侧图标</summary>
        [DefaultValue(""), Themeable(false), Category(" "), Description("左侧图标")]
        public String IconLeft
        {
            get
            {
                return (String)ViewState["IconLeft"];
            }
            set
            {
                ViewState["IconLeft"] = value;
            }
        }

        /// <summary>右侧图标</summary>
        [DefaultValue(""), Themeable(false), Category(" "), Description("右侧图标")]
        public String IconRight
        {
            get
            {
                return (String)ViewState["IconRight"];
            }
            set
            {
                ViewState["IconRight"] = value;
            }
        }

        /// <summary>显示前参数添加</summary>
        [DefaultValue(""), Themeable(false), Category(" "), Description("显示前利用js参数添加（参数实际为function类型）")]
        public String BeforeShow
        {
            get
            {
                return (String)ViewState["BeforeShow"];
            }
            set
            {
                ViewState["BeforeShow"] = value;
            }
        }
        #endregion

        void UpdateOnClientClick()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ID:'win{0}', ", new Random((Int32)DateTime.Now.Ticks).Next(1, 1000));
            sb.AppendFormat("Title:'{0}', ", Title);

            String url = Url;
            if (!String.IsNullOrEmpty(url) && !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                url = ResolveUrl(url);
            sb.AppendFormat("URL:'{0}', ", url);
            if (BoxWidth != Unit.Empty) sb.AppendFormat("Width:{0}, ", (Int32)BoxWidth.Value);
            if (BoxHeight != Unit.Empty) sb.AppendFormat("Height:{0}, ", (Int32)BoxHeight.Value);
            sb.AppendFormat("ShowMessageRow:{0}, ", ShowMessageRow.ToString().ToLower());
            sb.AppendFormat("MessageTitle:'{0}', ", MessageTitle);
            sb.AppendFormat("Message:'{0}', ", Message);
            sb.AppendFormat("ShowButtonRow:{0},", ShowButtonRow.ToString().ToLower());
            if (!string.IsNullOrEmpty(BeforeShow)) sb.AppendFormat("BeforeShow:{0}", BeforeShow);
            
            OnClientClick = "ShowDialog({" + sb.ToString() + "}); return false;";

            RegisterReloadFormJs(Page.ClientScript, Page.IsPostBack);
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            UpdateOnClientClick();

            base.OnPreRender(e);

            Page.ClientScript.RegisterClientScriptResource(typeof(BoxControl), "XControl.Box.Box.js");
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="writer"></param>
        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (!String.IsNullOrEmpty(IconLeft)) writer.Write("<img src=\"{0}\" style=\"border:none;\" />", ResolveUrl(IconLeft));
            base.RenderContents(writer);
            if (!String.IsNullOrEmpty(IconRight)) writer.Write("<img src=\"{0}\" style=\"border:none;\" />", ResolveUrl(IconRight));
        }
        /// <summary>
        /// 返回默认的reloadForm实现,配合Box.js中的Dialog.CloseAndRefresh
        /// </summary>
        /// <returns></returns>
        public static bool RegisterReloadFormJs(ClientScriptManager script, bool isPostback)
        {
            if (!script.IsClientScriptBlockRegistered(typeof(LinkBox), "ReloadFormJs"))
            {
                script.RegisterClientScriptBlock(typeof(LinkBox), "ReloadFormJs", Helper.JsMinSimple(!XControlConfig.Debug, @"
function reloadForm(){
    if(!" + ("" + isPostback).ToLower() + @"){
        location.reload();
        return true;
    }
    var buttons = document.getElementsByTagName('input');
    for(var i=0;i<buttons.length;i++){
        var ele = buttons[i];
        if((ele.type === 'submit' || ele.type === 'button') && ele.value === '查询'){
            if(ele.click){
                ele.click();
            }else if(document.createEvent && ele.dispatchEvent){
                var event = document.createEvent('MouseEvent');
                event.initMouseEvent('click', true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);
                ele.dispatchEvent(event);
            }else{
                break;
            }
            return true;
        }
    }
    return false;
}"), true);
                return true;
            }
            return false;
        }
    }
}
