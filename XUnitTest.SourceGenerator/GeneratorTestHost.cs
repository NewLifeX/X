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
        => RunCore(generator, source, null, additionalReferences);

    /// <summary>编译源码并运行生成器（指定预处理符号）</summary>
    /// <param name="generator">目标生成器</param>
    /// <param name="source">待编译源码</param>
    /// <param name="preprocessorSymbols">预处理符号</param>
    /// <param name="additionalReferences">附加元数据引用</param>
    /// <returns>生成后的编译与驱动运行结果</returns>
    public static (Compilation Compilation, GeneratorDriverRunResult Result) RunWithSymbols(IIncrementalGenerator generator, String source, String[] preprocessorSymbols, params MetadataReference[] additionalReferences)
        => RunCore(generator, source, preprocessorSymbols, additionalReferences);

    private static readonly Lazy<MetadataReference[]> _platformRefs = new(() =>
    {
        // 除 CoreLib 外，BCL 类型分散在系统程序集与 facade 中（如 System.Runtime 转发），
        // 内存编译需完整引用运行时平台程序集，避免 CS0012/CS0246
        var dir = System.IO.Path.GetDirectoryName(typeof(Object).Assembly.Location)!;
        var list = new List<MetadataReference>();
        foreach (var dll in System.IO.Directory.GetFiles(dir, "*.dll"))
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(dll);
            if (name.Contains("Native")) continue;
            if (name.StartsWith("System") || name is "mscorlib" or "netstandard" or "Microsoft.CSharp" or "WindowsBase")
                list.Add(MetadataReference.CreateFromFile(dll));
        }

        return list.ToArray();
    });

    private static (Compilation Compilation, GeneratorDriverRunResult Result) RunCore(IIncrementalGenerator generator, String source, String[]? preprocessorSymbols, MetadataReference[] additionalReferences)
    {
        var refs = new List<MetadataReference>(_platformRefs.Value);
        foreach (var item in additionalReferences) refs.Add(item);

        var options = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: preprocessorSymbols);
        var trees = new[]
        {
            CSharpSyntaxTree.ParseText("global using System;", options),
            CSharpSyntaxTree.ParseText(source, options),
        };
        var compilation = CSharpCompilation.Create(
            "GeneratorTest",
            trees,
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: options);
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
