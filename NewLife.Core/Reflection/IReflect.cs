using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>反射接口</summary>
    /// <remarks>该接口仅用于扩展，不建议外部使用</remarks>
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
        /// <param name="paramTypes">参数类型数组</param>
        /// <returns></returns>
        MethodInfo GetMethod(Type type, String name, params Type[] paramTypes);

        /// <summary>获取属性</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        PropertyInfo GetProperty(Type type, String name, Boolean ignoreCase);

        /// <summary>获取字段</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        FieldInfo GetField(Type type, String name, Boolean ignoreCase);

        /// <summary>获取成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        MemberInfo GetMember(Type type, String name, Boolean ignoreCase);
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters">参数数组</param>
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

        ///// <summary>获取目标对象指定名称的属性/字段值</summary>
        ///// <param name="target">目标对象</param>
        ///// <param name="name">名称</param>
        ///// <returns></returns>
        //Object GetValue(Object target, String name);

        /// <summary>获取目标对象的属性值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        Object GetValue(Object target, PropertyInfo property);

        /// <summary>获取目标对象的字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="field">字段</param>
        /// <returns></returns>
        Object GetValue(Object target, FieldInfo field);

        ///// <summary>设置目标对象指定名称的属性/字段值</summary>
        ///// <param name="target">目标对象</param>
        ///// <param name="name">名称</param>
        ///// <param name="value">数值</param>
        //void SetValue(Object target, String name, Object value);

        /// <summary>设置目标对象的属性值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="property">属性</param>
        /// <param name="value">数值</param>
        void SetValue(Object target, PropertyInfo property, Object value);

        /// <summary>设置目标对象的字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        void SetValue(Object target, FieldInfo field, Object value);
        #endregion

        #region 类型辅助
        /// <summary>获取一个类型的元素类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Type GetElementType(Type type);

        /// <summary>类型转换</summary>
        /// <param name="value">数值</param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        Object ChangeType(Object value, Type conversionType);

        /// <summary>获取类型的友好名称</summary>
        /// <param name="type">指定类型</param>
        /// <param name="isfull">是否全名，包含命名空间</param>
        /// <returns></returns>
        String GetName(Type type, Boolean isfull);
        #endregion

        #region 插件
        ///// <summary>是否插件</summary>
        ///// <param name="type">目标类型</param>
        ///// <param name="baseType">基类或接口</param>
        ///// <returns></returns>
        //Boolean IsSubclassOf(Type type, Type baseType);

        /// <summary>在指定程序集中查找指定基类或接口的所有子类实现</summary>
        /// <param name="asm">指定程序集</param>
        /// <param name="baseType">基类或接口，为空时返回所有类型</param>
        /// <returns></returns>
        IEnumerable<Type> GetSubclasses(Assembly asm, Type baseType);

        /// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
        /// <param name="baseType">基类或接口</param>
        /// <param name="isLoadAssembly">是否加载为加载程序集</param>
        /// <returns></returns>
        IEnumerable<Type> GetAllSubclasses(Type baseType, Boolean isLoadAssembly);
        #endregion
    }

    /// <summary>默认反射实现</summary>
    /// <remarks>该接口仅用于扩展，不建议外部使用</remarks>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
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
        static BindingFlags bfic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase;

        /// <summary>获取方法</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes">参数类型数组</param>
        /// <returns></returns>
        public virtual MethodInfo GetMethod(Type type, String name, params Type[] paramTypes)
        {
            return type.GetMethod(name, bf, null, paramTypes, null);
        }

        /// <summary>获取属性</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public virtual PropertyInfo GetProperty(Type type, String name, Boolean ignoreCase)
        {
            // 父类私有属性的获取需要递归，可见范围则不需要，有些类型的父类为空，比如接口
            while (type != null && type != typeof(Object))
            {
                var pi = type.GetProperty(name, ignoreCase ? bfic : bf);
                if (pi != null) return pi;

                type = type.BaseType;
            }
            return null;
        }

        /// <summary>获取字段</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public virtual FieldInfo GetField(Type type, String name, Boolean ignoreCase)
        {
            // 父类私有字段的获取需要递归，可见范围则不需要，有些类型的父类为空，比如接口
            while (type != null && type != typeof(Object))
            {
                var fi = type.GetField(name, ignoreCase ? bfic : bf);
                if (fi != null) return fi;

                type = type.BaseType;
            }
            return null;
        }

        /// <summary>获取成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public virtual MemberInfo GetMember(Type type, String name, Boolean ignoreCase)
        {
            // 父类私有成员的获取需要递归，可见范围则不需要，有些类型的父类为空，比如接口
            while (type != null && type != typeof(Object))
            {
                var fs = type.GetMember(name, ignoreCase ? bfic : bf);
                if (fs != null && fs.Length > 0)
                {
                    // 得到多个的时候，优先返回精确匹配
                    if (ignoreCase && fs.Length > 1)
                    {
                        foreach (var fi in fs)
                        {
                            if (fi.Name == name) return fi;
                        }
                    }
                    return fs[0];
                }

                type = type.BaseType;
            }
            return null;
        }
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        public virtual Object CreateInstance(Type type, params Object[] parameters) { return Activator.CreateInstance(type, parameters); }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public virtual Object Invoke(Object target, MethodBase method, params Object[] parameters)
        {
            return method.Invoke(target, parameters);
        }

        /// <summary>获取目标对象的属性值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        public virtual Object GetValue(Object target, PropertyInfo property)
        {
            return property.GetValue(target, null);
        }

        /// <summary>获取目标对象的字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public virtual Object GetValue(Object target, FieldInfo field)
        {
            return field.GetValue(target);
        }

        /// <summary>设置目标对象的属性值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="property">属性</param>
        /// <param name="value">数值</param>
        public virtual void SetValue(Object target, PropertyInfo property, Object value)
        {
            property.SetValue(target, value, null);
        }

        /// <summary>设置目标对象的字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        public virtual void SetValue(Object target, FieldInfo field, Object value)
        {
            field.SetValue(target, value);
        }
        #endregion

        #region 类型辅助
        /// <summary>获取一个类型的元素类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public virtual Type GetElementType(Type type) { return type.GetElementType(); }

        /// <summary>类型转换</summary>
        /// <param name="value">数值</param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public virtual Object ChangeType(Object value, Type conversionType) { return Convert.ChangeType(value, conversionType); }

        /// <summary>获取类型的友好名称</summary>
        /// <param name="type">指定类型</param>
        /// <param name="isfull">是否全名，包含命名空间</param>
        /// <returns></returns>
        public virtual String GetName(Type type, Boolean isfull) { return isfull ? type.FullName : type.Name; }
        #endregion

        #region 插件
        ///// <summary>是否插件</summary>
        ///// <param name="type">目标类型</param>
        ///// <param name="baseType">基类或接口</param>
        ///// <returns></returns>
        //public virtual Boolean IsSubclassOf(Type type, Type baseType)
        //{
        //    if (type.IsInterface || type.IsAbstract || type.IsGenericType) return false;

        //    if (baseType.IsInterface)
        //    {
        //        var ts = type.GetInterfaces();
        //        if (ts == null || ts.Length < 1) return false;

        //        return Array.IndexOf(ts, baseType) >= 0;
        //    }

        //    //return baseType.IsAssignableFrom(type);
        //    return type.IsSubclassOf(baseType);
        //}

        /// <summary>在指定程序集中查找指定基类的子类</summary>
        /// <param name="asm">指定程序集</param>
        /// <param name="baseType">基类或接口，为空时返回所有类型</param>
        /// <returns></returns>
        public virtual IEnumerable<Type> GetSubclasses(Assembly asm, Type baseType)
        {
            if (asm == null) throw new ArgumentNullException("asm");

            foreach (var item in asm.GetTypes())
            {
                if (baseType == null || baseType.IsAssignableFrom(item))
                    yield return item;
            }
        }

        /// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
        /// <param name="baseType">基类或接口</param>
        /// <param name="isLoadAssembly">是否加载为加载程序集</param>
        /// <returns></returns>
        public virtual IEnumerable<Type> GetAllSubclasses(Type baseType, Boolean isLoadAssembly)
        {
            // 不支持isLoadAssembly
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in GetSubclasses(asm, baseType))
                {
                    yield return type;
                }
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>获取类型，如果target是Type类型，则表示要反射的是静态成员</summary>
        /// <param name="target">目标对象</param>
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