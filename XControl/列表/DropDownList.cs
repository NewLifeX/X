using System;
using System.Collections;
using System.Web.UI;
using System.Web.UI.WebControls;
using DropDownList_Old = System.Web.UI.WebControls.DropDownList;
using System.Collections.Generic;

namespace XControl
{
    /*
     * 逻辑分析：
     * 选择项、绑定列表项 两者的先后顺序影响着控件的工作
     * AppendDataBoundItems=true
     *  在这种情况下，多次绑定后，会造成叠加，一般不使用。绑定前和绑定后一样，区别只是选择项加在开头还是结尾而已
     *      绑定前：如果静态Items没有，往里面加一个项，绑定的时候保留静态项。
     *      绑定后：每次绑定都会保留原来的项，这就导致了一直增加重复的选项！
     * 
     * AppendDataBoundItems=false
     *      绑定前：如果静态Items没有，往里面加一个项，绑定的时候会清空静态项，如果绑定的列表项刚好没有选择项，会抛异常！
     *              解决方法，绑定前一刻，人为清空所有项，添加选择项，绑定完成后视情况决定是否删除。
     *      绑定后：如果列表项中没有选择项，就会多一个异常项。正确！
     * 
     * */

    /// <summary>
    /// 下拉列表。绑定时，如果没有对应的选择项，则自动加上。
    /// </summary>
    [ToolboxData("<{0}:DropDownList runat=\"server\"> </{0}:DropDownList>")]
    public class DropDownList : DropDownList_Old
    {
        private const String ExceptionString = "（异常）";

        private string cachedSelectedValue
        {
            get
            {
                object obj2 = this.ViewState["cachedSelectedValue"];
                if (obj2 != null)
                {
                    return (string)obj2;
                }
                return string.Empty;
            }
            set
            {
                this.ViewState["cachedSelectedValue"] = value;
            }
        }

        //private String cachedSelectedValue;
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
                    // 列表项中并没有该选项，自动加入，并打上异常标识
                    ListItem item = new ListItem(value + ExceptionString, value);
                    item.Selected = true;
                    Items.Add(item);
                    cachedSelectedValue = value;

                    //AppendDataBoundItems = true;
                }

                base.SelectedValue = value;
            }
        }

        /// <summary>
        /// 绑定数据
        /// </summary>
        /// <param name="dataSource"></param>
        protected override void PerformDataBinding(IEnumerable dataSource)
        {
            ListItem item = null;
            //Boolean isUserCache = false;
            if (!AppendDataBoundItems)
            {
                //isUserCache = true;
                if (!String.IsNullOrEmpty(cachedSelectedValue))
                {
                    AppendDataBoundItems = true;

                    // 设置AppendDataBoundItems的目的就是为了在PerformDataBinding的时候清空
                    Items.Clear();

                    // 必须加上这一项，否则base.PerformDataBinding里面会抛出异常
                    // 因为基类还有一个类似cachedSelectedValue的东西
                    ClearSelection();
                    item = new ListItem(cachedSelectedValue + ExceptionString, cachedSelectedValue);
                    item.Selected = true;
                    Items.Add(item);
                }
            }

            //try
            {
                base.PerformDataBinding(dataSource);
            }
            //catch (ArgumentOutOfRangeException ex)
            //{
            //    if (ex.ParamName == "value" && !AppendDataBoundItems)
            //        throw new ArgumentOutOfRangeException("value", "没有设置AppendDataBoundItems属性为true！");
            //    else
            //        throw;
            //}

            if (item != null)
            {
                AppendDataBoundItems = false;

                // 尝试移除这一项
                Items.Remove(item);

                // 如果现有列表里面没有这一项，则加上
                ListItem item2 = Items.FindByValue(cachedSelectedValue);
                if (item2 == null)
                    Items.Add(item);
                else
                    item2.Selected = true;
            }

            //if (!AppendDataBoundItems)
            //{
            //    ClearSelection();

            //    Items.Add(item);
            //}

            ////处理异常重复值
            //List<String> list = new List<string>();
            //List<String> todel = new List<string>();
            //foreach (ListItem item in Items)
            //{
            //    // 重复出现的值进入todel
            //    if (list.Contains(item.Value))
            //        todel.Add(item.Value);
            //    else
            //        list.Add(item.Value);
            //}
            //String selectstr = SelectedValue;
            //for (int i = Items.Count - 1; i >= 0; i--)
            //{
            //    if (todel.Contains(Items[i].Value) && Items[i].Text.EndsWith(ExceptionString))
            //    {
            //        String value = Items[i].Value;
            //        Boolean bSelected = Items[i].Selected;

            //        Items.RemoveAt(i);

            //        //修正选择项
            //        if (bSelected)
            //        {
            //            //找到同值的另一项
            //            ListItem item = Items.FindByValue(value);
            //            //并标为选中
            //            if (item != null) item.Selected = true;
            //        }
            //    }
            //}

            //if (cachedSelectedValue != null)
            //{
            //    ClearSelection();

            //    // 重新设置选中项
            //    ListItem item = Items.FindByValue(cachedSelectedValue);
            //    if (item == null)
            //    {
            //        item = new ListItem(cachedSelectedValue + ExceptionString, cachedSelectedValue);
            //        Items.Add(item);
            //    }
            //    item.Selected = true;
            //}
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
                // DropDownList在绑定时，如果数据源返回null，它将不做任何动作，而我们一般习惯清空
                this.Items.Clear();
            }
            base.PerformSelect();
            selecting = false;
        }
    }
}