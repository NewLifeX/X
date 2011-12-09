using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NewLife.Reflection;
using System.Diagnostics;
using NewLife.Exceptions;

namespace System
{
    /// <summary>反射扩展</summary>
    public static class ReflectionExtensions
    {
        #region 特性Attribute
        /// <summary>获取自定义属性</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="member"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static TAttribute GetCustomAttribute<TAttribute>(this MemberInfo member, Boolean inherit)
        {
            if (member == null) return default(TAttribute);

            TAttribute[] avs = member.GetCustomAttributes(typeof(TAttribute), inherit) as TAttribute[];
            if (avs == null || avs.Length < 1) return default(TAttribute);

            return avs[0];
        }

        /// <summary>获取自定义属性的值。可用于ReflectionOnly加载的程序集</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static TResult GetCustomAttributeValue<TAttribute, TResult>(this Assembly target)
        {
            if (target == null) return default(TResult);

            IList<CustomAttributeData> list = CustomAttributeData.GetCustomAttributes(target);
            if (list == null || list.Count < 1) return default(TResult);

            foreach (CustomAttributeData item in list)
            {
                if (typeof(TAttribute) != item.Constructor.DeclaringType) continue;

                if (item.ConstructorArguments != null && item.ConstructorArguments.Count > 0)
                    return (TResult)item.ConstructorArguments[0].Value;
            }

            return default(TResult);
        }

        /// <summary>获取自定义属性的值。可用于ReflectionOnly加载的程序集</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="target"></param>
        /// <param name="inherit">是否递归</param>
        /// <returns></returns>
        public static TResult GetCustomAttributeValue<TAttribute, TResult>(this Type target, Boolean inherit)
        {
            if (target == null) return default(TResult);

            IList<CustomAttributeData> list = CustomAttributeData.GetCustomAttributes(target);

            if (list != null && list.Count > 0)
            {
                foreach (CustomAttributeData item in list)
                {
                    if (!TypeX.Equal(typeof(TAttribute), item.Constructor.DeclaringType)) continue;

                    if (item.ConstructorArguments != null && item.ConstructorArguments.Count > 0)
                        return (TResult)item.ConstructorArguments[0].Value;
                }
            }
            if (inherit)
            {
                target = target.BaseType;
                if (target != null && target != typeof(Object))
                    return GetCustomAttributeValue<TAttribute, TResult>(target, inherit);
            }

            return default(TResult);
        }
        #endregion

        #region 程序集Assembly

        #endregion

        #region 类型Type
        /// <summary>查找所有非系统程序集中的所有插件</summary>
        /// <remarks>继承类所在的程序集会引用baseType所在的程序集，利用这一点可以做一定程度的性能优化。</remarks>
        /// <param name="baseType">接口类型或者抽象类型基类</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <param name="excludeGlobalTypes">指示是否应检查来自所有引用程序集的类型。如果为 false，则检查来自所有引用程序集的类型。 否则，只检查来自非全局程序集缓存 (GAC) 引用的程序集的类型。</param>
        /// <returns></returns>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static IEnumerable<Type> FindAllPlugins(this Type baseType, Boolean isLoadAssembly = false, Boolean excludeGlobalTypes = true)
        {
            return AssemblyX.FindAllPlugins(baseType, isLoadAssembly, excludeGlobalTypes);
        }

        /// <summary>快速反射创建指定类型的实例</summary>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static Object CreateInstance(this Type type, params Object[] parameters)
        {
            if (type == null) throw new ArgumentNullException("type");

            return TypeX.CreateInstance(type, parameters);
        }

        /// <summary>类型转换</summary>
        /// <param name="value"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static Object ChangeType(this Object value, Type conversionType)
        {
            return TypeX.ChangeType(value, conversionType);
        }

        /// <summary>类型转换</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static TResult ChangeType<TResult>(this Object value)
        {
            return TypeX.ChangeType<TResult>(value);
        }
        #endregion

        #region 方法MethodInfo
        /// <summary>快速调用成员方法</summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static Object Invoke(this Object target, String name, params Object[] parameters)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            MethodInfoX mix = MethodInfoX.Create(target.GetType(), name, TypeX.GetTypeArray(parameters));
            if (mix == null) throw new XException("类{0}中无法找到{1}方法！", target.GetType().Name, name);

            return mix.Invoke(target, parameters);
        }

        /// <summary>快速调用静态方法</summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static Object Invoke(this Type type, String name, params Object[] parameters)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            MethodInfoX mix = MethodInfoX.Create(type, name, TypeX.GetTypeArray(parameters));
            if (mix == null) throw new XException("类{0}中无法找到{1}方法！", type.Name, name);

            return mix.Invoke(null, parameters);
        }
        #endregion

        #region 属性PropertyInfo
        /// <summary>快速获取静态属性</summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static Object GetPropertyValue(this Type type, String name) { return PropertyInfoX.GetValue(type, null, name); }

        /// <summary>静态属性快速赋值</summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static void SetPropertyValue(this Type type, String name, Object value) { PropertyInfoX.SetValue(type, null, name, value); }

        /// <summary>快速获取成员属性</summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static Object GetPropertyValue(this Object target, String name) { return PropertyInfoX.GetValue(null, target, name); }

        /// <summary>成员属性快速赋值</summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static void SetPropertyValue(this Object target, String name, Object value) { PropertyInfoX.SetValue(null, target, name, value); }
        #endregion

        #region 字段FieldInfo
        /// <summary>快速获取静态字段</summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static Object GetFieldValue(this Type type, String name) { return FieldInfoX.GetValue(type, null, name); }

        /// <summary>静态字段快速赋值</summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static void SetFieldValue(this Type type, String name, Object value) { FieldInfoX.SetValue(type, null, name, value); }

        /// <summary>快速获取成员字段</summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static Object GetFieldValue(this Object target, String name) { return FieldInfoX.GetValue(null, target, name); }

        /// <summary>成员字段快速赋值</summary>
        /// <param name="target"></param>
        /// <param name="name"></param>
        /// <param name="value"></param>
        [Obsolete("该扩展方法将来可能不再被支持！")]
        public static void SetFieldValue(this Object target, String name, Object value) { FieldInfoX.SetValue(null, target, name, value); }
        #endregion
    }
}