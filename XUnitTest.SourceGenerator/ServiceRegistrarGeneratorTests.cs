using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using NewLife.SourceGenerator;
using Xunit;

namespace XUnitTest.SourceGenerator;

/// <summary>服务注册扫描生成器测试</summary>
public class ServiceRegistrarGeneratorTests
{
    #region 测试源码
    private const String Stub = """
namespace NewLife.Model
{
    public interface IObjectContainer { }

    public static class ContainerStub
    {
        public static IObjectContainer AddTransient<TService, TImplementation>(this IObjectContainer container) => container;
        public static IObjectContainer TryAddTransient<TService, TImplementation>(this IObjectContainer container) => container;
        public static IObjectContainer AddSingleton<TService, TImplementation>(this IObjectContainer container) => container;
        public static IObjectContainer TryAddSingleton<TService, TImplementation>(this IObjectContainer container) => container;
        public static IObjectContainer AddScoped<TService, TImplementation>(this IObjectContainer container) => container;
        public static IObjectContainer TryAddScoped<TService, TImplementation>(this IObjectContainer container) => container;
        public static IObjectContainer AddTransient<TService>(this IObjectContainer container) => container;
        public static IObjectContainer AddScoped<TService>(this IObjectContainer container) => container;
        public static IObjectContainer AddSingleton<TService>(this IObjectContainer container, TService instance = null) where TService : class => container;
        public static IObjectContainer TryAddTransient<TService>(this IObjectContainer container, TService instance = null) where TService : class => container;
        public static IObjectContainer AddTransient(this IObjectContainer container, Type serviceType, Type implementationType) => container;
    }

    public static class ServiceFactoryTable
    {
        public static void Register(Type type, Func<Object> factory) { }
    }
}
""";

    private const String Source = """
public interface IFoo { }

public class Foo : IFoo { }

public class NoCtor : IFoo { }

public class WithDep { public WithDep(IFoo foo) { } }

public abstract class AbstractSvc { }

public class InternalCtor { internal InternalCtor() { } }

public class Multi { public Multi() { } public Multi(Int32 x) { } }

public class Consumer
{
    public void Configure(IObjectContainer container)
    {
        container.AddTransient<IFoo, Foo>();
        container.AddSingleton<IFoo, NoCtor>();
        container.TryAddScoped<IFoo, Foo>();
        container.AddScoped<Multi>();
        container.AddTransient<WithDep>();
        container.AddTransient<AbstractSvc>();
        container.AddSingleton<IFoo>(new Foo());
        container.TryAddTransient<IFoo>();
        container.AddTransient(typeof(IFoo), typeof(NoCtor));
        container.AddTransient<IFoo, InternalCtor>();
    }
}
""";

    private static readonly String FullSource = "using NewLife.Model;" + Environment.NewLine + Stub + Environment.NewLine + Source;
    #endregion

    #region 测试
    /// <summary>扫描注册调用点，为唯一公开无参构造的实现生成工厂注册</summary>
    [Fact(DisplayName = "扫描注册调用点为无参构造实现生成工厂注册")]
    public void GenerateFactories()
    {
        var (_, result) = GeneratorTestHost.RunWithSymbols(new ServiceRegistrarGenerator(), FullSource, ["NET5_0_OR_GREATER"]);
        var sources = GeneratorTestHost.GetGeneratedSources(result);

        var exception = result.Results.Select(r => r.Exception).FirstOrDefault(e => e != null);
        Assert.True(exception == null, exception?.ToString());

        var text = Assert.Single(sources).Value;
        Assert.Contains("typeof(global::Foo)", text);
        Assert.Contains("typeof(global::NoCtor)", text);
        Assert.Contains("ServiceFactoryTable.Register", text);
        Assert.Contains("ModuleInitializer", text);

        // 重复注册去重
        Assert.Equal(1, CountOccurrences(text, "typeof(global::Foo)"));

        // 有参构造/多构造/抽象/无公开构造/实例重载：均不生成
        Assert.DoesNotContain("WithDep", text);
        Assert.DoesNotContain("Multi", text);
        Assert.DoesNotContain("AbstractSvc", text);
        Assert.DoesNotContain("InternalCtor", text);
    }

    /// <summary>生成产物与用户代码一起编译无错误（含模块初始化器与工厂委托）</summary>
    [Fact(DisplayName = "生成产物参与编译无错误")]
    public void GeneratedSourceCompiles()
    {
        var (compilation, _) = GeneratorTestHost.RunWithSymbols(new ServiceRegistrarGenerator(), FullSource, ["NET5_0_OR_GREATER"]);
        var errors = compilation.GetDiagnostics().Where(e => e.Severity == DiagnosticSeverity.Error).ToList();

        Assert.True(errors.Count == 0, String.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    /// <summary>低版本目标（无 NET5_0_OR_GREATER 符号）不生成，行为零影响</summary>
    [Fact(DisplayName = "低版本目标不生成")]
    public void SkipBelowNet5()
    {
        var (_, result) = GeneratorTestHost.Run(new ServiceRegistrarGenerator(), FullSource);
        var sources = GeneratorTestHost.GetGeneratedSources(result);

        Assert.Empty(sources);
    }
    #endregion

    #region 辅助
    private static Int32 CountOccurrences(String text, String value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
    #endregion
}
