using System;

namespace NewLife.Reflection
{
    /// <summary>快速索引器接口的默认实现</summary>
    [Serializable]
    public class FastIndexAccessor : IIndexAccessor
    {
        /// <summary>
        /// 获取/设置 字段值。
        /// 一个索引，反射实现。
        /// 派生实体类可重写该索引，以避免发射带来的性能损耗
        /// </summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public virtual Object this[String name] { get { return GetValue(this, name); } set { SetValue(this, name, value); } }

        /// <summary>获取目标对象指定属性字段的值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Object GetValue(Object target, String name)
        {
            Object value = null;
            if (TryGetValue(target, name, out value)) return value;

            throw new ArgumentException("类[" + target.GetType().FullName + "]中不存在[" + name + "]属性或字段。");
        }

        /// <summary>尝试获取目标对象指定属性字段的值，返回是否成功</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Boolean TryGetValue(Object target, String name, out Object value)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            value = null;

            //尝试匹配属性
            PropertyInfoX property = PropertyInfoX.Create(target.GetType(), name);
            if (property != null)
            {
                value = property.GetValue(target);
                return true;
            }

            //尝试匹配字段
            FieldInfoX field = FieldInfoX.Create(target.GetType(), name);
            if (field != null)
            {
                value = field.GetValue(target);
                return true;
            }

            return false;
        }

        /// <summary>获取目标对象指定属性字段的值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public T GetValue<T>(String name)
        {
            return (T)GetValue(this, name);
        }

        /// <summary>尝试获取目标对象指定属性字段的值，返回是否成功</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">名称</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryGetValue<T>(String name, out T value)
        {
            value = default(T);
            Object obj = null;
            if (!TryGetValue(this, name, out obj)) return false;

            value = (T)obj;

            return true;
        }

        /// <summary>设置目标对象指定属性字段的值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value"></param>
        public static void SetValue(Object target, String name, Object value)
        {
            if (TrySetValue(target, name, value)) return;

            throw new ArgumentException("类[" + target.GetType().FullName + "]中不存在[" + name + "]属性或字段。");
        }

        /// <summary>尝试设置目标对象指定属性字段的值，返回是否成功</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Boolean TrySetValue(Object target, String name, Object value)
        {
            //尝试匹配属性
            PropertyInfoX property = PropertyInfoX.Create(target.GetType(), name);
            if (property != null)
            {
                property.SetValue(target, value);
                return true;
            }

            //尝试匹配字段
            FieldInfoX field = FieldInfoX.Create(target.GetType(), name);
            if (field != null)
            {
                field.SetValue(target, value);
                return true;
            }

            return false;
        }
    }
}