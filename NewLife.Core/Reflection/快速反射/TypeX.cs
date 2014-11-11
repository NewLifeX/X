using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using NewLife.Collections;
using NewLife.Exceptions;
using NewLife.Log;

namespace NewLife.Reflection
{
    /// <summary>类型辅助类</summary>
    public class TypeX : MemberInfoX
    {
        #region 属性
        private Type _Type;
        /// <summary>类型</summary>
        public override Type Type { get { return _Type; } }

        FastHandler _Handler;
        /// <summary>快速调用委托，延迟到首次使用才创建</summary>
        FastHandler Handler
        {
            get
            {
                if (_Handler == null)
                {
                    if (Type.IsValueType || Type.IsArray)
                        _Handler = GetConstructorInvoker(Type, null);
                    else
                    {
                        var cs = Type.GetConstructors(DefaultBinding);
                        if (cs != null && cs.Length > 0) _Handler = GetConstructorInvoker(Type, cs[0]);
                    }
                }
                return _Handler;
            }
        }
        #endregion

        #region 名称
        private String _Name;
        /// <summary>类型名称。主要处理泛型</summary>
        public override String Name { get { return _Name ?? (_Name = GetName(_Type, false)); } }

        private String _FullName;
        /// <summary>完整类型名称。包含命名空间，但是不包含程序集信息</summary>
        public String FullName { get { return _FullName ?? (_FullName = GetName(_Type, true)); } }

        /// <summary>获取类型的友好名称</summary>
        /// <param name="type">指定类型</param>
        /// <param name="isfull">是否全名，包含命名空间</param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static String GetName(Type type, Boolean isfull = false)
        {
            if (type.IsNested) return GetName(type.DeclaringType, isfull) + "." + type.Name;

            if (!type.IsGenericType) return isfull ? type.FullName : type.Name;

            var sb = new StringBuilder();
            var typeDef = type.GetGenericTypeDefinition();
            var name = isfull ? typeDef.FullName : typeDef.Name;
            var p = name.IndexOf("`");
            if (p >= 0)
                sb.Append(name.Substring(0, p));
            else
                sb.Append(name);
            sb.Append("<");
            var ts = type.GetGenericArguments();
            for (int i = 0; i < ts.Length; i++)
            {
                if (i > 0) sb.Append(",");
                if (!ts[i].IsGenericParameter) sb.Append(GetName(ts[i], isfull));
            }
            sb.Append(">");
            return sb.ToString();
        }
        #endregion

        #region 构造
        private TypeX(Type type) : base(type) { _Type = type; }

        private static DictionaryCache<Type, TypeX> cache = new DictionaryCache<Type, TypeX>();
        /// <summary>创建类型辅助对象</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static TypeX Create(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            return cache.GetItem(type, key => new TypeX(key));
        }
        #endregion

        #region 创建动态方法
        delegate Object FastHandler(Object[] parameters);

        private static FastHandler GetConstructorInvoker(Type target, ConstructorInfo constructor)
        {
            // 定义一个没有名字的动态方法。
            // 关联到模块，并且跳过JIT可见性检查，可以访问所有类型的所有成员
            var dynamicMethod = new DynamicMethod(String.Empty, typeof(Object), new Type[] { typeof(Object[]) }, target.Module, true);
            {
                var il = dynamicMethod.GetILGenerator();
                if (target.IsValueType)
                    il.NewValueType(target).BoxIfValueType(target).Ret();
                else if (target.IsArray)
                    il.PushParams(0, new Type[] { typeof(Int32) }).NewArray(target.GetElementType()).Ret();
                else
                    il.PushParams(0, constructor).NewObj(constructor).Ret();
            }
#if DEBUG
            //SaveIL(dynamicMethod, delegate(ILGenerator il)
            //     {
            //         EmitHelper help = new EmitHelper(il);
            //         if (target.IsValueType)
            //             help.NewValueType(target).BoxIfValueType(target).Ret();
            //         else if (target.IsArray)
            //             help.PushParams(0, new Type[] { typeof(Int32) }).NewArray(target.GetElementType()).Ret();
            //         else
            //             help.PushParams(0, constructor).NewObj(constructor).Ret();
            //     });
#endif

            return (FastHandler)dynamicMethod.CreateDelegate(typeof(FastHandler));
        }
        #endregion

