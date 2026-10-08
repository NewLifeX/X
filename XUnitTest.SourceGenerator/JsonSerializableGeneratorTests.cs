using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using NewLife.SourceGenerator;
using Xunit;

namespace XUnitTest.SourceGenerator;

/// <summary>Json 成员访问生成器测试</summary>
public class JsonSerializableGeneratorTests
{
    #region 测试源码
    private const String Stub = """
namespace NewLife.Reflection
{
    public static class MemberAccessorTable
    {
        public static void Register(Type type, String name, Func<Object, Object?>? getter, Action<Object, Object?>? setter) { }
    }
}
""";

    private const String Source = """
[JsonSerializable(typeof(SmokeModel))]
public partial class SmokeContext { }

[JsonSerializable(typeof(DerivedModel))]
public partial class DerivedContext { }

public class SmokeModel
{
    public String Name { get; set; } = "";
    public Int32 Count { get; set; }
    public String? Note { get; set; }
    public Int32? Level { get; set; }
    public String ReadOnly => "x";
    public String InitOnly { get; init; } = "";
    public readonly String Fixed = "";
    public String Field = "";
    public (Int32, String) Pair { get; set; }
    public Int32[]? Items { get; set; }
}

public class BaseModel
{
    public Int32 BaseProp { get; set; }
}

public class DerivedModel : BaseModel
{
    public String DerivedProp { get; set; } = "";
}
""";

    private static readonly String FullSource = "using System;" + Environment.NewLine + "using System.Text.Json.Serialization;" + Environment.NewLine + Stub + Environment.NewLine + Source;
    private static readonly MetadataReference[] ExtraRefs = [];
    #endregion

    #region 测试
    /// <summary>扫描 JsonSerializable 标记，为可静态访问的公开成员生成读写委托</summary>
    [Fact(DisplayName = "扫描标记类型生成成员访问注册")]
    public void GenerateMembers()
    {
        var (_, result) = GeneratorTestHost.RunWithSymbols(new JsonSerializableGenerator(), FullSource, ["NET5_0_OR_GREATER"], ExtraRefs);
        var sources = GeneratorTestHost.GetGeneratedSources(result);

        var text = Assert.Single(sources).Value;
        Assert.Contains("MemberAccessorTable.Register", text);

        // 常规成员
        Assert.Contains("\"Name\"", text);
        Assert.Contains("\"Count\"", text);
        Assert.Contains("\"Note\"", text);
        Assert.Contains("\"Level\"", text);
        Assert.Contains("\"Items\"", text);
        Assert.Contains("\"Field\"", text);

        // 只读属性/只读字段/init-only：有 getter、无 setter
        Assert.Contains("\"ReadOnly\"", text);
        Assert.Contains("\"Fixed\"", text);
        Assert.Contains("\"InitOnly\"", text);
        Assert.DoesNotContain(".ReadOnly = ", text);
        Assert.DoesNotContain(".Fixed = ", text);
        Assert.DoesNotContain(".InitOnly = ", text);

        // 值类型 setter 带 null 兜底；可空值类型无兜底
        Assert.Contains("default(global::System.Int32)", text);
        Assert.Contains("(global::System.Int32?)v!", text);

        // 元组成员跳过（避免额外类型引用）
        Assert.DoesNotContain("Pair", text);

        // 继承链成员一并生成
        Assert.Contains("\"BaseProp\"", text);
        Assert.Contains("\"DerivedProp\"", text);
    }

    /// <summary>生成产物与用户代码一起编译无错误（含模块初始化器与访问委托）</summary>
    [Fact(DisplayName = "生成产物参与编译无错误")]
    public void GeneratedSourceCompiles()
    {
        var (compilation, _) = GeneratorTestHost.RunWithSymbols(new JsonSerializableGenerator(), FullSource, ["NET5_0_OR_GREATER"], ExtraRefs);
        var errors = compilation.GetDiagnostics().Where(e => e.Severity == DiagnosticSeverity.Error).ToList();

        Assert.True(errors.Count == 0, String.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    /// <summary>低版本目标（无 NET5_0_OR_GREATER 符号）不生成</summary>
    [Fact(DisplayName = "低版本目标不生成")]
    public void SkipBelowNet5()
    {
        var (_, result) = GeneratorTestHost.RunWithSymbols(new JsonSerializableGenerator(), FullSource, [], ExtraRefs);
        var sources = GeneratorTestHost.GetGeneratedSources(result);

        Assert.Empty(sources);
    }
    #endregion
}
