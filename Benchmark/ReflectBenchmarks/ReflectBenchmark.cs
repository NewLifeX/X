using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using NewLife.Reflection;

namespace Benchmark.ReflectBenchmarks;

/// <summary>测试用模型类</summary>
internal class SampleModel
{
    public String Name { get; set; } = "";
    public Int32 Age { get; set; }
    public DateTime CreateTime { get; set; }

    public String GetDisplayName() => $"{Name}({Age})";
    public Int32 Add(Int32 a, Int32 b) => a + b;
}

/// <summary>Reflect 扩展与原始 .NET 反射性能对比基准测试</summary>
[MemoryDiagnoser]
[SimpleJob(iterationCount: 20)]
public class ReflectBenchmark
{
    private PropertyInfo _pi = null!;
    private MethodInfo _miNoParam = null!;
    private MethodInfo _miWithParam = null!;
    private Type _type = null!;
    private SampleModel _model = null!;

    // 预编译委托（用于 Baseline 对比）
    private Func<Object?, Object?> _directGetter = null!;
    private Action<Object?, Object?> _directSetter = null!;
    private Func<Object> _directCtor = null!;
    private Func<Object?, Object?> _directInvokeNoParam = null!;
    private Func<Object?, Object?[]?, Object?> _directInvokeWithParam = null!;

    private static readonly Object[] _invokeArgs = [3, 4];

    [GlobalSetup]
    public void Setup()
    {
        _type = typeof(SampleModel);
        _pi = _type.GetProperty(nameof(SampleModel.Name))!;
        _miNoParam = _type.GetMethod(nameof(SampleModel.GetDisplayName))!;
        _miWithParam = _type.GetMethod(nameof(SampleModel.Add))!;
        _model = new SampleModel { Name = "Hello", Age = 42, CreateTime = DateTime.Now };

        // 预先编译直接委托（基准对比用，排除首次编译开销）
        var obj = Expression.Parameter(typeof(Object), "obj");
        var cast = Expression.Convert(obj, _type);
        _directGetter = Expression.Lambda<Func<Object?, Object?>>(
            Expression.Convert(Expression.Property(cast, _pi), typeof(Object)), obj).Compile();

        var val = Expression.Parameter(typeof(Object), "val");
        _directSetter = Expression.Lambda<Action<Object?, Object?>>(
            Expression.Assign(Expression.Property(Expression.Convert(obj, _type), _pi),
                Expression.Convert(val, _pi.PropertyType)), obj, val).Compile();

        var ctor = _type.GetConstructor(Type.EmptyTypes)!;
        _directCtor = Expression.Lambda<Func<Object>>(Expression.New(ctor)).Compile();

        // 0-param 直接 Func<Object?,Object?> 委托（不含 Object[] 数组开销）
        _directInvokeNoParam = Expression.Lambda<Func<Object?, Object?>>(
            Expression.Convert(Expression.Call(Expression.Convert(obj, _type), _miNoParam), typeof(Object)),
            obj).Compile();

        var argsParam = Expression.Parameter(typeof(Object?[]), "args");
        _directInvokeWithParam = Expression.Lambda<Func<Object?, Object?[]?, Object?>>(
            Expression.Convert(
                Expression.Call(Expression.Convert(obj, _type), _miWithParam,
                    Expression.Convert(Expression.ArrayIndex(argsParam, Expression.Constant(0)), typeof(Int32)),
                    Expression.Convert(Expression.ArrayIndex(argsParam, Expression.Constant(1)), typeof(Int32))),
                typeof(Object)),
            obj, argsParam).Compile();

        // 预热 NewLife 缓存（排除首次编译开销）
        _ = _model.GetValue((MemberInfo)_pi);
        _model.SetValue((MemberInfo)_pi, "Warmup");
        _ = _type.CreateInstance();
        _ = _type.GetProperties(true);
        _ = _model.Invoke(_miNoParam, (Object?[]?)null);
        _ = _model.Invoke(_miWithParam, _invokeArgs);
        // 同时预热 Provider 直调路径
        _ = Reflect.Provider.GetValue(_model, _pi);
        Reflect.Provider.SetValue(_model, _pi, "Warmup");
        _ = Reflect.Provider.Invoke(_model, _miNoParam, null);
        _ = Reflect.Provider.Invoke(_model, _miWithParam, _invokeArgs);
    }