        #region 调用
        /// <summary>创建实例</summary>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public override Object CreateInstance(params Object[] parameters)
        {
            if (Type.ContainsGenericParameters || Type.IsGenericTypeDefinition)
                throw new XException(Type.FullName + "类是泛型定义类，缺少泛型参数！");

            if (Type.IsValueType) return Handler.Invoke(parameters);

            // 数组的动态构造参数是元素个数，如果未指定，应该默认0
            if (Type.IsArray)
            {
                if (parameters == null || parameters.Length < 1) parameters = new Object[] { 0 };
                return Handler.Invoke(parameters);
            }

            //// 无参数，直接构造
            //if (parameters == null || parameters.Length < 1) return Handler.Invoke(new Object[0]);

            // 准备参数类型数组，以匹配构造函数
            //var paramTypes = Type.EmptyTypes;
            //if (parameters != null && parameters.Length > 0)
            //{
            //    var list = new List<Type>();
            //    foreach (var item in parameters)
            //    {
            //        if (item != null)
            //            list.Add(item.GetType());
            //        else
            //            list.Add(typeof(Object));
            //    }
            //    paramTypes = list.ToArray();
            //}
            var paramTypes = TypeX.GetTypeArray(parameters);
            var ctor = GetConstructor(paramTypes);
            var handler = GetHandler(ctor);
            if (handler != null) return handler.Invoke(parameters);
            if (paramTypes != Type.EmptyTypes)
            {
                paramTypes = Type.EmptyTypes;
                ctor = GetConstructor(paramTypes);
                handler = GetHandler(ctor);
                if (handler != null)
                {
                    // 更换了构造函数，要重新构造构造函数的参数
                    var ps = ctor.GetParameters();
                    parameters = new Object[ps.Length];
                    for (int i = 0; i < ps.Length; i++)
                    {
                        // 处理值类型
                        if (ps[i].ParameterType.IsValueType)
                            parameters[i] = TypeX.CreateInstance(ps[i].ParameterType);
                        else
                            parameters[i] = null;
                    }

                    // 如果这里创建失败，后面还可以创建一个未初始化
                    try
                    {
                        return handler.Invoke(parameters);
                    }
                    catch { }
                }
            }

            // 如果没有找到构造函数，则创建一个未初始化的对象
            return FormatterServices.GetSafeUninitializedObject(Type);
        }

        DictionaryCache<ConstructorInfo, FastHandler> _cache = new DictionaryCache<ConstructorInfo, FastHandler>();
        FastHandler GetHandler(ConstructorInfo constructor)
        {
            if (constructor == null) return null;

            return _cache.GetItem(constructor, key => GetConstructorInvoker(Type, key));
        }

        ConstructorInfo GetConstructor(Type[] paramTypes)
        {
            var bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // 1，如果参数为null，则找第一个参数
            // 2，根据参数找一次，如果参数为空，也许能找到无参构造函数
            // 3，如果还没找到，参数又为空，采用第一个构造函数。这里之所以没有和第一步合并，主要是可能调用者只想要无参构造函数，而第一个不是

            ConstructorInfo constructor = null;
            if (paramTypes == null)
            {
                constructor = Type.GetConstructors(bf).FirstOrDefault();
                paramTypes = Type.EmptyTypes;
            }
            if (constructor == null) constructor = Type.GetConstructor(bf, null, paramTypes, null);
            //if (constructor == null) throw new Exception("没有找到匹配的构造函数！");
            if (constructor == null && paramTypes == Type.EmptyTypes) constructor = Type.GetConstructors(bf).FirstOrDefault();

            return constructor;
        }

        /// <summary>快速反射创建指定类型的实例</summary>
        /// <param name="type">类型</param>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        public static Object CreateInstance(Type type, params Object[] parameters)
        {
            if (type == null) throw new ArgumentNullException("type");

            return Create(type).CreateInstance(parameters);
        }

        /// <summary>快速反射创建指定类型的实例</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters">参数数组</param>
        /// <returns></returns>
        public static Object CreateInstance<T>(params Object[] parameters)
        {
            return Create(typeof(T)).CreateInstance(parameters);
        }

