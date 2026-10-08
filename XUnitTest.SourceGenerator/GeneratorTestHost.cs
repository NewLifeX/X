using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace XUnitTest.SourceGenerator;

/// <summary>生成器测试宿主。在内存中编译源码并驱动增量生成器，捕获生成结果</summary>
public static class GeneratorTestHost
{
    /// <summary>编译源码并运行生成器</summary>
    /// <param name="generator">目标生成器</param>
    /// <param name="source">待编译源码</param>
    /// <param name="additionalReferences">附加元数据引用</param>
    /// <returns>生成后的编译与驱动运行结果</returns>
    public static (Compilation Compilation, GeneratorDriverRunResult Result) Run(IIncrementalGenerator generator, String source, params MetadataReference[] additionalReferences)
    {
        var refs = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(Object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
        };
        foreach (var item in additionalReferences) refs.Add(item);

        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("global using System;"),
            CSharpSyntaxTree.ParseText(source),
        };
        var compilation = CSharpCompilation.Create(
            "GeneratorTest",
            trees,
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(generator.AsSourceGenerator());
        var updated = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        return (output, updated.GetRunResult());
    }

    /// <summary>获取生成器输出的源文件字典（提示名 → 文本）</summary>
    /// <param name="result">驱动运行结果</param>
    /// <returns>生成文件字典</returns>
    public static IDictionary<String, String> GetGeneratedSources(GeneratorDriverRunResult result)
    {
        var dic = new Dictionary<String, String>();
        foreach (var item in result.Results)
        {
            foreach (var source in item.GeneratedSources)
            {
                dic[source.HintName] = source.SourceText.ToString();
            }
        }

        return dic;
    }
}
