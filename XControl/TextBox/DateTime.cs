using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;
#region 资源引用
[assembly: WebResource("XControl.TextBox.DateTime.lhgcore.min.js", "text/javascript", PerformSubstitution = true)]
[assembly: WebResource("XControl.TextBox.DateTime.lhgcalendar.min.js", "text/javascript", PerformSubstitution = true)]
[assembly: WebResource("XControl.TextBox.DateTime.lhgcalendar.css", "text/css", PerformSubstitution = true)]

[assembly: WebResource("XControl.TextBox.DateTime.images.lhgcal_bg.gif", "image/gif")]
[assembly: WebResource("XControl.TextBox.DateTime.images.lhgcal_month.gif", "image/gif")]
[assembly: WebResource("XControl.TextBox.DateTime.images.lhgcal_x.gif", "image/gif")]
[assembly: WebResource("XControl.TextBox.DateTime.images.lhgcal_year.gif", "image/gif")]
[assembly: WebResource("XControl.TextBox.DateTime.images.datePicker.gif", "image/gif")]

#endregion
namespace XControl
{
    /// <summary>
    /// 日期时间选择器
    /// </summary>
    [DefaultProperty("Text")]
    [ToolboxData("<{0}:DateTimelhg runat=server></{0}:DateTimelhg>")]
    [ControlValueProperty("Value")]
    public class DateTimelhg : TextBox
    {
        #region 属性
        /// <summary>
        /// 是否长时间格式
        /// </summary>
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
        /// <summary>
        /// 客户端只读
        /// </summary>
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
        /// <summary>
        /// 客户端只读
        /// </summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("工具栏")]
        [DefaultValue(true)]
        [Localizable(true)]
        public Boolean btnBar
        {
            get
            {
                return ViewState["btnBar"] == null ? true : (Boolean)ViewState["btnBar"];
            }
            set
            {
                ViewState["btnBar"] = value;
            }
        }
        /// <summary>
        /// 客户端只读
        /// </summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("工具栏")]
        [DefaultValue(true)]
        [Localizable(true)]
        public String disWeek
        {
            get
            {
                return ViewState["disWeek"] == null ? "" : (String)ViewState["disWeek"];
            }
            set
            {
                ViewState["disWeek"] = value;
            }
        }
        /// <summary>
        /// 联动日历选择
        /// </summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("联动日历选择")]
        [DefaultValue(true)]
        [Localizable(true)]
        public String linkageObj
        {
            get
            {
                return ViewState["linkageObj"] == null ? "" : (String)ViewState["linkageObj"];
            }
            set
            {
                ViewState["linkageObj"] = value;
            }
        }
        /// <summary>
        /// 联动日历选择
        /// </summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("联动日历选择")]
        [DefaultValue(true)]
        [Localizable(true)]
        public Boolean linkage
        {
            get
            {
                return ViewState["linkage"] == null ? false : (Boolean)ViewState["linkage"];
            }
            set
            {
                ViewState["linkage"] = value;
            }
        }
        /// <summary>
        /// 联动日历选择
        /// </summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("联动日历选择")]
        [DefaultValue(true)]
        [Localizable(true)]
        public String maxDateID
        {
            get
            {
                return ViewState["maxDateID"] == null ? "" : (String)ViewState["maxDateID"];
            }
            set
            {
                ViewState["maxDateID"] = value;
            }
        }
        /// <summary>
        /// 联动日历选择
        /// </summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("联动日历选择")]
        [DefaultValue(true)]
        [Localizable(true)]
        public String minDateID
        {
            get
            {
                return ViewState["minDateID"] == null ? "" : (String)ViewState["minDateID"];
            }
            set
            {
                ViewState["minDateID"] = value;
            }
        }
        /// <summary>
        /// 联动日历选择
        /// </summary>
        [Bindable(true)]
        [Category("专用")]
        [Description("联动日历选择")]
        [DefaultValue(true)]
        [Localizable(true)]
        public String disDate
        {
            get
            {
                return ViewState["disDate"] == null ? "" : (String)ViewState["disDate"];
            }
            set
            {
                ViewState["disDate"] = value;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Bindable(true)]
        [Category("专用")]
        [DefaultValue("")]
        [Localizable(true)]
        public DateTime Value
        {
            get
            {
                if (String.IsNullOrEmpty(Text)) return DateTime.Now;
                if (Text == "") return DateTime.Now;
                return Convert.ToDateTime(Text);
            }
            set
            {
                if (LongTime)
                    Text = value.ToString("yyyy-MM-dd HH:mm:ss");
                else
                    Text = value.ToString("yyyy-MM-dd");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Bindable(true)]
        [Category("专用")]
        [DefaultValue("")]
        [Localizable(true)]
        public DateTime minDate
        {
            get
            {
                return ViewState["minDate"] == null ? DateTime.MinValue : (DateTime)ViewState["minDate"];
            }
            set
            {
                ViewState["minDate"] = value;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        [Bindable(true)]
        [Category("专用")]
        [DefaultValue("")]
        [Localizable(true)]
        public DateTime maxDate
        {
            get
            {
                return ViewState["maxDate"] == null ? DateTime.MaxValue : (DateTime)ViewState["maxDate"];
            }
            set
            {
                ViewState["maxDate"] = value;
            }
        }
        #endregion
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            if (LongTime)
            {
                if (Width == Unit.Empty)
                    Width = new Unit(152);
            }
            else
            {
                if (Width == Unit.Empty)
                    Width = new Unit(86);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            //if (!Page.IsPostBack)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("J('#" + ClientID + "').calendar({");
                if (btnBar)
                {
                    sb.Append("btnBar:true");
                }
                else
                {
                    sb.Append("btnBar:false");
                }
                if (LongTime)
                {
                    sb.Append(",format:'yyyy-MM-dd HH:mm:ss'");
                }
                else
                {
                    sb.Append(",format:'yyyy-MM-dd'");
                }
                if (minDate != DateTime.MinValue)
                {
                    sb.Append(",minDate:'" + minDate.ToString("yyyy-MM-dd") + "'");
                }
                if (maxDate != DateTime.MaxValue)
                {
                    sb.Append(",maxDate:'" + maxDate.ToString("yyyy-MM-dd") + "'");
                }
                if (!String.IsNullOrEmpty(disWeek))
                {
                    sb.Append(",disWeek:'" + disWeek + "'");
                }
                if (!String.IsNullOrEmpty(linkageObj))
                {
                    sb.Append(",linkageObj:'#" + linkageObj + "'");
                }
                if (!String.IsNullOrEmpty(disDate))
                {
                    sb.Append(",disDate:[" + disDate + "]");
                }
                if (linkage)
                {
                    Attributes.Add("onFocus", "J.calendar.Show({ minDate:'#" + linkageObj + "' });");
                }
                if (!String.IsNullOrEmpty(minDateID) && minDate == DateTime.MinValue)
                {
                    sb.Append(",minDate:'#" + minDateID + "'");
                }
                if (!String.IsNullOrEmpty(maxDateID) && maxDate == DateTime.MaxValue)
                {
                    sb.Append(",maxDate:'#" + maxDateID + "'");

                }
                sb.Append("});");
                sb.AppendLine("");
                new ScriptHelper().RegisterScript(sb.ToString());
                CssClass = "lhgcalWdate";
                ReadOnly = true;
                if (LongTime)
                {
                    if (Width == Unit.Empty)
                        Width = new Unit(152);
                }
                else
                {
                    if (Width == Unit.Empty)
                        Width = new Unit(86);
                }
            }

            //Page.ClientScript.RegisterClientScriptInclude("My97DatePicker", JsPath);
            String css = this.Page.ClientScript.GetWebResourceUrl(typeof(DateTimelhg), "XControl.TextBox.DateTime.lhgcalendar.css");
            // register the CSS 
            // this.Page.StyleSheetTheme = css;
            //this.Page.Header.LinkedStyleSheets.Add (css);  
            //早期版本的方法？只能用下面的代码来解决了
            HtmlLink link = new HtmlLink();
            link.Attributes.Add("type", "text/css");
            link.Attributes.Add("rel", "stylesheet");
            link.Attributes.Add("href", css);
            this.Page.Header.Controls.Add(link);


            Page.ClientScript.RegisterClientScriptResource(this.GetType(), "XControl.TextBox.DateTime.lhgcore.min.js");
            Page.ClientScript.RegisterClientScriptResource(this.GetType(), "XControl.TextBox.DateTime.lhgcalendar.min.js");
        }
    }
}