        /// <summary>取值，返回自己</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Object GetValue(Object obj)
        {
            return obj;
        }
        #endregion

        #region 扩展属性
        /// <summary>是否系统类型</summary>
        /// <returns></returns>
        public Boolean IsSystemType
        {
            get
            {
                return Type.Assembly.FullName.EndsWith("PublicKeyToken=b77a5c561934e089");
            }
        }
        #endregion

        #region 方法
        /// <summary>是否指定类型的插件</summary>
        /// <param name="baseType">指定类型</param>
        /// <returns></returns>
        public Boolean IsPlugin(Type baseType)
        {
            //if (type == null) throw new ArgumentNullException("type");
            // 如果基类为空，则表示是插件
            if (baseType == null) return true;

            // 是否严格匹配。严格匹配仅比较对象引用，否则比较名称
            // 对于只反射类型来说，不需要严格，因为它们不会是同一个引用，一般用于判断是插件意见才加载
            // 对于普通类型，很有可能一个程序集被加载多次，必须严格匹配所引用的就是那个接口类型，否则类型无法转换
            var strict = !Type.Assembly.ReflectionOnly;

            //为空、不是类、抽象类、泛型类 都不是实体类
            //if (!BaseType.IsClass || BaseType.IsAbstract || BaseType.IsGenericType) return false;
            // 允许值类型，仅排除接口
            if (Type.IsInterface || Type.IsAbstract || Type.IsGenericType) return false;

            if (baseType.IsInterface)
            {
                var ts = Type.GetInterfaces();
                if (ts == null || ts.Length < 1) return false;

                if (strict)
                    return Array.IndexOf(ts, baseType) >= 0;
                else
                    return ts.Any(e => e == baseType || e.FullName == baseType.FullName && e.AssemblyQualifiedName == baseType.AssemblyQualifiedName);
            }
            else
            {
                if (baseType.IsAssignableFrom(Type)) return true;

                var e = Type;
                while (e != null && e != typeof(Object))
                {
                    if (strict)
                    {
                        if (e == baseType) return true;
                    }
                    else
                    {
                        if (e.FullName == baseType.FullName && e.AssemblyQualifiedName == baseType.AssemblyQualifiedName) return true;
                    }
                    e = e.BaseType;
                }

                return false;
            }
        }

        /// <summary>根据名称获取类型</summary>
        /// <param name="typeName">类型名</param>
        /// <returns></returns>
        public static Type GetType(String typeName)
        {
            return GetType(typeName, false);
        }

        static DictionaryCache<String, Type> typeCache = new DictionaryCache<String, Type>();
        /// <summary>根据名称获取类型</summary>
        /// <param name="typeName">类型名</param>
        /// <param name="isLoadAssembly">是否从未加载程序集中获取类型。使用仅反射的方法检查目标类型，如果存在，则进行常规加载</param>
        /// <returns></returns>
        public static Type GetType(String typeName, Boolean isLoadAssembly)
        {
            if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");

            //String key = (isLoadAssembly ? "1" : "0") + typeName;

            // isLoadAssembly不参与缓存的键，对于缓存来说，只要能找到类型就行，不必关心是否外部程序集
            return typeCache.GetItem<Boolean>(typeName, isLoadAssembly, GetTypeInternal);
        }

