using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using System.Web.UI;
using System.ComponentModel;
using System.Drawing.Design;


// 特别要注意，这里得加上默认命名空间和目录名，因为vs2005编译的时候会给文件加上这些东东的
[assembly: WebResource("XControl.Box.Box.js", "text/javascript", PerformSubstitution = true)]
[assembly: WebResource("XControl.Box.Dialog.dialog_bg.jpg", "image/jpg")]
[assembly: WebResource("XControl.Box.Dialog.dialog_cb.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_closebtn.gif", "image/gif")]
[assembly: WebResource("XControl.Box.Dialog.dialog_closebtn.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_closebtn_over.gif", "image/gif")]
[assembly: WebResource("XControl.Box.Dialog.dialog_closebtn_over.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_ct.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_footercenter.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_footerleft.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_footerright.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_lb.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_lt.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_mc.gif", "image/gif")]
[assembly: WebResource("XControl.Box.Dialog.dialog_mc.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_mlb.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_mlm.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_mlt.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_mrb.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_mrm.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_mrt.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_rb.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.dialog_rt.png", "image/png")]
[assembly: WebResource("XControl.Box.Dialog.icon_alert.gif", "image/gif")]
[assembly: WebResource("XControl.Box.Dialog.icon_dialog.gif", "image/gif")]
[assembly: WebResource("XControl.Box.Dialog.icon_query.gif", "image/gif")]
[assembly: WebResource("XControl.Box.Dialog.window.gif", "image/gif")]

namespace XControl
{
    /// <summary>
    /// 链接弹出框字段
    /// </summary>
    public class LinkBoxField : LinkButtonField
    {
        #region 属性
        ///// <summary>标识</summary>
        //public String ID
        //{
        //    get
        //    {
        //        String str = (String)ViewState["ID"];
        //        if (str == null) return String.Empty;

        //        return str;
        //    }
        //    set
        //    {
        //        ViewState["ID"] = value;
        //    }
        //}

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

        private String _Url;
        /// <summary>字符串</summary>
        public String Url
        {
            get { return _Url ?? NavigateUrl; }
            set { _Url = value; }
        }

        /// <summary>宽度</summary>
        [DefaultValue(null), Themeable(false), Category(" "), Description("宽度")]
        public Unit Width
        {
            get
            {
                Object obj = ViewState["Width"];
                return obj == null ? Unit.Empty : (Unit)obj;
            }
            set
            {
                ViewState["Width"] = value;
            }
        }

        /// <summary>高度</summary>
        [DefaultValue(null), Themeable(false), Category(" "), Description("高度")]
        public Unit Height
        {
            get
            {
                Object obj = ViewState["Height"];
                return obj == null ? Unit.Empty : (Unit)obj;
            }
            set
            {
                ViewState["Height"] = value;
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
        #endregion

        /// <summary>
        /// 创建字段
        /// </summary>
        /// <returns></returns>
        protected override DataControlField CreateField()
        {
            return new LinkBoxField();
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="newField"></param>
        protected override void CopyProperties(DataControlField newField)
        {
            LinkBoxField link = newField as LinkBoxField;

            link.Title = this.Title;
            link.Width = this.Width;
            link.Height = this.Height;
            link.ShowMessageRow = this.ShowMessageRow;
            link.MessageTitle = this.MessageTitle;
            link.Message = this.Message;
            link.ShowButtonRow = this.ShowButtonRow;

            base.CopyProperties(newField);
        }

        void UpdateOnClientClick()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ID:'win{0}', ", new Random((Int32)DateTime.Now.Ticks).Next(1, 1000));
            sb.AppendFormat("Title:'{0}', ", Title);

            String url = Url;
            if (!url.StartsWith("http://", StringComparison.Ordinal))
                url = Control.ResolveUrl(url);
            sb.AppendFormat("URL:'{0}', ", url);
            if (Width != Unit.Empty) sb.AppendFormat("Width:{0}, ", (Int32)Width.Value);
            if (Height != Unit.Empty) sb.AppendFormat("Height:{0}, ", (Int32)Height.Value);
            sb.AppendFormat("ShowMessageRow:{0}, ", ShowMessageRow.ToString().ToLower());
            sb.AppendFormat("MessageTitle:'{0}', ", MessageTitle);
            sb.AppendFormat("Message:'{0}', ", Message);
            sb.AppendFormat("ShowButtonRow:{0}", ShowButtonRow.ToString().ToLower());

            OnClientClick = "ShowDialog({" + sb.ToString() + "}); return false;";
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="link"></param>
        protected override void InitializeControl(HyperLink link)
        {
            UpdateOnClientClick();

            base.InitializeControl(link);
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="link"></param>
        protected override void OnPreRender(HyperLink link)
        {
            Url = link.NavigateUrl;
            UpdateOnClientClick();

            base.OnPreRender(link);

            link.Page.ClientScript.RegisterClientScriptResource(typeof(BoxControl), "XControl.Box.Box.js");
        }
    }
}