using System;
using System.ComponentModel;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

#region 资源引用
[assembly: WebResource("XControl.TextBox.DateTimePicker.calendar.js", "application/javascript")]
[assembly: WebResource("XControl.TextBox.DateTimePicker.config.js", "application/javascript", PerformSubstitution = true)]
[assembly: WebResource("XControl.TextBox.DateTimePicker.My97DatePicker.htm", "text/html", PerformSubstitution = true)]
[assembly: WebResource("XControl.TextBox.DateTimePicker.WdatePicker.js", "application/javascript", PerformSubstitution = true)]

[assembly: WebResource("XControl.TextBox.DateTimePicker.lang.en.js", "application/javascript")]
[assembly: WebResource("XControl.TextBox.DateTimePicker.lang.zh-cn.js", "application/javascript")]
[assembly: WebResource("XControl.TextBox.DateTimePicker.lang.zh-tw.js", "application/javascript")]

[assembly: WebResource("XControl.TextBox.DateTimePicker.skin.datePicker.gif", "image/gif")]
[assembly: WebResource("XControl.TextBox.DateTimePicker.skin.WdatePicker.css", "text/css", PerformSubstitution = true)]

[assembly: WebResource("XControl.TextBox.DateTimePicker.skin.default.datepicker.css", "text/css", PerformSubstitution = true)]
[assembly: WebResource("XControl.TextBox.DateTimePicker.skin.default.img.gif", "image/gif")]

[assembly: WebResource("XControl.TextBox.DateTimePicker.skin.whyGreen.bg.jpg", "image/jpeg")]
[assembly: WebResource("XControl.TextBox.DateTimePicker.skin.whyGreen.datepicker.css", "text/css", PerformSubstitution = true)]
[assembly: WebResource("XControl.TextBox.DateTimePicker.skin.whyGreen.img.gif", "image/gif")]

#endregion

namespace XControl
{
    /// <summary>日期时间选择器</summary>
    [DefaultProperty("Text")]
    [ToolboxData("<{0}:DateTimePicker runat=server></{0}:DateTimePicker>")]
    [ControlValueProperty("Value")]
    public class DateTimePicker : TextBox
    {
        #region 属性
        /// <summary>是否长时间格式</summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("是否长时间格式")]
        [DefaultValue(true)]
        [Localizable(true)]
        public Boolean LongTime
        {
            get
            {
                return ViewState["LongTime"] == null ? true : (Boolean)ViewState["LongTime"];
            }
            set
            {
                ViewState["LongTime"] = value;
            }
        }

        /// <summary>是否只显示时间部分格式</summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("是否只显示时间部分格式")]
        [DefaultValue(false)]
        [Localizable(true)]
        public Boolean MinTime
        {
            get
            {
                return ViewState["MinTime"] == null ? false : (Boolean)ViewState["MinTime"];
            }
            set
            {
                ViewState["MinTime"] = value;
            }
        }

        /// <summary>客户端只读</summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("客户端只读")]
        [DefaultValue(true)]
        [Localizable(true)]
        public Boolean ClientReadOnly
        {
            get
            {
                return ViewState["ClientReadOnly"] == null ? true : (Boolean)ViewState["ClientReadOnly"];
            }
            set
            {
                ViewState["ClientReadOnly"] = value;
            }
        }

        /// <summary></summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("皮肤")]
        [DefaultValue(Skins.默认)]
        [Localizable(true)]
        public Skins Skin
        {
            get
            {
                if (ViewState["Skin"] == null || !(ViewState["Skin"] is Skins)) return Skins.默认;
                return (Skins)ViewState["Skin"];
            }
            set
            {
                ViewState["Skin"] = value;
            }
        }

        /// <summary>语言</summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("语言")]
        [DefaultValue(Langs.自动)]
        [Localizable(true)]
        public Langs Lang
        {
            get
            {
                if (ViewState["Lang"] == null || !(ViewState["Lang"] is Langs)) return Langs.自动;
                return (Langs)ViewState["Lang"];
            }
            set
            {
                ViewState["Lang"] = value;
            }
        }

