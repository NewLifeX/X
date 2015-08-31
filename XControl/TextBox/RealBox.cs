using System;
using System.ComponentModel;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace XControl
{
    /// <summary>浮点数输入控件。只能输入数字，并可以规定范围、间隔。</summary>
    [Description("浮点数输入控件")]
    [ToolboxData("<{0}:RealBox runat=server></{0}:RealBox>")]
    [ToolboxBitmap(typeof(TextBox))]
    [ControlValueProperty("Value")]
    public class RealBox : TextBox
    {
        /// <summary>初始化数字输入控件的样式。</summary>
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

        /// <summary>已重载。</summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            // 校验脚本
            Helper.HTMLPropertyEscape(this, "onkeypress", "return ValidReal({0});", AllowMinus ? 1 : 0);
            Helper.HTMLPropertyEscape(this, "onblur", "return ValidReal2();");
            Helper.HTMLPropertyEscape(this, "onkeyup", "FilterNumber(this,{0});", Helper.JsObjectString(
                // "allowFloat", 1, // 默认是true
                    "allowMinus", AllowMinus ? 1 : 0
                ));
            this.Page.ClientScript.RegisterClientScriptResource(typeof(NumberBox), "XControl.TextBox.Validator.js");

            //如果没有值，则默认显示0
            if (String.IsNullOrEmpty(Text)) Text = "0";
        }

        /// <summary>当前值</summary>
        [Category(" 专用属性"), DefaultValue(0), Description("当前值")]
        public Double Value
        {
            get
            {
                if (String.IsNullOrEmpty(Text)) return 0;
                Double k = 0;
                if (!Double.TryParse(Text, out k)) return 0;
                return k;
            }
            set
            {
                Text = value.ToString();
            }
        }

        /// <summary>是否允许负数</summary>
        [Category(" 专用属性"), DefaultValue(true), Description("是否允许负数,默认true")]
        public bool AllowMinus
        {
            get
            {
                object o = ViewState["AllowMinus"];
                if (o == null) o = true;
                bool r;
                if (bool.TryParse(o.ToString(), out r)) return r;
                return true;
            }
            set
            {
                ViewState["AllowMinus"] = value;
            }
        }
    }
}