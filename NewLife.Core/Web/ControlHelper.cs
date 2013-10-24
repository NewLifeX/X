using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Web;
using System.Web.UI;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif
using NewLife.Reflection;

namespace NewLife.Web
{
    /// <summary>控件助手</summary>
    public static class ControlHelper
    {
        /// <summary>查找指定类型的子孙控件</summary>
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
                    //if (String.IsNullOrEmpty(id) || String.Equals(control.ID, id, StringComparison.OrdinalIgnoreCase))
                    if (id.IsNullOrWhiteSpace() || id.EqualIgnoreCase(control.ID))
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

        /// <summary>查找指定控件附近的控件，向上搜索</summary>
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

        /// <summary>在页面查找指定ID的控件，采用反射字段的方法，避免遍历Controls引起子控件构造</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T FindControlInPage<T>(String id) where T : Control
        {
            if (HttpContext.Current == null) return null;

            var handler = HttpContext.Current.Handler;
            if (handler == null) return null;

            FieldInfo fi = null;
            if (!String.IsNullOrEmpty(id))
            {
                fi = Reflect.GetField(handler.GetType(), id);
            }
            else
            {
                fi = handler.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(item => item.FieldType == typeof(T));
            }

            if (fi == null) return null;

            return handler.GetValue(fi) as T;
        }

        /// <summary>根据字段查找指定ID的控件，采用反射字段的方法，避免遍历Controls引起子控件构造</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control">容器</param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T FindControlByField<T>(Control control, String id) where T : Control
        {
            if (control == null) return null;

            FieldInfo fi = null;
            if (!String.IsNullOrEmpty(id))
            {
                fi = Reflect.GetField(control.GetType(), id);
            }
            else
            {
                fi = control.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).FirstOrDefault(item => item.FieldType == typeof(T));
            }

            if (fi == null) return null;

            return control.GetValue(fi) as T;
        }

        /// <summary>查找控件的事件</summary>
        /// <param name="control"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public static Delegate FindEventHandler(Control control, String eventName)
        {
            if (control == null) return null;
            if (String.IsNullOrEmpty(eventName)) return null;

            //var pix = PropertyInfoX.Create(control.GetType(), "Events");
            //if (pix == null) return null;

            //var list = pix.GetValue(control) as EventHandlerList;

            var pi = Reflect.GetProperty(control.GetType(), "Events");
            if (pi == null) return null;

            var list = control.GetValue(pi) as EventHandlerList;
            if (list == null) return null;

            var fi = Reflect.GetField(control.GetType(), eventName);
            if (fi == null && !eventName.StartsWith("Event", StringComparison.OrdinalIgnoreCase)) fi = Reflect.GetField(control.GetType(), "Event" + eventName);
            if (fi == null) return null;

            return list[fi.GetValue(control)];
        }
    }
}