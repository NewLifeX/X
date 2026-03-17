using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Data;

namespace NewLife.Reflection;

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
    Type? GetType(String typeName, Boolean isLoadAssembly);

    /// <summary>获取方法</summary>
    /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
    /// <param name="type">类型</param>
    /// <param name="name">名称</param>
    /// <param name="paramTypes">参数类型数组</param>
    /// <returns></returns>
    MethodInfo? GetMethod(Type type, String name, params Type[] paramTypes);

    /// <summary>获取指定名称的方法集合，支持指定参数个数来匹配过滤</summary>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="paramCount">参数个数，-1表示不过滤参数个数</param>
    /// <returns></returns>
    MethodInfo[] GetMethods(Type type, String name, Int32 paramCount = -1);

    /// <summary>获取属性</summary>
    /// <param name="type">类型</param>
    /// <param name="name">名称</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    PropertyInfo? GetProperty(Type type, String name, Boolean ignoreCase);

    /// <summary>获取字段</summary>
    /// <param name="type">类型</param>
    /// <param name="name">名称</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    FieldInfo? GetField(Type type, String name, Boolean ignoreCase);

    /// <summary>获取成员</summary>
    /// <param name="type">类型</param>
    /// <param name="name">名称</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    MemberInfo? GetMember(Type type, String name, Boolean ignoreCase);

    /// <summary>获取字段</summary>
    /// <param name="type"></param>
    /// <param name="baseFirst"></param>
    /// <returns></returns>
    IList<FieldInfo> GetFields(Type type, Boolean baseFirst = true);

    /// <summary>获取属性</summary>
    /// <param name="type"></param>
    /// <param name="baseFirst"></param>
    /// <returns></returns>
    IList<PropertyInfo> GetProperties(Type type, Boolean baseFirst = true);
    #endregion

    #region 反射调用
    /// <summary>反射创建指定类型的实例</summary>
    /// <param name="type">类型</param>
    /// <param name="parameters">参数数组</param>
    /// <returns></returns>
    Object? CreateInstance(Type type, params Object?[] parameters);

    /// <summary>反射调用指定对象的方法</summary>
    /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
    /// <param name="method">方法</param>
    /// <param name="parameters">方法参数</param>
    /// <returns></returns>
    Object? Invoke(Object? target, MethodBase method, params Object?[]? parameters);

    /// <summary>反射调用指定对象的方法</summary>
    /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
    /// <param name="method">方法</param>
    /// <param name="parameters">方法参数字典</param>
    /// <returns></returns>
    Object? InvokeWithParams(Object? target, MethodBase method, IDictionary? parameters);

    /// <summary>获取目标对象的属性值</summary>
    /// <param name="target">目标对象</param>
    /// <param name="property">属性</param>
    /// <returns></returns>
    Object? GetValue(Object? target, PropertyInfo property);

    /// <summary>获取目标对象的字段值</summary>
    /// <param name="target">目标对象</param>
    /// <param name="field">字段</param>
    /// <returns></returns>
    Object? GetValue(Object? target, FieldInfo field);

    /// <summary>设置目标对象的属性值</summary>
    /// <param name="target">目标对象</param>
    /// <param name="property">属性</param>
    /// <param name="value">数值</param>
    void SetValue(Object target, PropertyInfo property, Object? value);

    /// <summary>设置目标对象的字段值</summary>
    /// <param name="target">目标对象</param>
    /// <param name="field">字段</param>
    /// <param name="value">数值</param>
    void SetValue(Object target, FieldInfo field, Object? value);

    /// <summary>从源对象拷贝数据到目标对象</summary>
    /// <param name="target">目标对象</param>
    /// <param name="src">源对象</param>
    /// <param name="deep">递归深度拷贝，直接拷贝成员值而不是引用</param>
    /// <param name="excludes">要忽略的成员</param>
    void Copy(Object target, Object src, Boolean deep = false, params String[] excludes);

    /// <summary>从源字典拷贝数据到目标对象</summary>
    /// <param name="target">目标对象</param>
    /// <param name="dic">源字典</param>
    /// <param name="deep">递归深度拷贝，直接拷贝成员值而不是引用</param>
    void Copy(Object target, IDictionary<String, Object?> dic, Boolean deep = false);
    #endregion

    #region 类型辅助
    /// <summary>获取一个类型的元素类型</summary>
    /// <param name="type">类型</param>
    /// <returns></returns>
    Type? GetElementType(Type type);

    /// <summary>类型转换</summary>
    /// <param name="value">数值</param>
    /// <param name="conversionType"></param>
    /// <returns></returns>
    Object? ChangeType(Object? value, Type conversionType);

    /// <summary>获取类型的友好名称</summary>
    /// <param name="type">指定类型</param>
    /// <param name="isfull">是否全名，包含命名空间</param>
    /// <returns></returns>
    String GetName(Type type, Boolean isfull);
    #endregion

    #region 插件
    /// <summary>是否能够转为指定基类</summary>
    /// <param name="type"></param>
    /// <param name="baseType"></param>
    /// <returns></returns>
    Boolean As(Type type, Type baseType);

    /// <summary>在指定程序集中查找指定基类或接口的所有子类实现</summary>
    /// <param name="asm">指定程序集</param>
    /// <param name="baseType">基类或接口，为空时返回所有类型</param>
    /// <returns></returns>
    IEnumerable<Type> GetSubclasses(Assembly asm, Type baseType);

    /// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
    /// <param name="baseType">基类或接口</param>
    /// <returns></returns>
    IEnumerable<Type> GetAllSubclasses(Type baseType);

    ///// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
    ///// <param name="baseType">基类或接口</param>
    ///// <param name="isLoadAssembly">是否加载为加载程序集</param>
    ///// <returns></returns>
    //IEnumerable<Type> GetAllSubclasses(Type baseType, Boolean isLoadAssembly);
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
    public virtual Type? GetType(String typeName, Boolean isLoadAssembly) => AssemblyX.GetType(typeName, isLoadAssembly);

    private static readonly BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    private static readonly BindingFlags bfic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.IgnoreCase;

    /// <summary>获取方法</summary>
    /// <remarks>用于具有多个签名的同名方法的场合，不确定是否存在性能问题，不建议普通场合使用</remarks>
    /// <param name="type">类型</param>
    /// <param name="name">名称</param>
    /// <param name="paramTypes">参数类型数组</param>
    /// <returns></returns>
    public virtual MethodInfo? GetMethod(Type type, String name, params Type[] paramTypes)
    {
        MethodInfo? mi = null;
        while (true)
        {
            if (paramTypes == null || paramTypes.Length == 0)
                mi = type.GetMethod(name, bf);
            else
                mi = type.GetMethod(name, bf, null, paramTypes, null);
            if (mi != null) return mi;

            if (type.BaseType == null) break;
            type = type.BaseType;
            if (type == null || type == typeof(Object)) break;
        }
        return null;
    }

    /// <summary>获取指定名称的方法集合，支持指定参数个数来匹配过滤</summary>
    /// <param name="type"></param>
    /// <param name="name"></param>
    /// <param name="paramCount">参数个数，-1表示不过滤参数个数</param>
    /// <returns></returns>
    public virtual MethodInfo[] GetMethods(Type type, String name, Int32 paramCount = -1)
    {
        var ms = type.GetMethods(bf);
        //if (ms == null || ms.Length == 0) return ms;

        var list = new List<MethodInfo>();
        foreach (var item in ms)
        {
            if (item.Name == name)
            {
                if (paramCount >= 0 && item.GetParameters().Length == paramCount) list.Add(item);
            }
        }
        return list.ToArray();
    }

    /// <summary>获取属性</summary>
    /// <param name="type">类型</param>
    /// <param name="name">名称</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public virtual PropertyInfo? GetProperty(Type type, String name, Boolean ignoreCase)
    {
        // 父类私有属性的获取需要递归，可见范围则不需要，有些类型的父类为空，比如接口
        var type2 = type;
        while (type2 != null && type2 != typeof(Object))
        {
            //var pi = type.GetProperty(name, ignoreCase ? bfic : bf);
            var pi = type2.GetProperty(name, bf);
            if (pi != null) return pi;
            if (ignoreCase)
            {
                pi = type2.GetProperty(name, bfic);
                if (pi != null) return pi;
            }

            type2 = type2.BaseType;
        }
        return null;
    }

    /// <summary>获取字段</summary>
    /// <param name="type">类型</param>
    /// <param name="name">名称</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public virtual FieldInfo? GetField(Type type, String name, Boolean ignoreCase)
    {
        // 父类私有字段的获取需要递归，可见范围则不需要，有些类型的父类为空，比如接口
        var type2 = type;
        while (type2 != null && type2 != typeof(Object))
        {
            //var fi = type.GetField(name, ignoreCase ? bfic : bf);
            var fi = type2.GetField(name, bf);
            if (fi != null) return fi;
            if (ignoreCase)
            {
                fi = type2.GetField(name, bfic);
                if (fi != null) return fi;
            }

            type2 = type2.BaseType;
        }
        return null;
    }

    /// <summary>获取成员</summary>
    /// <param name="type">类型</param>
    /// <param name="name">名称</param>
    /// <param name="ignoreCase">忽略大小写</param>
    /// <returns></returns>
    public virtual MemberInfo? GetMember(Type type, String name, Boolean ignoreCase)
    {
        // 父类私有成员的获取需要递归，可见范围则不需要，有些类型的父类为空，比如接口
        var type2 = type;
        while (type2 != null && type2 != typeof(Object))
        {
            var fs = type2.GetMember(name, ignoreCase ? bfic : bf);
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

            type2 = type2.BaseType;
        }
        return null;
    }
    #endregion

    #region 反射获取 字段/属性
    private readonly ConcurrentDictionary<Type, IList<FieldInfo>> _cache1 = new();
    private readonly ConcurrentDictionary<Type, IList<FieldInfo>> _cache2 = new();
    /// <summary>获取字段</summary>
    /// <param name="type"></param>
    /// <param name="baseFirst"></param>
    /// <returns></returns>
    public virtual IList<FieldInfo> GetFields(Type type, Boolean baseFirst = true)
    {
        if (baseFirst)
            return _cache1.GetOrAdd(type, key => GetFields2(key, true));
        else
            return _cache2.GetOrAdd(type, key => GetFields2(key, false));
    }

    private IList<FieldInfo> GetFields2(Type type, Boolean baseFirst)
    {
        var list = new List<FieldInfo>();

        // Void*的基类就是null
        if (type == typeof(Object) || type.BaseType == null) return list;

        if (baseFirst) list.AddRange(GetFields(type.BaseType));

        var fis = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var fi in fis)
        {
            if (fi.GetCustomAttribute<NonSerializedAttribute>() != null) continue;

            list.Add(fi);
        }

        if (!baseFirst) list.AddRange(GetFields(type.BaseType));

        return list;
    }

    private readonly ConcurrentDictionary<Type, IList<PropertyInfo>> _cache3 = new();
    private readonly ConcurrentDictionary<Type, IList<PropertyInfo>> _cache4 = new();
    /// <summary>获取属性</summary>
    /// <param name="type"></param>
    /// <param name="baseFirst"></param>
    /// <returns></returns>
    public virtual IList<PropertyInfo> GetProperties(Type type, Boolean baseFirst = true)
    {
        if (baseFirst)
            return _cache3.GetOrAdd(type, key => GetProperties2(key, true));
        else
            return _cache4.GetOrAdd(type, key => GetProperties2(key, false));
    }

    private IList<PropertyInfo> GetProperties2(Type type, Boolean baseFirst)
    {
        var list = new List<PropertyInfo>();

        // Void*的基类就是null
        if (type == typeof(Object) || type.BaseType == null) return list;

        // 本身type.GetProperties就可以得到父类属性，只是不能保证父类属性在子类属性之前
        if (baseFirst) list.AddRange(GetProperties(type.BaseType));

        // 父类子类可能因为继承而有重名的属性，此时以子类优先，否则反射父类属性会出错
        var set = new HashSet<String>(list.Select(e => e.Name));

        //var pis = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var pis = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var pi in pis)
        {
            if (pi.GetIndexParameters().Length > 0) continue;
            if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
            if (pi.GetCustomAttribute<ScriptIgnoreAttribute>() != null) continue;
            if (pi.GetCustomAttribute<IgnoreDataMemberAttribute>() != null) continue;

            if (!set.Contains(pi.Name))
            {
                list.Add(pi);
                set.Add(pi.Name);
            }
        }

        // 获取用于序列化的属性列表时，加上非公有的数据成员
        pis = type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);
        foreach (var pi in pis)
        {
            if (pi.GetIndexParameters().Length > 0) continue;
            if (pi.GetCustomAttribute<XmlElementAttribute>() == null && pi.GetCustomAttribute<DataMemberAttribute>() == null) continue;

            if (!set.Contains(pi.Name))
            {
                list.Add(pi);
                set.Add(pi.Name);
            }
        }

        if (!baseFirst) list.AddRange(GetProperties(type.BaseType).Where(e => !set.Contains(e.Name)));

        // 如果有属性定义了 DataObjectFieldAttribute ，则让它们排在前面，避免XCode实体类中的扩展属性排在前面
        if (list.Any(e => e.GetCustomAttribute<DataObjectFieldAttribute>() != null))
        {
            var list2 = new List<PropertyInfo>();
            var list3 = new List<PropertyInfo>();
            foreach (var pi in list)
            {
                if (pi.GetCustomAttribute<DataObjectFieldAttribute>() != null)
                    list2.Add(pi);
                else
                    list3.Add(pi);
            }
            list2.AddRange(list3);

            list = list2;
        }

        return list;
    }
    #endregion

    #region 反射调用
    // 写时复制字典（全部为静态）：热路径仅需一次静态 volatile 读 + Dictionary.TryGetValue，无锁无CAS；
    // 静态字段使 Reflect.cs 扩展方法可直接访问，省去 _directProvider volatile 读 + 接口分派（约 2 ns）；
    // 冷路径在 lock 内生成新快照后原子发布，首次编译后后续调用开销极低
    internal static volatile Dictionary<PropertyInfo, Func<Object?, Object?>?> _propGetterDict = [];
    internal static volatile Dictionary<PropertyInfo, Action<Object?, Object?>?> _propSetterDict = [];
    internal static volatile Dictionary<FieldInfo, Func<Object?, Object?>?> _fieldGetterDict = [];
    internal static volatile Dictionary<FieldInfo, Action<Object?, Object?>?> _fieldSetterDict = [];
    // 0-param 专用缓存：Func<Object?,Object?> 省去 Object[] 参数数组开销
    internal static volatile Dictionary<MethodInfo, Func<Object?, Object?>?> _invoker0Dict = [];
    // N-param 通用缓存：Func<Object?,Object?[]?,Object?>
    internal static volatile Dictionary<MethodInfo, Func<Object?, Object?[]?, Object?>?> _invokerNDict = [];
    internal static volatile Dictionary<Type, Func<Object>?> _instanceFactoryDict = BuildPrimitiveFactories();
    private static readonly Object _cacheLock = new();

    // 预填充基元类型工厂委托，捕获缓存装箱对象避免每次 CreateInstance 重复分配
    private static Dictionary<Type, Func<Object>?> BuildPrimitiveFactories()
    {
        Object boxFalse = false, boxChar0 = '\0', boxSByte0 = (SByte)0, boxByte0 = (Byte)0;
        Object boxI160 = (Int16)0, boxU160 = (UInt16)0, boxI320 = 0, boxU320 = 0U;
        Object boxI640 = 0L, boxU640 = 0UL, boxF0 = 0F, boxD0 = 0D, boxM0 = 0M;
        Object boxDt = DateTime.MinValue;
        return new()
        {
            [typeof(Boolean)] = () => boxFalse,
            [typeof(Char)] = () => boxChar0,
            [typeof(SByte)] = () => boxSByte0,
            [typeof(Byte)] = () => boxByte0,
            [typeof(Int16)] = () => boxI160,
            [typeof(UInt16)] = () => boxU160,
            [typeof(Int32)] = () => boxI320,
            [typeof(UInt32)] = () => boxU320,
            [typeof(Int64)] = () => boxI640,
            [typeof(UInt64)] = () => boxU640,
            [typeof(Single)] = () => boxF0,
            [typeof(Double)] = () => boxD0,
            [typeof(Decimal)] = () => boxM0,
            [typeof(DateTime)] = () => boxDt,
            [typeof(String)] = static () => String.Empty,
        };
    }

    /// <summary>反射创建指定类型的实例</summary>
    /// <param name="type">类型</param>
    /// <param name="parameters">参数数组</param>
    /// <returns></returns>
    public virtual Object? CreateInstance(Type type, params Object?[] parameters) => CreateInstanceCore(type, parameters);

    // 非虚核心实现，供 Reflect.cs 直接调用
    internal Object? CreateInstanceCore(Type type, Object?[]? parameters)
    {
        try
        {
            if (parameters == null || parameters.Length == 0)
            {
                // 热路径：已缓存的工厂委托，绕过所有 TypeCode/IList/IDictionary 预检
                if (_instanceFactoryDict.TryGetValue(type, out var factory))
                    return factory != null ? factory() : Activator.CreateInstance(type, true);

                return RegisterAndCreate(type);
            }
            return Activator.CreateInstance(type, parameters);
        }
        catch (Exception ex)
        {
            throw new Exception($"Fail to create object type={type.FullName} parameters={parameters?.Join()} {ex.GetTrue()?.Message}", ex);
        }
    }

    // 冷路径：首次为该类型生成工厂并注册到缓存
    private Object? RegisterAndCreate(Type type)
    {
        Func<Object>? factory;
        var code = type.GetTypeCode();
        if (code != TypeCode.Object)
        {
            // 枚举等 TypeCode 非 Object 类型（基元已在 BuildPrimitiveFactories 预填充，此处兜底）
            factory = code switch
            {
                TypeCode.Boolean => static () => false,
                TypeCode.Char => static () => '\0',
                TypeCode.SByte => static () => (SByte)0,
                TypeCode.Byte => static () => (Byte)0,
                TypeCode.Int16 => static () => (Int16)0,
                TypeCode.UInt16 => static () => (UInt16)0,
                TypeCode.Int32 => static () => 0,
                TypeCode.UInt32 => static () => 0U,
                TypeCode.Int64 => static () => 0L,
                TypeCode.UInt64 => static () => 0UL,
                TypeCode.Single => static () => 0F,
                TypeCode.Double => static () => 0D,
                TypeCode.Decimal => static () => 0M,
                TypeCode.DateTime => static () => DateTime.MinValue,
                TypeCode.String => static () => String.Empty,
                _ => null,
            };
        }
        else
        {
            // IList / IDictionary 接口解析（冷路径，仅首次调用该接口类型）
            var targetType = type;
            if (type.IsInterface || type.IsAbstract)
            {
                if (type.As<IList>() || type.As(typeof(IList<>)))
                {
                    if (type.IsGenericType)
                        targetType = typeof(List<>).MakeGenericType(type.GetGenericArguments());
                    else if (type == typeof(IList))
                        targetType = typeof(List<Object>);
                }
                else if (type.As<IDictionary>() || type.As(typeof(IDictionary<,>)))
                {
                    if (type.IsGenericType)
                        targetType = typeof(Dictionary<,>).MakeGenericType(type.GetGenericArguments());
                    else if (type == typeof(IDictionary))
                        targetType = typeof(Dictionary<Object, Object>);
                }
            }

            var ctor = targetType.GetConstructor(Type.EmptyTypes);
            factory = ctor != null ? Expression.Lambda<Func<Object>>(Expression.New(ctor)).Compile() : null;
        }

        // 写时复制发布（lock 保证唯一写入，volatile 保证所有读者可见）
        lock (_cacheLock)
        {
            if (!_instanceFactoryDict.ContainsKey(type))
            {
                var next = new Dictionary<Type, Func<Object>?>(_instanceFactoryDict) { [type] = factory };
                _instanceFactoryDict = next;
            }
        }

        return factory != null ? factory() : Activator.CreateInstance(type, true);
    }

    /// <summary>反射调用指定对象的方法</summary>
    /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
    /// <param name="method">方法</param>
    /// <param name="parameters">方法参数</param>
    /// <returns></returns>
    public virtual Object? Invoke(Object? target, MethodBase method, Object?[]? parameters) => InvokeCore(target, method, parameters);

    // 非虚核心实现，供 Reflect.cs 直接调用绕过接口分派
    internal Object? InvokeCore(Object? target, MethodBase method, Object?[]? parameters)
    {
        if (method is MethodInfo mi)
        {
            if (parameters == null || parameters.Length == 0)
            {
                // 0-param 专用路径：省去 Object[] 传递开销
                var dict0 = _invoker0Dict;
                if (!dict0.TryGetValue(mi, out var inv0))
                    inv0 = AddInvoker0(mi);
                if (inv0 != null)
                    return inv0(target);
            }
            else
            {
                // N-param 通用路径
                var dictN = _invokerNDict;
                if (!dictN.TryGetValue(mi, out var invN))
                    invN = AddInvokerN(mi);
                if (invN != null)
                    return invN(target, parameters);
            }
        }
        return method.Invoke(target, parameters);
    }

    // 冷路径：为 0-param 方法编译 Func<Object?,Object?> 委托（省去 Object[] 参数开销）
    internal Func<Object?, Object?>? AddInvoker0(MethodInfo method)
    {
        Func<Object?, Object?>? invoker = null;
        try
        {
            if (!method.IsGenericMethod && method.DeclaringType != null && method.GetParameters().Length == 0)
            {
                var instanceParam = Expression.Parameter(typeof(Object), "instance");
                Expression callExpr = method.IsStatic
                    ? Expression.Call(method)
                    : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType), method);
                var resultExpr = method.ReturnType == typeof(void)
                    ? (Expression)Expression.Block(typeof(Object), callExpr, Expression.Constant(null, typeof(Object)))
                    : Expression.Convert(callExpr, typeof(Object));
                invoker = Expression.Lambda<Func<Object?, Object?>>(resultExpr, instanceParam).Compile();
            }
        }
        catch { invoker = null; }

        lock (_cacheLock)
        {
            if (!_invoker0Dict.ContainsKey(method))
            {
                var next = new Dictionary<MethodInfo, Func<Object?, Object?>?>(_invoker0Dict) { [method] = invoker };
                _invoker0Dict = next;
            }
        }
        return invoker;
    }

    // 冷路径：为 N-param 方法编译 Func<Object?,Object?[]?,Object?> 委托
    internal Func<Object?, Object?[]?, Object?>? AddInvokerN(MethodInfo method)
    {
        Func<Object?, Object?[]?, Object?>? invoker;
        try
        {
            if (method.IsGenericMethod || method.DeclaringType == null)
            {
                invoker = null;
            }
            else
            {
                var pis = method.GetParameters();
                // 含 ref/out 参数时回退原始反射
                if (Array.Exists(pis, static p => p.ParameterType.IsByRef))
                {
                    invoker = null;
                }
                else
                {
                    var instanceParam = Expression.Parameter(typeof(Object), "instance");
                    var argsParam = Expression.Parameter(typeof(Object?[]), "args");
                    var argExprs = new Expression[pis.Length];
                    for (var i = 0; i < pis.Length; i++)
                        argExprs[i] = Expression.Convert(Expression.ArrayIndex(argsParam, Expression.Constant(i)), pis[i].ParameterType);

                    Expression callExpr = method.IsStatic
                        ? Expression.Call(method, argExprs)
                        : Expression.Call(Expression.Convert(instanceParam, method.DeclaringType), method, argExprs);

                    var resultExpr = method.ReturnType == typeof(void)
                        ? (Expression)Expression.Block(typeof(Object), callExpr, Expression.Constant(null, typeof(Object)))
                        : Expression.Convert(callExpr, typeof(Object));

                    invoker = Expression.Lambda<Func<Object?, Object?[]?, Object?>>(resultExpr, instanceParam, argsParam).Compile();
                }
            }
        }
        catch { invoker = null; }

        lock (_cacheLock)
        {
            if (!_invokerNDict.ContainsKey(method))
            {
                var next = new Dictionary<MethodInfo, Func<Object?, Object?[]?, Object?>?>(_invokerNDict) { [method] = invoker };
                _invokerNDict = next;
            }
        }
        return invoker;
    }

    /// <summary>反射调用指定对象的方法</summary>
    /// <param name="target">要调用其方法的对象，如果要调用静态方法，则target是类型</param>
    /// <param name="method">方法</param>
    /// <param name="parameters">方法参数字典</param>
    /// <returns></returns>
    public virtual Object? InvokeWithParams(Object? target, MethodBase method, IDictionary? parameters)
    {
        // 该方法没有参数，无视外部传入参数
        var pis = method.GetParameters();
        if (pis == null || pis.Length == 0) return Invoke(target, method, null);

        var ps = new Object?[pis.Length];
        for (var i = 0; i < pis.Length; i++)
        {
            Object? v = null;
            var name = pis[i].Name;
            if (parameters != null && !name.IsNullOrEmpty() && parameters.Contains(name)) v = parameters[name];
            ps[i] = v.ChangeType(pis[i].ParameterType);
        }

        return Invoke(target, method, ps);
    }

    /// <summary>获取目标对象的属性值</summary>
    /// <param name="target">目标对象</param>
    /// <param name="property">属性</param>
    /// <returns></returns>
    public virtual Object? GetValue(Object? target, PropertyInfo property) => GetValueCore(target, property);

    // 非虚核心实现，供 Reflect.cs 直接调用绕过接口分派
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Object? GetValueCore(Object? target, PropertyInfo property)
    {
        var dict = _propGetterDict;
        if (!dict.TryGetValue(property, out var getter))
            getter = AddPropGetter(property);
        return getter != null ? getter(target) : property.GetValue(target, null);
    }

    // 冷路径：编译并缓存属性 getter
    internal Func<Object?, Object?>? AddPropGetter(PropertyInfo pi)
    {
        Func<Object?, Object?>? getter;
        if (pi.GetGetMethod(true)?.IsStatic == true)
        {
            getter = null;
        }
        else
        {
            var obj = Expression.Parameter(typeof(Object), "obj");
            getter = Expression.Lambda<Func<Object?, Object?>>(
                Expression.Convert(Expression.Property(Expression.Convert(obj, pi.DeclaringType!), pi), typeof(Object)), obj).Compile();
        }

        lock (_cacheLock)
        {
            if (!_propGetterDict.ContainsKey(pi))
            {
                var next = new Dictionary<PropertyInfo, Func<Object?, Object?>?>(_propGetterDict) { [pi] = getter };
                _propGetterDict = next;
            }
        }
        return getter;
    }

    /// <summary>获取目标对象的字段值</summary>
    /// <param name="target">目标对象</param>
    /// <param name="field">字段</param>
    /// <returns></returns>
    public virtual Object? GetValue(Object? target, FieldInfo field) => GetValueFieldCore(target, field);

    // 非虚核心实现
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Object? GetValueFieldCore(Object? target, FieldInfo field)
    {
        var dict = _fieldGetterDict;
        if (!dict.TryGetValue(field, out var getter))
            getter = AddFieldGetter(field);
        return getter != null ? getter(target) : field.GetValue(target);
    }

    // 冷路径：编译并缓存字段 getter
    internal Func<Object?, Object?>? AddFieldGetter(FieldInfo fi)
    {
        Func<Object?, Object?>? getter;
        if (fi.IsStatic)
        {
            getter = null;
        }
        else
        {
            var obj = Expression.Parameter(typeof(Object), "obj");
            getter = Expression.Lambda<Func<Object?, Object?>>(
                Expression.Convert(Expression.Field(Expression.Convert(obj, fi.DeclaringType!), fi), typeof(Object)), obj).Compile();
        }

        lock (_cacheLock)
        {
            if (!_fieldGetterDict.ContainsKey(fi))
            {
                var next = new Dictionary<FieldInfo, Func<Object?, Object?>?>(_fieldGetterDict) { [fi] = getter };
                _fieldGetterDict = next;
            }
        }
        return getter;
    }

    /// <summary>设置目标对象的属性值</summary>
    /// <param name="target">目标对象</param>
    /// <param name="property">属性</param>
    /// <param name="value">数值</param>
    public virtual void SetValue(Object target, PropertyInfo property, Object? value) => SetValueCore(target, property, value);

    // 非虚核心实现
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetValueCore(Object target, PropertyInfo property, Object? value)
    {
        var pt = property.PropertyType;
        var converted = value == null || value.GetType() == pt ? value : value.ChangeType(pt);
        var dict = _propSetterDict;
        if (!dict.TryGetValue(property, out var setter))
            setter = AddPropSetter(property);
        if (setter != null)
            setter(target, converted);
        else
            property.SetValue(target, converted, null);
    }

    // 冷路径：编译并缓存属性 setter
    internal Action<Object?, Object?>? AddPropSetter(PropertyInfo pi)
    {
        Action<Object?, Object?>? setter;
        if (!pi.CanWrite || pi.DeclaringType?.IsValueType == true || pi.GetSetMethod(true)?.IsStatic == true)
        {
            setter = null;
        }
        else
        {
            var obj = Expression.Parameter(typeof(Object), "obj");
            var val = Expression.Parameter(typeof(Object), "val");
            setter = Expression.Lambda<Action<Object?, Object?>>(
                Expression.Assign(
                    Expression.Property(Expression.Convert(obj, pi.DeclaringType!), pi),
                    Expression.Convert(val, pi.PropertyType)),
                obj, val).Compile();
        }

        lock (_cacheLock)
        {
            if (!_propSetterDict.ContainsKey(pi))
            {
                var next = new Dictionary<PropertyInfo, Action<Object?, Object?>?>(_propSetterDict) { [pi] = setter };
                _propSetterDict = next;
            }
        }
        return setter;
    }

    /// <summary>设置目标对象的字段值</summary>
    /// <param name="target">目标对象</param>
    /// <param name="field">字段</param>
    /// <param name="value">数值</param>
    public virtual void SetValue(Object target, FieldInfo field, Object? value) => SetValueFieldCore(target, field, value);

    // 非虚核心实现
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetValueFieldCore(Object target, FieldInfo field, Object? value)
    {
        var ft = field.FieldType;
        var converted = value == null || value.GetType() == ft ? value : value.ChangeType(ft);
        var dict = _fieldSetterDict;
        if (!dict.TryGetValue(field, out var setter))
            setter = AddFieldSetter(field);
        if (setter != null)
            setter(target, converted);
        else
            field.SetValue(target, converted);
    }

    // 冷路径：编译并缓存字段 setter
    internal Action<Object?, Object?>? AddFieldSetter(FieldInfo fi)
    {
        Action<Object?, Object?>? setter;
        if (fi.IsInitOnly || fi.IsLiteral || fi.DeclaringType?.IsValueType == true || fi.IsStatic)
        {
            setter = null;
        }
        else
        {
            var obj = Expression.Parameter(typeof(Object), "obj");
            var val = Expression.Parameter(typeof(Object), "val");
            setter = Expression.Lambda<Action<Object?, Object?>>(
                Expression.Assign(
                    Expression.Field(Expression.Convert(obj, fi.DeclaringType!), fi),
                    Expression.Convert(val, fi.FieldType)),
                obj, val).Compile();
        }

        lock (_cacheLock)
        {
            if (!_fieldSetterDict.ContainsKey(fi))
            {
                var next = new Dictionary<FieldInfo, Action<Object?, Object?>?>(_fieldSetterDict) { [fi] = setter };
                _fieldSetterDict = next;
            }
        }
        return setter;
    }
    #endregion

    #region 对象拷贝
    private static Dictionary<Type, IDictionary<String, PropertyInfo>> _properties = [];
    /// <summary>从源对象拷贝数据到目标对象。针对IModel优化</summary>
    /// <remarks>
    /// 来源或目标对象为IModel时，借助IModel的索引器来取值赋值，提升性能。
    /// </remarks>
    /// <param name="target">目标对象</param>
    /// <param name="source">源对象</param>
    /// <param name="deep">递归深度拷贝，直接拷贝成员值而不是引用</param>
    /// <param name="excludes">要忽略的成员</param>
    public virtual void Copy(Object target, Object source, Boolean deep = false, params String[] excludes)
    {
        if (target == null || source == null || target == source) return;

        var targetType = target.GetType();
        // 基础类型无法拷贝
        if (targetType.IsBaseType()) throw new XException("The base type {0} cannot be copied", targetType.FullName);
        if (!_properties.TryGetValue(targetType, out var targetProperties))
            _properties[targetType] = targetProperties = targetType.GetProperties(true).ToDictionary(e => e.Name, e => e);

        var sourceType = source.GetType();
        if (!_properties.TryGetValue(sourceType, out var sourceProperties))
            _properties[sourceType] = sourceProperties = sourceType.GetProperties(true).ToDictionary(e => e.Name, e => e);

        // 不是深度拷贝时，直接复制引用
        if (!deep)
        {
            // 借助 IModel 优化取值赋值，有 IExtend 扩展属性的实体类过于复杂而不支持，例如IEntity就有脏数据问题
            if (target is IModel dst and not IExtend)
            {
                foreach (var pi in targetProperties.Values)
                {
                    if (!pi.CanWrite) continue;
                    if (excludes != null && excludes.Contains(pi.Name)) continue;

                    if (sourceProperties.TryGetValue(pi.Name, out var pi2) && pi2.CanRead)
                        dst[pi.Name] = source is IModel src ? src[pi2.Name] : GetValue(source, pi2);
                }
            }
            else
            {
                foreach (var pi in targetProperties.Values)
                {
                    if (!pi.CanWrite) continue;
                    if (excludes != null && excludes.Contains(pi.Name)) continue;

                    if (sourceProperties.TryGetValue(pi.Name, out var pi2) && pi2.CanRead)
                        SetValue(target, pi, source is IModel src ? src[pi2.Name] : GetValue(source, pi2));
                }
            }
            return;
        }

        // 来源对象转为字典
        var dic = new Dictionary<String, Object?>();
        foreach (var pi in sourceProperties.Values)
        {
            if (!pi.CanRead) continue;
            if (excludes != null && excludes.Contains(pi.Name)) continue;

            dic[pi.Name] = GetValue(source, pi);
        }

        Copy(target, dic, deep);
    }

    /// <summary>从源字典拷贝数据到目标对象</summary>
    /// <param name="target">目标对象</param>
    /// <param name="source">源字典</param>
    /// <param name="deep">递归深度拷贝，直接拷贝成员值而不是引用</param>
    public virtual void Copy(Object target, IDictionary<String, Object?> source, Boolean deep = false)
    {
        if (target == null || source == null || source.Count == 0 || target == source) return;

        foreach (var pi in target.GetType().GetProperties(true))
        {
            if (!pi.CanWrite) continue;

            if (source.TryGetValue(pi.Name, out var obj))
            {
                // 基础类型直接拷贝，不考虑深拷贝
                if (!deep || pi.PropertyType.IsBaseType())
                    SetValue(target, pi, obj);
                else
                {
                    var v = GetValue(target, pi);

                    // 如果目标对象该成员为空，需要创建再拷贝
                    if (v == null)
                    {
                        v = pi.PropertyType.CreateInstance();
                        SetValue(target, pi, v);
                    }
                    if (v != null && obj != null) Copy(v, obj, deep);
                }
            }
        }
    }
    #endregion

    #region 类型辅助
    /// <summary>获取一个类型的元素类型</summary>
    /// <param name="type">类型</param>
    /// <returns></returns>
    public virtual Type? GetElementType(Type type)
    {
        if (type.HasElementType) return type.GetElementType();

        if (type.As<IEnumerable>())
        {
            // 如果实现了IEnumerable<>接口，那么取泛型参数
            foreach (var item in type.GetInterfaces())
            {
                if (item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return item.GetGenericArguments()[0];
            }
            //// 通过索引器猜测元素类型
            //var pi = type.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            //if (pi != null) return pi.PropertyType;
        }

        return null;
    }

    /// <summary>类型转换</summary>
    /// <param name="value">数值</param>
    /// <param name="conversionType"></param>
    /// <returns></returns>
    public virtual Object? ChangeType(Object? value, Type conversionType)
    {
        // 值类型就是目标类型
        Type? vtype = null;
        if (value != null) vtype = value.GetType();
        if (vtype == conversionType) return value;

        // 可空类型
        var utype = Nullable.GetUnderlyingType(conversionType);
        if (utype != null)
        {
            if (value == null) return null;

            // 时间日期可空处理
            if (value is DateTime dt && dt == DateTime.MinValue) return null;

            conversionType = utype;
        }

        var code = Type.GetTypeCode(conversionType);
        //conversionType = Nullable.GetUnderlyingType(conversionType) ?? conversionType;
        if (conversionType.IsEnum)
        {
            if (vtype == typeof(String))
                return Enum.Parse(conversionType, (String)(value ?? String.Empty), true);
            else
                return Enum.ToObject(conversionType, value ?? 0);
        }

        // 字符串转为货币类型，处理一下
        if (vtype == typeof(String))
        {
            var str = (String)(value ?? String.Empty);
            if (code == TypeCode.Decimal)
            {
                value = str.TrimStart(['$', '￥']);
            }
            else if (conversionType.As<Type>())
            {
                return GetType(str, false);
            }

            // 字符串转为简单整型，如果长度比较小，满足32位整型要求，则先转为32位再改变类型
            if (code >= TypeCode.Int16 && code <= TypeCode.UInt64 && str.Length <= 10)
                return Convert.ChangeType(value.ToLong(), conversionType);
        }

        if (value != null)
        {
            // 尝试基础类型转换
            switch (code)
            {
                case TypeCode.Boolean:
                    return value.ToBoolean();
                case TypeCode.DateTime:
                    return value.ToDateTime();
                case TypeCode.Double:
                    return value.ToDouble();
                case TypeCode.Single:
                    return (Single)value.ToDouble();
                case TypeCode.Decimal:
                    return value.ToDecimal();
                case TypeCode.Int16:
                    return (Int16)value.ToInt();
                case TypeCode.Int32:
                    return value.ToInt();
                case TypeCode.Int64:
                    return value.ToLong();
                case TypeCode.UInt16:
                    return (UInt16)value.ToInt();
                case TypeCode.UInt32:
                    return (UInt32)value.ToInt();
                case TypeCode.UInt64:
                    return (UInt64)value.ToLong();
                default:
                    break;
            }

            // 支持DateTimeOffset转换
            if (conversionType == typeof(DateTimeOffset)) return value.ToDateTimeOffset();

            if (value is String str)
            {
                // 特殊处理几种类型，避免后续反射影响性能
                if (conversionType == typeof(Guid)) return Guid.Parse(str);
                if (conversionType == typeof(TimeSpan)) return TimeSpan.Parse(str);
#if NET5_0_OR_GREATER
                if (conversionType == typeof(IntPtr)) return IntPtr.Parse(str);
                if (conversionType == typeof(UIntPtr)) return UIntPtr.Parse(str);
                if (conversionType == typeof(Half)) return Half.Parse(str);
#endif
#if NET6_0_OR_GREATER
                if (conversionType == typeof(DateOnly)) return DateOnly.Parse(str);
                if (conversionType == typeof(TimeOnly)) return TimeOnly.Parse(str);
#endif

#if NET7_0_OR_GREATER
                // 支持IParsable<TSelf>接口
                if (conversionType.GetInterfaces().Any(e => e.IsGenericType && e.GetGenericTypeDefinition() == typeof(IParsable<>)))
                {
                    // 获取 TryParse 静态方法
                    var tryParse = conversionType.GetMethod("TryParse", [typeof(String), typeof(IFormatProvider), conversionType.MakeByRefType()]);
                    if (tryParse != null)
                    {
                        var parameters = new Object?[] { str, null, null };
                        var success = (Boolean)tryParse.Invoke(null, parameters)!;
                        if (success) return parameters[2];
                        //return null;
                    }
                    else
                    {
                        var mi = conversionType.GetMethod("Parse", [typeof(String), typeof(IFormatProvider)]);
                        if (mi != null) return mi.Invoke(null, [value, null]);
                    }
                }
#endif
            }

            if (value is IConvertible) value = Convert.ChangeType(value, conversionType);
        }
        else
        {
            // 如果原始值是null，要转为值类型，则new一个空白的返回
            if (conversionType.IsValueType) value = CreateInstance(conversionType);
        }

        if (conversionType.IsAssignableFrom(vtype)) return value;

        return value;
    }

    /// <summary>获取类型的友好名称</summary>
    /// <param name="type">指定类型</param>
    /// <param name="isfull">是否全名，包含命名空间</param>
    /// <returns></returns>
    public virtual String GetName(Type type, Boolean isfull) => isfull ? (type.FullName ?? type.Name) : type.Name;
    #endregion

    #region 插件
    //private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Boolean>> _as_cache = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Boolean>>();
    /// <summary>是否子类</summary>
    /// <param name="type"></param>
    /// <param name="baseType"></param>
    /// <returns></returns>
    public Boolean As(Type type, Type baseType)
    {
        if (type == null) return false;
        if (type == baseType) return true;

        // 如果基类是泛型定义，补充完整，例如IList<>
        if (baseType.IsGenericTypeDefinition
            && type.IsGenericType && !type.IsGenericTypeDefinition
            && baseType is TypeInfo inf && inf.GenericTypeParameters.Length == type.GenericTypeArguments.Length)
            baseType = baseType.MakeGenericType(type.GenericTypeArguments);

        if (type == baseType) return true;

        if (baseType.IsAssignableFrom(type)) return true;

        //// 绝大部分子类判断可通过IsAssignableFrom完成，除非其中一方ReflectionOnly
        //if (type.Assembly.ReflectionOnly == baseType.Assembly.ReflectionOnly) return false;

        // 缓存
        //var key = $"{type.FullName}_{baseType.FullName}";
        //if (!_as_cache.TryGetValue(type, out var dic))
        //{
        //    dic = new ConcurrentDictionary<Type, Boolean>();
        //    _as_cache.TryAdd(type, dic);
        //}

        //if (dic.TryGetValue(baseType, out var rs)) return rs;
        var rs = false;

        //// 接口
        //if (baseType.IsInterface)
        //{
        //    if (type.GetInterface(baseType.FullName) != null)
        //        rs = true;
        //    else if (type.GetInterfaces().Any(e => e.IsGenericType && baseType.IsGenericTypeDefinition ? e.GetGenericTypeDefinition() == baseType : e == baseType))
        //        rs = true;
        //}

        //// 判断是否子类时，支持只反射加载的程序集
        //if (!rs && type.Assembly.ReflectionOnly)
        //{
        //    // 反射加载时，需要特殊处理接口
        //    //if (baseType.IsInterface && type.GetInterface(baseType.Name) != null) return true;
        //    while (!rs && type != typeof(Object))
        //    {
        //        if (type.FullName == baseType.FullName &&
        //            type.AssemblyQualifiedName == baseType.AssemblyQualifiedName)
        //            rs = true;
        //        type = type.BaseType;
        //    }
        //}

        //dic.TryAdd(baseType, rs);

        return rs;
    }

    /// <summary>在指定程序集中查找指定基类的子类</summary>
    /// <param name="asm">指定程序集</param>
    /// <param name="baseType">基类或接口，为空时返回所有类型</param>
    /// <returns></returns>
    public virtual IEnumerable<Type> GetSubclasses(Assembly asm, Type baseType)
    {
        if (asm == null) throw new ArgumentNullException(nameof(asm));
        if (baseType == null) throw new ArgumentNullException(nameof(baseType));

        var asmx = AssemblyX.Create(asm);
        if (asmx == null) return Enumerable.Empty<Type>();

        return asmx.FindPlugins(baseType);
    }

    /// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
    /// <param name="baseType">基类或接口</param>
    /// <returns></returns>
    public virtual IEnumerable<Type> GetAllSubclasses(Type baseType)
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

    ///// <summary>在所有程序集中查找指定基类或接口的子类实现</summary>
    ///// <param name="baseType">基类或接口</param>
    ///// <param name="isLoadAssembly">是否加载为加载程序集</param>
    ///// <returns></returns>
    //public virtual IEnumerable<Type> GetAllSubclasses(Type baseType, Boolean isLoadAssembly)
    //{
    //    //// 不支持isLoadAssembly
    //    //foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
    //    //{
    //    //    foreach (var type in GetSubclasses(asm, baseType))
    //    //    {
    //    //        yield return type;
    //    //    }
    //    //}
    //    return AssemblyX.FindAllPlugins(baseType, isLoadAssembly);
    //}
    #endregion
}