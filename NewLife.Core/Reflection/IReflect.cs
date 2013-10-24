using System;
using System.ComponentModel;
using System.Reflection;
using NewLife.Exceptions;

namespace NewLife.Reflection
{
    /// <summary>反射接口</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public interface IReflect
    {
        #region 反射获取
        /// <summary>根据名称获取类型</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        Type GetType(String typeName, Boolean isLoadAssembly);

        /// <summary>获取方法</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes"></param>
        /// <returns></returns>
        MethodInfo GetMethod(Type type, String name, params Type[] paramTypes);

        /// <summary>获取属性</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        PropertyInfo GetProperty(Type type, String name);

        /// <summary>获取字段</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        FieldInfo GetField(Type type, String name);
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Object CreateInstance(Type type, params Object[] parameters);

        ///// <summary>反射调用指定对象的方法</summary>
        ///// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        ///// <param name="name">方法名</param>
        ///// <param name="parameters">方法参数</param>
        ///// <returns></returns>
        //Object Invoke(Object target, String name, params Object[] parameters);

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        Object Invoke(Object target, MethodBase method, params Object[] parameters);

        ///// <summary></summary>
        ///// <param name="target"></param>
        ///// <param name="name">名称</param>
        ///// <returns></returns>
        //Object GetValue(Object target, String name);

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        Object GetValue(Object target, PropertyInfo property);

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        Object GetValue(Object target, FieldInfo field);

        ///// <summary></summary>
        ///// <param name="target"></param>
        ///// <param name="name">名称</param>
        ///// <param name="value"></param>
        //void SetValue(Object target, String name, Object value);

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        void SetValue(Object target, PropertyInfo property, Object value);

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        void SetValue(Object target, FieldInfo field, Object value);
        #endregion
    }

    /// <summary>默认反射实现</summary>
    public class DefaultReflect : IReflect
    {
        #region 反射获取
        /// <summary>根据名称获取类型</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        public virtual Type GetType(String typeName, Boolean isLoadAssembly)
        {
            return Type.GetType(typeName);
        }

        static BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

        /// <summary>获取方法</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes"></param>
        /// <returns></returns>
        public virtual MethodInfo GetMethod(Type type, String name, params Type[] paramTypes)
        {
            return type.GetMethod(name, bf, null, paramTypes, null);
        }

        /// <summary>获取属性</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual PropertyInfo GetProperty(Type type, String name)
        {
            return type.GetProperty(name, bf);
        }

        /// <summary>获取字段</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual FieldInfo GetField(Type type, String name)
        {
            return type.GetField(name, bf);
        }
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public virtual Object CreateInstance(Type type, params Object[] parameters) { return Activator.CreateInstance(type, parameters); }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="name">方法名</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public virtual Object Invoke(Object target, String name, params Object[] parameters)
        {
            if (name == null) throw new ArgumentNullException("name");

            var type = GetType(ref target);

            var method = type.GetMethod(name);
            if (method == null) throw new XException("类{0}中找不到名为{1}的方法！", type, name);

            //return method.Invoke(target, parameters);
            return Invoke(target, method, parameters);
        }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public virtual Object Invoke(Object target, MethodBase method, params Object[] parameters)
        {
            return method.Invoke(target, parameters);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public virtual Object GetValue(Object target, String name)
        {
            if (name == null) throw new ArgumentNullException("name");

            var type = GetType(ref target);

            var pi = type.GetProperty(name);
            if (pi != null) return GetValue(target, pi);

            var fi = type.GetField(name);
            if (fi != null) return GetValue(target, fi);

            throw new XException("类{0}中找不到名为{1}的属性或字段！", type, name);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public virtual Object GetValue(Object target, PropertyInfo property)
        {
            return property.GetValue(target, null);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public virtual Object GetValue(Object target, FieldInfo field)
        {
            return field.GetValue(target);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="name">名称</param>
        /// <param name="value"></param>
        public virtual void SetValue(Object target, String name, Object value)
        {
            if (name == null) throw new ArgumentNullException("name");

            var type = GetType(ref target);

            var pi = type.GetProperty(name);
            if (pi != null) { SetValue(target, pi, value); return; }

            var fi = type.GetField(name);
            if (fi != null) { SetValue(target, fi, value); return; }

            throw new XException("类{0}中找不到名为{1}的属性或字段！", type, name);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public virtual void SetValue(Object target, PropertyInfo property, Object value)
        {
            property.SetValue(target, value, null);
        }

        /// <summary></summary>
        /// <param name="target"></param>
        /// <param name="field"></param>
        /// <param name="value"></param>
        public virtual void SetValue(Object target, FieldInfo field, Object value)
        {
            field.SetValue(target, value);
        }
        #endregion

        #region 辅助方法
        /// <summary>获取类型，如果target是Type类型，则表示要反射的是静态成员</summary>
        /// <param name="target"></param>
        /// <returns></returns>
        protected virtual Type GetType(ref Object target)
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