        private static Type GetTypeInternal(String typeName, Boolean isLoadAssembly)
        {
            //if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");

            // 基本获取
            var type = Type.GetType(typeName);
            if (type != null) return type;

            // 处理泛型
            if (typeName[typeName.Length - 1] == '>') return GetGenericType(typeName, isLoadAssembly);

            // 处理数组   有可能是   aa [[ dddd ]]  ,也有可能是  aa[dddd]
            if (typeName[typeName.Length - 1] == ']') return GetArrayType(typeName, isLoadAssembly);

            // 处理内嵌类型

            // 尝试本程序集
            var asms = new[] { 
                AssemblyX.Create(Assembly.GetExecutingAssembly()),
                AssemblyX.Create(Assembly.GetCallingAssembly()), 
                AssemblyX.Create(Assembly.GetEntryAssembly()) };
            var loads = new List<AssemblyX>();

            foreach (var asm in asms)
            {
                if (asm == null || loads.Contains(asm)) continue;
                loads.Add(asm);

                type = asm.GetType(typeName);
                if (type != null) return type;
            }

            // 尝试所有程序集
            foreach (var asm in AssemblyX.GetAssemblies())
            {
                if (loads.Contains(asm)) continue;
                loads.Add(asm);

                type = asm.GetType(typeName);
                if (type != null) return type;
            }

            // 尝试加载只读程序集
            if (isLoadAssembly)
            {
                foreach (var asm in AssemblyX.ReflectionOnlyGetAssemblies())
                {
                    type = asm.GetType(typeName);
                    if (type != null)
                    {
                        // 真实加载
                        //var file = asm.Asm.CodeBase;
                        //if (String.IsNullOrEmpty(file))
                        //    file = asm.Asm.Location;
                        //else if (file.StartsWith("file:///"))
                        //    file = file.Substring("file:///".Length);
                        // ASP.Net中不会锁定原始DLL文件
                        var file = asm.Asm.Location;
                        if (XTrace.Debug) XTrace.WriteLine("TypeX.GetType(\"{0}\")导致加载{1}", typeName, file);
                        var asm2 = Assembly.LoadFile(file);
                        var type2 = AssemblyX.Create(asm2).GetType(typeName);
                        if (type2 != null) type = type2;

                        return type;
                    }
                }
            }

            return null;
        }

        private static Type GetGenericType(String typeName, Boolean isLoadAssembly)
        {
            var start = typeName.IndexOf("<");
            if (start <= 0) return null;

            var end = typeName.LastIndexOf(">");
            // <>也不行
            if (end <= start + 1 || end != typeName.Length - 1) return null;

            // GT<P1,P2,P3,P4>
            var gname = typeName.Substring(0, start);
            var pname = typeName.Substring(start + 1, end - start - 1);
            //pname = "P1,P2<aa,bb>,P3,P4";
            var pnames = new List<String>();

            // 因为泛型参数里面还可能含有泛型，只能用栈来分析泛型参数了
            var count = 0;
            var last = 0;
            for (var i = 0; i < pname.Length; i++)
            {
                var item = pname[i];

                if (item == '<')
                    count++;
                else if (item == '>')
                    count--;
                else if (item == ',' && count == 0)
                {
                    pnames.Add(pname.Substring(last, i - last).Trim());
                    last = i + 1;
                }
            }
            if (last <= pname.Length) pnames.Add(pname.Substring(last, pname.Length - last).Trim());

            // 泛型定义名称等于泛型类名加上参数数量
            gname += "`" + pnames.Count;

            // 先找外部的，如果外部都找不到，那就没意义了
            var gt = GetType(gname, isLoadAssembly);
            if (gt == null) return null;

            var ts = new List<Type>(pnames.Count);
            foreach (var item in pnames)
            {
                // 如果任何一个参数为空，说明这只是一个泛型定义而已
                if (String.IsNullOrEmpty(item)) return gt;

                var t = GetType(item, isLoadAssembly);
                if (t == null) return null;

                ts.Add(t);
            }

            return gt.MakeGenericType(ts.ToArray());
        }

        private static Type GetArrayType(String typeName, Boolean isLoadAssembly)
        {
            // 处理数组   有可能是   aa [[ dddd ]]  ,也有可能是  aa[dddd]
            // 因Json.cs 序列化Dictionary或泛型数组 导致报错，追踪至此作了调整，只是优化了算法，应该不会产生后果  (上海石头 2013.4.8)
            bool blnFlag = false;
            var start = typeName.LastIndexOf("[[");
            if (start > 0) blnFlag = true;
            if (start < 0) start = typeName.LastIndexOf("[");
            if (start > 0) return null;

            Int32 end = typeName.LastIndexOf("]]");
            if (end < 0) end = typeName.LastIndexOf("]");
            if (end > start) return null;

            // Int32[][]  String[,,]
            var gname = typeName.Substring(0, start);
            var pname = "";
            if (blnFlag == false)
                pname = typeName.Substring(start + 1, end - start - 1);
            else
                pname = typeName.Substring(start + 2, end - start - 2);

            // 先找外部的，如果外部都找不到，那就没意义了
            var gt = GetType(gname, isLoadAssembly);
            if (gt == null) return null;

            if (String.IsNullOrEmpty(pname)) return gt.MakeArrayType();

            //pname = ",,,";
            var pnames = pname.Split(new Char[] { ',' });
            if (pnames == null || pnames.Length < 1) return gt.MakeArrayType();

            if (gt.IsGenericType == true)   //如果是泛型对象
            {
                //Type[] tmpType = { Type.GetType(pnames[0]) };
                //return gt.MakeGenericType(tmpType);
                return gt.MakeGenericType(pnames.Select(n => GetType(n, isLoadAssembly)).ToArray());
            }
            else
                return gt.MakeArrayType(pnames.Length);
        }
        #endregion

