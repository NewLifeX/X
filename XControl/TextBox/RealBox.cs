using System;
using System.Collections.Generic;
using System.Text;

using System.Web.UI.WebControls;
using System.Drawing;
using System.ComponentModel;
using System.Web.UI;

namespace XControl
{
    /// <summary>
    /// 浮点数输入控件。只能输入数字，并可以规定范围、间隔。
    /// </summary>
    [Description("浮点数输入控件")]
    [ToolboxData("<{0}:RealBox runat=server></{0}:RealBox>")]
    [ToolboxBitmap(typeof(TextBox))]
    [ControlValueProperty("Value")]
    public class RealBox : TextBox
    {
        /// <summary>
        /// 初始化数字输入控件的样式。
        /// </summary>
        public RealBox()
            : base()
        {
            this.ToolTip = "只能输入浮点数！";
            BorderWidth = Unit.Pixel(0);
            BorderColor = Color.Black;
            BorderStyle = BorderStyle.Solid;
            Font.Size = FontUnit.Point(10);
            Width = Unit.Pixel(70);
            if (String.IsNullOrEmpty(Attributes["style"])) this.Attributes.Add("style", "border-bottom-width:1px;text-align : right ");
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            // 校验脚本
            this.Attributes.Add("onkeypress", "return ValidReal();");
            this.Attributes.Add("onblur", "return ValidReal2();");
            this.Page.ClientScript.RegisterClientScriptResource(typeof(NumberBox), "XControl.TextBox.Validator.js");
        }

        /// <summary>
        /// 当前值
        /// </summary>
        [Category(" 专用属性"), DefaultValue(0), Description("当前值")]
        public Double Value
        {
            get
            {
                if (String.IsNullOrEmpty(Text)) return Double.NaN;
                Double k = 0;
                if (!Double.TryParse(Text, out k)) return Double.NaN;
                return k;
            }
            set
            {
                Text = value.ToString();
            }
        }
    }
}
