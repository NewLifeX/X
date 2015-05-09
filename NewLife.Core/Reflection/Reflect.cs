using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>反射工具类</summary>
    public static class Reflect
    {
        #region 属性
        //private static IReflect _Current = new DefaultReflect();
        private static IReflect _Provider = new EmitReflect();
        /// <summary>当前反射提供者</summary>
        public static IReflect Provider { get { return _Provider; } set { _Provider = value; } }
        #endregion

        #region 反射获取
        /// <summary>根据名称获取类型。可搜索当前目录DLL，自动加载</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        public static Type GetTypeEx(this String typeName, Boolean isLoadAssembly = true)
        {
            if (String.IsNullOrEmpty(typeName)) return null;

            return _Provider.GetType(typeName, isLoadAssembly);
        }

        /// <summary>获取方法</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes">参数类型数组</param>
        /// <returns></returns>
        public static MethodInfo GetMethodEx(this Type type, String name, params Type[] paramTypes)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return _Provider.GetMethod(type, name, paramTypes);
        }

        /// <summary>获取指定名称的方法集合，支持指定参数个数来匹配过滤</summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="paramCount">参数个数，-1表示不过滤参数个数</param>
        /// <returns></returns>
        public static MethodInfo[] GetMethodsEx(this Type type, String name, Int32 paramCount = -1)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return _Provider.GetMethods(type, name, paramCount);
        }

        /// <summary>获取属性。搜索私有、静态、基类，优先返回大小写精确匹配成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyEx(this Type type, String name, Boolean ignoreCase = false)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return _Provider.GetProperty(type, name, ignoreCase);
        }

        /// <summary>获取字段。搜索私有、静态、基类，优先返回大小写精确匹配成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public static FieldInfo GetFieldEx(this Type type, String name, Boolean ignoreCase = false)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return _Provider.GetField(type, name, ignoreCase);
        }

        /// <summary>获取成员。搜索私有、静态、基类，优先返回大小写精确匹配成员</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="ignoreCase">忽略大小写</param>
        /// <returns></returns>
        public static MemberInfo GetMemberEx(this Type type, String name, Boolean ignoreCase = false)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return _Provider.GetMember(type, name, ignoreCase);
        }
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object CreateInstance(this Type type, params Object[] parameters)
        {
            if (type == null) throw new ArgumentNullException("type");

            return _Provider.CreateInstance(type, parameters);
        }

        /// <summary>反射调用指定对象的方法。target为类型时调用其静态方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="name">方法名</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        public static Object Invoke(this Object target, String name, params Object[] parameters)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            Object value = null;
            if (TryInvoke(target, name, out value, parameters)) return value;

            var type = GetType(ref target);
            throw new XException("类{0}中找不到名为{1}的方法！", type, name);
        }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="name">方法名</param>
        /// <param name="value">数值</param>
        /// <param name="parameters">方法参数</param>
        /// <remarks>反射调用是否成功</remarks>
        public static Boolean TryInvoke(this Object target, String name, out Object value, params Object[] parameters)
        {
            value = null;

            if (String.IsNullOrEmpty(name)) return false;

            var type = GetType(ref target);

            // 参数类型数组
            var list = new List<Type>();
            foreach (var item in parameters)
            {
                Type t = null;
                if (item != null) t = item.GetType();

                list.Add(t);
            }

            // 如果参数数组出现null，则无法精确匹配，可按参数个数进行匹配
            var method = GetMethodEx(type, name, list.ToArray());
            if (method == null) return false;

            value = Invoke(target, method, parameters);

            return true;
        }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object Invoke(this Object target, MethodBase method, params Object[] parameters)
        {
            //if (target == null) throw new ArgumentNullException("target");
            if (method == null) throw new ArgumentNullException("method");
            if (!method.IsStatic && target == null) throw new ArgumentNullException("target");

            return _Provider.Invoke(target, method, parameters);
        }

        /// <summary>反射调用指定对象的方法</summary>
        /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
        /// <param name="method">方法</param>
        /// <param name="parameters">方法参数字典</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object InvokeWithParams(this Object target, MethodBase method, IDictionary parameters)
        {
            //if (target == null) throw new ArgumentNullException("target");
            if (method == null) throw new ArgumentNullException("method");
            if (!method.IsStatic && target == null) throw new ArgumentNullException("target");

            return _Provider.InvokeWithParams(target, method, parameters);
        }

        /// <summary>获取目标对象指定名称的属性/字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="throwOnError">出错时是否抛出异常</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object GetValue(this Object target, String name, Boolean throwOnError = true)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            Object value = null;
            if (TryGetValue(target, name, out value)) return value;

            if (!throwOnError) return null;

            var type = GetType(ref target);
            throw new ArgumentException("类[" + type.FullName + "]中不存在[" + name + "]属性或字段。");
        }

        /// <summary>获取目标对象指定名称的属性/字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <returns>是否成功获取数值</returns>
        public static Boolean TryGetValue(this Object target, String name, out Object value)
        {
            value = null;

            if (String.IsNullOrEmpty(name)) return false;

            var type = GetType(ref target);
            //var pi = GetPropertyEx(type, name);
            //if (pi != null)
            //{
            //    value = target.GetValue(pi);
            //    return true;
            //}

            //var fi = GetFieldEx(type, name);
            //if (fi != null)
            //{
            //    value = target.GetValue(fi);
            //    return true;
            //}

            var mi = type.GetMemberEx(name, true);
            if (mi == null) return false;

            value = target.GetValue(mi);

            return true;
        }

        ///// <summary>获取目标对象的属性值</summary>
        ///// <param name="target">目标对象</param>
        ///// <param name="property">属性</param>
        ///// <returns></returns>
        //public static Object GetValue(this Object target, PropertyInfo property)
        //{
        //    return _Provider.GetValue(target, property);
        //}

        ///// <summary>获取目标对象的字段值</summary>
        ///// <param name="target">目标对象</param>
        ///// <param name="field">字段</param>
        ///// <returns></returns>
        //public static Object GetValue(this Object target, FieldInfo field)
        //{
        //    return _Provider.GetValue(target, field);
        //}

        /// <summary>获取目标对象的成员值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <returns></returns>
        [DebuggerHidden]
        public static Object GetValue(this Object target, MemberInfo member)
        {
            if (member is PropertyInfo)
                return _Provider.GetValue(target, member as PropertyInfo);
            else if (member is FieldInfo)
                return _Provider.GetValue(target, member as FieldInfo);
            else
                throw new ArgumentOutOfRangeException("member");
        }

        /// <summary>设置目标对象指定名称的属性/字段值，若不存在返回false</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <remarks>反射调用是否成功</remarks>
        [DebuggerHidden]
        public static Boolean SetValue(this Object target, String name, Object value)
        {
            if (String.IsNullOrEmpty(name)) return false;

            var type = GetType(ref target);
            //var pi = GetPropertyEx(type, name);
            //if (pi != null) { target.SetValue(pi, value); return true; }

            //var fi = GetFieldEx(type, name);
            //if (fi != null) { target.SetValue(fi, value); return true; }

            var mi = type.GetMemberEx(name, true);
            if (mi == null) return false;

            target.SetValue(mi, value);

            //throw new ArgumentException("类[" + type.FullName + "]中不存在[" + name + "]属性或字段。");
            return true;
        }

        ///// <summary>设置目标对象的属性值</summary>
        ///// <param name="target">目标对象</param>
        ///// <param name="property">属性</param>
        ///// <param name="value">数值</param>
        //public static void SetValue(this Object target, PropertyInfo property, Object value)
        //{
        //    _Provider.SetValue(target, property, value);
        //}

        ///// <summary>设置目标对象的字段值</summary>
        ///// <param name="target">目标对象</param>
        ///// <param name="field">字段</param>
        ///// <param name="value">数值</param>
        //public static void SetValue(this Object target, FieldInfo field, Object value)
        //{
        //    _Provider.SetValue(target, field, value);
        //}

        /// <summary>设置目标对象的成员值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="value">数值</param>
        [DebuggerHidden]
        public static void SetValue(this Object target, MemberInfo member, Object value)
        {
            if (member is PropertyInfo)
                _Provider.SetValue(target, member as PropertyInfo, value);
            else if (member is FieldInfo)
                _Provider.SetValue(target, member as FieldInfo, value);
            else
                throw new ArgumentOutOfRangeException("member");
        }
        #endregion

        #region 类型辅助
        /// <summary>获取一个类型的元素类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static Type GetElementTypeEx(this Type type) { return _Provider.GetElementType(type); }

        /// <summary>类型转换</summary>
        /// <param name="value">数值</param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static Object ChangeType(this Object value, Type conversionType) { return _Provider.ChangeType(value, conversionType); }

        /// <summary>类型转换</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static TResult ChangeType<TResult>(this Object value)
        {
            if (value is TResult) return (TResult)value;

            return (TResult)ChangeType(value, typeof(TResult));
        }

        /// <summary>获取类型的友好名称</summary>
        /// <param name="type">指定类型</param>
        /// <param name="isfull">是否全名，包含命名空间</param>
        /// <returns></returns>
        public static String GetName(this Type type, Boolean isfull = false) { return _Provider.GetName(type, isfull); }

        /// <summary>从参数数组中获取类型数组</summary>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static Type[] GetTypeArray(this Object[] args)
        {
            if (args == null) return Type.EmptyTypes;

            var typeArray = new Type[args.Length];
            for (int i = 0; i < typeArray.Length; i++)
            {
                if (args[i] == null)
                    typeArray[i] = typeof(Object);
                else
                    typeArray[i] = args[i].GetType();
            }
            return typeArray;
        }

        /// <summary>获取成员的类型，字段和属性是它们的类型，方法是返回类型，类型是自身</summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static Type GetMemberType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    return (member as ConstructorInfo).DeclaringType;
                case MemberTypes.Field:
                    return (member as FieldInfo).FieldType;
                case MemberTypes.Method:
                    return (member as MethodInfo).ReturnType;
                case MemberTypes.Property:
                    return (member as PropertyInfo).PropertyType;
                case MemberTypes.TypeInfo:
                case MemberTypes.NestedType:
                    return member as Type;
                default:
                    return null;
            }
        }
        #endregion

        #region 插件
        ///// <summary>是否插件</summary>
        ///// <param name="type">目标类型</param>
        ///// <param name="baseType">基类或接口</param>
        ///// <returns></returns>
        //public static Boolean IsSubclassOfEx(this Type type, Type baseType) { return _Provider.IsSubclassOf(type, baseType); }

        /// <summary>在指定程序集中查找指定基类的子类</summary>
        /// <param name="asm">指定程序集</param>
        /// <param name="baseType">基类或接口</param>
        /// <returns></returns>
        public static IEnumerable<Type> GetSubclasses(this Assembly asm, Type baseType)
        {
            return _Provider.GetSubclasses(asm, baseType);
        }

        /// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
        /// <param name="baseType">基类或接口</param>
        /// <param name="isLoadAssembly">是否加载为加载程序集</param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllSubclasses(this Type baseType, Boolean isLoadAssembly = false)
        {
            return _Provider.GetAllSubclasses(baseType, isLoadAssembly);
        }
        #endregion

        #region 辅助方法
        /// <summary>获取类型，如果target是Type类型，则表示要反射的是静态成员</summary>
        /// <param name="target">目标对象</param>
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

        /// <summary>判断某个类型是否可空类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        static Boolean IsNullable(Type type)
        {
            //if (type.IsValueType) return false;

            if (type.IsGenericType && !type.IsGenericTypeDefinition &&
                Object.ReferenceEquals(type.GetGenericTypeDefinition(), typeof(Nullable<>))) return true;

            return false;
        }

        /// <summary>把一个方法转为泛型委托，便于快速反射调用</summary>
        /// <typeparam name="TFunc"></typeparam>
        /// <param name="method"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static TFunc As<TFunc>(this MethodInfo method, Object target = null)
        {
            if (target == null)
                return (TFunc)(Object)Delegate.CreateDelegate(typeof(TFunc), method, true);
            else
                return (TFunc)(Object)Delegate.CreateDelegate(typeof(TFunc), target, method, true);
        }
        #endregion
    }
}