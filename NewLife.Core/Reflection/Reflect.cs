using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Reflection
{
    /// <summary>反射工具类</summary>
    public static class Reflect
    {
        private static IReflect _Current = new DefaultReflect();
        /// <summary>当前反射提供者</summary>
        public static IReflect Current { get { return _Current; } set { _Current = value; } }

        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Object CreateInstance(this Type type, params Object[] parameters)
        {
            return _Current.CreateInstance(type, parameters);
        }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="name">方法名</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public static Object Invoke(this Object target, String name, params Object[] parameters)
        {
            return _Current.Invoke(target, name, parameters);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Object GetValue(this Object target, String name)
        {
            return _Current.GetValue(target, name);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public static void SetValue(this Object target, String name, Object value)
        {
            _Current.SetValue(target, name, value);
        }
    }
}