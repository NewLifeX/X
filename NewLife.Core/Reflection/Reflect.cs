using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using NewLife.Collections;
using NewLife.Exceptions;

namespace NewLife.Reflection
{
    /// <summary>反射工具类</summary>
    public static class Reflect
    {
        #region 属性
        //private static IReflect _Current = new DefaultReflect();
        private static IReflect _Current = new EmitReflect();
        /// <summary>当前反射提供者</summary>
        public static IReflect Current { get { return _Current; } set { _Current = value; } }
        #endregion

        #region 反射获取
        /// <summary>根据名称获取类型</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        public static Type GetType(String typeName, Boolean isLoadAssembly = true)
        {
            if (String.IsNullOrEmpty(typeName)) return null;

            return _Current.GetType(typeName, isLoadAssembly);
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

            return _Current.GetMethod(type, name, paramTypes);
        }

        /// <summary>获取属性</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static PropertyInfo GetPropertyEx(this Type type, String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return _Current.GetProperty(type, name);
        }

        /// <summary>获取字段</summary>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static FieldInfo GetFieldEx(this Type type, String name)
        {
            if (String.IsNullOrEmpty(name)) return null;

            return _Current.GetField(type, name);
        }
        #endregion

        #region 反射调用
        /// <summary>反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters">参数数组</param>
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
        public static Object Invoke(this Object target, MethodBase method, params Object[] parameters)
        {
            return _Current.Invoke(target, method, parameters);
        }

        /// <summary>获取目标对象指定名称的属性/字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="throwOnError">出错时是否抛出异常</param>
        /// <returns></returns>
        public static Object GetValue(this Object target, String name, Boolean throwOnError = true)
        {
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
            var pi = GetPropertyEx(type, name);
            if (pi != null)
            {
                value = target.GetValue(pi);
                return true;
            }

            var fi = GetFieldEx(type, name);
            if (fi != null)
            {
                value = target.GetValue(fi);
                return true;
            }

            return false;
        }

        /// <summary>获取目标对象的属性值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="property">属性</param>
        /// <returns></returns>
        public static Object GetValue(this Object target, PropertyInfo property)
        {
            return _Current.GetValue(target, property);
        }

        /// <summary>获取目标对象的字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public static Object GetValue(this Object target, FieldInfo field)
        {
            return _Current.GetValue(target, field);
        }

        /// <summary>获取目标对象的成员值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <returns></returns>
        public static Object GetValue(this Object target, MemberInfo member)
        {
            if (member is PropertyInfo)
                return target.GetValue(member as PropertyInfo);
            else if (member is FieldInfo)
                return target.GetValue(member as FieldInfo);
            else
                throw new ArgumentOutOfRangeException("member");
        }

        /// <summary>设置目标对象指定名称的属性/字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="name">名称</param>
        /// <param name="value">数值</param>
        /// <remarks>反射调用是否成功</remarks>
        public static Boolean SetValue(this Object target, String name, Object value)
        {
            if (String.IsNullOrEmpty(name)) return false;

            var type = GetType(ref target);
            var pi = GetPropertyEx(type, name);
            if (pi != null) { target.SetValue(pi, value); return true; }

            var fi = GetFieldEx(type, name);
            if (fi != null) { target.SetValue(fi, value); return true; }

            //throw new ArgumentException("类[" + type.FullName + "]中不存在[" + name + "]属性或字段。");
            return false;
        }

        /// <summary>设置目标对象的属性值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="property">属性</param>
        /// <param name="value">数值</param>
        public static void SetValue(this Object target, PropertyInfo property, Object value)
        {
            _Current.SetValue(target, property, value);
        }

        /// <summary>设置目标对象的字段值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        public static void SetValue(this Object target, FieldInfo field, Object value)
        {
            _Current.SetValue(target, field, value);
        }

        /// <summary>设置目标对象的成员值</summary>
        /// <param name="target">目标对象</param>
        /// <param name="member">成员</param>
        /// <param name="value">数值</param>
        public static void SetValue(this Object target, MemberInfo member, Object value)
        {
            if (member is PropertyInfo)
                _Current.SetValue(target, member as PropertyInfo, value);
            else if (member is FieldInfo)
                _Current.SetValue(target, member as FieldInfo, value);
            else
                throw new ArgumentOutOfRangeException("member");
        }
        #endregion

        #region 类型辅助
        private static DictionaryCache<Type, Type> _elmCache = new DictionaryCache<Type, Type>();
        /// <summary>获取一个类型的元素类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static Type GetElementTypeEx(this Type type)
        {
            return _elmCache.GetItem(type, t =>
            {
                if (t.HasElementType) return t.GetElementType();

                if (typeof(IEnumerable).IsAssignableFrom(t))
                {
                    // 如果实现了IEnumerable<>接口，那么取泛型参数
                    foreach (var item in t.GetInterfaces())
                    {
                        if (item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>)) return item.GetGenericArguments()[0];
                    }
                    // 通过索引器猜测元素类型
                    var pi = t.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pi != null) return pi.PropertyType;
                }

                return null;
            });
        }

        /// <summary>类型转换</summary>
        /// <param name="value">数值</param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static Object ChangeType(this Object value, Type conversionType)
        {
            Type vtype = null;
            if (value != null) vtype = value.GetType();
            //if (vtype == conversionType || conversionType.IsAssignableFrom(vtype)) return value;
            if (vtype == conversionType) return value;

            //var cx = Create(conversionType);

            // 处理可空类型
            if (!conversionType.IsValueType && IsNullable(conversionType))
            {
                if (value == null) return null;

                conversionType = Nullable.GetUnderlyingType(conversionType);
            }

            if (conversionType.IsEnum)
            {
                if (vtype == typeof(String))
                    return Enum.Parse(conversionType, (String)value, true);
                else
                    return Enum.ToObject(conversionType, value);
            }

            // 字符串转为货币类型，处理一下
            if (vtype == typeof(String))
            {
                if (Type.GetTypeCode(conversionType) == TypeCode.Decimal)
                {
                    String str = (String)value;
                    value = str.TrimStart(new Char[] { '$', '￥' });
                }
                else if (typeof(Type).IsAssignableFrom(conversionType))
                {
                    return GetType((String)value, true);
                }
            }

            if (value != null)
            {
                if (value is IConvertible)
                {
                    // 上海石头 发现这里导致Json序列化问题
                    // http://www.newlifex.com/showtopic-282.aspx
                    if (conversionType.IsGenericType && conversionType.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
                    {
                        var nullableConverter = new NullableConverter(conversionType);
                        conversionType = nullableConverter.UnderlyingType;
                    }
                    value = Convert.ChangeType(value, conversionType);
                }
                //else if (conversionType.IsInterface)
                //    value = DuckTyping.Implement(value, conversionType);
            }
            else
            {
                // 如果原始值是null，要转为值类型，则new一个空白的返回
                if (conversionType.IsValueType) value = CreateInstance(conversionType);
            }

            if (conversionType.IsAssignableFrom(vtype)) return value;
            return value;
        }

        /// <summary>类型转换</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static TResult ChangeType<TResult>(this Object value)
        {
            if (value is TResult) return (TResult)value;

            return (TResult)ChangeType(value, typeof(TResult));
        }

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
        #endregion
    }
}