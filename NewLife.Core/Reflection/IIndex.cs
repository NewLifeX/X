using System;
using System.ComponentModel;

namespace NewLife.Reflection
{
    /// <summary>索引器接访问口</summary>
    /// <remarks>该接口用于通过名称快速访问对象属性或字段（属性优先）。</remarks>
    public interface IIndex
    {
        /// <summary>获取/设置 指定名称的属性或字段的值</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
    }

    /// <summary>索引器帮助类</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static class IndexHelper
    {
        /// <summary>获取目标对象指定属性字段的值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Object GetValue(IIndex target, String name)
        {
            Object value = null;
            if (TryGetValue(target, name, out value)) return value;

            throw new ArgumentException("类[" + target.GetType().FullName + "]中不存在[" + name + "]属性或字段。");
        }

        /// <summary>尝试获取目标对象指定属性字段的值，返回是否成功</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Boolean TryGetValue(IIndex target, String name, out Object value)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            return target.TryGetValue(name, out value);

            //value = null;

            ////尝试匹配属性
            ////var property = PropertyInfoX.Create(target.GetType(), name);
            //var property = target.GetType().GetProperty(name);
            //if (property != null)
            //{
            //    //value = property.GetValue(target);
            //    value = target.GetValue(property);
            //    return true;
            //}

            ////尝试匹配字段
            //var field = target.GetType().GetField(name);
            //if (field != null)
            //{
            //    value = target.GetValue(field);
            //    return true;
            //}

            //return false;
        }

        /// <summary>获取目标对象指定属性字段的值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static T GetValue<T>(IIndex target, String name)
        {
            return (T)GetValue(target, name);
        }

        /// <summary>尝试获取目标对象指定属性字段的值，返回是否成功</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Boolean TryGetValue<T>(IIndex target, String name, out T value)
        {
            value = default(T);
            Object obj = null;
            if (!TryGetValue(target, name, out obj)) return false;

            value = (T)obj;

            return true;
        }

        /// <summary>设置目标对象指定属性字段的值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        public static void SetValue(IIndex target, String name, Object value)
        {
            if (TrySetValue(target, name, value)) return;

            throw new ArgumentException("类[" + target.GetType().FullName + "]中不存在[" + name + "]属性或字段。");
        }

        /// <summary>尝试设置目标对象指定属性字段的值，返回是否成功</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Boolean TrySetValue(IIndex target, String name, Object value)
        {
            return target.SetValue(name, value);
            ////尝试匹配属性
            //var property = Reflect.GetProperty(target.GetType(), name);
            //if (property != null)
            //{
            //    target.SetValue(property, value);
            //    return true;
            //}

            ////尝试匹配字段
            //var field = Reflect.GetField(target.GetType(), name);
            //if (field != null)
            //{
            //    //field.SetValue(target, value);
            //    target.SetValue(field, value);
            //    return true;
            //}

            //return false;
        }
    }
}