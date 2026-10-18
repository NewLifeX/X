using System.Linq;
using Microsoft.CodeAnalysis;
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

    [Fact(DisplayName = "Core 模式：生成静态表与查表方法")]
    public void CoreMode()
    {
        var (_, result) = GeneratorTestHost.RunWithAdditionalTexts(
            new LocalizationGenerator(), "", "NewLife.Core", null,
            new[] { ("Locales/zh-Hans.yml", Yaml) });

        var text = Assert.Single(GeneratorTestHost.GetGeneratedSources(result)).Value;
        Assert.Contains("public static partial class Localization", text);
        Assert.Contains("GetCultureKey", text);
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
        Assert.Contains("internal static class L", text);
        Assert.Contains("Localization.Register", text);
        Assert.DoesNotContain("GetCultureKey", text);
        // 下游可能补 ModuleInitializerAttribute，必须使用块级命名空间
        Assert.Contains("namespace NewLife", text);
        Assert.DoesNotContain("namespace NewLife;", text);
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
}
