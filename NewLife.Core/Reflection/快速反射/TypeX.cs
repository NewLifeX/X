using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using System.Text;
using NewLife.Collections;
using NewLife.Exceptions;
using NewLife.Linq;

namespace NewLife.Reflection
{
    /// <summary>类型辅助类</summary>
    public class TypeX : MemberInfoX
    {
        #region 属性
        private Type _BaseType;
        /// <summary>类型</summary>
        public Type BaseType
        {
            get { return _BaseType; }
            //set { _Type = value; }
        }

        FastHandler _Handler;
        /// <summary>快速调用委托，延迟到首次使用才创建</summary>
        FastHandler Handler
        {
            get
            {
                if (_Handler == null)
                {
                    if (BaseType.IsValueType || BaseType.IsArray)
                        _Handler = GetConstructorInvoker(BaseType, null);
                    else
                    {
                        //ListX<ConstructorInfo> list = Constructors;
                        ConstructorInfo[] cs = BaseType.GetConstructors(DefaultBinding);
                        if (cs != null && cs.Length > 0) _Handler = GetConstructorInvoker(BaseType, cs[0]);
                    }
                }
                return _Handler;
            }
        }

        private String _Name;
        /// <summary>类型名称。主要处理泛型</summary>
        public override String Name
        {
            get
            {
                if (_Name != null) return _Name;
                _Name = "";

                Type type = BaseType;
                if (type.IsGenericType && !type.IsGenericTypeDefinition)
                {
                    StringBuilder sb = new StringBuilder();
                    String name = type.GetGenericTypeDefinition().Name;
                    sb.Append(name.Substring(0, name.IndexOf("`")));
                    sb.Append("<");
                    Type[] ts = type.GetGenericArguments();
                    for (int i = 0; i < ts.Length; i++)
                    {
                        if (i > 0) sb.Append(",");
                        if (!ts[i].IsGenericParameter) sb.Append(TypeX.Create(ts[i]).Name);
                    }
                    sb.Append(">");
                    _Name = sb.ToString();
                }
                else
                    _Name = type.Name;

                return _Name;
            }
        }

        private String _FullName;
        /// <summary>完整类型名称。包含命名空间，但是不包含程序集信息</summary>
        public String FullName
        {
            get
            {
                if (_FullName != null) return _FullName;
                _FullName = "";

                Type type = BaseType;
                if (type.IsGenericType)
                {
                    StringBuilder sb = new StringBuilder();
                    String name = type.GetGenericTypeDefinition().FullName;
                    sb.Append(name.Substring(0, name.IndexOf("`")));
                    sb.Append("<");
                    Type[] ts = type.GetGenericArguments();
                    for (int i = 0; i < ts.Length; i++)
                    {
                        if (i > 0) sb.Append(",");
                        if (!ts[i].IsGenericParameter) sb.Append(TypeX.Create(ts[i]).FullName);
                    }
                    sb.Append(">");
                    _FullName = sb.ToString();
                }
                else if (type.IsNested)
                {
                    _FullName = TypeX.Create(type.DeclaringType).FullName + "." + type.Name;
                }
                else
                    _FullName = type.FullName;

                return _FullName;
            }
        }
        #endregion

        #region 构造
        private TypeX(Type type) : base(type) { _BaseType = type; }

        private static DictionaryCache<Type, TypeX> cache = new DictionaryCache<Type, TypeX>();
        /// <summary>创建类型辅助对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static TypeX Create(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            return cache.GetItem(type, delegate(Type key)
            {
                return new TypeX(key);
            });
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
        /// <param name="parameters"></param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public override Object CreateInstance(params Object[] parameters)
        {
            if (BaseType.ContainsGenericParameters || BaseType.IsGenericTypeDefinition)
                throw new XException(BaseType.FullName + "类是泛型定义类，缺少泛型参数！");

            if (BaseType.IsValueType || BaseType.IsArray) return Handler.Invoke(parameters);

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
            return FormatterServices.GetSafeUninitializedObject(BaseType);
        }

        DictionaryCache<ConstructorInfo, FastHandler> _cache = new DictionaryCache<ConstructorInfo, FastHandler>();
        FastHandler GetHandler(ConstructorInfo constructor)
        {
            if (constructor == null) return null;

            return _cache.GetItem(constructor, key => GetConstructorInvoker(BaseType, key));
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
                constructor = BaseType.GetConstructors(bf).FirstOrDefault();
                paramTypes = Type.EmptyTypes;
            }
            if (constructor == null) constructor = BaseType.GetConstructor(bf, null, paramTypes, null);
            //if (constructor == null) throw new Exception("没有找到匹配的构造函数！");
            if (constructor == null && paramTypes == Type.EmptyTypes) constructor = BaseType.GetConstructors(bf).FirstOrDefault();

