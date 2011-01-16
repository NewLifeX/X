using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;

namespace NewLife.Web
{
    /// <summary>
    /// 控件助手
    /// </summary>
    public static class ControlHelper
    {
        /// <summary>
        /// 查找指定类型的子孙控件
        /// </summary>
        /// <typeparam name="T">目标控件类型</typeparam>
        /// <param name="control">父控件，从该控件开始向下进行广度搜索</param>
        /// <param name="id">控件ID，不指定表示不限制</param>
        /// <returns></returns>
        public static T FindControl<T>(Control control, String id) where T : Control
        {
            if (control == null) return null;

            // 准备队列，进行广度搜索
            Queue<Control> queue = new Queue<Control>();
            queue.Enqueue(control);

            while (queue.Count > 0)
            {
                control = queue.Dequeue();

                // 类型匹配
                if (control is T)
                {
                    // 没有指定控件ID，或者控件ID匹配
                    if (String.IsNullOrEmpty(id) || String.Equals(control.ID, id, StringComparison.OrdinalIgnoreCase))
                        return control as T;
                }

                // 虽然类型匹配不一定成功，但是子控件还是要入队的
                if (control.Controls != null && control.Controls.Count > 0)
                {
                    foreach (Control item in control.Controls)
                    {
                        queue.Enqueue(item);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 查找指定控件附近的控件，向上搜索
        /// </summary>
        /// <typeparam name="T">目标控件类型</typeparam>
        /// <param name="control">指定控件</param>
        /// <param name="id">控件ID，不指定表示不限制</param>
        /// <returns></returns>
        public static T FindControlUp<T>(Control control, String id) where T : Control
        {
            if (control == null) return null;

            // 准备队列，进行广度搜索
            Queue<Control> queue = new Queue<Control>();
            queue.Enqueue(control);

            // 已经分析过的
            List<Control> parsed = new List<Control>();
            parsed.Add(control);

            while (queue.Count > 0)
            {
                control = queue.Dequeue();

                // 类型匹配
                if (control is T)
                {
                    // 没有指定控件ID，或者控件ID匹配
                    if (String.IsNullOrEmpty(id) || String.Equals(control.ID, id, StringComparison.OrdinalIgnoreCase))
                        return control as T;
                }

                // 虽然类型匹配不一定成功，但是遍历还是需要继续的
                Control parent = control.Page;
                if (parent == null) continue;

                if (parent.Controls != null && parent.Controls.Count > 0)
                {
                    foreach (Control item in parent.Controls)
                    {
                        if (!parsed.Contains(item))
                        {
                            queue.Enqueue(item);
                            parsed.Add(item);
                        }
                    }
                }
            }

            return null;
        }
    }
}