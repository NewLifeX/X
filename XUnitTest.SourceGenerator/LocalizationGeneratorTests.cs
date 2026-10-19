using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NewLife.SourceGenerator;
using Xunit;

namespace XUnitTest.SourceGenerator;

/// <summary>多语言源生成器测试</summary>
public class LocalizationGeneratorTests
{
    private const String Yaml = """
# 测试表
"Session {0} is closed.": "会话 {0} 已关闭。"
"Ready.": "准备就绪。"
""";

    [Fact(DisplayName = "Core 模式：生成共享键表与压缩译文")]
    public void CoreMode()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "NewLife.Core", null,
            new[] { ("Locales/zh-Hans.yml", Yaml) });

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("public static partial class Localization", text);
        Assert.Contains("GetCultureKey", text);
        Assert.Contains("private static readonly String[] _keys", text);
        Assert.Contains("private static readonly Byte[] _t_zh_Hans", text);
        Assert.Contains("DecodeTable(_keys, _t_zh_Hans)", text);
        // 译文以压缩字节数组内嵌，不再作为明文字符串常量出现在生成代码中
        Assert.DoesNotContain("会话 {0} 已关闭。", text);
        Assert.DoesNotContain("LocalizationTable", text);
    }

    [Fact(DisplayName = "下游模式：生成注册表、模块初始化器与 L 快捷入口")]
    public void DownstreamMode()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "My.Component", null,
            new[] { ("Locales/ja.yml", Yaml) });

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("internal static class LocalizationTable", text);
        Assert.Contains("ModuleInitializer", text);
        Assert.Contains("public static String T(String template)", text);
        Assert.Contains("public static String TLog(String template)", text);
        Assert.Contains("Localization.Register", text);
        Assert.DoesNotContain("GetCultureKey", text);
        // 下游可能补 ModuleInitializerAttribute，必须使用块级命名空间
        Assert.Contains("namespace NewLife", text);
        Assert.DoesNotContain("namespace NewLife;", text);
    }

    [Fact(DisplayName = "下游引用 Core：仍生成自己的 L 快捷入口")]
    public void DownstreamReferencedCoreStillGeneratesL()
    {
        // Core 程序集里有 internal NewLife.L；引用它不应被误判为"下游已自带 L"
        var core = MetadataReference.CreateFromFile(typeof(NewLife.Localization).Assembly.Location);
        var (output, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "using NewLife; internal static class Program { static void Main() => L.T(\"x\"); }", "My.Component", null,
            new[] { ("Locales/ja.yml", Yaml) }, null, OutputKind.ConsoleApplication, core);

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("public static String T(String template)", text);
        Assert.Contains("public static String TLog(String template)", text);
        Assert.DoesNotContain(output.GetDiagnostics(), d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact(DisplayName = "下游引用 public NewLife.L：不重复生成快捷入口")]
    public void DownstreamReferencedPublicL()
    {
        var lib = CSharpCompilation.Create(
            "Lib",
            new[] { CSharpSyntaxTree.ParseText("namespace NewLife { public static class L { } }") },
            new[] { MetadataReference.CreateFromFile(typeof(Object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var ms = new MemoryStream();
        var emit = lib.Emit(ms);
        Assert.True(emit.Success, String.Join("\n", emit.Diagnostics));
        var reference = MetadataReference.CreateFromImage(ms.ToArray());

        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "using NewLife; internal static class Program { static void Main() { } }", "My.Component", null,
            new[] { ("Locales/ja.yml", Yaml) }, null, OutputKind.DynamicallyLinkedLibrary, reference);

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.DoesNotContain("public static String T(String template)", text);
    }

    [Fact(DisplayName = "下游自带 NewLife.L：不重复生成快捷入口")]
    public void DownstreamOwnL()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "namespace NewLife { internal static class L { } }", "My.Component", null,
            new[] { ("Locales/ja.yml", Yaml) });

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("internal static class LocalizationTable", text);
        Assert.DoesNotContain("public static String T(", text);
        Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Fact(DisplayName = "下游可通过 MSBuild 属性关闭 L 生成")]
    public void DownstreamOptOutL()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "My.Component", null,
            new[] { ("Locales/ja.yml", Yaml) },
            new Dictionary<String, String> { ["NewLifeLocalizationGenerateL"] = "false" });

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("internal static class LocalizationTable", text);
        Assert.DoesNotContain("public static String T(", text);
    }

    [Fact(DisplayName = "下游类库：默认不覆盖 Core 同名译文")]
    public void DownstreamLibraryKeepsBuiltin()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "My.Component", null,
            new[] { ("Locales/ja.yml", Yaml) });

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("overwrite: false", text);
    }

    [Fact(DisplayName = "下游应用：默认可覆盖 Core 同名译文")]
    public void DownstreamApplicationOverridesBuiltin()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "My.App", null,
            new[] { ("Locales/ja.yml", Yaml) },
            outputKind: OutputKind.ConsoleApplication);

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("overwrite: true", text);
    }

    [Fact(DisplayName = "下游类库：MSBuild 属性可显式开启覆盖")]
    public void DownstreamOptInOverwrite()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "My.Component", null,
            new[] { ("Locales/ja.yml", Yaml) },
            new Dictionary<String, String> { ["NewLifeLocalizationOverwrite"] = "true" });

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("overwrite: true", text);
    }

    [Fact(DisplayName = "下游应用：MSBuild 属性可显式关闭覆盖")]
    public void DownstreamOptOutOverwrite()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "My.App", null,
            new[] { ("Locales/ja.yml", Yaml) },
            new Dictionary<String, String> { ["NewLifeLocalizationOverwrite"] = "false" },
            outputKind: OutputKind.ConsoleApplication);

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("overwrite: false", text);
    }

    [Fact(DisplayName = "下游模式：注册对不变全球化做防御")]
    public void DownstreamInvariantGuard()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "My.Component", null,
            new[] { ("Locales/ja.yml", Yaml) });

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("catch (Exception ex) when (ex is CultureNotFoundException || ex is ArgumentException)", text);
    }

    [Fact(DisplayName = "非 Locales 目录的 yml 不参与生成")]
    public void IgnoreNonLocaleYaml()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "NewLife.Core", null,
            new[] { ("config/app.yml", Yaml) });

        Assert.Empty(GeneratorTestHost.GetGeneratedSources(result));
    }

    [Fact(DisplayName = "占位符不一致：编译错误")]
    public void PlaceholderMismatch()
    {
        var yaml = "\"Session {0} is closed.\": \"会话已关闭。\"";
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "NewLife.Core", null,
            new[] { ("Locales/zh-Hans.yml", yaml) });

        Assert.Contains(result.Results[0].Diagnostics, d => d.Id == "LOC003" && d.Severity == DiagnosticSeverity.Error);
    }

    [Fact(DisplayName = "格式说明符不一致：警告")]
    public void SpecifierMismatch()
    {
        var yaml = "\"Size {0:n0} bytes.\": \"大小 {0} 字节。\"";
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "NewLife.Core", null,
            new[] { ("Locales/zh-Hans.yml", yaml) });

        Assert.Contains(result.Results[0].Diagnostics, d => d.Id == "LOC004" && d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact(DisplayName = "对齐宽度占位符：译文丢失占位符时报编译错误")]
    public void AlignmentPlaceholderMissing()
    {
        var yaml = "\"Read: {0,7:n0} ms.\": \"读取毫秒。\"";
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "NewLife.Core", null,
            new[] { ("Locales/zh-Hans.yml", yaml) });

        Assert.Contains(result.Results[0].Diagnostics, d => d.Id == "LOC003" && d.Severity == DiagnosticSeverity.Error);
    }
}
