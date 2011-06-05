using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

// 特别要注意，这里得加上默认命名空间和目录名，因为vs2005编译的时候会给文件加上这些东东的
[assembly: WebResource("XControl.Button.choose.gif", "image/gif")]
[assembly: WebResource("XControl.Button.choose.js", "text/javascript")]
[assembly: WebResource("XControl.Button.choose.css", "text/css", PerformSubstitution = true)]

namespace XControl
{
    /// <summary>
    /// 选择输入控件
    /// </summary>
    [Description("选择输入控件")]
    [ToolboxData("<{0}:ChooseButton runat=server></{0}:ChooseButton>")]
    [ToolboxBitmap(typeof(Button))]
    [ControlValueProperty("Value")]
    [DefaultProperty("Text"), ValidationProperty("Text"), DefaultEvent("ValueChanged")]
    public class ChooseButton : CompositeControl, /*IPostBackDataHandler, IEditableTextControl,*/ ITextControl
    {
        #region 属性
        /// <summary>
        /// 文本
        /// </summary>
        [Bindable(true)]
        [Category(" 专用属性"), DefaultValue(null), Description("文本")]
        public String Text
        {
            get
            {
                if (BtnControl != null) return BtnControl.Text;

                return null;
            }
            set
            {
                if (BtnControl != null) BtnControl.Text = value;
            }
        }

        /// <summary>
        /// 值
        /// </summary>
        [Bindable(true)]
        [Category(" 专用属性"), DefaultValue(null), Description("值")]
        public String Value
        {
            get
            {
                //return (String)ViewState["Value"];

                if (HiddenControl != null) return HiddenControl.Value;

                return null;
            }
            set
            {
                //ViewState["Value"] = value;

                if (HiddenControl != null) HiddenControl.Value = value;
            }
        }

        /// <summary>
        /// 选择页地址
        /// </summary>
        [Bindable(false)]
        [Category(" 专用属性"), DefaultValue(null), Description("选择页地址")]
        public String Url
        {
            get
            {
                return (String)ViewState["Url"];
            }
            set
            {
                _ProcessedUrl = null;
                ViewState["Url"] = value;
            }
        }
        private string _ProcessedUrl;
        /// <summary>
        /// 返回处理过~/的url地址
        /// </summary>
        internal string ProcessedUrl
        {
            get
            {
                if (_ProcessedUrl == null)
                {
                    string url = Url;
                    if (url[0] == '~' && url[1] == '/')
                    {
                        _ProcessedUrl = Page.ResolveUrl(url);
                    }
                    else
                    {
                        _ProcessedUrl = url;
                    }
                }
                return _ProcessedUrl;
            }
        }

        /// <summary>
        /// 控件ID
        /// </summary>
        [Bindable(false)]
        [IDReferenceProperty(typeof(Control))]
        [Category(" 专用属性"), DefaultValue(null), Description("控件ID")]
        public String ControlID
        {
            get
            {
                return (String)ViewState["ControlID"];
            }
            set
            {
                ViewState["ControlID"] = value;
            }
        }

        /// <summary>
        /// 自动回发
        /// </summary>
        [Themeable(false), WebCategory("Behavior"), WebSysDescription("TextBox_AutoPostBack"), DefaultValue(false)]
        public virtual bool AutoPostBack
        {
            get
            {
                object obj2 = this.ViewState["AutoPostBack"];
                return ((obj2 != null) && ((bool)obj2));
            }
            set
            {
                this.ViewState["AutoPostBack"] = value;
            }
        }

