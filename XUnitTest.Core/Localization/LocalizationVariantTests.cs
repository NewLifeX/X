using System.Globalization;
using NewLife;
using Xunit;

namespace XUnitTest.I18N;

/// <summary>同语种不同变体的覆盖表互不干扰</summary>
public class LocalizationVariantTests
{
    [Fact(DisplayName = "pt-BR 与 pt-PT 各自保留自己的译文")]
    public void VariantOverridesAreIsolated()
    {
        CultureInfo br, pt;
        try
        {
            br = new CultureInfo("pt-BR");
            pt = new CultureInfo("pt-PT");
        }
        catch (CultureNotFoundException)
        {
            // 无 ICU 的精简环境跳过（下拉选项本就不含这些语言）
            return;
        }

        try
        {
            Localization.Register(br, new Dictionary<String, String> { ["Hello"] = "Ola BR" });
            Localization.Register(pt, new Dictionary<String, String> { ["Hello"] = "Ola PT" });

            Assert.Equal("Ola BR", Localization.Translate(br, "Hello"));
            Assert.Equal("Ola PT", Localization.Translate(pt, "Hello"));

            // 注销其中一个变体，另一个不受影响
            Assert.True(Localization.Unregister(pt));
            Assert.Equal("Ola BR", Localization.Translate(br, "Hello"));
        }
        finally
        {
            Localization.Unregister(br);
            Localization.Unregister(pt);
        }
    }
}
