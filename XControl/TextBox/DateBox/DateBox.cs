using System;
using System.Collections.Generic;
using System.Text;

using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.ComponentModel;
using System.IO;
using System.Drawing;

// 特别要注意，这里得加上默认命名空间和目录名，因为vs2005编译的时候会给js文件加上这些东东的
[assembly: WebResource("XControl.TextBox.DateBox.SelectDate.js", "application/x-javascript")]

namespace XControl
{
    /// <summary>日期选择控件</summary>>
    [Description("日期选择控件")]
    [ToolboxData("<{0}:DateBox runat=server></{0}:DateBox>")]
    [ToolboxBitmap(typeof(TextBox))]
    public class DateBox : TextBox
    {
        /// <summary>初始化选择框的样式。</summary>>
        public DateBox()
            : base()
        {
            //this.BackColor = Color.FromArgb(0xff, 0xe0, 0xc0);
            this.ToolTip = "点击即可选择日期";
            BorderWidth = Unit.Pixel(0);
            BorderColor = Color.Olive;
            BorderStyle = BorderStyle.Dotted;
            Font.Size = FontUnit.Point(10);
            Width = Unit.Pixel(118);
            if (String.IsNullOrEmpty(Attributes["style"])) this.Attributes.Add("style", "border-bottom-width:1px;");
        }

        /// <summary>已重载。</summary>>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            // 弹出Popup
            this.Attributes.Add("onclick", "SelectDate(this);");
            this.Page.ClientScript.RegisterClientScriptResource(this.GetType(), "XControl.TextBox.DateBox.SelectDate.js");
        }

        /// <summary>当前值</summary>>
        [Category(" 专用属性"), DefaultValue(0), Description("当前值")]
        public DateTime Value
        {
            get
            {
                if (String.IsNullOrEmpty(Text)) return DateTime.MinValue;
                DateTime k;
                if (!DateTime.TryParse(Text, out k)) return DateTime.MinValue;
                return k;
            }
            set
            {
                Text = value.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
}