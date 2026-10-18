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
}
