using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Drawing;

namespace XControl
{
    /// <summary>
    /// 下拉列表。绑定时，如果没有对应的选择项，则自动加上。
    /// </summary>
    [ToolboxData("<{0}:XDropDownList runat=\"server\"> </{0}:XDropDownList>")]
    [ToolboxBitmap(typeof(DropDownList))]
    public class XDropDownList : DropDownList
    {
        public XDropDownList()
        {
            //
            //TODO: 在此处添加构造函数逻辑
            //
        }

        private List<String> NewItems = new List<string>();
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
                    Items.Add(new ListItem(value, value));
                    NewItems.Add(value);
                }
                base.SelectedValue = value;
            }
        }

        protected override void PerformDataBinding(IEnumerable dataSource)
        {
            //标记已有值为异常
            foreach (ListItem item in Items)
            {
                if (NewItems.Contains(item.Value)) item.Text += "（异常）";
            }

            try
            {
                base.PerformDataBinding(dataSource);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if (ex.ParamName == "value" && !AppendDataBoundItems)
                    throw new ArgumentOutOfRangeException("value", "没有设置AppendDataBoundItems属性为true！");
                else
                    throw;
            }

            //处理异常重复值
            List<String> list = new List<string>();
            List<String> todel = new List<string>();
            foreach (ListItem item in Items)
            {
                if (list.Contains(item.Value))
                    todel.Add(item.Value);
                else
                    list.Add(item.Value);
            }
            String selectstr = SelectedValue;
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (todel.Contains(Items[i].Value) && Items[i].Text.EndsWith("（异常）"))
                {
                    String value = Items[i].Value;
                    Boolean bSelected = Items[i].Selected;

                    Items.RemoveAt(i);

                    //修正选择项
                    if (bSelected)
                    {
                        //找到同值的另一项
                        ListItem item = Items.FindByValue(value);
                        //并标为选中
                        if (item != null) item.Selected = true;
                    }
                }
            }
        }
    }
}