            return constructor;
        }

        /// <summary>快速反射创建指定类型的实例</summary>
        /// <param name="type"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static Object CreateInstance(Type type, params Object[] parameters)
        {
            if (type == null) throw new ArgumentNullException("type");

            return Create(type).CreateInstance(parameters);
        }

        /// <summary>快速反射创建指定类型的实例</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters"></param>
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
        private List<String> hasLoad = new List<String>();

        /// <summary>是否系统类型</summary>
        /// <returns></returns>
        public Boolean IsSystemType
        {
            get
            {
                return BaseType.Assembly.FullName.EndsWith("PublicKeyToken=b77a5c561934e089");
            }
        }

        private String _Description;
        /// <summary>说明</summary>
        public String Description
        {
            get
            {
                if (String.IsNullOrEmpty(_Description) && !hasLoad.Contains("Description"))
                {
                    hasLoad.Add("Description");

                    _Description = GetCustomAttributeValue<DescriptionAttribute, String>();
                }
                return _Description;
            }
            //set { _Description = value; }
        }
        #endregion

        #region 方法
        /// <summary>是否指定类型的插件</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Boolean IsPlugin(Type type)
        {
            //if (type == null) throw new ArgumentNullException("type");
            // 如果基类为空，则表示是插件
            if (type == null) return true;

            //为空、不是类、抽象类、泛型类 都不是实体类
            //if (!BaseType.IsClass || BaseType.IsAbstract || BaseType.IsGenericType) return false;
            // 允许值类型，仅排除接口
            if (BaseType.IsInterface || BaseType.IsAbstract || BaseType.IsGenericType) return false;

            if (type.IsInterface)
            {
                ////if (!Interfaces.Contains(type)) return false;
                //if (Interfaces == null || Interfaces.Count < 1) return false;

                //Boolean b = false;
                //foreach (Type item in Interfaces)
                //{
                //    if (item == type) { b = true; break; }

                //    if (item.FullName == type.FullName && item.AssemblyQualifiedName == type.AssemblyQualifiedName) { b = true; break; }
                //}
                //if (!b) return false;

                Type[] ts = BaseType.GetInterfaces();
                if (ts == null || ts.Length < 1) return false;

                return ts.Any(e => e == type || e.FullName == type.FullName && e.AssemblyQualifiedName == type.AssemblyQualifiedName);
            }
            else
            {
                if (!type.IsAssignableFrom(BaseType)) return false;
            }

            return true;
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
            if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");

            // 基本获取
            Type type = Type.GetType(typeName);
            if (type != null) return type;

            #region 处理泛型
            Int32 start = typeName.IndexOf("<");
            if (start > 0)
            {
                Int32 end = typeName.LastIndexOf(">");
                // <>也不行
                if (end > start + 1)
                {
                    // GT<P1,P2,P3,P4>
                    String gname = typeName.Substring(0, start);
                    String pname = typeName.Substring(start + 1, end - start - 1);
                    //pname = "P1,P2<aa,bb>,P3,P4";
                    List<String> pnames = new List<String>();

                    // 只能用栈来分析泛型参数了
                    Int32 count = 0;
                    Int32 last = 0;
                    for (Int32 i = 0; i < pname.Length; i++)
                    {
                        Char item = pname[i];

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

                    gname += "`" + pnames.Count;

                    // 先找外部的，如果外部都找不到，那就没意义了
                    Type gt = GetType(gname, isLoadAssembly);
                    if (gt == null) return null;

                    List<Type> ts = new List<Type>(pnames.Count);
                    foreach (String item in pnames)
                    {
                        // 如果任何一个参数为空，说明这只是一个泛型定义而已
                        if (String.IsNullOrEmpty(item)) return gt;

                        Type t = GetType(item, isLoadAssembly);
                        if (t == null) return null;

                        ts.Add(t);
                    }

                    return gt.MakeGenericType(ts.ToArray());
                }
            }
            #endregion

            #region 处理数组
            start = typeName.LastIndexOf("[");
            if (start > 0)
            {
                Int32 end = typeName.LastIndexOf("]");
                if (end > start)
                {
                    // Int32[][]  String[,,]
                    String gname = typeName.Substring(0, start);
                    String pname = typeName.Substring(start + 1, end - start - 1);

                    // 先找外部的，如果外部都找不到，那就没意义了
                    Type gt = GetType(gname, isLoadAssembly);
                    if (gt == null) return null;

                    if (String.IsNullOrEmpty(pname)) return gt.MakeArrayType();

                    //pname = ",,,";
                    String[] pnames = pname.Split(new Char[] { ',' });
                    if (pnames == null || pnames.Length < 1) return gt.MakeArrayType();

                    return gt.MakeArrayType(pnames.Length);
                }
            }
            #endregion

            // 处理内嵌类型

            // 尝试本程序集
            Assembly[] asms = new[] { 
                Assembly.GetExecutingAssembly(),
                Assembly.GetCallingAssembly(), 
                Assembly.GetEntryAssembly() };
            var loads = new List<Assembly>();

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
                if (loads.Contains(asm.Asm)) continue;
                loads.Add(asm.Asm);

                type = asm.GetType(typeName);
                if (type != null) return type;
            }

            if (isLoadAssembly)
            {
                foreach (AssemblyX asm in AssemblyX.ReflectionOnlyGetAssemblies())
                {
                    type = asm.GetType(typeName);
                    if (type != null)
                    {
                        // 真实加载
                        Assembly asm2 = Assembly.LoadFile(asm.Asm.Location);
                        Type type2 = AssemblyX.Create(asm2).GetType(typeName);
                        if (type2 != null) type = type2;

                        return type;
                    }
                }
            }

            //// 尝试系统的
            //if (!typeName.Contains("."))
            //{
            //    type = Type.GetType("System." + typeName);
            //    if (type != null) return type;
            //    type = Type.GetType("NewLife." + typeName);
            //    if (type != null) return type;
            //}

            return null;
        }

        //static DictionaryCache<String, Type> typeCache2 = new DictionaryCache<String, Type>();
        ///// <summary>
        ///// 从指定程序集查找指定名称的类型，如果查找不到，则进行忽略大小写的查找
        ///// </summary>
        ///// <param name="asm"></param>
        ///// <param name="typeName"></param>
        ///// <returns></returns>
        //public static Type GetType(Assembly asm, String typeName)
        //{
        //    if (asm == null) throw new ArgumentNullException("asm");
        //    if (String.IsNullOrEmpty(typeName)) throw new ArgumentNullException("typeName");

        //    return typeCache2.GetItem<Assembly>(typeName, asm, GetTypeInternal);
        //}

        //private static Type GetTypeInternal(String typeName, Assembly asm)
        //{
        //    Type type = asm.GetType(typeName);
        //    if (type == null) type = asm.GetType(typeName, false, true);
        //    if (type == null)
        //    {
        //        Type[] ts = asm.GetTypes();
        //        foreach (Type item in ts)
        //        {
        //            if (item.Name == typeName)
        //            {
        //                type = item;
        //                break;
        //            }
        //        }
        //        if (type == null)
        //        {
        //            foreach (Type item in ts)
        //            {
        //                if (String.Equals(item.Name, typeName, StringComparison.OrdinalIgnoreCase))
        //                {
        //                    type = item;
        //                    break;
        //                }
        //            }
        //        }
        //    }

        //    return type;
        //}
        #endregion

        #region 获取方法
        //private DictionaryCache<String, MethodInfo> _mdCache = new DictionaryCache<String, MethodInfo>();

        /// <summary>获取方法。</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="paramTypes"></param>
        /// <returns></returns>
        public static MethodInfo GetMethod(Type type, String name, Type[] paramTypes)
        {
            var bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            if (paramTypes == null) paramTypes = Type.EmptyTypes;
            MethodInfo mi = null;
            while (type != null && type != typeof(Object))
            {
                mi = type.GetMethod(name, bf, Binder, paramTypes, null);
                if (mi != null) return mi;

                type = type.BaseType;
            }
            return mi;
        }

        /// <summary>获取方法。</summary>
        /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
        /// <param name="name"></param>
        /// <param name="paramTypes"></param>
        /// <returns></returns>
        public MethodInfo GetMethod(String name, Type[] paramTypes) { return GetMethod(BaseType, name, paramTypes); }

        //private MethodInfo[] _Methods;
        ///// <summary>方法集合</summary>
        //private MethodInfo[] Methods { get { return _Methods ?? (_Methods = BaseType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance) ?? new MethodInfo[0]); } }

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
                        if (!types[i].IsAssignableFrom(mps[i].ParameterType))
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

        #region 辅助方法
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            String des = Description;
            if (!String.IsNullOrEmpty(des))
                return des;
            else
                return BaseType.FullName;
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

        /// <summary>获取自定义属性的值。可用于ReflectionOnly加载的程序集</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult GetCustomAttributeValue<TAttribute, TResult>()
        {
            return BaseType.GetCustomAttributeValue<TAttribute, TResult>(true);
        }

        /// <summary>类型转换</summary>
        /// <param name="value"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static Object ChangeType(object value, Type conversionType)
        {
            Type vtype = null;
            if (value != null) vtype = value.GetType();
            if (vtype == conversionType || conversionType.IsAssignableFrom(vtype)) return value;

            // 处理可空类型
            if (IsNullable(conversionType))
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
                    value = Convert.ChangeType(value, conversionType);
                else if (conversionType.IsInterface)
                    value = DuckTyping.Implement(value, conversionType);
            }
            else
            {
                // 如果原始值是null，要转为值类型，则new一个空白的返回
                if (conversionType.IsValueType) value = CreateInstance(conversionType);
            }

            return value;
        }

        /// <summary>类型转换</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static TResult ChangeType<TResult>(Object value)
        {
            if (value is TResult) return (TResult)value;

            return (TResult)ChangeType(value, typeof(TResult));
        }

        /// <summary>判断某个类型是否可空类型</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean IsNullable(Type type)
        {
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

        /// <summary>获取元素类型</summary>
        /// <returns></returns>
        public Type GetElementType() { return GetElementType(BaseType); }

        private static DictionaryCache<Type, Type> _elmCache = new DictionaryCache<Type, Type>();
        /// <summary>获取一个类型的元素类型</summary>
        /// <param name="type"></param>
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
    }
}