        /// <summary></summary>
        [Bindable(true)]
        [Category("专用")]
        [DefaultValue("")]
        [Localizable(true)]
        public DateTime Value
        {
            get
            {
                if (String.IsNullOrEmpty(Text))
                    if (ValueNullSetCurrent)
                        Value = DateTime.Now;
                    else
                        return DateTime.MinValue;

                return Convert.ToDateTime(Text);
            }
            set
            {
                if (LongTime)
                {
                    if (MinTime)
                        Text = value.ToString("HH:mm:ss");
                    else
                        Text = value.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                    Text = value.ToString("yyyy-MM-dd");
            }
        }

        /// <summary></summary>
        [Bindable(true)]
        [Category("专用")]
        [DefaultValue(false)]
        [Localizable(true)]
        [Description("时间未设置，使用当前时间")]
        public Boolean ValueNullSetCurrent
        {
            get
            {
                return ViewState["ValueNullSetCurrent"] == null ? false : (Boolean)ViewState["ValueNullSetCurrent"];
            }
            set
            {
                ViewState["ValueNullSetCurrent"] = value;
            }
        }

        ///// <summary>
        ///// 
        ///// </summary>
        //[Bindable(true)]
        //[Category("专用")]
        //[Description("路径")]
        //[DefaultValue(@"My97DatePicker/WdatePicker.js")]
        //[Localizable(true)]
        //public String JsPath
        //{
        //    get
        //    {
        //        if (ViewState["JsPath"] != null) return (String)ViewState["JsPath"];

        //        String p = @"My97DatePicker/WdatePicker.js";
        //        String root = HttpContext.Current.Request.PhysicalApplicationPath;
        //        if (root.EndsWith(@"\")) root = root.Substring(0, root.Length - 1);
        //        String curpath = HttpContext.Current.Request.PhysicalPath;
        //        curpath = Path.GetDirectoryName(curpath);
        //        while (curpath.StartsWith(root))
        //        {
        //            String tmp = Path.Combine(curpath, p);
        //            if (File.Exists(tmp))
        //            {
        //                tmp = tmp.Substring(root.Length);
        //                tmp= HttpContext.Current.Request.ApplicationPath + tmp.Replace(@"\", @"/");
        //                if (tmp.StartsWith(@"//")) tmp = tmp.Substring(1);
        //                return tmp;
        //            }

        //            curpath = curpath.Substring(0, curpath.LastIndexOf(@"\"));
        //        }
        //        return p;
        //    }
        //    set
        //    {
        //        ViewState["JsPath"] = value;
        //    }
        //}
        #endregion

        /// <summary></summary>
        /// <param name="e"></param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (LongTime && !MinTime)
                Width = new Unit(152);
            else
                Width = new Unit(86);
        }

        /// <summary></summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            //if (!Page.IsPostBack)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("WdatePicker({");
                sb.Append("autoPickDate:true");
                if (LongTime)
                {
                    if (MinTime)
                        sb.Append(",dateFmt:'HH:mm:ss'");
                    else
                        sb.Append(",dateFmt:'yyyy-MM-dd HH:mm:ss'");
                }
                //else
                //    sb.Append(",dateFmt:'yyyy-MM-dd'");
                //if (Skin != null)
                sb.AppendFormat(",skin:'{0}'", GetSkin(Skin));
                //else
                //    sb.AppendFormat(",skin:'{0}'", "blue");
                sb.AppendFormat(",lang:'{0}'", GetLang(Lang));
                sb.AppendFormat(",readOnly:{0}", ClientReadOnly ? "true" : "false");
                //sb.AppendFormat(",minDate:'{0}',maxDate:'{1}'", DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.MaxValue.ToString("yyyy-MM-dd HH:mm:ss"));
                sb.Append("})");

                Attributes.Add("onFocus", sb.ToString());
                CssClass = "Wdate";

                if (LongTime && !MinTime)
                    Width = new Unit(152);
                else
                    Width = new Unit(86);
            }

            //Page.ClientScript.RegisterClientScriptInclude("My97DatePicker", JsPath);
            Page.ClientScript.RegisterClientScriptResource(this.GetType(), "XControl.TextBox.DateTimePicker.WdatePicker.js");
        }

        /// <summary>
        /// 呈现前修改初始化一次Text
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(HtmlTextWriter writer)
        {
            DateTime v = Value;
            base.Render(writer);
        }

        static String GetSkin(Skins skin)
        {
            switch (skin)
            {
                case Skins.默认:
                    return "default";
                case Skins.绿色:
                    return "whyGreen";
                default:
                    return "default";
            }
        }

        static String GetLang(Langs lang)
        {
            switch (lang)
            {
                case Langs.自动:
                    return "auto";
                case Langs.英文:
                    return "en";
                case Langs.简体中文:
                    return "zh-cn";
                case Langs.繁体中文:
                    return "zh-tw";
                default:
                    return "zh-cn";
            }
        }
    }

    /// <summary>皮肤</summary>
    public enum Skins
    {
        /// <summary>默认</summary>
        默认,

        /// <summary>绿色</summary>
        绿色
    }
    /// <summary>语言</summary>
    public enum Langs
    {
        /// <summary>自动</summary>
        自动,

        /// <summary>英文</summary>
        英文,

        /// <summary>简体中文</summary>
        简体中文,

        /// <summary>繁体中文</summary>
        繁体中文
    }
}