        /// <summary>
        /// 弹出的模式窗口选项
        /// </summary>
        [Category(" 专用属性"), DefaultValue(""), Description("弹出的模式窗口选项")]
        public string ModalDialogOptions
        {
            get
            {
                return (string)ViewState["ModelDialogOptions"];
            }
            set
            {
                ViewState["ModelDialogOptions"] = value;
            }

        }
        /// <summary>
        /// 扩展的客户端选项
        /// </summary>
        [Category(" 专用属性"), DefaultValue(""), Description("扩展的客户端选项")]
        public string ExtraClientOptions
        {
            get
            {
                return (string)ViewState["ExtraClientOptions"];
            }
            set
            {
                ViewState["ExtraClientOptions"] = value;
            }
        }
        /// <summary>
        /// <see cref="System.Web.UI.Control.ClientID"/>
        /// </summary>
        public override string ClientID
        {
            get
            {
                return BtnControl.ClientID;
            }
        }
        #endregion

        #region 初始化
        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="e"></param>
        //protected override void OnInit(EventArgs e)
        //{
        //    base.OnInit(e);

        //    //BackColor = Color.FromArgb(0xE7, 0xE7, 0xE7);
        //    Style.Add(HtmlTextWriterStyle.BackgroundColor, "0xE7E7E7");
        //    Style.Add(HtmlTextWriterStyle.BackgroundImage, Page.ClientScript.GetWebResourceUrl(this.GetType(), "XControl.Button.choose.gif"));

        //    BorderWidth = Unit.Pixel(0);
        //    BorderStyle = BorderStyle.Solid;
        //    //BorderColor = Color.FromArgb(0xFF, 0xFF, 0xFF);
        //    Style.Add(HtmlTextWriterStyle.BorderColor, "0xFFFFFF");

        //    //ForeColor = Color.FromArgb(0x33, 0x33, 0x33);
        //    Style.Add(HtmlTextWriterStyle.Color, "0x333333");
        //    this.Font.Size = FontUnit.Point(12);

        //    Style.Add(HtmlTextWriterStyle.Cursor, "pointer");

        //    Height = Unit.Pixel(25);
        //    Width = Unit.Pixel(128);
        //    Style.Add(HtmlTextWriterStyle.MarginTop, "5px");
        //}
        #endregion

        #region 子控件
        //private Button btnControl;
        //private HiddenField hiddenControl;

        private Button _BtnControl;
        /// <summary>按钮</summary>
        public Button BtnControl
        {
            get
            {
                if (_BtnControl == null) EnsureChildControls();
                return _BtnControl;
            }
        }

        private HiddenField _HiddenControl;
        /// <summary>隐藏域</summary>
        public HiddenField HiddenControl
        {
            get
            {
                if (_HiddenControl == null) EnsureChildControls();
                return _HiddenControl;
            }
        }

        /// <summary>
        /// 创建一个隐藏子控件
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            if (_BtnControl == null)
            {
                _BtnControl = new Button();
                ////BackColor = Color.FromArgb(0xE7, 0xE7, 0xE7);
                //_BtnControl.Style.Add(HtmlTextWriterStyle.BackgroundColor, "0xE7E7E7");
                ////_BtnControl.Style.Add(HtmlTextWriterStyle.BackgroundImage, Page.ClientScript.GetWebResourceUrl(this.GetType(), "XControl.Button.choose.gif"));

                //_BtnControl.BorderWidth = Unit.Pixel(0);
                //_BtnControl.BorderStyle = BorderStyle.Solid;
                ////BorderColor = Color.FromArgb(0xFF, 0xFF, 0xFF);
                //_BtnControl.Style.Add(HtmlTextWriterStyle.BorderColor, "0xFFFFFF");

                ////ForeColor = Color.FromArgb(0x33, 0x33, 0x33);
                //_BtnControl.Style.Add(HtmlTextWriterStyle.Color, "0x333333");
                //_BtnControl.Font.Size = FontUnit.Point(12);

                //_BtnControl.Style.Add(HtmlTextWriterStyle.Cursor, "pointer");

                //_BtnControl.Height = Unit.Pixel(25);
                //_BtnControl.Width = Unit.Pixel(128);
                //_BtnControl.Style.Add(HtmlTextWriterStyle.MarginTop, "5px");
                _BtnControl.ID = "ChooseText";
                Controls.Add(_BtnControl);
            }