        #region 获取方法
        /// <summary>获取方法。</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type">类型</param>
        /// <param name="name">名称</param>
        /// <param name="paramTypes"></param>
        /// <returns></returns>
        public static MethodInfo GetMethod(Type type, String name, Type[] paramTypes)
        {
            var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            if (paramTypes == null) paramTypes = Type.EmptyTypes;

            MethodInfo mi = null;
            // 如果没有传入参数类型，则考虑返回第一个符合的
            if (paramTypes.Length <= 0)
            {
                var t = type;
                while (t != null && t != typeof(Object))
                {
                    var mis = t.GetMethods(bf).Where(m => m.Name == name);
                    if (mis != null)
                    {
                        foreach (var item in mis)
                        {
                            // 碰巧找到也是无参的方法
                            if (item.GetParameters().Length == 0) return item;

                            // 记录第一个
                            if (mi == null) mi = item;
                        }
                    }

                    t = t.BaseType;
                }
            }

            {
                var t = type;
                while (t != null && t != typeof(Object))
                {
                    var m = t.GetMethod(name, bf, Binder, paramTypes, null);
                    if (m != null) return m;

                    t = t.BaseType;
                }
            }

            // 如果没有找到匹配项，返回第一个
            return mi;
        }

        /// <summary>获取方法。</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="name">名称</param>
        /// <param name="paramTypes"></param>
        /// <returns></returns>
        public MethodInfoX GetMethod(String name, Type[] paramTypes) { return GetMethod(Type, name, paramTypes); }

        private static Binder _Binder;
        /// <summary>专用绑定器</summary>
        private static Binder Binder { get { return _Binder ?? (_Binder = new MyBinder()); } }

        class MyBinder : Binder
        {
            public override FieldInfo BindToField(BindingFlags bindingAttr, FieldInfo[] match, object value, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public override MethodBase BindToMethod(BindingFlags bindingAttr, MethodBase[] match, ref object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] names, out object state)
            {
                throw new NotImplementedException();
            }

