using System;
using System.Collections.Generic;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>反射工具类</summary>
    public static class Reflect
    {
        private static IReflect _Current = new DefaultReflect();
        /// <summary>当前反射提供者</summary>
        public static IReflect Current { get { return _Current; } set { _Current = value; } }

        #region 反射获取
        /// <summary>根据名称获取类型</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        public static Type GetType(String typeName, Boolean isLoadAssembly = false) { return _Current.GetType(typeName, isLoadAssembly); }

        /// <summary>获取方法</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes"></param>
        /// <returns></returns>
        public static MethodInfo GetMethod(Type type, String name, params Type[] paramTypes) { return _Current.GetMethod(type, name, paramTypes); }

        /// <summary>获取属性</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static PropertyInfo GetProperty(Type type, String name) { return _Current.GetProperty(type, name); }

        /// <summary>获取字段</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static FieldInfo GetField(Type type, String name) { return _Current.GetField(type, name); }
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
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
            var type = GetType(ref target);

            // 参数类型数组
            var list = new List<Type>();
            foreach (var item in parameters)
            {
                Type t = null;
                if (item != null) t = item.GetType();
                list.Add(t);
            }

            var method = GetMethod(type, name, list.ToArray());
            return Invoke(target, method, parameters);
        }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public static Object Invoke(this Object target, MethodBase method, params Object[] parameters)
        {
            return _Current.Invoke(target, method, parameters);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static Object GetValue(this Object target, String name)
        {
            //return _Current.GetValue(target, name);

            var type = GetType(ref target);
            var pi = GetProperty(type, name);
            if (pi != null) return target.GetValue(pi);

            var fi = GetField(type, name);
            if (fi != null) return target.GetValue(fi);

            throw new ArgumentException("类[" + type.FullName + "]中不存在[" + name + "]属性或字段。");
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public static Object GetValue(this Object target, PropertyInfo property)
        {
            return _Current.GetValue(target, property);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static Object GetValue(this Object target, FieldInfo field)
        {
            return _Current.GetValue(target, field);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="name">名称</param>
        /// <param name="value"></param>
        public static void SetValue(this Object target, String name, Object value)
        {
            //_Current.SetValue(target, name, value);

            var type = GetType(ref target);
            var pi = GetProperty(type, name);
            if (pi != null) { target.SetValue(pi, value); return; }

            var fi = GetField(type, name);
            if (fi != null) { target.SetValue(fi, value); return; }

            throw new ArgumentException("类[" + type.FullName + "]中不存在[" + name + "]属性或字段。");
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public static void SetValue(this Object target, PropertyInfo property, Object value)
        {
            _Current.SetValue(target, property, value);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public static void SetValue(this Object target, FieldInfo field, Object value)
        {
            _Current.SetValue(target, field, value);
        }
        #endregion

        #region 辅助方法
        /// <summary>获取类型，如果target是Type类型，则表示要反射的是静态成员</summary>
        /// <param name="target"></param>
        /// <returns></returns>
        static Type GetType(ref Object target)
        {
            if (target == null) throw new ArgumentNullException("target");

            var type = target as Type;
            if (type == null)
                type = target.GetType();
            else
                target = null;

            return type;
        }
        #endregion
    }
}