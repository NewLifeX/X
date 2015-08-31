using System;
using System.ComponentModel;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace XControl
{
    /// <summary>邮件地址输入控件。只能输入数字，并可以规定范围、间隔。</summary>
    [Description("邮件地址输入控件")]
    [ToolboxData("<{0}:MailBox runat=server></{0}:MailBox>")]
    [ToolboxBitmap(typeof(TextBox))]
    public class MailBox : TextBox
    {
        /// <summary>初始化邮件地址输入控件的样式。</summary>
        public MailBox()
            : base()
        {
            this.ToolTip = "只能输入邮件地址！";
            BorderWidth = Unit.Pixel(0);
            BorderColor = Color.Black;
            BorderStyle = BorderStyle.Solid;
            Font.Size = FontUnit.Point(10);
            Width = Unit.Pixel(120);
            if (String.IsNullOrEmpty(Attributes["style"])) this.Attributes.Add("style", "border-bottom-width:1px;");
        }

        /// <summary>已重载。</summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            // 校验脚本
            this.Attributes.Add("onblur", "return ValidMail();");
            this.Page.ClientScript.RegisterClientScriptResource(typeof(NumberBox), "XControl.TextBox.Validator.js");
        }
    }
}