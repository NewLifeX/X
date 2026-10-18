using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

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

    /// <summary>编译源码并运行生成器（指定程序集名、预处理符号与附加文本）</summary>
    /// <param name="generator">目标生成器</param>
    /// <param name="source">待编译源码</param>
    /// <param name="assemblyName">程序集名，用于区分 Core 与下游模式</param>
    /// <param name="preprocessorSymbols">预处理符号</param>
    /// <param name="additionalTexts">附加文本（路径与内容）</param>
    /// <param name="buildProperties">MSBuild 全局属性（对应 build_property.*）</param>
    /// <param name="additionalReferences">附加元数据引用</param>
    /// <returns>生成后的编译与驱动运行结果</returns>
    public static (Compilation Compilation, GeneratorDriverRunResult Result) RunWithAdditionalTexts(IIncrementalGenerator generator, String source, String assemblyName, String[]? preprocessorSymbols, IEnumerable<(String Path, String Text)> additionalTexts, IReadOnlyDictionary<String, String>? buildProperties = null, params MetadataReference[] additionalReferences)
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
            assemblyName,
            trees,
            refs,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var texts = additionalTexts.Select(x => (AdditionalText)new InMemoryAdditionalText(x.Path, x.Text)).ToArray();
        var provider = new TestAnalyzerConfigOptionsProvider(buildProperties);
        var driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, texts, options, provider);
        var updated = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        return (output, updated.GetRunResult());
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestAnalyzerConfigOptions _options;

        public TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<String, String>? buildProperties)
            => _options = new TestAnalyzerConfigOptions(buildProperties);

        public override AnalyzerConfigOptions GlobalOptions => _options;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<String, String>? _buildProperties;

        public TestAnalyzerConfigOptions(IReadOnlyDictionary<String, String>? buildProperties) => _buildProperties = buildProperties;

        public override Boolean TryGetValue(String key, out String value)
        {
            if (_buildProperties != null && key.StartsWith("build_property.") && _buildProperties.TryGetValue(key.Substring("build_property.".Length), out var found))
            {
                value = found;
                return true;
            }

            value = "";
            return false;
        }
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly String _text;

        public InMemoryAdditionalText(String path, String text)
        {
            Path = path;
            _text = text;
        }

        public override String Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(_text, Encoding.UTF8);
    }

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
