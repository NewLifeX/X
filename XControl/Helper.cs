using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;

namespace XControl
{
    static class Helper
    {
        /// <summary>
        /// 逐层向上找控件
        /// </summary>
        /// <typeparam name="TControl"></typeparam>
        /// <param name="control"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TControl FindControlUp<TControl>(Control control, String id) where TControl : Control
        {
            if (control == null || String.IsNullOrEmpty(id)) return null;
            if (control.ID == id && control is TControl) return control as TControl;

            if (control.Parent == null) return null;

            Control parent = control.Parent;
            if (parent.ID == id && parent is TControl) return parent as TControl;

            // 在兄弟节点向下找
            if (parent.Controls != null && parent.Controls.Count > 0)
            {
                foreach (Control item in parent.Controls)
                {
                    // 向上搜索的关键是要避开自己
                    if (item == control) continue;

                    TControl elm = FindControl<TControl>(item, id);
                    if (elm != null) return elm;
                }
            }

            // 向上递归
            return FindControlUp<TControl>(parent, id);
        }

        /// <summary>
        /// 逐层向下找控件
        /// </summary>
        /// <typeparam name="TControl"></typeparam>
        /// <param name="control"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static TControl FindControl<TControl>(Control control, String id) where TControl : Control
        {
            if (control == null || String.IsNullOrEmpty(id)) return null;
            if (control.ID == id && control is TControl) return control as TControl;

            if (control.Controls == null || control.Controls.Count <= 0) return null;

            //// 深度搜索算法
            //foreach (Control item in control.Controls)
            //{
            //    Control elm = FindControl<TControl>(item, id);
            //    if (elm != null) return elm;
            //}

            // 广度搜索算法
            Queue<Control> queue = new Queue<Control>();
            queue.Enqueue(control);
            while (queue.Count > 0)
            {
                control = queue.Dequeue();
                if (control.ID == id && control is TControl) return control as TControl;
                if (control.Controls == null || control.Controls.Count <= 0) continue;

                // 子控件进入队列
                foreach (Control item in control.Controls)
                {
                    queue.Enqueue(item);
                }
            }

            return null;
        }
    }
}
