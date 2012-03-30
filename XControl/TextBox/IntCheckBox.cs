using System;
using System.ComponentModel;
using System.Drawing;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace XControl
{
    /// <summary>整型选择控件。</summary>>
    [Description("整型选择控件")]
    [ToolboxData("<{0}:IntCheckBox runat=server></{0}:IntCheckBox>")]
    [ToolboxBitmap(typeof(CheckBox))]
    [ControlValueProperty("Value")]
    public class IntCheckBox : CheckBox
    {

        /// <summary>选中值</summary>>
        [Category(" 专用属性"), DefaultValue(1), Description("选中值")]
        public Int32 SelectedValue
        {
            get
            {
                String str = (String)ViewState["SelectedValue"];
                if (String.IsNullOrEmpty(str)) return 1;
                Int32 k = 1;
                if (!int.TryParse(str, out k)) return 1;
                return k;
            }
            set
            {
                ViewState["SelectedValue"] = value.ToString();
            }
        }

        /// <summary>非选中值</summary>>
        [Category(" 专用属性"), DefaultValue(0), Description("非选中值")]
        public Int32 UnSelectedValue
        {
            get
            {
                String str = (String)ViewState["UnSelectedValue"];
                if (String.IsNullOrEmpty(str)) return 0;
                Int32 k = 0;
                if (!int.TryParse(str, out k)) return 0;
                return k;
            }
            set
            {
                ViewState["UnSelectedValue"] = value.ToString();
            }
        }

        /// <summary>是否仅仅选中</summary>>
        [Category(" 专用属性"), DefaultValue(false), Description("当前值Value是否仅仅等于选中值时才选中")]
        public Boolean OnlySelect
        {
            get
            {
                String str = (String)ViewState["OnlySelect"];
                if (String.IsNullOrEmpty(str)) return false;
                Boolean k = false;
                if (!Boolean.TryParse(str, out k)) return false;
                return k;
            }
            set
            {
                ViewState["OnlySelect"] = value.ToString();
            }
        }

        /// <summary>当前值</summary>>
        [Category(" 专用属性"), DefaultValue(0), Description("当前值")]
        public Int32 Value
        {
            get
            {
                return Checked ? SelectedValue : UnSelectedValue;
            }
            set
            {
                if (OnlySelect)
                {
                    //只有等于选中值时才选中
                    Checked = (value == SelectedValue);
                }
                else
                {
                    Checked = !(value == UnSelectedValue);
                }
            }
        }
    }
}