            public override object ChangeType(object value, Type type, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public override void ReorderArgumentArray(ref object[] args, object state)
            {
                throw new NotImplementedException();
            }

            public override MethodBase SelectMethod(BindingFlags bindingAttr, MethodBase[] match, Type[] types, ParameterModifier[] modifiers)
            {
                // 参数个数
                var pcount = types == null ? 0 : types.Length;

                foreach (var item in match)
                {
                    // 参数比对
                    var mps = item.GetParameters();
                    // 无参数时
                    if (mps == null || mps.Length < 1)
                        if (pcount == 0)
                            return item;
                        else
                            continue;

                    // 比对参数个数
                    if (mps.Length != pcount) continue;

                    Boolean valid = true;
                    for (int i = 0; i < pcount; i++)
                    {
                        // 传入的参数类型为空或者Object，可以匹配所有参数
                        if (types[i] == null || types[i] == typeof(Object)) continue;

                        // 检查参数继承，不一定是精确匹配。任何一个不匹配就失败
                        //if (!types[i].IsAssignableFrom(mps[i].ParameterType))
                        if (mps[i].ParameterType != typeof(Object) && !mps[i].ParameterType.IsAssignableFrom(types[i]))
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (valid) return item;
                }

                return null;
            }

            public override PropertyInfo SelectProperty(BindingFlags bindingAttr, PropertyInfo[] match, Type returnType, Type[] indexes, ParameterModifier[] modifiers)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        #region 获取属性或字段
        private PropertyInfoX[] _Properties;
        /// <summary>属性集合</summary>
        public PropertyInfoX[] Properties
        {
            get
            {
                if (_Properties == null)
                {
                    var pis = Type.GetProperties();
                    if (pis == null || pis.Length < 1)
                        _Properties = new PropertyInfoX[0];
                    else
                        _Properties = pis.Select(e => PropertyInfoX.Create(e)).ToArray();
                }
                return _Properties;
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            String des = Description;
            if (!String.IsNullOrEmpty(des))
                return des;
            else
                return Type.FullName;
        }

        /// <summary>判断两个类型是否相同，避免引用加载和执行上下文加载的相同类型显示不同</summary>
        /// <param name="type1"></param>
        /// <param name="type2"></param>
        /// <returns></returns>
        public static Boolean Equal(Type type1, Type type2)
        {
            if (type1 == type2) return true;

            return type1.FullName == type2.FullName && type1.AssemblyQualifiedName == type2.AssemblyQualifiedName;
        }

        /// <summary>类型转换</summary>
        /// <param name="value">数值</param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static Object ChangeType(object value, Type conversionType)
        {
            Type vtype = null;
            if (value != null) vtype = value.GetType();
            //if (vtype == conversionType || conversionType.IsAssignableFrom(vtype)) return value;
            if (vtype == conversionType) return value;

            var cx = Create(conversionType);

            // 处理可空类型
            if (!cx.IsValueType && IsNullable(conversionType))
            {
                if (value == null) return null;

                conversionType = Nullable.GetUnderlyingType(conversionType);
            }

            if (cx.IsEnum)
            {
                if (vtype == _.String)
                    return Enum.Parse(conversionType, (String)value, true);
                else
                    return Enum.ToObject(conversionType, value);
            }

            // 字符串转为货币类型，处理一下
            if (vtype == _.String)
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
                if (cx.IsValueType) value = CreateInstance(conversionType);
            }

            if (conversionType.IsAssignableFrom(vtype)) return value;
            return value;
        }

        /// <summary>类型转换</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static TResult ChangeType<TResult>(Object value)
        {
            if (value is TResult) return (TResult)value;

            return (TResult)ChangeType(value, typeof(TResult));
        }

        /// <summary>判断某个类型是否可空类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static Boolean IsNullable(Type type)
        {
            //if (type.IsValueType) return false;

            if (type.IsGenericType && !type.IsGenericTypeDefinition &&
                object.ReferenceEquals(type.GetGenericTypeDefinition(), typeof(Nullable<>))) return true;

            return false;
        }

        ///// <summary>
        ///// 获取可空类型的真是类型
        ///// </summary>
        ///// <param name="nullableType"></param>
        ///// <returns></returns>
        //public static Type GetUnderlyingType(Type nullableType)
        //{
        //    if (nullableType == null) throw new ArgumentNullException("nullableType");
        //    Type type = null;
        //    if (nullableType.IsGenericType && !nullableType.IsGenericTypeDefinition &&
        //        object.ReferenceEquals(nullableType.GetGenericTypeDefinition(), typeof(Nullable<>))) type = nullableType.GetGenericArguments()[0];
        //    return type;
        //}

        /// <summary>从参数数组中获取类型数组</summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Type[] GetTypeArray(object[] args)
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

        ///// <summary>获取元素类型</summary>
        ///// <returns></returns>
        //public Type GetElementType() { return GetElementType(Type); }

        private static DictionaryCache<Type, Type> _elmCache = new DictionaryCache<Type, Type>();
        /// <summary>获取一个类型的元素类型</summary>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public static Type GetElementType(Type type)
        {
            return _elmCache.GetItem(type, t =>
            {
                if (t.HasElementType) return t.GetElementType();

                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    // 如果实现了IEnumerable<>接口，那么取泛型参数
                    foreach (var item in t.GetInterfaces())
                    {
                        if (item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>)) return item.GetGenericArguments()[0];
                    }
                    // 通过索引器猜测元素类型
                    var pi = type.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (pi != null) return pi.PropertyType;
                }

                return null;
            });
        }
        #endregion

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator Type(TypeX obj)
        {
            return obj != null ? obj.Type : null;
        }

        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator TypeX(Type obj)
        {
            return obj != null ? Create(obj) : null;
        }
        #endregion

