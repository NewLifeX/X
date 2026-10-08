using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using NewLife.SourceGenerator;
using Xunit;

namespace XUnitTest.SourceGenerator;

/// <summary>配置模型生成器测试</summary>
public class ConfigModelGeneratorTests
{
    #region 测试源码
    private const String Stub = """
namespace NewLife.Configuration
{
    public class Config<TConfig> where TConfig : Config<TConfig>, new() { }
}

namespace NewLife.Reflection
{
    public static class MemberAccessorTable
    {
        public static void Register(Type type, String name, Func<Object, Object?>? getter, Action<Object, Object?>? setter) { }
    }

    public static class Reflect
    {
        public static T? ChangeType<T>(Object? value) => default;
    }
}
""";

    private const String Source = """
public class MyConfig : Config<MyConfig>
{
    public String Host { get; set; } = "";
    public Int32 Port { get; set; }
}

public class BaseSetting : Config<BaseSetting>
{
    public String Name { get; set; } = "";
}

public class ChildSetting : BaseSetting
{
    public String Extra { get; set; } = "";
}

public class NotConfig
{
    public String Value { get; set; } = "";
}
""";

    private static readonly String FullSource = "using System;" + Environment.NewLine + "using NewLife.Configuration;" + Environment.NewLine + Stub + Environment.NewLine + Source;
    #endregion

    #region 测试
    /// <summary>扫描 Config 继承链，为配置模型与声明类生成成员访问注册</summary>
    [Fact(DisplayName = "扫描配置类生成成员访问注册")]
    public void GenerateMembers()
    {
        var (_, result) = GeneratorTestHost.RunWithSymbols(new ConfigModelGenerator(), FullSource, ["NET5_0_OR_GREATER"]);
        var sources = GeneratorTestHost.GetGeneratedSources(result);

        var text = Assert.Single(sources).Value;
        Assert.Contains("MemberAccessorTable.Register", text);

        // 标准配置类（泛型参数即自身）
        Assert.Contains("typeof(global::MyConfig)", text);
        Assert.Contains("\"Host\"", text);
        Assert.Contains("\"Port\"", text);

        // 多级继承：泛型参数类型与声明类都生成，声明类含继承成员
        Assert.Contains("typeof(global::BaseSetting)", text);
        Assert.Contains("typeof(global::ChildSetting)", text);
        Assert.Contains("\"Extra\"", text);
        Assert.Contains("((global::ChildSetting)o).Name", text);

        // 非配置类不生成
        Assert.DoesNotContain("NotConfig", text);
    }

    /// <summary>生成产物与用户代码一起编译无错误</summary>
    [Fact(DisplayName = "生成产物参与编译无错误")]
    public void GeneratedSourceCompiles()
    {
        var (compilation, _) = GeneratorTestHost.RunWithSymbols(new ConfigModelGenerator(), FullSource, ["NET5_0_OR_GREATER"]);
        var errors = compilation.GetDiagnostics().Where(e => e.Severity == DiagnosticSeverity.Error).ToList();

        Assert.True(errors.Count == 0, String.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    /// <summary>低版本目标（无 NET5_0_OR_GREATER 符号）不生成</summary>
    [Fact(DisplayName = "低版本目标不生成")]
    public void SkipBelowNet5()
    {
        var (_, result) = GeneratorTestHost.RunWithSymbols(new ConfigModelGenerator(), FullSource, []);
        var sources = GeneratorTestHost.GetGeneratedSources(result);

        Assert.Empty(sources);
    }
    #endregion
}
