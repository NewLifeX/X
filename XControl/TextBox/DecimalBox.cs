using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Exceptions;

namespace XControl
{
    /// <summary>
    /// 价格输入控件，只能输入数字，通常只作为输入价格时候使用
    /// </summary>
    [Description("价格输入控件")]
    [ToolboxData("<{0}:DecimalBox runat=server></{0}:DecimalBox>")]
    [ToolboxBitmap(typeof(TextBox))]
    [ControlValueProperty("Value")]
    public class DecimalBox : TextBox
    {
        /// <summary>
        /// 小数点右边位数
        /// </summary>
        [Description("小数点右边精度值（默认为2位）")]
        [DefaultValue(2)]
        public Int32? CurrencyDecimalDigits
        {
            get
            {
                //从ViewState中取值，所以第一次无法取到默认值
                String num = (String)ViewState["CurrencyDecimalDigits"];
                if (String.IsNullOrEmpty(num)) return 2;
                Int32 k = 0;
                if (!Int32.TryParse(num, out k)) return 0;
                return k;
            }
            set
            {
                if (value == null)
                {
                    ViewState["CurrencyDecimalDigits"] = "2";
                }
                ViewState["CurrencyDecimalDigits"] = value.ToString();
            }
        }

        /// <summary>
        /// 小数点左边部分每组数字位数
        /// </summary>
        [Description("小数点部分每一组位数（如果多重分组使用逗号分隔）")]
        public String CurrencyGroupSizes
        {
            get
            {
                String num = (String)ViewState["CurrencyGroupSizes"];
                if (String.IsNullOrEmpty(num)) return null;
                return num;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    ViewState.Remove("CurrencyGroupSizes");
                    return;
                }
                ViewState["CurrencyGroupSizes"] = value;
            }
        }

        /// <summary>
        /// 小数点左边部分每组数字分组符
        /// </summary>
        [Description("小数点左边部分每组数字分组符")]
        [DefaultValue(",")]
        public String CurrencyGroupSeparator
        {
            get
            {
                String str = (String)ViewState["CurrencyGroupSeparator"];
                if (String.IsNullOrEmpty(str)) return ",";
                return str;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    ViewState.Remove("CurrencyGroupSeparator");
                    return;
                }
                ViewState["CurrencyGroupSeparator"] = value;
            }
        }

        /// <summary>
        /// 获取或设置用作货币符号的字符串
        /// </summary>
        [Description("获取或设置用作货币符号的字符串")]
        [DefaultValue("￥")]
        public String CurrencySymbol
        {
            get
            {
                String symbol = (String)ViewState["CurrencySymbol"];
                if (String.IsNullOrEmpty(symbol)) symbol = "￥";

                return symbol;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) ViewState["CurrencySymbol"] = "￥";

                ViewState["CurrencySymbol"] = value;
            }
        }

        /// <summary>
        /// 初始化价格输入控件的样式
        /// </summary>
        public DecimalBox()
            : base()
        {
            this.ToolTip = "只能输入数字价格！";
            BorderWidth = Unit.Pixel(0);
            BorderColor = Color.Black;
            BorderStyle = BorderStyle.Solid;
            Font.Size = FontUnit.Point(10);
            Width = Unit.Pixel(70);
            if (String.IsNullOrEmpty(Attributes["style"])) this.Attributes.Add("style", "border-bottom-width:1px;text-align : right ");
            //if (String.IsNullOrEmpty(Attributes["CurrencyDecimalDigits"])) this.Attributes.Add("CurrencyDecimalDigits","2");
        }

        /// <summary>
        /// 已重载
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            //校验脚本
            Helper.HTMLPropertyEscape(this, "onkeypress", "return ValidReal({0});", AllowMinus ? 1 : 0);
            Helper.HTMLPropertyEscape(this, "onblur", "return VaildDecimal1('{0}');", Helper.JsStringEscape(CurrencySymbol));
            Helper.HTMLPropertyEscape(this, "onkeyup", "FilterNumber(this,{0});", Helper.JsObjectString(
                // "allowFloat", 1, // 默认是true
                    "allowMinus", AllowMinus ? 1 : 0,
                    "allowChars", CurrencySymbol
                ));
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
                if (ViewState["Value"] == null) return Decimal.Zero;

                Decimal value = (Decimal)ViewState["Value"];

                return value;
            }
            set
            {
                ViewState["Value"] = value;
            }
        }

        /// <summary>
        /// 是否允许负数
        /// </summary>
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

        /// <summary>
        /// 重新包装Text属性，数据转换以及格式化部分在Text内完成
        /// </summary>
        public override string Text
        {
            get
            {
                Int32[] intArray = new Int32[] { };
                NumberFormatInfo nf = new NumberFormatInfo();

                if (!String.IsNullOrEmpty(CurrencyGroupSizes))
                {
                    try
                    {
                        String[] strArray = CurrencyGroupSizes.Split(',');
                        ArrayList list = new ArrayList();

                        foreach (var item in strArray)
                        {
                            Int32 i = Int32.Parse(item);
                            list.Add(i);
                        }

                        intArray = (Int32[])list.ToArray(typeof(Int32));
                    }
                    catch (Exception ex)
                    {
                        throw new XException("请检查分组输入！", ex);
                    }
                    nf.CurrencyGroupSizes = intArray;
                }
                if (CurrencyDecimalDigits == null) CurrencyDecimalDigits = 2;
                nf.CurrencyDecimalDigits = (Int32)CurrencyDecimalDigits;
                if (!String.IsNullOrEmpty(CurrencyGroupSeparator))
                    nf.CurrencyGroupSeparator = CurrencyGroupSeparator;
                nf.CurrencySymbol = CurrencySymbol;

                return Value.ToString("c", nf);
            }
            set
            {
                Decimal d = 0;
                if (!String.IsNullOrEmpty(value))
                {
                    //去除空字符干扰
                    value = value.Trim();
                    if (value.Contains(CurrencySymbol))
                        //去除当前CurrencySymbol设置的符号
                        value = value.Replace(CurrencySymbol, "");

                    if (Decimal.TryParse(value, out d))
                        Value = d;
                }
            }
        }
    }
}