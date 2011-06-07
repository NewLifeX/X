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
    /// 价格输入控件，只能输入数字，通常只作为输入价格时候使用
    /// </summary>
    [Description("价格输入控件")]
    [ToolboxData("<{0}:DecimalBox runat=server></{0}:DecimalBox>")]
    [ToolboxBitmap(typeof(TextBox))]
    class DecimalBox : TextBox
    {
        /// <summary>
        /// 初始化价格输入控件的样式
        /// </summary>
        public DecimalBox()
            : base()
        {
            this.ToolTip = "只能输入数字价格！";
            BorderWidth = Unit.Pixel(0);
            BorderColor = Color.Red;
            BorderStyle = BorderStyle.Solid;
            Font.Size = FontUnit.Point(10);
            Width = Unit.Pixel(70);
            if (String.IsNullOrEmpty(Attributes["style"])) this.Attributes.Add("style", "border-bottom-width:1px;text-align : right ");
        }

        /// <summary>
        /// 已重载
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            //校验脚本
            this.Attributes.Add("onkeypress", "return ValidReal();");
            this.Attributes.Add("onblur", "return ValidReal2();");
            this.Page.ClientScript.RegisterClientScriptResource(typeof(NumberBox), "XControl.TextBox.Validator.js");
        }

        /// <summary>
        /// 当前值
        /// </summary>
        [Category("专用属性"), DefaultValue(0), Description("当前值")]
        public Decimal Value
        {
            get
            {
                if (String.IsNullOrEmpty(Text)) return Decimal.Zero;
                Decimal d = 0;
                if (!Decimal.TryParse(Text, out d)) return Decimal.Zero;
                return d;
            }
            set
            {
                Text = value.ToString();
            }
        }
    }
}
