using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using NewLife.SourceGenerator;
using Xunit;

namespace XUnitTest.SourceGenerator;

/// <summary>Http 控制器静态分发生成器测试</summary>
public class HttpControllerDispatchGeneratorTests
{
    #region 测试源码
    private const String Stub = """
namespace NewLife.Http
{
    public interface IHttpContext
    {
        IDictionary<String, Object?> Parameters { get; }
        HttpResponse Response { get; }
        IServiceProvider? ServiceProvider { get; }
    }

    public class HttpResponse
    {
        public void SetResult(Object result, String? contentType = null) { }
    }

    public class HttpDispatchTable
    {
        public delegate Task ControllerInvokeDelegate(Object controller, IHttpContext context);

        public static void RegisterController(Type type, Dictionary<String, ControllerInvokeDelegate> dispatchers) { }
    }

    public class HttpServer
    {
        public void MapController<TController>(String? path = null) { }

        public void MapController(Type controllerType, String? path = null) { }
    }
}

namespace NewLife.Reflection
{
    public static class Reflect
    {
        public static T? ChangeType<T>(Object? value) => default;
    }
}

namespace NewLife.Serialization
{
    public class JsonHelper
    {
        public static JsonHelper Default { get; } = new();

        public Object? Convert(Object value, Type type) => null;
    }
}
""";

    private const String Source = """
public class OrderController
{
    public String Info() => "info";

    public async Task<String> AsyncInfo() => await Task.FromResult("async");

    public Int32 Sum(Int32 a, Int32 b) => a + b;

    public String Echo(String name, Int32 level = 7) => name;

    public String Where(IHttpContext ctx) => "where";

    public static String StaticInfo() => "static";
}

public class OddController
{
    public String Run(Int32 id) => "a";

    public String Run(String name) => "b";

    public String Generic<T>() => "g";

    public String Ref(ref Int32 value) => "r";

    public ValueTask<String> Async() => new ValueTask<String>("v");

    public String Ok() => "ok";
}

public interface INotController
{
    String Info();
}

public class Client
{
    public void Setup(HttpServer server)
    {
        server.MapController<OrderController>("/api/order");
        server.MapController(typeof(OddController));
        server.MapController<INotController>();
    }
}
""";

    private static readonly String FullSource = "using System;" + Environment.NewLine
        + "using System.Collections.Generic;" + Environment.NewLine
        + "using System.Threading.Tasks;" + Environment.NewLine
        + "using NewLife.Http;" + Environment.NewLine
        + Stub + Environment.NewLine + Source;
    #endregion

    #region 测试
    /// <summary>扫描 MapController 注册点，为控制器方法生成静态分发</summary>
    [Fact(DisplayName = "扫描控制器注册生成静态分发")]
    public void GenerateDispatchers()
    {
        var (_, result) = GeneratorTestHost.RunWithSymbols(new HttpControllerDispatchGenerator(), FullSource, ["NET5_0_OR_GREATER"]);
        var sources = GeneratorTestHost.GetGeneratedSources(result);

        var text = Assert.Single(sources).Value;
        Assert.Contains("HttpDispatchTable.RegisterController", text);

        // 泛型与 typeof 两种注册形式都生成
        Assert.Contains("typeof(global::OrderController)", text);
        Assert.Contains("typeof(global::OddController)", text);

        // 方法登记：普通、异步、值类型、带默认值、上下文注入、静态
        Assert.Contains("[\"Info\"]", text);
        Assert.Contains("[\"AsyncInfo\"]", text);
        Assert.Contains("[\"Sum\"]", text);
        Assert.Contains("[\"Echo\"]", text);
        Assert.Contains("[\"Where\"]", text);
        Assert.Contains("global::OrderController.StaticInfo()", text);

        // 参数绑定：按名取值转换、显式默认值、上下文注入、缺失必填值类型抛异常
        Assert.Contains("Reflect.ChangeType<global::System.Int32>(raw", text);
        Assert.Contains("= 7;", text);
        Assert.Contains("= context;", text);
        Assert.Contains("Missing required parameter [{0}]", text);

        // 异步返回值强类型等待后写回
        Assert.Contains("await instance.AsyncInfo().ConfigureAwait(false)", text);

        // 不可静态化形态跳过：重载歧义、泛型方法、ref 参数、ValueTask、非类控制器
        Assert.DoesNotContain("[\"Run\"]", text);
        Assert.DoesNotContain("[\"Generic\"]", text);
        Assert.DoesNotContain("[\"Ref\"]", text);
        Assert.DoesNotContain("[\"Async\"]", text);
        Assert.DoesNotContain("INotController", text);
        Assert.Contains("[\"Ok\"]", text);
    }

    /// <summary>生成产物与用户代码一起编译无错误</summary>
    [Fact(DisplayName = "生成产物参与编译无错误")]
    public void GeneratedSourceCompiles()
    {
        var coreRef = MetadataReference.CreateFromFile(typeof(NewLife.Localization).Assembly.Location);
        var (compilation, _) = GeneratorTestHost.RunWithSymbols(new HttpControllerDispatchGenerator(), FullSource, ["NET5_0_OR_GREATER"], coreRef);
        var errors = compilation.GetDiagnostics().Where(e => e.Severity == DiagnosticSeverity.Error).ToList();

        Assert.True(errors.Count == 0, String.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    /// <summary>低版本目标（无 NET5_0_OR_GREATER 符号）不生成</summary>
    [Fact(DisplayName = "低版本目标不生成")]
    public void SkipBelowNet5()
    {
        var (_, result) = GeneratorTestHost.RunWithSymbols(new HttpControllerDispatchGenerator(), FullSource, []);
        var sources = GeneratorTestHost.GetGeneratedSources(result);

        Assert.Empty(sources);
    }

    /// <summary>未注册控制器时不产出</summary>
    [Fact(DisplayName = "无注册调用点不生成")]
    public void SkipWithoutRegistration()
    {
        var source = "using System;" + Environment.NewLine + "using NewLife.Http;" + Environment.NewLine
            + Stub + Environment.NewLine + "public class PlainController { public String Info() => \"\"; }";
        var (_, result) = GeneratorTestHost.RunWithSymbols(new HttpControllerDispatchGenerator(), source, ["NET5_0_OR_GREATER"]);
        var sources = GeneratorTestHost.GetGeneratedSources(result);

        Assert.Empty(sources);
    }
    #endregion
}
