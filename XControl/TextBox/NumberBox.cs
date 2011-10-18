using System;
using System.ComponentModel;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Exceptions;

namespace XControl
{
    /// <summary>
    /// 数字输入控件。只能输入数字，并可以规定范围、间隔。
    /// <remarks>最大最小值只对正整数有效</remarks>
    /// </summary>
    [Description("数字输入控件")]
    [ToolboxData("<{0}:NumberBox runat=server></{0}:NumberBox>")]
    [ToolboxBitmap(typeof(TextBox))]
    [ControlValueProperty("Value")]
    public class NumberBox : TextBox
    {
        /// <summary>
        /// 初始化数字输入控件的样式。
        /// </summary>
        public NumberBox()
            : base()
        {
            this.ToolTip = "只能输入数字！";
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

            if (Min > Max) ShowError("数字输入控件中，设置的最小值比最大值大，请重新设置。");
            if (Min > -1 && Max > -1)
            {
                this.ToolTip = "只能输入 " + Min + " 到 " + Max + " 之间数字！";
            }
            else if (Min > -1 && Max < 0)
            {
                this.ToolTip = "只能输入大于或等于 " + Min + " 数字！";
            }
            else if (Min < 0 && Max > -1)
            {
                this.ToolTip = "只能输入小于或等于 " + Max + " 数字！";
            }
            // 校验脚本
            this.Attributes.Add("onkeypress", "return ValidNumber();");
            this.Attributes.Add("onblur", "return ValidNumber2(" + (Min??-1) + "," + (Max??-1) + ");");
            this.Page.ClientScript.RegisterClientScriptResource(typeof(NumberBox), "XControl.TextBox.Validator.js");
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(HtmlTextWriter writer)
        {
            //如果没有值，则默认显示0
            if (String.IsNullOrEmpty(Text) || Text.Trim().Length < 1) Text = "0";

            base.Render(writer);
        }

        /// <summary>
        /// 处理错误。
        /// </summary>
        /// <param name="err">错误信息</param>
        private void ShowError(String err)
        {
            if (this.DesignMode)
            {
                System.Windows.Forms.MessageBox.Show(err, "XControl控件设计时错误");
            }
            else
            {
                throw new XException(err);
            }
        }

        /// <summary>
        /// 最小值
        /// </summary>
        [Category(" 专用属性"), DefaultValue(null), Description("最小值")]
        public Int32? Min
        {
            get
            {
                String str = (String)ViewState["Min"];
                if (String.IsNullOrEmpty(str)) return null;
                Int32 k = 0;
                if (!int.TryParse(str, out k)) return null;
                return k;
            }
            set
            {
                //if (value < -1) ShowError("非法最小值Min。最小值必须大于0或为-1(表示不限制)。");
                if (value == null)
                {
                    ViewState.Remove("Min");
                    return;
                }

                if (Max != null && value > Max)
                {
                    ShowError("数字输入控件中，设置的最小值比最大值大，请重新设置。");
                    return;
                }

                if (Max != null)
                    ToolTip = "只能输入 " + Min + " 到 " + Max + " 之间数字！";
                else
                    ToolTip = "只能输入大于或等于 " + Min + " 数字！";

                if (Value < value.Value) Value = value.Value;

                ViewState["Min"] = value.ToString();
            }
        }

        /// <summary>
        /// 最大值
        /// </summary>
        [Category(" 专用属性"), DefaultValue(null), Description("最大值")]
        public Int32? Max
        {
            get
            {
                String str = (String)ViewState["Max"];
                if (String.IsNullOrEmpty(str)) return null;
                Int32 k = 0;
                if (!int.TryParse(str, out k)) return null;
                return k;
            }
            set
            {
                if (value == null)
                {
                    //ShowError("非法最大值Max。最大值必须大于0或为-1(表示不限制)。");
                    ViewState.Remove("Max");
                    return;
                }

                if (Min != null && value < Min)
                {
                    ShowError("数字输入控件中，设置的最小值比最大值大，请重新设置。");
                    return;
                }

                Width = Unit.Empty;
                Columns = value.ToString().Length;
                if (Columns > 3) Columns -= 3;

                if (Min != null)
                    ToolTip = "只能输入 " + Min + " 到 " + Max + " 之间数字！";
                else
                    ToolTip = "只能输入小于或等于 " + Max + " 数字！";

                if (Value > value.Value) Value = value.Value;

                ViewState["Max"] = value.ToString();
            }
        }

        /// <summary>
        /// 当前值
        /// </summary>
        [Category(" 专用属性"), DefaultValue(0), Description("当前值")]
        public Int32 Value
        {
            get
            {
                if (String.IsNullOrEmpty(Text)) return 0;
                Int32 k = 0;
                if (!Int32.TryParse(Text, out k)) return 0;
                return k;
            }
            set
            {
                Text = value.ToString();

                Check();
            }
        }

        /// <summary>
        /// 已重载。校验输入数据是否在指定范围内
        /// </summary>
        protected override void RaisePostDataChangedEvent()
        {
            try
            {
                Check();
            }
            catch (Exception ex)
            {
                Page.ClientScript.RegisterStartupScript(this.GetType(), "err", String.Format("alert('{0}');", ex.Message), true);
                this.Focus();
            }

            base.RaisePostDataChangedEvent();
        }
        
        void Check()
        {
            if (Min != null && Value < Min) throw new ArgumentOutOfRangeException("Min", "只能输入大于或等于 " + Min + " 的数字！");
            if (Max != null && Value > Max) throw new ArgumentOutOfRangeException("Min", "只能输入小于或等于 " + Max + " 的数字！");
        }
    }
}