    #region 属性 Get
    [Benchmark(Description = "属性Get-原始反射")]
    public Object? RawPropertyGet() => _pi.GetValue(_model, null);

    [Benchmark(Description = "属性Get-直接Lambda")]
    public Object? DirectLambdaGet() => _directGetter(_model);

    [Benchmark(Description = "属性Get-Provider直调")]
    public Object? ProviderPropertyGet() => Reflect.Provider.GetValue(_model, _pi);

    [Benchmark(Description = "属性Get-扩展方法")]
    public Object? ExtMethodPropertyGet() => _model.GetValue((MemberInfo)_pi);
    #endregion

    #region 属性 Set
    [Benchmark(Description = "属性Set-原始反射")]
    public void RawPropertySet() => _pi.SetValue(_model, "World", null);

    [Benchmark(Description = "属性Set-直接Lambda")]
    public void DirectLambdaSet() => _directSetter(_model, "World");

    [Benchmark(Description = "属性Set-Provider直调")]
    public void ProviderPropertySet() => Reflect.Provider.SetValue(_model, _pi, "World");

    [Benchmark(Description = "属性Set-扩展方法")]
    public void ExtMethodPropertySet() => _model.SetValue((MemberInfo)_pi, "World");
    #endregion

    #region CreateInstance
    [Benchmark(Description = "CreateInstance-Activator")]
    public Object ActivatorCreate() => Activator.CreateInstance(_type)!;

    [Benchmark(Description = "CreateInstance-直接Lambda")]
    public Object DirectLambdaCreate() => _directCtor();

    [Benchmark(Description = "CreateInstance-扩展方法")]
    public Object? NewLifeCreate() => _type.CreateInstance();
    #endregion

    #region Invoke（无参数）
    [Benchmark(Description = "Invoke无参-原始反射")]
    public Object? RawInvokeNoParam() => _miNoParam.Invoke(_model, null);

    [Benchmark(Description = "Invoke无参-直接Lambda")]
    public Object? DirectLambdaInvokeNoParam() => _directInvokeNoParam(_model);

    [Benchmark(Description = "Invoke无参-Provider直调")]
    public Object? ProviderInvokeNoParam() => Reflect.Provider.Invoke(_model, _miNoParam, null);

    [Benchmark(Description = "Invoke无参-扩展方法")]
    public Object? ExtMethodInvokeNoParam() => _model.Invoke(_miNoParam, (Object?[]?)null);
    #endregion

    #region Invoke（带参数）
    [Benchmark(Description = "Invoke带参-原始反射")]
    public Object? RawInvokeWithParam() => _miWithParam.Invoke(_model, _invokeArgs);

    [Benchmark(Description = "Invoke带参-直接Lambda")]
    public Object? DirectLambdaInvokeWithParam() => _directInvokeWithParam(_model, _invokeArgs);

    [Benchmark(Description = "Invoke带参-Provider直调")]
    public Object? ProviderInvokeWithParam() => Reflect.Provider.Invoke(_model, _miWithParam, _invokeArgs);

    [Benchmark(Description = "Invoke带参-扩展方法")]
    public Object? ExtMethodInvokeWithParam() => _model.Invoke(_miWithParam, _invokeArgs);
    #endregion

    #region GetProperties
    [Benchmark(Description = "GetProperties-原始反射")]
    public PropertyInfo[] RawGetProperties() => _type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

    [Benchmark(Description = "GetProperties-扩展方法缓存")]
    public IList<PropertyInfo> CachedGetProperties() => _type.GetProperties(true);
    #endregion
}

