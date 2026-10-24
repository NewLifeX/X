using System.Globalization;
using System.Runtime.CompilerServices;
using NewLife;

namespace XUnitTest;

internal static class TestSetup
{
    [ModuleInitializer]
    internal static void Init()
    {
        // 测试固定英文，避免机器区域设置影响异常与日志断言
        Localization.Culture = CultureInfo.GetCultureInfo("en-US");
        Localization.LogCulture = CultureInfo.GetCultureInfo("en-US");
    }

    /// <summary>运行环境是否提供完整 ICU 区域数据。不变量全球化或缺少区域数据（如只装 icu-libs 的 Alpine）下显示名与数字格式退回英文</summary>
    internal static Boolean HasCultureData => CultureInfo.GetCultureInfo("de-DE").NumberFormat.NumberGroupSeparator == ".";
}