        #region 常用类型
        /// <summary>常用类型</summary>
        public static class _
        {
            /// <summary>类型</summary>
            public static readonly Type Type = typeof(Type);

            /// <summary>值类型</summary>
            public static readonly Type ValueType = typeof(ValueType);

            /// <summary>枚举类型</summary>
            public static readonly Type Enum = typeof(Enum);

            /// <summary>对象类型</summary>
            public static readonly Type Object = typeof(Object);

            /// <summary>字符串类型</summary>
            public static readonly Type String = typeof(String);
        }
        #endregion

        #region 原生扩展
        private Boolean initBaseType;
        private TypeX _BaseType;
        /// <summary>基类。因计算类型基类极慢，故缓存</summary>
        /// <remarks><see cref="P:Type.BaseType"/>实在太慢了</remarks>
        public TypeX BaseType
        {
            get
            {
                if (!initBaseType)
                {
                    var bt = Type.BaseType;
                    if (bt != null) _BaseType = TypeX.Create(bt);

                    initBaseType = true;
                }
                return _BaseType;
            }
        }

        /// <summary>确定当前 <see cref="T:System.Type" /> 表示的类是否是从指定的 <see cref="T:System.Type" /> 表示的类派生的。</summary>
        /// <returns>如果 Type 由 <paramref name="c" /> 参数表示并且当前的 Type 表示类，并且当前的 Type 所表示的类是从 <paramref name="c" /> 所表示的类派生的，则为 true；否则为 false。如果 <paramref name="c" /> 和当前的 Type 表示相同的类，则此方法还返回 false。</returns>
        /// <param name="c">与当前的 Type 进行比较的 Type。</param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="c" /> 参数为 null。</exception>
        public Boolean IsSubclassOf(Type c)
        {
            var baseType = this;
            if (baseType.Type != c)
            {
                while (baseType != null)
                {
                    if (baseType.Type == c) return true;
                    baseType = baseType.BaseType;
                }
                return false;
            }
            return false;
        }

        ///// <summary>确定当前的 <see cref="T:System.Type" /> 的实例是否可以从指定 Type 的实例分配。</summary>
        ///// <returns>如果满足下列任一条件，则为 true：<paramref name="c" /> 和当前 Type 表示同一类型；当前 Type 位于 <paramref name="c" /> 的继承层次结构中；当前 Type 是 <paramref name="c" /> 实现的接口；<paramref name="c" /> 是泛型类型参数且当前 Type 表示 <paramref name="c" /> 的约束之一。如果不满足上述任何一个条件或者 <paramref name="c" /> 为 null，则为 false。</returns>
        ///// <param name="c">与当前的 Type 进行比较的 Type。</param>
        //public Boolean IsAssignableFrom(Type c)
        //{
        //    var cx = Create(c);
        //    if (cx.IsSubclassOf(Type)) return true;

        //    return Type.IsAssignableFrom(c);
        //}

        /// <summary>基础类型代码</summary>
        public TypeCode Code { get { return Type.GetTypeCode(Type); } }

        /// <summary>获取一个值，该值指示当前的 <see cref="T:System.Type" /> 是否表示枚举。</summary>
        /// <returns>如果当前 <see cref="T:System.Type" /> 表示枚举，则为 true；否则为 false。</returns>
        public Boolean IsEnum { get { return IsSubclassOf(_.Enum); } }

        /// <summary>是否整型。从Int16到UInt64共六种</summary>
        public Boolean IsInt { get { var code = Code; return code >= TypeCode.Int16 && code <= TypeCode.UInt64; } }

        private Boolean initIsValueType;
        private Boolean _IsValueType;
        /// <summary>获取一个值，通过该值指示 <see cref="T:System.Type" /> 是否为值类型。</summary>
        /// <returns>如果 <see cref="T:System.Type" /> 是值类型，则为 true；否则为 false。</returns>
        public Boolean IsValueType
        {
            get
            {
                if (!initIsValueType)
                {
                    var type = Type;
                    _IsValueType = type != _.ValueType && type != _.Enum && IsSubclassOf(_.ValueType);
                    initIsValueType = true;
                }
                return _IsValueType;
            }
        }
        #endregion
    }
}