using System;
using System.Collections;
using System.ComponentModel;
using System.Web.UI;
using System.Web.UI.WebControls;
using DropDownList_Old = System.Web.UI.WebControls.DropDownList;

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

    /*
     * 2012-05-22 by netwjx :
     *
     * 针对SelectedValue赋值和DataBind()可能会抛出异常的现象修改, 理想的策略是
     *
     * 无论任何时候 给SelectedValue赋值, 都确保不抛出异常, 而是选择添加异常项或缓存设置的值,暂时使用旧值
     * 无论任何时候 DataBind()时, 都确保不抛出异常, 而是选择添加异常项或尝试使用缓存的值或者使用默认第一项,或者null
     * TODO 尚未实现SelectedIndex赋值的处理
     * */

    /// <summary>下拉列表。绑定时，如果没有对应的选择项，则自动加上。</summary>
    /// <remarks>
    /// 注意:在实践中发现DropDownList.SelectedIndex有可能修改this.Items[0].Selected 为true
    ///
    /// 进而会导致错误的结果,所以尽量不在这个控件内部使用SelectedIndex SelectedValue属性
    /// </remarks>
    [ToolboxData("<{0}:DropDownList runat=\"server\"> </{0}:DropDownList>")]
    public class DropDownList : DropDownList_Old
    {
        private const String ExceptionString = "无效的值:{0}";

        /// <summary>
        /// 当前实际选中项的值,没有值将返回null,而不是SelectedValue会修改为第一项默认值
        /// </summary>
        public string RealSelectedValue
        {
            get
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    if (item.Selected) return item.Value;
                }
                return null;
            }
        }

        /// <summary>
        /// 是否不添加异常项
        /// </summary>
        [WebCategory("专用属性 "), DefaultValue(false),
        Description("当SelectedValue的值是不存在的项时会添加一个异常的项, 这个属性控制是否不要添加这个项, 注意: 最终仍旧不存在时将会默认选中第一项")]
        public bool NoExceptionItem
        {
            get
            {
                object val = ViewState["NoExceptionItem"];
                if (val != null)
                {
                    return (bool)val;
                }
                return false;
            }
            set
            {
                ViewState["NoExceptionItem"] = value;
            }
        }

        private bool hasExceptItem;
        private string cachedSelectedValue;

        /// <summary>已重载。加上未添加到列表的项。</summary>
        public override string SelectedValue
        {
            get
            {
                return base.SelectedValue;
            }
            set
            {
                RemoveExceptItem();
                cachedSelectedValue = null;
                try
                {
                    base.SelectedValue = value;
                    if (value != null && base.SelectedValue != value) throw new ArgumentOutOfRangeException();
                }
                catch (ArgumentOutOfRangeException)
                {
                    cachedSelectedValue = value;
                    if (NoExceptionItem)
                    {
                        // 记录真实值后跳出
                        return;
                    }
                    else
                    {
                        // 列表项中并没有该选项，自动加入，并打上异常标识
                        ClearSelection();
                        Items.Insert(0, new ListItem(string.Format(ExceptionString, value), value) { Selected = true });
                        hasExceptItem = true;
                        base.SelectedValue = value;
                    }
                }
            }
        }

        private void RemoveExceptItem()
        {
            if (hasExceptItem && cachedSelectedValue != null)
            {
                // 尝试移除上次调用时产生的异常项
                var item = Items.FindByValue(cachedSelectedValue);
                if (item != null)
                {
                    Items.Remove(item);
                }
            }
        }

        /// <summary>绑定数据</summary>
        /// <param name="dataSource"></param>
        protected override void PerformDataBinding(IEnumerable dataSource)
        {
            string real = cachedSelectedValue ?? RealSelectedValue;
            RemoveExceptItem();
            try
            {
                base.PerformDataBinding(dataSource);
            }
            catch (ArgumentException)
            {
                // SelectedValue会在抛出异常时改变为第一项的Value
            }
            if (!string.IsNullOrEmpty(real) && SelectedValue != real)
            {
                var item = Items.FindByValue(real);
                if (item != null)
                {
                    ClearSelection();
                    item.Selected = true;
                }
                else
                {
                    if (NoExceptionItem)
                    {
                        if (Items.Count > 0)
                        {
                            ClearSelection();
                            Items[0].Selected = true;
                        }
                    }
                    else
                    {
                        ClearSelection();
                        item = new ListItem(string.Format(ExceptionString, real), real) { Selected = true };
                        Items.Insert(0, item);
                    }
                }
            }
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            base.RenderContents(writer);
        }

        private Boolean selecting = false;

        /// <summary>已重载。避免绑定时重入该方法</summary>
        protected override void PerformSelect()
        {
            if (selecting) return;

            selecting = true;
            if (!this.AppendDataBoundItems)
            {
                if (this.DataSource != null || !string.IsNullOrEmpty(this.DataSourceID))
                {
                    // DropDownList在绑定时，如果数据源返回null，它将不做任何动作，而我们一般习惯清空
                    this.Items.Clear();
                }
            }
            base.PerformSelect();
            selecting = false;
        }
    }
}