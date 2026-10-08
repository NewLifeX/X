using Microsoft.CodeAnalysis;
using Xunit;

namespace XUnitTest.SourceGenerator;

public class GeneratorTestHostTests
{
    [Fact(DisplayName = "测试宿主_驱动生成器并产出生成文件")]
    public void Run_GeneratesSource()
    {
        var (_, result) = GeneratorTestHost.Run(new SampleGenerator(), "class A { }");

        var sources = GeneratorTestHost.GetGeneratedSources(result);
        Assert.True(sources.ContainsKey("SampleGenerated.g.cs"));
        Assert.Contains("SampleGenerated", sources["SampleGenerated.g.cs"]);
    }

    [Fact(DisplayName = "测试宿主_生成代码参与编译且无错误")]
    public void Run_GeneratedCodeIsCompiled()
    {
        // 主源码引用生成类型，验证生成物参与编译
        var (compilation, result) = GeneratorTestHost.Run(new SampleGenerator(), "class A { public String V => SampleGenerated.Value; }");

        var errors = compilation.GetDiagnostics().Where(e => e.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);
        Assert.Single(GeneratorTestHost.GetGeneratedSources(result));
    }

    private sealed class SampleGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(static ctx =>
                ctx.AddSource("SampleGenerated.g.cs", "public static class SampleGenerated { public const String Value = \"ok\"; }"));
        }
    }
}
