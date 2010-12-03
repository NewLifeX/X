using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using CheckBoxList_Old = System.Web.UI.WebControls.CheckBoxList;

namespace XControl
{
    /// <summary>
    /// 已重载。修改了绑定时参数改变导致二次绑定的问题
    /// </summary>
    [ToolboxData("<{0}:CheckBoxList runat=\"server\"> </{0}:CheckBoxList>")]
    public class CheckBoxList : CheckBoxList_Old
    {
        private const String ExceptionString = "（异常）";

        //private List<String> NewItems = new List<string>();
        private String cachedSelectedValue;
        /// <summary>
        /// 已重载。加上未添加到列表的项。
        /// </summary>
        public override string SelectedValue
        {
            get
            {
                return base.SelectedValue;
            }
            set
            {
                if (Items.FindByValue(value) == null)
                {
                    //Items.Add(new ListItem(value, value));
                    //NewItems.Add(value);
                    Items.Add(new ListItem(value + ExceptionString, value));
                    cachedSelectedValue = value;
                }
                base.SelectedValue = value;
            }
        }

        private Boolean selecting = false;
        /// <summary>
        /// 已重载。避免绑定时重入该方法
        /// </summary>
        protected override void PerformSelect()
        {
            if (selecting) return;

            selecting = true;
            if (!this.AppendDataBoundItems)
            {
                this.Items.Clear();
            }
            base.PerformSelect();
            selecting = false;
        }
    }
}