            if (_HiddenControl == null)
            {
                _HiddenControl = new HiddenField();
                _HiddenControl.ID = "ChooseValue";
                _HiddenControl.Value = Value;
                _HiddenControl.ValueChanged += new EventHandler(OnValueChanged);
                Controls.Add(_HiddenControl);
            }
        }
        #endregion

        #region 呈现
        /// <summary>
        /// 预呈现。输出脚本
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            //EnsureChildControls();

            base.OnPreRender(e);

            // 输出控制脚本
            Page.ClientScript.RegisterClientScriptResource(this.GetType(), "XControl.Button.choose.js");

            // 输出默认样式
            if (String.IsNullOrEmpty(BtnControl.CssClass) || BtnControl.CssClass == "choose")
            {
                try
                {
                    //Page.ClientScript.RegisterClientScriptResource(this.GetType(), "XControl.Button.choose.css");
                    HtmlLink link = new HtmlLink();
                    link.Href = Page.ClientScript.GetWebResourceUrl(this.GetType(), "XControl.Button.choose.css");
                    link.Attributes["rel"] = "stylesheet";
                    link.Attributes["type"] = "text/css";
                    Page.Header.Controls.Add(link);

                    BtnControl.CssClass = "choose";
                }
                catch { }
            }

            BtnControl.Attributes.Add("val", HiddenControl.ClientID);

            string modalDialogOpts = !string.IsNullOrEmpty(ModalDialogOptions) ? "{" + ModalDialogOptions + "}" : "null";
            string extraClientOpts = !string.IsNullOrEmpty(ExtraClientOptions) ? "{" + ExtraClientOptions + "}" : (AutoPostBack ? "{after:'__doPostBack(\\\'" + ClientID + "\\\', \\\'\\\')'}" : "null");

            string otherClientClick = "return false;";
            //if (!String.IsNullOrEmpty(BtnControl.OnClientClick))
            //{
            //    otherClientClick = BtnControl.OnClientClick;
            //}

            // 由于Button控件将OnClientClick值保存到ViewState,所以在post之后,OnClientClick属性值会恢复
            // 所以这里不需要考虑保留旧值,并且ChooseButton控件没提供OnClientClick属性,外部也无法访问到Button控件的OnClientClick
            // 原有代码会在post一次之后反复叠加Choose()的js调用
            BtnControl.OnClientClick = string.Format("Choose(this,'{0}',{1},{2});{3}", ProcessedUrl, modalDialogOpts, extraClientOpts, otherClientClick);

            //if (String.IsNullOrEmpty(BtnControl.OnClientClick))
            //    BtnControl.OnClientClick = "Choose(this,'" + Url + "');return false;";
            //else
            //    BtnControl.OnClientClick = "Choose(this,'" + Url + "');" + BtnControl.OnClientClick;
        }

        /// <summary>
        /// 已重写。忽略外部标签
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            //base.RenderBeginTag(writer);
        }

        /// <summary>
        /// 已重写。忽略外部标签
        /// </summary>
        /// <param name="writer"></param>
        public override void RenderEndTag(HtmlTextWriter writer)
        {
            //base.RenderEndTag(writer);
        }
        #endregion

        #region 回发事件处理
        private static readonly object EventValueChanged = new object();
        /// <summary>
        /// 值改变时触发
        /// </summary>
        [WebSysDescription("HiddenField_OnValueChanged"), WebCategory("Action")]
        public event EventHandler ValueChanged
        {
            add
            {
                base.Events.AddHandler(EventValueChanged, value);
            }
            remove
            {
                base.Events.RemoveHandler(EventValueChanged, value);
            }
        }

        void OnValueChanged(Object sender, EventArgs e)
        {
            EventHandler handler = (EventHandler)base.Events[EventValueChanged];
            if (handler != null) handler(this, e);
        }
        #endregion
    }
}